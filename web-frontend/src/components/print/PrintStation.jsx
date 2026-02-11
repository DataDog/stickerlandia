/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { useState, useEffect, useCallback, useRef } from 'react'
import { useLocation, useParams, Link } from 'react-router'
import { useAuth } from '../../context/AuthContext'
import { getPrintersWithStatus, deletePrinter } from '../../services/print'
import { setLastActiveEvent } from '../../services/eventStorage'
import PrinterCard from './PrinterCard'
import PrintDialog from './PrintDialog'
import RegisterPrinterDialog from './RegisterPrinterDialog'
import EventSelector from './EventSelector'
import Sidebar from '../Sidebar'

export default function PrintStation() {
  const location = useLocation()
  const { eventName } = useParams()
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

  const isAdmin = user?.role?.some(r => r.toLowerCase() === 'admin')
  const isMountedRef = useRef(true)

  const PRINTER_STATUS_POLL_INTERVAL_MS = 30000

  // Track visited event in localStorage
  useEffect(() => {
    if (eventName) {
      setLastActiveEvent(eventName)
    }
  }, [eventName])

  // Fetch printers - separates initial load from background refresh
  const fetchPrinters = useCallback(async (isBackgroundRefresh = false) => {
    if (!eventName || !isAuthenticated) return

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
  }, [eventName, isAuthenticated])

  // Initial fetch + polling (only when authenticated)
  useEffect(() => {
    if (!eventName || !isAuthenticated) return

    isMountedRef.current = true
    fetchPrinters(false) // Initial load shows spinner

    // Poll every N seconds (background refresh, no spinner)
    const interval = setInterval(() => fetchPrinters(true), PRINTER_STATUS_POLL_INTERVAL_MS)
    return () => {
      isMountedRef.current = false
      clearInterval(interval)
    }
  }, [fetchPrinters, eventName, isAuthenticated])

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

  // No event selected — show event selector (after auth is confirmed)
  if (!eventName) {
    return <EventSelector />
  }

  const handlePrintClick = (printer) => {
    setSelectedPrinter(printer)
    setPrintDialogOpen(true)
  }

  const handlePrintDialogClose = () => {
    setPrintDialogOpen(false)
    setSelectedPrinter(null)
  }

  const handleDeletePrinter = async (printer) => {
    if (!window.confirm(`Delete printer "${printer.printerName}"? This will also delete all its print jobs.`)) {
      return
    }

    try {
      await deletePrinter(eventName, printer.printerName)
      fetchPrinters(false)
    } catch (err) {
      if (err.status === 409) {
        if (window.confirm(`${err.message}\n\nForce delete this printer and all its jobs?`)) {
          try {
            await deletePrinter(eventName, printer.printerName, true)
            fetchPrinters(false)
          } catch (forceErr) {
            setError(forceErr.message)
          }
        }
      } else {
        setError(err.message)
      }
    }
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
                <p className="text-gray-600">
                  Event: {eventName}
                  <Link
                    to="/print-station"
                    className="ml-3 text-purple-600 hover:text-purple-800 text-sm"
                  >
                    Switch Event
                  </Link>
                </p>
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
                    isAdmin={isAdmin}
                    onPrintClick={() => handlePrintClick(printer)}
                    onDeleteClick={() => handleDeletePrinter(printer)}
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
