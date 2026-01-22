import React from "react";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import UserInfo from "./UserInfo";
import { MainNavList, SecondaryNavList } from "./NavItems";

function Sidebar() {
  return (
    <div className="hidden lg:block col-span-1 border-gray-300 border-solid border-r h-screen">
      <nav className="user-nav">
        <div className="user-nav-header">
          <div className="pt-8 px-5 pb-5">
            <span className="text-xl font-bold">
              <span className="text-blue-500">
                <AutoAwesomeOutlinedIcon />
              </span>
              Stickerlandia
            </span>
          </div>
        </div>
        <UserInfo />
        <MainNavList />
      </nav>
      <nav className="user-nav border-gray-300 border-solid border-t">
        <SecondaryNavList />
      </nav>
    </div>
  );
}

export default Sidebar;
