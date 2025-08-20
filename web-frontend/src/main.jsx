import { StrictMode } from 'react'
import { ReactDOM } from 'react-dom/client'
import { BrowserRouter } from "react-router";
import './index.css'
import App from './App.jsx'
import { initializeDatadogRum } from './services/DatadogRum.js'

// Initialize Datadog RUM
initializeDatadogRum();

const root = document.getElementById("root");
ReactDOM.createRoot(root).render(
  <BrowserRouter>
    <App />
  </BrowserRouter>,
);
