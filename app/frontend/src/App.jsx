import React from 'react'
import { AuthProvider, useAuth } from './context/AuthContext'
import LoginButton from './components/LoginButton'
import LogoutButton from './components/LogoutButton'
import UserProfile from './components/UserProfile'
import './App.css'

function AppContent() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <h2>Loading...</h2>
      </div>
    )
  }

  return (
    <div style={{ maxWidth: '800px', margin: '0 auto', padding: '20px' }}>
      <h1>Stickerlandia</h1>
      
      <div style={{ marginBottom: '20px' }}>
        {!isAuthenticated ? (
          <div>
            <p>Please sign in to access the application.</p>
            <LoginButton />
          </div>
        ) : (
          <div>
            <UserProfile />
            <LogoutButton />
          </div>
        )}
      </div>
      
      {isAuthenticated && (
        <div>
          <h2>Welcome to Stickerlandia!</h2>
          <p>You are now authenticated and can access the application.</p>
        </div>
      )}
    </div>
  )
}

function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  )
}

export default App
