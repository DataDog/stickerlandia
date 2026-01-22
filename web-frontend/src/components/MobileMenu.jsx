import React, { useState } from "react";
import { Link } from "react-router";
import { useAuth } from "../context/AuthContext";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import MenuIcon from "@mui/icons-material/Menu";
import CloseIcon from "@mui/icons-material/Close";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";

function MobileMenu() {
  const [isOpen, setIsOpen] = useState(false);
  const { user, isLoading, logout } = useAuth();

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
        aria-label="menu"
        onClick={toggleDrawer(true)}
        className="lg:hidden"
        sx={{ color: "black" }}
      >
        <MenuIcon />
      </IconButton>

      <Drawer anchor="left" open={isOpen} onClose={toggleDrawer(false)}>
        <div className="w-64 h-full bg-white">
          <div className="flex justify-between items-center p-4 border-b border-gray-300">
            <span className="text-xl font-bold">
              <span className="text-blue-500">
                <AutoAwesomeOutlinedIcon />
              </span>
              Stickerlandia
            </span>
            <IconButton onClick={toggleDrawer(false)}>
              <CloseIcon />
            </IconButton>
          </div>

          <div className="my-4 px-5 flex gap-4 items-center">
            <div className="flex-shrink-0">
              <p className="bg-gray-200 rounded-full inline-block p-4 font-bold">
                {user?.name
                  ? user.name
                      .split(" ")
                      .map((n) => n[0])
                      .join("")
                      .substring(0, 2)
                      .toUpperCase()
                  : "UN"}
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

          <nav className="flex-1">
            <ul>
              <li className="my-3 px-5">
                <Link
                  className="block py-2"
                  to="/dashboard"
                  onClick={handleLinkClick}
                >
                  <HomeOutlinedIcon />
                  User Dashboard
                </Link>
              </li>
              <li className="my-3 px-5">
                <Link
                  className="block py-2"
                  to="/collection"
                  onClick={handleLinkClick}
                >
                  <MenuBookOutlinedIcon />
                  My Collection
                </Link>
              </li>
              <li className="my-3 px-5">
                <Link
                  className="block py-2"
                  to="/catalogue"
                  onClick={handleLinkClick}
                >
                  <AssessmentOutlinedIcon />
                  Catalogue
                </Link>
              </li>
              <li className="my-3 px-5">
                <Link
                  className="block py-2"
                  to="/public-dashboard"
                  onClick={handleLinkClick}
                >
                  <AssessmentOutlinedIcon />
                  Public Dashboard
                </Link>
              </li>
              <li className="my-3 px-5">
                <a className="block py-2" href="">
                  <LocalPrintshopOutlinedIcon />
                  Print Station
                </a>
              </li>
            </ul>
          </nav>

          <nav className="border-t border-gray-300 mt-auto">
            <ul>
              <li className="my-3 px-5">
                <a className="block py-2" href="">
                  <PersonOutlineOutlinedIcon />
                  Profile
                </a>
              </li>
              <li className="my-3 px-5">
                <a className="block py-2" href="">
                  <SettingsOutlinedIcon />
                  Settings
                </a>
              </li>
              <li className="my-3 px-5">
                <button className="block w-full text-left py-2" onClick={logout}>
                  <LogoutOutlinedIcon />
                  Sign Out
                </button>
              </li>
            </ul>
          </nav>
        </div>
      </Drawer>
    </>
  );
}

export default MobileMenu;
