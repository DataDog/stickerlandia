import React from 'react'
import { useAuth } from '../context/AuthContext'

const LoginButton = () => {
  const { login, isLoading } = useAuth()

  return (
    <button 
      onClick={login} 
      disabled={isLoading}
      style={{
        padding: '10px 20px',
        fontSize: '16px',
        backgroundColor: '#007bff',
        color: 'white',
        border: 'none',
        borderRadius: '4px',
        cursor: isLoading ? 'not-allowed' : 'pointer',
        opacity: isLoading ? 0.6 : 1
      }}
    >
      {isLoading ? 'Loading...' : 'Sign In'}
    </button>
  )
}

export default LoginButton