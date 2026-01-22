import React from "react";
import HeaderBar from "./HeaderBar";
import UserProfile from "./UserProfile";

function UserDashboard() {
  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <UserProfile />
      </main>
    </div>
  );
}

export default UserDashboard;
