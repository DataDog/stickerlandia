/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router'
import { useAuth } from '../../context/AuthContext'
import { getKnownEvents, addKnownEvent, removeKnownEvent } from '../../services/eventStorage'
import { getPrintersWithStatus } from '../../services/print'
import Sidebar from '../Sidebar'

export default function EventSelector() {
  const navigate = useNavigate()
  const { user, isAuthenticated, isLoading: authLoading } = useAuth()
  const [knownEvents, setKnownEvents] = useState([])
  const [eventCounts, setEventCounts] = useState({})
  const [newEventName, setNewEventName] = useState('')
  const [manualEventName, setManualEventName] = useState('')
  const [error, setError] = useState(null)

  const isAdmin = user?.role?.some(r => r.toLowerCase() === 'admin')

  useEffect(() => {
    setKnownEvents(getKnownEvents())
  }, [])

  // Fetch printer counts for known events
  useEffect(() => {
    if (!isAuthenticated || knownEvents.length === 0) return

    const controller = new AbortController()
    const fetchCounts = async () => {
      const counts = {}
      await Promise.all(
        knownEvents.map(async (eventName) => {
          try {
            const printers = await getPrintersWithStatus(eventName)
            if (!controller.signal.aborted) {
              counts[eventName] = printers.length
            }
          } catch {
            counts[eventName] = null
          }
        })
      )
      if (!controller.signal.aborted) {
        setEventCounts(counts)
      }
    }
    fetchCounts()
    return () => controller.abort()
  }, [isAuthenticated, knownEvents])

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

  const handleCreateEvent = (e) => {
    e.preventDefault()
    const name = newEventName.trim()
    if (!name) return

    addKnownEvent(name)
    setKnownEvents(getKnownEvents())
    setNewEventName('')
    navigate(`/print-station/${encodeURIComponent(name)}`)
  }

  const handleGoToEvent = (e) => {
    e.preventDefault()
    const name = manualEventName.trim()
    if (!name) return
    navigate(`/print-station/${encodeURIComponent(name)}`)
  }

  const handleRemoveEvent = (eventName) => {
    removeKnownEvent(eventName)
    setKnownEvents(getKnownEvents())
  }

  return (
    <div className="isolate flex flex-auto flex-col bg-[--root-bg]">
      <main id="main">
        <div className="grid grid-cols-5">
          <Sidebar />
          <div className="col-span-4 p-8">
            <div className="mb-6">
              <h1 className="text-3xl font-bold text-gray-800">Print Station</h1>
              <p className="text-gray-600">Select or create an event to manage printers and print jobs.</p>
            </div>

            {error && (
              <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
                {error}
              </div>
            )}

            {/* Admin: Create New Event */}
            {isAdmin && (
              <div className="mb-8 p-6 bg-white rounded-lg shadow-sm border border-gray-200">
                <h2 className="text-lg font-semibold text-gray-800 mb-3">Create New Event</h2>
                <form onSubmit={handleCreateEvent} className="flex gap-3">
                  <input
                    type="text"
                    value={newEventName}
                    onChange={(e) => setNewEventName(e.target.value)}
                    placeholder="e.g., stickerlandia-2026"
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                  />
                  <button
                    type="submit"
                    disabled={!newEventName.trim()}
                    className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                  >
                    Create Event
                  </button>
                </form>
              </div>
            )}

            {/* Known Events List */}
            {knownEvents.length > 0 ? (
              <>
                <h2 className="text-lg font-semibold text-gray-800 mb-4">Your Events</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
                  {knownEvents.map((eventName) => (
                    <div
                      key={eventName}
                      className="bg-white rounded-lg shadow-sm border border-gray-200 hover:shadow-md transition-shadow cursor-pointer"
                      onClick={() => navigate(`/print-station/${encodeURIComponent(eventName)}`)}
                    >
                      <div className="p-5">
                        <div className="flex justify-between items-start">
                          <h3 className="text-lg font-medium text-gray-800">{eventName}</h3>
                          {isAdmin && (
                            <button
                              onClick={(e) => {
                                e.stopPropagation()
                                handleRemoveEvent(eventName)
                              }}
                              className="text-gray-400 hover:text-red-500 transition-colors p-1"
                              title="Remove event"
                            >
                              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                              </svg>
                            </button>
                          )}
                        </div>
                        <p className="text-sm text-gray-500 mt-2">
                          {eventCounts[eventName] != null
                            ? `${eventCounts[eventName]} printer${eventCounts[eventName] !== 1 ? 's' : ''} registered`
                            : 'Loading...'}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="text-center py-12 landing-card mb-8">
                <p className="text-gray-500">No events saved yet.</p>
                {isAdmin ? (
                  <p className="text-gray-400 mt-2">Create your first event above to get started.</p>
                ) : (
                  <p className="text-gray-400 mt-2">Enter an event name below to get started.</p>
                )}
              </div>
            )}

            {/* Non-admin: Go to event by name */}
            {!isAdmin && (
              <div className="p-6 bg-white rounded-lg shadow-sm border border-gray-200">
                <h2 className="text-lg font-semibold text-gray-800 mb-3">Go to Event</h2>
                <form onSubmit={handleGoToEvent} className="flex gap-3">
                  <input
                    type="text"
                    value={manualEventName}
                    onChange={(e) => setManualEventName(e.target.value)}
                    placeholder="Enter event name"
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                  />
                  <button
                    type="submit"
                    disabled={!manualEventName.trim()}
                    className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors"
                  >
                    Go
                  </button>
                </form>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}
