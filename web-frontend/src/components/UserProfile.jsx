import React, { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import AuthService from "../services/AuthService";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import AssessmentOutlinedIcon from "@mui/icons-material/AssessmentOutlined";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import PersonOutlineOutlinedIcon from "@mui/icons-material/PersonOutlineOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";
import LogoutOutlinedIcon from "@mui/icons-material/LogoutOutlined";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import HotelClassOutlinedIcon from '@mui/icons-material/HotelClassOutlined';

const UserProfile = () => {
  const { user, isAuthenticated } = useAuth();
  const [userStickers, setUserStickers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchStickers = async () => {
      try {
        setLoading(true);
        const response = await fetch(
          "http://localhost:8080/api/awards/v1/assignments/" + user.email
        );

        if (!response.ok) {
          throw new Error(`Failed to fetch stickers: ${response.status}`);
        }

        const data = await response.json();
        const sortedStickers = (data.stickers || []).sort((a, b) =>
          a.stickerId.localeCompare(b.stickerId)
        );
        setUserStickers(sortedStickers);
      } catch (err) {
        console.error("Error fetching stickers:", err);
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchStickers();
  }, []);

  const getSessionExpiry = () => {
    const tokenData = AuthService.getStoredToken();
    if (tokenData?.expires_at) {
      const expiryDate = new Date(tokenData.expires_at * 1000);
      return expiryDate.toLocaleString();
    }
    return "Unknown";
  };

  if (!isAuthenticated || !user) {
    return null;
  }

  return (
    <div className="profile-wrapper">
      <div className="profile-menu bg-white h-screen">
        <nav className="profile-nav">
          <div className="my-4 px-5 border-gray-300 border-solid border-b">
            <div className="logo font-bold my-2">
              <span className="sparkle-logo">
                <AutoAwesomeOutlinedIcon />
              </span>
              Stickerlandia
            </div>
          </div>
          <div className="my-4 px-5 profile-card-wrapper grid grid-cols-4">
            <div className="my-3 col-span-1 text-center">
              <p className="bg-gray-200 rounded-full inline p-4 font-bold">
                UN
              </p>
            </div>
            <div className="profile-card col-span-3">
              <span className="block font-bold">User Name</span>
              <span className="block text-sm text-gray-600">24 Stickers</span>
            </div>
          </div>
          <ul className="">
            <li className="my-3 px-5">
              <a className="block" href="">
                <HomeOutlinedIcon />
                User Dashboard
              </a>
            </li>
            <li className="my-3 px-5">
              <a className="block" href="">
                <MenuBookOutlinedIcon />
                My Collection
              </a>
            </li>
            <li className="my-3 px-5">
              <a className="block" href="">
                <AssessmentOutlinedIcon />
                Public Dashboard
              </a>
            </li>
            <li className="my-3 px-5">
              <a className="block" href="">
                <LocalPrintshopOutlinedIcon />
                Print Station
              </a>
            </li>
          </ul>
        </nav>
        <nav className="user-nav border-gray-300 border-solid border-t">
          <ul>
            <li className="my-3 px-5">
              <a className="block" href="">
                <PersonOutlineOutlinedIcon />
                Profile
              </a>
            </li>
            <li className="my-3 px-5">
              <a className="block" href="">
                <SettingsOutlinedIcon />
                Settings
              </a>
            </li>
            <li className="my-3 px-5">
              <a className="block" href="">
                <LogoutOutlinedIcon />
                Sign Out
              </a>
            </li>
          </ul>
        </nav>
      </div>
      <div className="user-profile-wrapper">
        <div className="user-profile-greeting">
          <div className="text-3xl font-bold my-3">
            Welcome Back, User Name!
          </div>
          <div className="text-gray-600 my-3">
            Here's what's happening with your collection.
          </div>
        </div>
        <div className="user-profile-info grid grid-cols-4 gap-4">
          <div className="col-span-1 landing-card items-start">
            <span className="text-gray-400 font-bold">Total Stickers</span>
            <span className="text-gray-600 font-bold text-xl">5</span>
            <div className="collection-progress-bar bg-linear-65 from-gray-800 via-gray-400 to-gray-400 to-75% block h-5 w-full"></div>
            <span className="text-gray-600">75% of available</span>
          </div>
          <div className="col-span-1 landing-card items-start">
            <span className="text-gray-400 font-bold">Legendary Count</span>
            <span className="text-gray-600 font-bold text-xl">1</span>
            <span className="text-gray-600 text-yellow-500"><HotelClassOutlinedIcon/> Ultra Rare</span>
          </div>
          <div className="col-span-1 landing-card items-start">
            <span className="text-gray-400 font-bold">Print Credits</span>
            <span className="text-gray-600 font-bold text-xl">10</span>
            <span className=" text-green-500">Ready for Events</span>
          </div>
          <div className="col-span-1 landing-card items-start">
            <span className="text-gray-400 font-bold">Member Since</span>
            <span className="text-gray-600 font-bold text-xl">July 2025</span>
            <span className="text-gray-600">2 Months Collecting</span>
          </div>
          <div className="col-span-2 landing-card items-start">
            <span className="text-gray-600 font-bold">Recent Stickers</span>
            <span className="text-gray-600 font-bold text-xl">Your latest additions</span>
            {user.roles && (
          <p style={{ color: "inherit" }}>
            <strong>Roles:</strong>{" "}
            {Array.isArray(user.roles) ? user.roles.join(", ") : user.roles}
          </p>
        )}
        <p style={{ color: "inherit" }}>
          <strong>Session expires:</strong> {getSessionExpiry()}
        </p>
        {userStickers.map((sticker) => (
          <tr
            key={sticker.stickerId}
            style={{ borderBottom: "1px solid rgba(255, 255, 255, 0.1)" }}
          >
            <td style={{ padding: "12px" }}>
              <img
                src={`http://localhost:8080/api/stickers/v1/${sticker.stickerId}/image`}
                alt={sticker.stickerName}
                style={{
                  width: "50px",
                  height: "50px",
                  objectFit: "cover",
                  borderRadius: "4px",
                  border: "1px solid rgba(255, 255, 255, 0.2)",
                }}
                onError={(e) => {
                  e.target.style.display = "none";
                }}
              />
            </td>
            <td style={{ padding: "12px", color: "inherit" }}>
              {sticker.stickerId}
            </td>
            <td style={{ padding: "12px", color: "inherit" }}>
              {sticker.stickerName}
            </td>
            <td style={{ padding: "12px", color: "inherit" }}>
              {sticker.reason}
            </td>
            <td style={{ padding: "12px", color: "inherit" }}>
              {sticker.assignedAt}
            </td>
          </tr>
        ))}
          </div>
          <div className="col-span-2 landing-card items-start">
            <span className="text-gray-600 font-bold">Available Rewards</span>
            <span className="text-gray-600 font-bold text-xl">Stickers you can claim</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserProfile;
