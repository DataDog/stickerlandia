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
    issuerMetadata = await Issuer.discover(OAUTH_ISSUER_INTERNAL)
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
    if (!client) {
      return res.status(500).send('OIDC client not initialized')
    }

    const { code, state } = req.query
    const sessionOAuth = req.session.oauth

    if (!sessionOAuth) {
      return res.status(400).send('No OAuth session found')
    }

    // Verify state matches
    if (state !== sessionOAuth.state) {
      return res.status(400).send('Invalid state parameter')
    }

    // Exchange authorization code for tokens
    const tokenSet = await client.callback(OAUTH_REDIRECT_URI, { code, state }, { 
      code_verifier: sessionOAuth.code_verifier,
      nonce: sessionOAuth.nonce,
      state: sessionOAuth.state
    })

    // Store tokens in server session (never sent to frontend)
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

    // Redirect back to frontend
    res.redirect('http://localhost:3000')
  } catch (error) {
    console.error('OAuth callback failed:', error)
    res.status(400).send('Authentication failed')
  }
})

// Get current user info (frontend checks this)
app.get('/api/app/auth/user', (req, res) => {
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

app.get('/api/app', (req, res) => {
  res.send('Hello World!')
})

app.listen(port, () => {
  console.log(`BFF listening on port ${port}`)
})
