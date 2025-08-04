const express = require('express')
const session = require('express-session')
const crypto = require('crypto')
const { Issuer, generators } = require('openid-client')
const app = express()
const port = 3000

// Session configuration - HttpOnly, SameSite cookies
app.use(session({
  secret: process.env.SESSION_SECRET || 'dev-session-secret-change-in-production',
  resave: false,
  saveUninitialized: false,
  cookie: { 
    secure: false, // Set to true in production with HTTPS
    httpOnly: true,
    sameSite: 'lax',
    maxAge: 24 * 60 * 60 * 1000 // 24 hours
  }
}))

app.use(express.json())

// OAuth configuration
const OAUTH_ISSUER_INTERNAL = process.env.OAUTH_ISSUER_INTERNAL || 'http://user-management:8080'
const OAUTH_ISSUER_EXTERNAL = process.env.OAUTH_ISSUER_EXTERNAL || 'http://localhost:8080'
const OAUTH_CLIENT_ID = process.env.OAUTH_CLIENT_ID || 'web-ui'
const OAUTH_CLIENT_SECRET = process.env.OAUTH_CLIENT_SECRET || 'stickerlandia-web-ui-secret-2025'
const OAUTH_REDIRECT_URI = process.env.OAUTH_REDIRECT_URI || 'http://localhost:8080/api/app/auth/callback'

let client = null
let issuerMetadata = null

// Initialize OIDC client
async function initializeOIDC() {
  try {
    // Discover from internal endpoint
    console.log('Discovering OIDC metadata from:', OAUTH_ISSUER_INTERNAL)
    issuerMetadata = await Issuer.discover(OAUTH_ISSUER_INTERNAL)
    console.log('Discovered issuer:', issuerMetadata.issuer)
    console.log('Token endpoint:', issuerMetadata.token_endpoint)
    
    client = new issuerMetadata.Client({
      client_id: OAUTH_CLIENT_ID,
      client_secret: OAUTH_CLIENT_SECRET,
      redirect_uris: [OAUTH_REDIRECT_URI],
      response_types: ['code']
    })
    console.log('OIDC client initialized')
  } catch (error) {
    console.error('Failed to initialize OIDC client:', error)
  }
}

// Initialize OIDC on startup
initializeOIDC()

// BFF Auth Endpoints

// Step 1: Frontend calls POST /login, BFF generates PKCE and redirects to IdP
app.post('/api/app/auth/login', (req, res) => {
  if (!client) {
    return res.status(500).json({ error: 'OIDC client not initialized' })
  }

  // Generate PKCE codes
  const code_verifier = generators.codeVerifier()
  const code_challenge = generators.codeChallenge(code_verifier)
  const state = generators.state()
  const nonce = generators.nonce()
  
  // Store PKCE verifier and state in server session
  req.session.oauth = {
    code_verifier,
    state,
    nonce
  }
  
  // Generate authorization URL and make it relative by dropping hostname
  const internalAuthUrl = client.authorizationUrl({
    scope: 'openid profile email roles',
    code_challenge,
    code_challenge_method: 'S256',
    state,
    nonce
  })
  
  // Convert to relative URL by removing the hostname
  const authUrl = internalAuthUrl.replace(OAUTH_ISSUER_INTERNAL, '')
  
  // Redirect browser to IdP
  res.redirect(authUrl)
})

// Step 3: IdP redirects back to BFF with authorization code
app.get('/api/app/auth/callback', async (req, res) => {
  try {
    console.log('Callback URL:', req.url)
    console.log('Query params:', req.query)
    
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
    console.log('Attempting token exchange with:', {
      redirect_uri: OAUTH_REDIRECT_URI,
      code: code.substring(0, 10) + '...',
      state: state.substring(0, 10) + '...',
      issuer: client.issuer.issuer
    })
    
    console.log('Full callback parameters:', { code, state })
    console.log('Session OAuth data:', sessionOAuth)
    console.log('Client issuer metadata:', {
      issuer: client.issuer.issuer,
      token_endpoint: client.issuer.token_endpoint
    })
    
    // Debug what we're passing to callback
    const checks = { 
      code_verifier: sessionOAuth.code_verifier,
      nonce: sessionOAuth.nonce,
      state: sessionOAuth.state
    }
    console.log('Callback params:', callbackParams)
    console.log('Callback checks:', checks)
    
    const tokenSet = await client.callback(OAUTH_REDIRECT_URI, callbackParams, checks)
    
    console.log('Token exchange successful, received tokens')

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

    // Redirect back to frontend with token in query params (for new client-side flow)
    const queryParams = new URLSearchParams()
    queryParams.set('access_token', tokenSet.access_token)
    queryParams.set('expires_at', tokenSet.expires_at)
    res.redirect(`/?${queryParams.toString()}`)
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

// Logout - clear session and optionally call IdP logout
app.post('/api/app/auth/logout', (req, res) => {
  if (req.session.tokens?.id_token && client) {
    // Generate IdP logout URL
    const logoutUrl = client.endSessionUrl({
      id_token_hint: req.session.tokens.id_token,
      post_logout_redirect_uri: 'http://localhost:3000'
    })
    
    // Clear session
    req.session.destroy((err) => {
      if (err) console.error('Session destroy error:', err)
    })
    
    // Redirect to IdP logout
    res.redirect(logoutUrl)
  } else {
    // Just clear local session
    req.session.destroy((err) => {
      if (err) console.error('Session destroy error:', err)
    })
    res.json({ success: true })
  }
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
  console.log(`BFF listening on port ${port}`)
})
