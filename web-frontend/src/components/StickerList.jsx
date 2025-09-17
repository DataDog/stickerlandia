// Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
// This product includes software developed at Datadog (https://www.datadoghq.com/).
// Copyright 2025 Datadog, Inc.
import React, { useState, useEffect } from 'react'

const StickerList = () => {
  const [stickers, setStickers] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    const fetchStickers = async () => {
      try {
        setLoading(true)
        const response = await fetch('http://localhost:8080/api/stickers/v1')
        
        if (!response.ok) {
          throw new Error(`Failed to fetch stickers: ${response.status}`)
        }
        
        const data = await response.json()
        const sortedStickers = (data.stickers || []).sort((a, b) => a.stickerId.localeCompare(b.stickerId))
        setStickers(sortedStickers)
      } catch (err) {
        console.error('Error fetching stickers:', err)
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }

    fetchStickers()
  }, [])

  if (loading) {
    return <div style={{ color: 'inherit', padding: '20px' }}>Loading stickers...</div>
  }

  if (error) {
    return <div style={{ color: '#ff6b6b', padding: '20px' }}>Error: {error}</div>
  }

  if (!stickers || stickers.length === 0) {
    return <div style={{ color: 'inherit', padding: '20px' }}>No stickers found.</div>
  }

  return (
    <div className="mt-8">
      <h2 className="text-xl font-semibold mb-4 text-gray-800">Available Stickers</h2>
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Image
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                ID
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Name
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Description
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Quantity
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {stickers.map((sticker) => (
              <tr key={sticker.stickerId} className="hover:bg-gray-50">
                <td className="px-4 py-4">
                  <img 
                    src={`http://localhost:8080/api/stickers/v1/${sticker.stickerId}/image`}
                    alt={sticker.stickerName}
                    className="w-12 h-12 object-cover rounded border border-gray-200"
                    onError={(e) => {
                      e.target.style.display = 'none'
                    }}
                  />
                </td>
                <td className="px-4 py-4 text-sm text-gray-900">
                  {sticker.stickerId}
                </td>
                <td className="px-4 py-4 text-sm font-medium text-gray-900">
                  {sticker.stickerName}
                </td>
                <td className="px-4 py-4 text-sm text-gray-700">
                  {sticker.stickerDescription}
                </td>
                <td className="px-4 py-4 text-sm text-gray-900">
                  {sticker.stickerQuantityRemaining === -1 ? 'Unlimited' : sticker.stickerQuantityRemaining}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

export default StickerList