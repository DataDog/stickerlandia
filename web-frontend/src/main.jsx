import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { BrowserRouter, Routes, Route } from "react-router";
import App from "./App.jsx";
import PublicDashboardPage from "./components/PublicDashboardPage.jsx";
import UserDashboard from "./components/UserDashboard.jsx";
import MyCollection from "./components/MyCollection.jsx";
import { AuthProvider } from "./context/AuthContext";
import { initializeDatadogRum } from "./services/DatadogRum.js";

// Initialize Datadog RUM
initializeDatadogRum();

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<App />} />
          <Route path="dashboard" element={<UserDashboard />} />
          <Route path="collection" element={<MyCollection />} />
          <Route path="public-dashboard" element={<PublicDashboardPage />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>
);
