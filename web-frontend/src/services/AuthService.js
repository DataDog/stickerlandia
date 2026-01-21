/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { API_BASE_URL } from '../config'

class AuthService {
  constructor() {
    this.baseUrl = `${API_BASE_URL}/api/app/auth`
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

  // Broadcast logout to other tabs via localStorage
  broadcastLogout() {
    // Set a logout event timestamp - other tabs will detect this change
    localStorage.setItem('logout_event', Date.now().toString())
    // Clean up immediately - the event itself is what matters, not the stored value
    localStorage.removeItem('logout_event')
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
    const response = await fetch(`${this.baseUrl}/login`, {
      method: 'POST',
      credentials: 'include'
    })

    if (response.status === 429) {
      // Rate limited
      const data = await response.json().catch(() => ({}))
      throw new Error(data.error || 'Too many login attempts. Please wait a moment and try again.')
    }

    if (response.redirected) {
      // BFF is redirecting to IdP
      window.location.href = response.url
      return
    }

    if (response.ok) {
      // If no redirect, manually redirect to the auth URL
      const data = await response.json()
      if (data.authUrl) {
        window.location.href = data.authUrl
      }
      return
    }

    // Other errors
    throw new Error('Login failed. Please try again.')
  }

  async logout() {
    try {
      // Clear stored token and broadcast to other tabs
      this.clearStoredToken()
      this.broadcastLogout()

      const response = await fetch(`${this.baseUrl}/logout`, {
        method: 'POST',
        credentials: 'include'
      })

      if (response.redirected) {
        // Server redirected (through IdP logout), navigate to final URL
        window.location.href = response.url
      } else if (response.ok) {
        // Local logout only, redirect to home
        window.location.href = '/'
      } else {
        // Logout endpoint returned an error, still redirect to home
        console.warn('Logout response not ok:', response.status)
        window.location.href = '/'
      }
    } catch (error) {
      console.error('Logout failed:', error)
      // Fallback: clear token and redirect to home
      this.clearStoredToken()
      this.broadcastLogout()
      window.location.href = '/'
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