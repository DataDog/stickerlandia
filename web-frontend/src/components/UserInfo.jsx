import React from "react";
import { useAuth } from "../context/AuthContext";

function UserInfo() {
  const { user, isLoading } = useAuth();

  const getInitials = () => {
    if (!user?.name) return "UN";
    return user.name
      .split(" ")
      .map((n) => n[0])
      .join("")
      .substring(0, 2)
      .toUpperCase();
  };

  return (
    <div className="my-4 px-5 flex gap-4 items-center">
      <div className="flex-shrink-0">
        <p className="bg-gray-200 rounded-full inline-block p-4 font-bold">
          {getInitials()}
        </p>
      </div>
      <div className="flex-1 min-w-0">
        <span className="block font-bold">
          {user?.name || user?.email || "User Name"}
        </span>
        <span className="block text-sm text-gray-600">
          {isLoading ? "..." : "Sticker Collector"}
        </span>
      </div>
    </div>
  );
}

export default UserInfo;
