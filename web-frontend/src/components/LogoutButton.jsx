import React from 'react'
import { useAuth } from '../context/AuthContext'

const LogoutButton = () => {
  const { logout } = useAuth()

  return (
    <button 
      onClick={logout}
      style={{
        padding: '10px 20px',
        fontSize: '16px',
        backgroundColor: '#dc3545',
        color: 'white',
        border: 'none',
        borderRadius: '4px',
        cursor: 'pointer'
      }}
    >
      Sign Out
    </button>
  )
}

export default LogoutButton