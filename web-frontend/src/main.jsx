import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { initializeDatadogRum } from './services/DatadogRum.js'

// Initialize Datadog RUM
initializeDatadogRum();

createRoot(document.getElementById('main')).render(
  <StrictMode>
      <App />
  </StrictMode>,
)
