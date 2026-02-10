/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

// Datadog APM initialisation (must be required before other imports)
require('dd-trace').init({
  service: 'web-backend',
  env: process.env.DD_ENV || 'local',
  runtimeMetrics: true,
  analytics: true
});
const express = require('express')
const session = require('express-session')
const crypto = require('crypto')
const helmet = require('helmet')
const { Issuer, generators } = require('openid-client')
const lusca = require('lusca')
const rateLimit = require('express-rate-limit')
const app = express()
const port = 3000

// Security headers with Helmet
app.use(helmet({
  contentSecurityPolicy: {
    directives: {
      defaultSrc: ["'self'"],
      styleSrc: ["'self'", "'unsafe-inline'"],
      scriptSrc: ["'self'"],
      imgSrc: ["'self'", "data:", "https:"],
      connectSrc: ["'self'", "http://localhost:*", "http://user-management:*"], // Allow OAuth endpoints
      fontSrc: ["'self'"],
      objectSrc: ["'none'"],
      mediaSrc: ["'self'"],
      frameSrc: ["'none'"],
      formAction: ["'self'", "http://localhost:*", "http://user-management:*"], // Allow OAuth form submissions
    },
  },
  crossOriginEmbedderPolicy: false // Allow iframe embedding if needed
}))

// Session configuration - HttpOnly, SameSite cookies
const isProduction = process.env.NODE_ENV === 'production'
app.use(session({
  name: 'sessionId', // Custom session cookie name (avoids default 'connect.sid')
  secret: process.env.SESSION_SECRET || 'dev-session-secret-change-in-production',
  resave: false,
  saveUninitialized: false,
  cookie: { 
    secure: isProduction, // Secure cookies in production (HTTPS required)
    httpOnly: true, // Prevent XSS attacks by blocking client-side access
    sameSite: 'lax', // CSRF protection while allowing some cross-site requests
    maxAge: 24 * 60 * 60 * 1000 // 24 hours
  }
}))

app.use(express.json())

// CSRF protection using Lusca
app.use(lusca({
  csrf: {
    secret: process.env.CSRF_SECRET || 'dev-csrf-secret-change-in-production',
    cookie: true, // Store CSRF token in cookie
    header: 'x-csrf-token', // Accept token in header
    whitelist: ['/api/app/auth/callback', '/api/app/auth/token'] // Skip CSRF for OAuth callback and token exchange
  },
  csp: false, // Disable Lusca's CSP (we use Helmet)
  xframe: false, // Disable Lusca's X-Frame-Options (we use Helmet)
  xssProtection: false, // Disable Lusca's XSS protection (we use Helmet)
  nosniff: false, // Disable Lusca's nosniff (we use Helmet)
  hsts: false // Disable Lusca's HSTS (we use Helmet)
}))

// OAuth configuration
const OAUTH_ISSUER_INTERNAL = process.env.OAUTH_ISSUER_INTERNAL || 'http://user-management:8080'
const OAUTH_CLIENT_ID = process.env.OAUTH_CLIENT_ID || 'web-ui'
const OAUTH_CLIENT_SECRET = process.env.OAUTH_CLIENT_SECRET || 'stickerlandia-web-ui-secret-2025'
const DEPLOYMENT_HOST_URL = process.env.DEPLOYMENT_HOST_URL || 'http://localhost:8080'

let client = null
let issuerMetadata = null

// Initialize OIDC client
async function initializeOIDC() {
  try {
    // Discover from internal endpoint
    console.info('Discovering OIDC metadata from:', OAUTH_ISSUER_INTERNAL)
    const discoveredIssuer = await Issuer.discover(OAUTH_ISSUER_INTERNAL)
    console.info('Discovered issuer:', discoveredIssuer.issuer)
    console.info('Discovered token endpoint:', discoveredIssuer.token_endpoint)

    // Override endpoints to use internal URLs for server-to-server communication
    // The discovered metadata has external URLs (OPENIDDICT_ISSUER), but we need
    // internal Docker network URLs for token exchange, userinfo, and JWKS calls
    issuerMetadata = new Issuer({
      ...discoveredIssuer.metadata,
      token_endpoint: `${OAUTH_ISSUER_INTERNAL}/api/users/v1/connect/token`,
      userinfo_endpoint: `${OAUTH_ISSUER_INTERNAL}/api/users/v1/connect/userinfo`,
      jwks_uri: `${OAUTH_ISSUER_INTERNAL}/.well-known/jwks`,
    })
    console.info('Using internal token endpoint:', issuerMetadata.token_endpoint)

    client = new issuerMetadata.Client({
      client_id: OAUTH_CLIENT_ID,
      client_secret: OAUTH_CLIENT_SECRET,
      response_types: ['code']
    })
    console.info('OIDC client initialized')
  } catch (error) {
    console.error('Failed to initialize OIDC client:', error)
  }
}

