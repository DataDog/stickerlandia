import React from "react";
import { BrowserRouter } from "react-router";
import { AuthProvider, useAuth } from "./context/AuthContext";
import LoginButton from "./components/LoginButton";
import LogoutButton from "./components/LogoutButton";
import UserProfile from "./components/UserProfile";
import StickerList from "./components/StickerList";
import HeaderBar from "./components/HeaderBar";
import Landing from "./components/Landing";
import "./App.css";

function AppContent() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div style={{ textAlign: "center", padding: "50px" }}>
        <h2>Loading...</h2>
      </div>
    );
  }

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="text-center mx-auto w-full px-6 sm:max-w-[40rem] md:max-w-[48rem] md:px-8 lg:max-w-[64rem] xl:max-w-[80rem]">
          <div style={{ marginBottom: "20px" }}>
            {!isAuthenticated ? (
              <Landing />
            ) : (
              <div>
                <UserProfile />
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}

export default App;
