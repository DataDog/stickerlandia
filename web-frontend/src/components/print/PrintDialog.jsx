/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { useState, useEffect, useRef } from 'react'
import { useAuth } from '../../context/AuthContext'
import { API_BASE_URL } from '../../config'
import { submitPrintJob } from '../../services/print'
import AuthService from '../../services/AuthService'

export default function PrintDialog({ printer, eventName, preselectedSticker, onClose }) {
  const { user } = useAuth()
  const [stickers, setStickers] = useState([])
  const [selectedSticker, setSelectedSticker] = useState(preselectedSticker)
  const [loading, setLoading] = useState(!preselectedSticker)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(false)

  const isSubmittingRef = useRef(false)
  const userId = user?.sub || user?.email

  // Fetch user's sticker collection (skip if preselected)
  useEffect(() => {
    if (preselectedSticker) {
      setStickers([preselectedSticker])
      return
    }

    // Validate userId before fetching
    if (!userId) {
      setError('Unable to identify user. Please log in again.')
      setLoading(false)
      return
    }

    const controller = new AbortController()

    const fetchStickers = async () => {
      try {
        setLoading(true)
        const tokenData = AuthService.getStoredToken()
        const headers = {
          'Content-Type': 'application/json'
        }
        if (tokenData?.access_token) {
          headers['Authorization'] = `Bearer ${tokenData.access_token}`
        }

        const response = await fetch(
          `${API_BASE_URL}/api/awards/v1/assignments/${encodeURIComponent(userId)}`,
          {
            headers,
            credentials: 'include',
            signal: controller.signal
          }
        )
        if (!response.ok) throw new Error('Failed to fetch your sticker collection')
        const data = await response.json()

        if (!controller.signal.aborted) {
          setStickers(data.stickers || [])
        }
      } catch (err) {
        if (err.name !== 'AbortError' && !controller.signal.aborted) {
          setError(err.message)
        }
      } finally {
        if (!controller.signal.aborted) {
          setLoading(false)
        }
      }
    }

    fetchStickers()
    return () => controller.abort()
  }, [userId, preselectedSticker])

  const handleSubmit = async () => {
    if (!selectedSticker || isSubmittingRef.current) return

    // Validate userId before submitting
    if (!userId) {
      setError('Unable to identify user. Please log in again.')
      return
    }

    isSubmittingRef.current = true
    setSubmitting(true)
    setError(null)

    try {
      await submitPrintJob(eventName, printer.printerName, {
        userId,
        stickerId: selectedSticker.stickerId,
        stickerUrl: `${API_BASE_URL}/api/stickers/v1/${encodeURIComponent(selectedSticker.stickerId)}/image`,
      });

      setSuccess(true)
    } catch (err) {
      setError(err.message)
    } finally {
      isSubmittingRef.current = false
      setSubmitting(false)
    }
  }

  // Close on Escape key
  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', handleEscape)
    return () => document.removeEventListener('keydown', handleEscape)
  }, [onClose])

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
      onClick={(e) => e.target === e.currentTarget && onClose()}
    >
      <div
        className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-hidden"
        role="dialog"
        aria-modal="true"
        aria-labelledby="print-dialog-title"
      >
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 id="print-dialog-title" className="text-xl font-bold text-gray-800">
            Print to {printer.printerName}
          </h2>
        </div>

        {/* Content */}
        <div className="px-6 py-4 overflow-y-auto" style={{ maxHeight: 'calc(90vh - 140px)' }}>
          {success ? (
            <div className="py-8 text-center">
              <div className="text-green-600 text-xl mb-2">Print job submitted!</div>
              <p className="text-gray-600">Your sticker is being sent to the printer.</p>
            </div>
          ) : (
            <>
              <p className="text-gray-600 mb-4">
                {preselectedSticker
                  ? 'Confirm you want to print this sticker:'
                  : 'Select a sticker from your collection to print:'}
              </p>

              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
                </div>
              ) : stickers.length === 0 ? (
                <div className="text-center py-8 text-gray-500">
                  You don't have any stickers to print yet.
                </div>
              ) : (
                <div className="grid grid-cols-3 gap-4">
                  {stickers.map((sticker) => (
                    <div
                      key={sticker.stickerId}
                      onClick={() => setSelectedSticker(sticker)}
                      className={`cursor-pointer p-2 rounded-lg border-2 transition-colors ${
                        selectedSticker?.stickerId === sticker.stickerId
                          ? 'border-purple-600 bg-purple-50'
                          : 'border-gray-200 hover:border-purple-300'
                      }`}
                    >
                      <img
                        src={`${API_BASE_URL}/api/stickers/v1/${sticker.stickerId}/image`}
                        alt={sticker.stickerName}
                        className="w-full aspect-square object-contain rounded"
                      />
                      <p className="text-sm text-center mt-2 truncate">
                        {sticker.stickerName}
                      </p>
                    </div>
                  ))}
                </div>
              )}

              {error && (
                <div className="mt-4 p-3 bg-red-50 text-red-700 rounded-lg">
                  {error}
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex justify-end gap-3">
          <button
            onClick={onClose}
            className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
          >
            {success ? 'Close' : 'Cancel'}
          </button>
          {!success && (
            <button
              onClick={handleSubmit}
              disabled={!selectedSticker || submitting}
              className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
            >
              {submitting && (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
              )}
              {submitting ? 'Submitting...' : 'Print'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
