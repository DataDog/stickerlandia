import React, { useState, useEffect } from 'react'
import { Link } from 'react-router'
import { API_BASE_URL } from '../config'
import HeaderBar from './HeaderBar'
import Sidebar from './Sidebar'

const StickerList = () => {
  const [stickers, setStickers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [page, setPage] = useState(0)
  const [pageSize] = useState(12)
  const [totalPages, setTotalPages] = useState(0)
  const [total, setTotal] = useState(0)

  useEffect(() => {
    const fetchStickers = async () => {
      try {
        setLoading(true)
        const response = await fetch(
          `${API_BASE_URL}/api/stickers/v1?page=${page}&size=${pageSize}`
        )

        if (!response.ok) {
          throw new Error(`Failed to fetch stickers: ${response.status}`)
        }

        const data = await response.json()
        const sortedStickers = (data.stickers || []).sort((a, b) =>
          a.stickerId.localeCompare(b.stickerId)
        )
        setStickers(sortedStickers)

        if (data.pagination) {
          setTotalPages(data.pagination.totalPages || 1)
          setTotal(data.pagination.total || sortedStickers.length)
        } else {
          setTotalPages(1)
          setTotal(sortedStickers.length)
        }
      } catch (err) {
        console.error('Error fetching stickers:', err)
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }

    fetchStickers()
  }, [page, pageSize])

  const handlePreviousPage = () => {
    if (page > 0) {
      setPage(page - 1)
    }
  }

  const handleNextPage = () => {
    if (page < totalPages - 1) {
      setPage(page + 1)
    }
  }

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <HeaderBar />
      <main id="main">
        <div className="grid grid-cols-1 lg:grid-cols-5">
          <Sidebar />
          <div className="col-span-1 lg:col-span-4 p-4 sm:p-6 lg:p-8">
            <h1 className="text-2xl sm:text-3xl font-bold mb-4">Sticker Catalogue</h1>
            <p className="text-gray-600 mb-8">
              Browse all available stickers in the collection.
            </p>

            {loading && (
              <div className="text-center py-8">
                <p className="text-gray-500">Loading stickers...</p>
              </div>
            )}

            {error && (
              <div className="text-center py-8">
                <p className="text-red-500">Error: {error}</p>
              </div>
            )}

            {!loading && !error && stickers.length === 0 && (
              <div className="text-center py-8">
                <p className="text-gray-500">No stickers found in the catalogue.</p>
              </div>
            )}

            {!loading && !error && stickers.length > 0 && (
              <>
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
                            e.target.style.display = 'none'
                          }}
                        />
                      </div>
                      <p className="text-sm text-gray-500 line-clamp-3 mb-2">
                        {sticker.stickerDescription || 'No description available'}
                      </p>
                      <p className="text-sm text-gray-400 mt-auto">
                        {sticker.stickerQuantityRemaining === -1
                          ? 'Unlimited available'
                          : `${sticker.stickerQuantityRemaining} remaining`}
                      </p>
                    </Link>
                  ))}
                </div>

                <div className="mt-8 flex items-center justify-between border-t border-gray-200 pt-6">
                  <div className="text-sm text-gray-500">
                    Showing {page * pageSize + 1} to {Math.min((page + 1) * pageSize, total)} of {total} stickers
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={handlePreviousPage}
                      disabled={page === 0}
                      className={`px-4 py-2 rounded-lg border ${
                        page === 0
                          ? 'border-gray-200 text-gray-300 cursor-not-allowed'
                          : 'border-gray-300 text-gray-700 hover:bg-gray-50'
                      }`}
                    >
                      Previous
                    </button>
                    <span className="px-4 py-2 text-gray-600">
                      Page {page + 1} of {totalPages}
                    </span>
                    <button
                      onClick={handleNextPage}
                      disabled={page >= totalPages - 1}
                      className={`px-4 py-2 rounded-lg border ${
                        page >= totalPages - 1
                          ? 'border-gray-200 text-gray-300 cursor-not-allowed'
                          : 'border-gray-300 text-gray-700 hover:bg-gray-50'
                      }`}
                    >
                      Next
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}

export default StickerList
