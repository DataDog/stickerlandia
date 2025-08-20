import React from 'react'
import { useAuth } from '../context/AuthContext'

const LoginButton = () => {
  const { login, isLoading } = useAuth()

  return (
    <button 
      onClick={login} 
      disabled={isLoading}
      className="bg-black text-white rounded-md"
    >
      {isLoading ? 'Loading...' : 'Sign In'}
    </button>
  )
}

export default LoginButton