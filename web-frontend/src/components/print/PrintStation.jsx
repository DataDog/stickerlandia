/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { useLocation } from 'react-router'
import { useAuth } from '../../context/AuthContext'
import { DEFAULT_EVENT } from '../../config'
import { getPrintersWithStatus } from '../../services/print'
import PrinterCard from './PrinterCard'
import PrintDialog from './PrintDialog'
import RegisterPrinterDialog from './RegisterPrinterDialog'
import Sidebar from '../Sidebar'

export default function PrintStation() {
  const location = useLocation()
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()

  // Data fetching state (inline, following MyCollection pattern)
  const [printers, setPrinters] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Dialog state
  const [printDialogOpen, setPrintDialogOpen] = useState(false)
  const [selectedPrinter, setSelectedPrinter] = useState(null)
  const [registerDialogOpen, setRegisterDialogOpen] = useState(false)

  // Pre-selected sticker from navigation (e.g., from StickerDetail page)
  const preselectedSticker = location.state?.sticker || null

  const eventName = DEFAULT_EVENT
  const isAdmin = user?.roles?.includes('admin')
  const isMountedRef = useRef(true)

  const PRINTER_STATUS_POLL_INTERVAL_MS = 30000

  // Fetch printers - separates initial load from background refresh
  const fetchPrinters = useCallback(async (isBackgroundRefresh = false) => {
    try {
      if (!isBackgroundRefresh) {
        setLoading(true)
      }

      const data = await getPrintersWithStatus(eventName)
      // Only update state if component is still mounted
      if (isMountedRef.current) {
        setPrinters(data)
        setError(null)
      }
    } catch (err) {
      if (isMountedRef.current) {
        setError(err.message)
      }
    } finally {
      if (isMountedRef.current) {
        setLoading(false)
      }
    }
  }, [eventName])

  // Initial fetch + polling
  useEffect(() => {
    isMountedRef.current = true
    fetchPrinters(false) // Initial load shows spinner

    // Poll every N seconds (background refresh, no spinner)
    const interval = setInterval(() => fetchPrinters(true), PRINTER_STATUS_POLL_INTERVAL_MS)
    return () => {
      isMountedRef.current = false
      clearInterval(interval)
    }
  }, [fetchPrinters])

  // Show loading while checking auth
  if (authLoading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="p-8 text-center">
        <p className="text-gray-600">Please log in to access the Print Station.</p>
      </div>
    )
  }

  const handlePrintClick = (printer) => {
    setSelectedPrinter(printer)
    setPrintDialogOpen(true)
  }

  const handlePrintDialogClose = () => {
    setPrintDialogOpen(false)
    setSelectedPrinter(null)
  }

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <main id="main">
        <div className="grid grid-cols-5">
          <Sidebar />
          <div className="col-span-4 p-8">
            {/* Header */}
            <div className="flex justify-between items-center mb-6">
              <div>
                <h1 className="text-3xl font-bold text-gray-800">Print Station</h1>
                <p className="text-gray-600">Event: {eventName}</p>
              </div>

              {isAdmin && (
                <button
                  onClick={() => setRegisterDialogOpen(true)}
                  className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 transition-colors"
                >
                  Register Printer
                </button>
              )}
            </div>

            {/* Pre-selected sticker notice */}
            {preselectedSticker && (
              <div className="mb-6 p-4 bg-purple-50 border border-purple-200 rounded-lg">
                <p className="text-purple-800">
                  Ready to print: <strong>{preselectedSticker.stickerName}</strong>
                  <span className="text-purple-600 ml-2">— Select a printer below</span>
                </p>
              </div>
            )}

            {/* Printer List (inline) */}
            {loading ? (
              <div className="flex justify-center items-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-purple-600"></div>
              </div>
            ) : error ? (
              <div className="text-center py-8">
                <p className="text-red-500">{error}</p>
                <button
                  onClick={() => fetchPrinters(false)}
                  className="mt-4 text-purple-600 hover:underline"
                >
                  Try again
                </button>
              </div>
            ) : printers.length === 0 ? (
              <div className="text-center py-12 landing-card">
                <p className="text-gray-500">No printers registered for this event yet.</p>
                {isAdmin && (
                  <button
                    onClick={() => setRegisterDialogOpen(true)}
                    className="mt-4 text-purple-600 hover:underline"
                  >
                    Register the first printer
                  </button>
                )}
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {printers.map((printer) => (
                  <PrinterCard
                    key={printer.printerId}
                    printer={printer}
                    onPrintClick={() => handlePrintClick(printer)}
                  />
                ))}
              </div>
            )}

            {/* Print Dialog */}
            {printDialogOpen && selectedPrinter && (
              <PrintDialog
                printer={selectedPrinter}
                eventName={eventName}
                preselectedSticker={preselectedSticker}
                onClose={handlePrintDialogClose}
              />
            )}

            {/* Register Printer Dialog (Admin) */}
            {registerDialogOpen && (
              <RegisterPrinterDialog
                eventName={eventName}
                onClose={() => setRegisterDialogOpen(false)}
                onSuccess={() => {
                  setRegisterDialogOpen(false)
                  fetchPrinters(false)
                }}
              />
            )}
          </div>
        </div>
      </main>
    </div>
  )
}
