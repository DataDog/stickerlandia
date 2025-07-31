import React from 'react'
import { useAuth } from '../context/AuthContext'

const UserProfile = () => {
  const { user, isAuthenticated } = useAuth()

  if (!isAuthenticated || !user) {
    return null
  }

  return (
    <div style={{
      padding: '15px',
      border: '1px solid #ddd',
      borderRadius: '4px',
      backgroundColor: '#f8f9fa',
      margin: '10px 0'
    }}>
      <h3>Welcome!</h3>
      <p><strong>Name:</strong> {user.name || user.given_name || 'N/A'}</p>
      <p><strong>Email:</strong> {user.email || 'N/A'}</p>
      {user.roles && (
        <p><strong>Roles:</strong> {Array.isArray(user.roles) ? user.roles.join(', ') : user.roles}</p>
      )}
    </div>
  )
}

export default UserProfile