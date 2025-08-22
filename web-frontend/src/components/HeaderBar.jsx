import React from "react";
import LoginButton from "./LoginButton";
import LogoutButton from "./LogoutButton";
import AutoAwesomeOutlinedIcon from '@mui/icons-material/AutoAwesomeOutlined';
import { AuthProvider, useAuth } from "../context/AuthContext";

const HeaderBar = () => {
  const { isAuthenticated, isLoading } = useAuth();
  if (isLoading) {
    return (
      <div style={{ textAlign: "center", padding: "50px" }}>
        <h2>Loading...</h2>
      </div>
    );
  }

  return (
    <header
      id="header"
      className="bg-white pointer-events-none sticky top-0 isolate z-40 mt-[--header-mt,0] w-full"
    >
      <div className="pointer-events-auto relative mx-auto flex h-16 items-center px-6 lg:max-w-[60rem] lg:px-0 xl:max-w-[76rem]">
        <div className="logo font-bold"><span className="sparkle-logo mr-1"><AutoAwesomeOutlinedIcon/></span>Stickerlandia</div>
        <div className="ml-auto flex items-center gap-6 font-medium [@media(width<22.5rem)]:hidden">
          {!isAuthenticated ? (
            <div>
              <LoginButton />
            </div>
          ) : (
            <div>
              <LogoutButton />
            </div>
          )}
        </div>
      </div>
    </header>
  );
};

export default HeaderBar;
