import React from 'react'
import { useAuth } from '../context/AuthContext'
import AuthService from '../services/AuthService'

const UserProfile = () => {
  const { user, isAuthenticated } = useAuth()
  
  const getSessionExpiry = () => {
    const tokenData = AuthService.getStoredToken()
    if (tokenData?.expires_at) {
      const expiryDate = new Date(tokenData.expires_at * 1000)
      return expiryDate.toLocaleString()
    }
    return 'Unknown'
  }

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <div style={{
      padding: '15px',
      border: '1px solid #646cff',
      borderRadius: '8px',
      backgroundColor: 'rgba(255, 255, 255, 0.1)',
      margin: '10px 0',
      color: 'inherit'
    }}>
      <h3 style={{ color: 'inherit', marginTop: '0' }}>Welcome!</h3>

      <p style={{ color: 'inherit' }}><strong>User ID:</strong> {user.sub || 'N/A'}</p>
      <p style={{ color: 'inherit' }}><strong>Name:</strong> {user.name || user.given_name || 'N/A'}</p>
      <p style={{ color: 'inherit' }}><strong>Email:</strong> {user.email || 'N/A'}</p>
      {user.roles && (
        <p style={{ color: 'inherit' }}><strong>Roles:</strong> {Array.isArray(user.roles) ? user.roles.join(', ') : user.roles}</p>
      )}
      <p style={{ color: 'inherit' }}>
        <strong>Session expires:</strong> {getSessionExpiry()}
      </p>
    </div>
  )
}

export default UserProfile
