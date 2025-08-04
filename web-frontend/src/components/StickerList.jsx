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
    <div style={{ margin: '20px 0' }}>
      <h2 style={{ color: 'inherit', marginBottom: '20px' }}>Stickers</h2>
      <table style={{
        width: '100%',
        borderCollapse: 'collapse',
        backgroundColor: 'rgba(255, 255, 255, 0.05)',
        borderRadius: '8px',
        overflow: 'hidden'
      }}>
        <thead>
          <tr style={{ backgroundColor: 'rgba(255, 255, 255, 0.1)' }}>
            <th style={{ padding: '12px', textAlign: 'left', color: 'inherit', borderBottom: '1px solid rgba(255, 255, 255, 0.2)' }}>
              Image
            </th>
            <th style={{ padding: '12px', textAlign: 'left', color: 'inherit', borderBottom: '1px solid rgba(255, 255, 255, 0.2)' }}>
              ID
            </th>
            <th style={{ padding: '12px', textAlign: 'left', color: 'inherit', borderBottom: '1px solid rgba(255, 255, 255, 0.2)' }}>
              Name
            </th>
            <th style={{ padding: '12px', textAlign: 'left', color: 'inherit', borderBottom: '1px solid rgba(255, 255, 255, 0.2)' }}>
              Description
            </th>
            <th style={{ padding: '12px', textAlign: 'left', color: 'inherit', borderBottom: '1px solid rgba(255, 255, 255, 0.2)' }}>
              Quantity
            </th>
          </tr>
        </thead>
        <tbody>
          {stickers.map((sticker) => (
            <tr key={sticker.stickerId} style={{ borderBottom: '1px solid rgba(255, 255, 255, 0.1)' }}>
              <td style={{ padding: '12px' }}>
                <img 
                  src={`http://localhost:8080/api/stickers/v1/${sticker.stickerId}/image`}
                  alt={sticker.stickerName}
                  style={{
                    width: '50px',
                    height: '50px',
                    objectFit: 'cover',
                    borderRadius: '4px',
                    border: '1px solid rgba(255, 255, 255, 0.2)'
                  }}
                  onError={(e) => {
                    e.target.style.display = 'none'
                  }}
                />
              </td>
              <td style={{ padding: '12px', color: 'inherit' }}>
                {sticker.stickerId}
              </td>
              <td style={{ padding: '12px', color: 'inherit' }}>
                {sticker.stickerName}
              </td>
              <td style={{ padding: '12px', color: 'inherit' }}>
                {sticker.stickerDescription}
              </td>
              <td style={{ padding: '12px', color: 'inherit' }}>
                {sticker.stickerQuantityRemaining === -1 ? 'Unlimited' : sticker.stickerQuantityRemaining}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export default StickerList