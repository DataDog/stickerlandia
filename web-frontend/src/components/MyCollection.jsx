import React from "react";
import { useAuth } from "../context/AuthContext";
import Sidebar from "./Sidebar";

function MyCollection() {
  const { user } = useAuth();

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <main id="main">
        <div className="grid grid-cols-5">
          <Sidebar />
          <div className="col-span-4 p-8">
            <h1 className="text-3xl font-bold mb-4">My Collection</h1>
            <p className="text-gray-600">
              Welcome to your sticker collection, {user?.given_name || 'collector'}!
            </p>
            <div className="mt-8">
              <p className="text-gray-500">
                Collection view coming soon...
              </p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default MyCollection;
