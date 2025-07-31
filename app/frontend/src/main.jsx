import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css';
import App from './App.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
    <link rel="stylesheet" href="https://rsms.me/inter/inter.css" />
  </StrictMode>,
)
