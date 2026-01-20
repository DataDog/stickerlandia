import { useState, useEffect } from "react";
import { useParams, Link, useNavigate } from "react-router";
import LocalPrintshopOutlinedIcon from "@mui/icons-material/LocalPrintshopOutlined";
import { useAuth } from "../context/AuthContext";
import Sidebar from "./Sidebar";
import { API_BASE_URL } from "../config";
import AuthService from "../services/AuthService";

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
    const currentId = id; // Capture current ID for race condition check

    const fetchSticker = async () => {
      try {
        setLoading(true);
        setError(null);

        const tokenData = AuthService.getStoredToken();
        const headers = {
          'Content-Type': 'application/json'
        };
        if (tokenData?.access_token) {
          headers['Authorization'] = `Bearer ${tokenData.access_token}`;
        }

        const response = await fetch(
          `${API_BASE_URL}/api/awards/v1/assignments/${encodeURIComponent(userId)}`,
          {
            headers,
            credentials: 'include',
            signal: controller.signal
          }
        );

        if (!response.ok) {
          throw new Error('Failed to fetch sticker collection');
        }

        const data = await response.json();

        // Only update if ID hasn't changed and not aborted
        if (!controller.signal.aborted && currentId === id) {
          const found = (data.stickers || []).find(s => s.stickerId === id);
          setSticker(found);
        }
      } catch (err) {
        if (err.name !== 'AbortError' && !controller.signal.aborted) {
          console.error("Error fetching sticker:", err);
          setError(err.message);
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      }
    };

    fetchSticker();
    return () => controller.abort();
  }, [user, id, authLoading]);

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
              <div className="flex justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
              </div>
            ) : error ? (
              <div className="text-center py-12">
                <p className="text-red-500">{error}</p>
              </div>
            ) : sticker ? (
              <div className="max-w-2xl">
                <h1 className="text-3xl font-bold mb-6">{sticker.stickerName}</h1>
                <div className="landing-card">
                  <img
                    src={`${API_BASE_URL}/api/stickers/v1/${sticker.stickerId}/image`}
                    alt={sticker.stickerName}
                    className="w-full max-w-md mx-auto aspect-square object-contain rounded-lg mb-6"
                  />
                  <div className="flex justify-center">
                    <button
                      onClick={() => navigate('/print-station', { state: { sticker } })}
                      className="flex items-center gap-2 px-6 py-3 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors"
                    >
                      <LocalPrintshopOutlinedIcon />
                      Print This Sticker
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="text-center py-12">
                <p className="text-gray-500">Sticker not found in your collection.</p>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

export default StickerDetail;
