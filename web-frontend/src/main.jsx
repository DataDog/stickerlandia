import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { BrowserRouter, Routes, Route } from "react-router";
import App from "./App.jsx";
import Dashboard from "./components/Dashboard.jsx";
import { initializeDatadogRum } from "./services/DatadogRum.js";

// Initialize Datadog RUM
initializeDatadogRum();

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<App />} />
        <Route path="dashboard" element={<Dashboard />} />
      </Routes>
      
    </BrowserRouter>
    ,
  </StrictMode>
);
