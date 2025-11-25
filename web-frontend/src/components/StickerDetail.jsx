import React, { useState, useEffect } from "react";
import { useParams, Link } from "react-router";
import { useAuth } from "../context/AuthContext";
import Sidebar from "./Sidebar";

function StickerDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const [sticker, setSticker] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSticker = async () => {
      try {
        const userId = user.sub || user.email;
        const response = await fetch(
          `http://localhost:8080/api/awards/v1/assignments/${userId}`
        );
        const data = await response.json();
        const found = (data.stickers || []).find(s => s.stickerId === id);
        setSticker(found);
      } catch (err) {
        console.error("Error fetching sticker:", err);
      } finally {
        setLoading(false);
      }
    };

    if (user) {
      fetchSticker();
    }
  }, [user, id]);

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <main id="main">
        <div className="grid grid-cols-5">
          <Sidebar />
          <div className="col-span-4 p-8">
            <Link to="/collection" className="text-blue-600 hover:text-blue-800 mb-4 inline-block">
              ← Back to Collection
            </Link>
            {loading ? (
              <h1 className="text-3xl font-bold mb-4">Loading...</h1>
            ) : (
              <h1 className="text-3xl font-bold mb-4">
                {sticker?.stickerName || id}
              </h1>
            )}
            <p className="text-gray-500">TODO</p>
          </div>
        </div>
      </main>
    </div>
  );
}

export default StickerDetail;
