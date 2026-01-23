import React from "react";
import { useAuth } from "../context/AuthContext";
import HeaderBar from "./HeaderBar";
import Sidebar from "./Sidebar";
import Dashboard from "./Dashboard";

function PublicDashboardPage() {
  const { isAuthenticated } = useAuth();

  return isAuthenticated ? (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="grid grid-cols-1 lg:grid-cols-5">
          <Sidebar />
          <div className="col-span-1 lg:col-span-4 p-4 sm:p-6 lg:p-8">
            <Dashboard />
          </div>
        </div>
      </main>
    </div>
  ) : (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="mx-auto w-full px-6 sm:max-w-[40rem] md:max-w-[48rem] md:px-8 lg:max-w-[64rem] xl:max-w-[80rem] py-8">
          <Dashboard />
        </div>
      </main>
    </div>
  );
}

export default PublicDashboardPage;
