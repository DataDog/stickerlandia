import React, { useState, useEffect } from "react";
import { Link } from "react-router";
import { useAuth } from "../context/AuthContext";
import HeaderBar from "./HeaderBar";
import Sidebar from "./Sidebar";
import { API_BASE_URL } from "../config";

function MyCollection() {
  const { user } = useAuth();
  const [stickers, setStickers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchStickers = async () => {
      try {
        setLoading(true);
        const userId = user.sub || user.email;

        // Fetch user's sticker assignments
        const assignmentsResponse = await fetch(
          `${API_BASE_URL}/api/awards/v1/assignments/${userId}`
        );

        if (!assignmentsResponse.ok) {
          throw new Error(`Failed to fetch assignments: ${assignmentsResponse.status}`);
        }

        const assignmentsData = await assignmentsResponse.json();
        const assignments = assignmentsData.stickers || [];

        // Enrich with sticker metadata from catalogue
        const enrichedStickers = await Promise.all(
          assignments.map(async (assignment) => {
            try {
              const metadataResponse = await fetch(
                `${API_BASE_URL}/api/stickers/v1/${assignment.stickerId}`
              );

              if (metadataResponse.ok) {
                const metadata = await metadataResponse.json();
                return {
                  ...assignment,
                  stickerName: metadata.stickerName,
                  stickerDescription: metadata.stickerDescription,
                };
              }
            } catch (err) {
              console.error(`Failed to fetch metadata for ${assignment.stickerId}:`, err);
            }

            // Return assignment without enrichment if fetch fails
            return {
              ...assignment,
              stickerName: assignment.stickerId,
              stickerDescription: assignment.reason || "Earned on your journey",
            };
          })
        );

        const sortedStickers = enrichedStickers.sort((a, b) =>
          a.stickerId.localeCompare(b.stickerId)
        );
        setStickers(sortedStickers);
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

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="grid grid-cols-1 lg:grid-cols-5">
          <Sidebar />
          <div className="col-span-1 lg:col-span-4 p-4 sm:p-6 lg:p-8">
            <h1 className="text-2xl sm:text-3xl font-bold mb-4">My Collection</h1>
            <p className="text-gray-600 mb-8">
              Welcome to your sticker collection, {user?.given_name || 'collector'}!
            </p>

            {loading && (
              <div className="text-center py-8">
                <p className="text-gray-500">Loading your collection...</p>
              </div>
            )}

            {error && (
              <div className="text-center py-8">
                <p className="text-red-500">Error: {error}</p>
              </div>
            )}

            {!loading && !error && stickers.length === 0 && (
              <div className="text-center py-8">
                <p className="text-gray-500">No stickers in your collection yet.</p>
              </div>
            )}

            {!loading && !error && stickers.length > 0 && (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
                {stickers.map((sticker) => (
                  <Link
                    key={sticker.stickerId}
                    to={`/stickers/${sticker.stickerId}`}
                    className="landing-card hover:shadow-lg transition-shadow duration-200 flex flex-col"
                  >
                    <h3 className="font-bold text-xl mb-4 line-clamp-2">
                      {sticker.stickerName}
                    </h3>
                    <div className="aspect-square w-full mb-4 flex items-center justify-center bg-gray-50 rounded-lg overflow-hidden">
                      <img
                        src={`${API_BASE_URL}/api/stickers/v1/${sticker.stickerId}/image`}
                        alt={sticker.stickerName}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          e.target.style.display = "none";
                        }}
                      />
                    </div>
                    <p className="text-sm text-gray-500 line-clamp-3">
                      {sticker.stickerDescription || "Earned on your journey"}
                    </p>
                  </Link>
                ))}
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

export default MyCollection;
