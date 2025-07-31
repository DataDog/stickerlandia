class AuthService {
  constructor() {
    this.baseUrl = '/api/app/auth'
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
      // Fallback: reload the page
      window.location.reload()
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
    const result = await this.getUser()
    return result.authenticated
  }
}

export default new AuthService()