// Initialize OIDC on startup
initializeOIDC()

// CSRF token endpoint
app.get('/api/app/csrf-token', (req, res) => {
  res.json({ csrfToken: res.locals._csrf })
})

// Rate limiting for login attempts
const loginRateLimit = rateLimit({
  windowMs: 1 * 60 * 1000, // 1 minute window
  max: 5, // Max 5 login attempts per IP per minute
  message: {
    error: 'Too many login attempts, please try again later',
    retryAfter: '1 minute'
  },
  standardHeaders: true, // Include rate limit info in headers
  legacyHeaders: false, // Disable legacy X-RateLimit-* headers
  skip: (req) => {
    // Skip rate limiting in development for easier testing
    return process.env.NODE_ENV === 'development' && process.env.SKIP_RATE_LIMIT === 'true'
  }
})

// BFF Auth Endpoints

// Step 1: Frontend calls POST /login, BFF generates PKCE and redirects to IdP
app.post('/api/app/auth/login', loginRateLimit, (req, res) => {
  if (!client) {
    return res.status(500).json({ error: 'OIDC client not initialized' })
  }

  // Generate PKCE codes
  const code_verifier = generators.codeVerifier()
  const code_challenge = generators.codeChallenge(code_verifier)
  const state = generators.state()
  const nonce = generators.nonce()
  
  // Use environment-configured redirect URI
  const redirectUri = `${DEPLOYMENT_HOST_URL}/api/app/auth/callback`
  
  // Store PKCE verifier, state, and redirect URI in server session
  req.session.oauth = {
    code_verifier,
    state,
    nonce,
    redirect_uri: redirectUri
  }
  
  // Generate authorization URL using configured redirect URI
  const internalAuthUrl = client.authorizationUrl({
    scope: 'openid profile email roles',
    code_challenge,
    code_challenge_method: 'S256',
    state,
    nonce,
    redirect_uri: redirectUri
  })
  
  // Extract just the path and query from the authorization URL
  const authUrlObj = new URL(internalAuthUrl)
  const authUrl = authUrlObj.pathname + authUrlObj.search
  
  // Redirect browser to IdP
  res.redirect(authUrl)
})

// Step 3: IdP redirects back to BFF with authorization code
app.get('/api/app/auth/callback', async (req, res) => {
  try {
    if (!client) {
      return res.status(500).send('OIDC client not initialized')
    }

    const { code, state, iss } = req.query
    
    // If iss parameter is missing but we know what it should be, add it
    // This works around the openid-client validation issue
    const callbackParams = { code, state }
    if (iss) {
      callbackParams.iss = iss
    } else {
      // Add the expected issuer to satisfy openid-client validation
      callbackParams.iss = client.issuer.issuer
    }
    const sessionOAuth = req.session.oauth

    if (!sessionOAuth) {
      return res.status(400).send('No OAuth session found')
    }

    // Verify state matches
    if (state !== sessionOAuth.state) {
      return res.status(400).send('Invalid state parameter')
    }

    // Exchange authorization code for tokens
    const checks = {
      code_verifier: sessionOAuth.code_verifier,
      nonce: sessionOAuth.nonce,
      state: sessionOAuth.state
    }
    
    const tokenSet = await client.callback(sessionOAuth.redirect_uri, callbackParams, checks)
    
    console.info('Token exchange successful, received tokens')

    // Store tokens in server session 
    req.session.tokens = {
      access_token: tokenSet.access_token,
      refresh_token: tokenSet.refresh_token,
      id_token: tokenSet.id_token,
      expires_at: tokenSet.expires_at
    }

    // Get user info and store in session
    const userinfo = await client.userinfo(tokenSet.access_token)
    req.session.user = userinfo

    // Clear OAuth temp data
    delete req.session.oauth

    // Flag that a token is ready for pickup via /api/app/auth/token
    req.session.pendingToken = {
      access_token: tokenSet.access_token,
      expires_at: tokenSet.expires_at
    }

    res.redirect('/?auth=complete')
  } catch (error) {
    console.error('OAuth callback failed:', error)
    res.status(400).send('Authentication failed')
  }
})

