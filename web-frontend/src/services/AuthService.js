// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.
class AuthService {
  constructor() {
    this.baseUrl = '/api/app/auth'
  }

  storeToken(accessToken, expiresAt) {
    const tokenData = {
      access_token: accessToken,
      expires_at: expiresAt
    }
    sessionStorage.setItem('auth_token', JSON.stringify(tokenData))
  }

  getStoredToken() {
    try {
      const tokenData = sessionStorage.getItem('auth_token')
      return tokenData ? JSON.parse(tokenData) : null
    } catch (error) {
      console.error('Failed to parse stored token:', error)
      sessionStorage.removeItem('auth_token')
      return null
    }
  }

  clearStoredToken() {
    sessionStorage.removeItem('auth_token')
  }

  isTokenValid() {
    const tokenData = this.getStoredToken()
    if (!tokenData?.access_token || !tokenData.expires_at) {
      return false
    }
    
    // Check if token is expired (with 30 second buffer)
    const now = Math.floor(Date.now() / 1000)
    const expiresAt = tokenData.expires_at
    return now < (expiresAt - 30)
  }

  async login() {
    try {
      const response = await fetch(`${this.baseUrl}/login`, {
        method: 'POST',
        credentials: 'include'
      })
      
      if (response.redirected) {
        // BFF is redirecting to IdP
        window.location.href = response.url
      } else if (response.ok) {
        // If no redirect, manually redirect to the auth URL
        const data = await response.json()
        if (data.authUrl) {
          window.location.href = data.authUrl
        }
      } else {
        throw new Error('Login failed')
      }
    } catch (error) {
      console.error('Login failed:', error)
    }
  }

  async logout() {
    try {
      // Clear stored token first
      this.clearStoredToken()
      
      const response = await fetch(`${this.baseUrl}/logout`, {
        method: 'POST',
        credentials: 'include'
      })
      
      if (response.redirected) {
        // BFF is redirecting to IdP logout
        window.location.href = response.url
      } else {
        // Local logout only, reload the page
        window.location.reload()
      }
    } catch (error) {
      console.error('Logout failed:', error)
      // Fallback: clear token and reload the page
      this.clearStoredToken()
      window.location.reload()
    }
  }

  async getUserWithToken() {
    const tokenData = this.getStoredToken()
    if (!tokenData?.access_token) {
      return { authenticated: false }
    }

    try {
      const response = await fetch(`${this.baseUrl}/user`, {
        headers: {
          'Authorization': `Bearer ${tokenData.access_token}`
        },
        credentials: 'include'
      })
      
      if (!response.ok) {
        throw new Error('Failed to get user')
      }
      
      return await response.json()
    } catch (error) {
      console.error('Get user failed:', error)
      return { authenticated: false }
    }
  }

  async getUser() {
    try {
      const response = await fetch(`${this.baseUrl}/user`, {
        credentials: 'include'
      })
      
      if (!response.ok) {
        throw new Error('Failed to get user')
      }
      
      return await response.json()
    } catch (error) {
      console.error('Get user failed:', error)
      return { authenticated: false }
    }
  }

  async checkAuth() {
    if (this.isTokenValid()) {
      return true
    }
    this.clearStoredToken()
    return false
  }
}

export default new AuthService()