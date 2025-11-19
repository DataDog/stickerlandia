import React from "react";
import UserProfile from "./UserProfile";

function UserDashboard() {
  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <main id="main">
        <UserProfile />
      </main>
    </div>
  );
}

export default UserDashboard;
