import React, { useState, useEffect } from "react";
import { useParams, Link, useNavigate } from "react-router";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import HeaderBar from "./HeaderBar";
import Sidebar from "./Sidebar";
import { API_BASE_URL } from "../config";
import AuthService from "../services/AuthService";
import { useAuth } from "../context/AuthContext";
import { authFetch } from "../utils/authFetch";

function StickerDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, isLoading: authLoading } = useAuth();
  const [sticker, setSticker] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const userId = user?.sub || user?.email;

    // Validate userId before fetching
    if (!userId) {
      if (!authLoading) {
        setError('Unable to identify user. Please log in again.');
        setLoading(false);
      }
      return;
    }

    const controller = new AbortController();

    const fetchSticker = async () => {
      try {
        setLoading(true);
        const response = await authFetch(
          `${API_BASE_URL}/api/stickers/v1/${id}`
        );

        if (!response.ok) {
          if (response.status === 404) {
            throw new Error("Sticker not found");
          }
          throw new Error(`Failed to fetch sticker: ${response.status}`);
        }

        const data = await response.json();
        setSticker(data);
      } catch (err) {
        console.error("Error fetching sticker:", err);
        setError(err.message);
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    };

    fetchSticker();
  }, [id]);

  const formatDate = (dateString) => {
    if (!dateString) return "Unknown";
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="grid grid-cols-1 lg:grid-cols-5">
          <Sidebar />
          <div className="col-span-1 lg:col-span-4 p-4 sm:p-6 lg:p-8">
            <Link
              to="/catalogue"
              className="text-blue-600 hover:text-blue-800 mb-4 inline-block"
            >
              ← Back to Catalogue
            </Link>

            {loading && (
              <div className="text-center py-8">
                <p className="text-gray-500">Loading sticker details...</p>
              </div>
            )}

            {error && (
              <div className="text-center py-8">
                <p className="text-red-500">Error: {error}</p>
                <Link
                  to="/catalogue"
                  className="text-blue-600 hover:text-blue-800 mt-4 inline-block"
                >
                  Return to Catalogue
                </Link>
              </div>
            )}

            {!loading && !error && sticker && (
              <div className="mt-4">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                    <div className="aspect-square w-full flex items-center justify-center bg-gray-50 rounded-lg overflow-hidden">
                      <img
                        src={`${API_BASE_URL}/api/stickers/v1/${sticker.stickerId}/image`}
                        alt={sticker.stickerName}
                        className="w-full h-full object-contain"
                        onError={(e) => {
                          e.target.src = "";
                          e.target.alt = "Image not available";
                          e.target.className =
                            "w-full h-full flex items-center justify-center text-gray-400";
                        }}
                      />
                    </div>
                  </div>

                  <div className="space-y-6">
                    <div>
                      <h1 className="text-3xl font-bold text-gray-900 mb-2">
                        {sticker.stickerName}
                      </h1>
                      <p className="text-sm text-gray-500">
                        ID: {sticker.stickerId}
                      </p>
                    </div>

                    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                      <h2 className="text-lg font-semibold text-gray-800 mb-3">
                        Description
                      </h2>
                      <p className="text-gray-600">
                        {sticker.stickerDescription || "No description available."}
                      </p>
                    </div>

                    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                      <h2 className="text-lg font-semibold text-gray-800 mb-3">
                        Availability
                      </h2>
                      <div className="flex items-center gap-2">
                        <span
                          className={`inline-block w-3 h-3 rounded-full ${
                            sticker.stickerQuantityRemaining === -1 ||
                            sticker.stickerQuantityRemaining > 0
                              ? "bg-green-500"
                              : "bg-red-500"
                          }`}
                        ></span>
                        <span className="text-gray-600">
                          {sticker.stickerQuantityRemaining === -1
                            ? "Unlimited availability"
                            : sticker.stickerQuantityRemaining > 0
                            ? `${sticker.stickerQuantityRemaining} remaining`
                            : "Out of stock"}
                        </span>
                      </div>
                    </div>
                    <div className="flex justify-center">
                      <button
                        onClick={() => navigate('/print-station', { state: { sticker } })}
                        className="flex items-center gap-2 px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors"
                      >
                        <LocalPrintshopOutlinedIcon />
                        Print This Sticker
                      </button>
                    </div>

                    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                      <h2 className="text-lg font-semibold text-gray-800 mb-3">
                        Details
                      </h2>
                      <dl className="space-y-2">
                        <div className="flex justify-between">
                          <dt className="text-gray-500">Created</dt>
                          <dd className="text-gray-900">
                            {formatDate(sticker.createdAt)}
                          </dd>
                        </div>
                        <div className="flex justify-between">
                          <dt className="text-gray-500">Last Updated</dt>
                          <dd className="text-gray-900">
                            {formatDate(sticker.updatedAt)}
                          </dd>
                        </div>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

export default StickerDetail;
