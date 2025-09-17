// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.
import React, { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import AuthService from "../services/AuthService";
import HomeOutlinedIcon from '@mui/icons-material/HomeOutlined';
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined';
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined';
import LocalPrintshopOutlinedIcon from '@mui/icons-material/LocalPrintshopOutlined';
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined';
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined';
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined';

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
          <ul>
            <li className="flex">
              <a className="flex-1" href=""><HomeOutlinedIcon/>User Dashboard</a>
            </li>
            <li className="flex">
              <a className="flex-1" href=""><MenuBookOutlinedIcon/>My Collection</a>
            </li>
            <li className="flex">
              <a className="flex-1" href=""><AssessmentOutlinedIcon/>Public Dashboard</a>
            </li>
            <li className="flex">
              <a className="flex-1" href=""><LocalPrintshopOutlinedIcon/>Print Station</a>
            </li>
          </ul>
        </nav>
        <nav className="user-nav">
          <ul>
            <li className="flex">
              <a className="flex-1" href=""><PersonOutlineOutlinedIcon/>Profile</a>
            </li>
            <li className="flex">
              <a className="flex-1" href=""><SettingsOutlinedIcon/>Settings</a>
            </li>
            <li className="flex">
              <a className="flex-1" href=""><LogoutOutlinedIcon/>Sign Out</a>
            </li>
          </ul>
        </nav>
      </div>
      <div className="user-profile-wrapper">
        <p style={{ color: "inherit" }}>
          <strong>User ID:</strong> {user.sub || "N/A"}
        </p>
        <p style={{ color: "inherit" }}>
          <strong>Name:</strong> {user.name || user.given_name || "N/A"}
        </p>
        <p style={{ color: "inherit" }}>
          <strong>Email:</strong> {user.email || "N/A"}
        </p>
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
    </div>
  );
};

export default UserProfile;
