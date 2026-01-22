import React, { useState, useEffect } from "react";
import { useAuth } from "../context/AuthContext";
import AuthService from "../services/AuthService";
import { API_BASE_URL } from "../config";
import Sidebar from "./Sidebar";
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
        // Use sub (subject) as the unique user identifier from OIDC
        const userId = user.sub || user.email;
        const response = await fetch(
          `${API_BASE_URL}/api/awards/v1/assignments/${userId}`
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

    if (user) {
      fetchStickers();
    }
  }, [user]);

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
    <div className="grid grid-cols-1 lg:grid-cols-5">
      <Sidebar />
      <div className="col-span-1 lg:col-span-4 p-4 sm:p-6 lg:p-8">
        <div className="text-2xl sm:text-3xl font-bold mb-4">
          Welcome Back, {user.given_name || user.name?.split(' ')[0] || user.email || 'User'}!
        </div>
        <div className="text-gray-600 mb-6">
          Here's what's happening with your collection.
        </div>
        <div className="user-profile-info grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="col-span-1 landing-card items-start">
            <span className="text-gray-400 font-bold">Total Stickers</span>
            <span className="text-gray-600 font-bold text-xl">
              {loading ? '...' : userStickers.length}
            </span>
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
            <span className="text-gray-600 font-bold text-xl">
              {user.signup_date
                ? new Date(user.signup_date).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
                : 'July 2025'}
            </span>
            <span className="text-gray-600">
              {user.signup_date
                ? Math.max(0, Math.floor((Date.now() - new Date(user.signup_date)) / (1000 * 60 * 60 * 24 * 30))) + ' Months Collecting'
                : '2 Months Collecting'}
            </span>
          </div>
          <div className="col-span-1 sm:col-span-2 landing-card items-start">
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
            <div className="overflow-x-auto">
            <table style={{ width: "100%", marginTop: "12px" }}>
              <tbody>
                {userStickers.map((sticker) => (
                  <tr
                    key={sticker.stickerId}
                    style={{ borderBottom: "1px solid rgba(255, 255, 255, 0.1)" }}
                  >
                    <td style={{ padding: "12px" }}>
                      <img
                        src={`${API_BASE_URL}/api/stickers/v1/${sticker.stickerId}/image`}
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
              </tbody>
            </table>
            </div>
          </div>
          <div className="col-span-1 sm:col-span-2 landing-card items-start">
            <span className="text-gray-600 font-bold">Available Rewards</span>
            <span className="text-gray-600 font-bold text-xl">Stickers you can claim</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserProfile;
