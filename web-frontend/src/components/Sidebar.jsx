import React from "react";
import { Link } from "react-router";
import { useAuth } from "../context/AuthContext";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";

function Sidebar() {
  const { user, isLoading, logout } = useAuth();

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
        <div className="my-4 px-5 flex gap-4 items-center">
          <div className="flex-shrink-0">
            <p className="bg-gray-200 rounded-full inline-block p-4 font-bold">
              {user?.name ? user.name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : 'UN'}
            </p>
          </div>
          <div className="flex-1 min-w-0">
            <span className="block font-bold">{user?.name || user?.email || 'User Name'}</span>
            <span className="block text-sm text-gray-600">
              {isLoading ? '...' : 'Sticker Collector'}
            </span>
          </div>
        </div>
        <ul className="">
          <li className="my-3 px-5">
            <Link className="block py-2" to="/dashboard">
              <HomeOutlinedIcon />
              User Dashboard
            </Link>
          </li>
          <li className="my-3 px-5">
            <Link className="block py-2" to="/collection">
              <MenuBookOutlinedIcon />
              My Collection
            </Link>
          </li>
          <li className="my-3 px-5">
            <Link className="block py-2" to="/catalogue">
              <AssessmentOutlinedIcon />
              Catalogue
            </Link>
          </li>
          <li className="my-3 px-5">
            <Link className="block py-2" to="/public-dashboard">
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
      <nav className="user-nav border-gray-300 border-solid border-t">
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
  );
}

export default Sidebar;
