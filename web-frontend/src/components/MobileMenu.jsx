import React, { useState } from "react";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import MenuIcon from "@mui/icons-material/Menu";
import CloseIcon from "@mui/icons-material/Close";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import UserInfo from "./UserInfo";
import { MainNavList, SecondaryNavList } from "./NavItems";

function MobileMenu() {
  const [isOpen, setIsOpen] = useState(false);

  const toggleDrawer = (open) => (event) => {
    if (
      event.type === "keydown" &&
      (event.key === "Tab" || event.key === "Shift")
    ) {
      return;
    }
    setIsOpen(open);
  };

  const handleLinkClick = () => {
    setIsOpen(false);
  };

  return (
    <>
      <IconButton
        edge="start"
        color="inherit"
        aria-label="Open navigation menu"
        aria-expanded={isOpen}
        onClick={toggleDrawer(true)}
        className="lg:hidden text-black"
      >
        <MenuIcon />
      </IconButton>
      <Drawer
        anchor="left"
        open={isOpen}
        onClose={toggleDrawer(false)}
        aria-label="Navigation menu"
      >
        <div className="w-64 h-full bg-white">
          <div className="flex justify-between items-center p-4 border-b border-gray-300">
            <span className="text-xl font-bold">
              <span className="text-blue-500">
                <AutoAwesomeOutlinedIcon />
              </span>
              Stickerlandia
            </span>
            <IconButton onClick={toggleDrawer(false)} aria-label="Close navigation menu">
              <CloseIcon />
            </IconButton>
          </div>

          <UserInfo />

          <nav className="flex-1">
            <MainNavList onItemClick={handleLinkClick} />
          </nav>

          <nav className="border-t border-gray-300 mt-auto">
            <SecondaryNavList onItemClick={handleLinkClick} />
          </nav>
        </div>
      </Drawer>
    </>
  );
}

export default MobileMenu;
