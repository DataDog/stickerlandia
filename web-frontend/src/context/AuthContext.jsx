import React, { createContext, useContext, useState, useEffect } from 'react'
import AuthService from '../services/AuthService'

const AuthContext = createContext()

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [loginError, setLoginError] = useState(null)

  const checkAuthStatus = async () => {
    setIsLoading(true)
    try {
      // First check if we have a valid token in sessionStorage
      if (AuthService.isTokenValid()) {
        const result = await AuthService.getUserWithToken()
        if (result.authenticated) {
          setUser(result.user)
          setIsAuthenticated(true)
        } else {
          // Token is invalid, clear it
          AuthService.clearStoredToken()
          setUser(null)
          setIsAuthenticated(false)
        }
      } else {
        // No valid token, check if we have one in the URL (from OAuth callback)
        const urlParams = new URLSearchParams(window.location.search)
        const accessToken = urlParams.get('access_token')
        const expiresAt = urlParams.get('expires_at')
        
        if (accessToken && expiresAt) {
          // Store the token and get user info
          AuthService.storeToken(accessToken, parseInt(expiresAt, 10))
          const result = await AuthService.getUserWithToken()
          if (result.authenticated) {
            setUser(result.user)
            setIsAuthenticated(true)
          }
          
          // Clean up the URL
          window.history.replaceState({}, document.title, window.location.pathname)
        } else {
          setUser(null)
          setIsAuthenticated(false)
        }
      }
    } catch (error) {
      console.error('Auth check failed:', error)
      AuthService.clearStoredToken()
      setUser(null)
      setIsAuthenticated(false)
    } finally {
      setIsLoading(false)
    }
  }

  const login = async () => {
    setLoginError(null)
    try {
      await AuthService.login()
    } catch (error) {
      setLoginError(error.message)
    }
  }

  const clearLoginError = () => {
    setLoginError(null)
  }

  const logout = async () => {
    await AuthService.logout()
    // After logout, the page will reload or redirect
    // so we don't need to update state here
  }

  useEffect(() => {
    checkAuthStatus()
  }, [])

  // Listen for logout events from other tabs
  useEffect(() => {
    const handleStorageChange = (event) => {
      // Detect logout broadcast from another tab
      if (event.key === 'logout_event') {
        // Clear local auth state
        AuthService.clearStoredToken()
        setUser(null)
        setIsAuthenticated(false)
      }
    }

    window.addEventListener('storage', handleStorageChange)
    return () => {
      window.removeEventListener('storage', handleStorageChange)
    }
  }, [])

  const value = {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    checkAuthStatus,
    loginError,
    clearLoginError
  }

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}