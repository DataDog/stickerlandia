import React, { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAuth } from "./context/AuthContext";
import HeaderBar from "./components/HeaderBar";
import Landing from "./components/Landing";
import "./App.css";

function App() {
  const { isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!isLoading && isAuthenticated) {
      navigate("/dashboard");
    }
  }, [isAuthenticated, isLoading, navigate]);

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
            <Landing />
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
