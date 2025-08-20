import React from 'react'
import LoginButton from './LoginButton'
import LogoutButton from './LogoutButton'
import { AuthProvider, useAuth } from '../context/AuthContext'


const HeaderBar = () => {
const { isAuthenticated, isLoading } = useAuth()
if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <h2>Loading...</h2>
      </div>
    )
  }

  return (
    <div className="page-header">
      <div className="logo">Stickerlandia</div>
      <div className="login-auth">
        {!isAuthenticated ? (
            <div>
              <LoginButton />
            </div>
          ) : (
            <div>
              <LogoutButton />
            </div>
          )}
      </div>
    </div>
    
  )
}

export default HeaderBar