// Get current user info (frontend checks this)
app.get('/api/app/auth/user', async (req, res) => {
  // Check for Bearer token in Authorization header
  const authHeader = req.headers.authorization
  if (authHeader && authHeader.startsWith('Bearer ')) {
    const token = authHeader.slice(7)
    try {
      // Validate token with OIDC provider
      if (client) {
        const userinfo = await client.userinfo(token)
        res.json({ 
          user: userinfo, 
          authenticated: true 
        })
        return
      }
    } catch (error) {
      console.error('Token validation failed:', error)
      res.json({ authenticated: false })
      return
    }
  }
  
  // Fallback to session-based auth
  if (req.session.user && req.session.tokens) {
    res.json({ 
      user: req.session.user, 
      authenticated: true 
    })
  } else {
    res.json({ authenticated: false })
  }
})

// One-time token exchange — frontend calls this after ?auth=complete redirect
app.post('/api/app/auth/token', (req, res) => {
  const pending = req.session.pendingToken
  if (!pending) {
    return res.status(404).json({ error: 'No pending token' })
  }

  // Clear immediately so the token can only be retrieved once
  delete req.session.pendingToken

  res.json({
    access_token: pending.access_token,
    expires_at: pending.expires_at
  })
})

// Logout - clear session and optionally call IdP logout
app.post('/api/app/auth/logout', (req, res) => {
  // Capture id_token before destroying session
  const idToken = req.session?.tokens?.id_token

  // Clear session and cookie
  req.session.destroy((err) => {
    if (err) console.error('Session destroy error:', err)
    res.clearCookie('sessionId', { path: '/' })

    if (idToken && client) {
      // Generate IdP logout URL with correct deployment host
      const logoutUrl = client.endSessionUrl({
        id_token_hint: idToken,
        post_logout_redirect_uri: `${DEPLOYMENT_HOST_URL.replace(/\/$/, '')}/`
      })
      res.redirect(logoutUrl)
    } else {
      // No IdP session, just redirect to home
      res.redirect('/')
    }
  })
})

// Service mapping table based on docker-compose routes
const SERVICE_MAPPING = {
  '/api/users': 'http://user-management:8080',
  '/api/awards': 'http://sticker-award:8080', 
  '/api/stickers': 'http://sticker-catalogue:8080'
}

// Middleware to check authentication
function requireAuth(req, res, next) {
  if (!req.session.tokens || !req.session.tokens.access_token) {
    return res.status(401).json({ error: 'Not authenticated' })
  }
  next()
}

// Proxy endpoint for authenticated API calls
// NOTE: This proxy system may be useful for future server-side token management
app.use('/api/app/proxy', requireAuth, async (req, res) => {
  try {
    const originalPath = req.originalUrl.replace('/api/app/proxy', '')
    let targetService = null
    
    // Find matching service based on path prefix
    for (const [prefix, service] of Object.entries(SERVICE_MAPPING)) {
      if (originalPath.startsWith(prefix)) {
        targetService = service
        break
      }
    }
    
    if (!targetService) {
      return res.status(404).json({ error: 'Service not found' })
    }
    
    const targetUrl = `${targetService}${originalPath}`
    
    // Forward request with Bearer token
    const response = await fetch(targetUrl, {
      method: req.method,
      headers: {
        'Authorization': `Bearer ${req.session.tokens.access_token}`,
        'Content-Type': req.headers['content-type'] || 'application/json',
        'Accept': req.headers.accept || 'application/json'
      },
      body: req.method !== 'GET' && req.method !== 'HEAD' ? JSON.stringify(req.body) : undefined
    })
    
    const data = await response.text()
    
    // Forward response
    res.status(response.status)
    res.set('Content-Type', response.headers.get('content-type'))
    res.send(data)
    
  } catch (error) {
    console.error('Proxy request failed:', error)
    res.status(500).json({ error: 'Proxy request failed' })
  }
})

app.get('/api/app', (req, res) => {
  res.send('Hello World!')
})

app.listen(port, () => {
  console.info(`BFF listening on port ${port}`)
})
