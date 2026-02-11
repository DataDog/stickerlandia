import React from "react";
import { Link } from "react-router";
import { useAuth } from "../context/AuthContext";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";

const mainNavItems = [
  { to: "/dashboard", icon: HomeOutlinedIcon, label: "User Dashboard" },
  { to: "/collection", icon: MenuBookOutlinedIcon, label: "My Collection" },
  { to: "/catalogue", icon: AssessmentOutlinedIcon, label: "Catalogue" },
  { to: "/public-dashboard", icon: AssessmentOutlinedIcon, label: "Public Dashboard" },
  { to: "/print-station", icon: LocalPrintshopOutlinedIcon, label: "Print Station", adminOnly: true },
];

const secondaryNavItems = [
  { to: "", icon: PersonOutlineOutlinedIcon, label: "Profile", isExternal: true },
  { to: "", icon: SettingsOutlinedIcon, label: "Settings", isExternal: true },
];

function MainNavList({ onItemClick }) {
  const { user } = useAuth();
  const isAdmin = user?.role?.some(r => r.toLowerCase() === 'admin');

  const visibleItems = mainNavItems.filter(item => !item.adminOnly || isAdmin);

  return (
    <ul>
      {visibleItems.map((item) => (
        <li key={item.label} className="my-3 px-5">
          {item.isExternal ? (
            <a className="block py-2" href={item.to} onClick={onItemClick}>
              <item.icon />
              {item.label}
            </a>
          ) : (
            <Link className="block py-2" to={item.to} onClick={onItemClick}>
              <item.icon />
              {item.label}
            </Link>
          )}
        </li>
      ))}
    </ul>
  );
}

function SecondaryNavList({ onItemClick }) {
  const { logout } = useAuth();

  const handleLogout = () => {
    if (onItemClick) {
      onItemClick();
    }
    logout();
  };

  return (
    <ul>
      {secondaryNavItems.map((item) => (
        <li key={item.label} className="my-3 px-5">
          <a className="block py-2" href={item.to} onClick={onItemClick}>
            <item.icon />
            {item.label}
          </a>
        </li>
      ))}
      <li className="my-3 px-5">
        <button className="block w-full text-left py-2" onClick={handleLogout}>
          <LogoutOutlinedIcon />
          Sign Out
        </button>
      </li>
    </ul>
  );
}

export { MainNavList, SecondaryNavList };
