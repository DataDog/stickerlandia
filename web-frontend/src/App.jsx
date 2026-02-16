import React, { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAuth } from "./context/AuthContext";
import HeaderBar from "./components/HeaderBar";
import Landing from "./components/Landing";
import { datadogRum } from '@datadog/browser-rum';
import "./App.css";

function App() {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  if (isAuthenticated) {
      datadogRum.setUser({
        name: user.preferred_username,
        email: user.email
      })
  }

  useEffect(() => {
    if (isAuthenticated) {
      navigate("/dashboard");
    }
  }, [isAuthenticated, navigate]);

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="text-center mx-auto w-full px-6 sm:max-w-[40rem] md:max-w-[48rem] md:px-8 lg:max-w-[64rem] xl:max-w-[80rem]">
          <div style={{ marginBottom: "20px" }}>
            <Landing />
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
