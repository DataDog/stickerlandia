/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { useState, useEffect, useRef, useCallback } from 'react'
import { registerPrinter } from '../../services/print'

export default function RegisterPrinterDialog({ eventName, onClose, onSuccess }) {
  const [printerName, setPrinterName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState(null)
  const [registrationResult, setRegistrationResult] = useState(null)
  const [copied, setCopied] = useState(false)
  const [acknowledged, setAcknowledged] = useState(false)

  const isSubmittingRef = useRef(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    if (!printerName.trim() || isSubmittingRef.current) return

    isSubmittingRef.current = true
    setSubmitting(true)
    setError(null)

    try {
      const result = await registerPrinter(eventName, printerName.trim())
      setRegistrationResult(result.data)
    } catch (err) {
      setError(err.message)
    } finally {
      isSubmittingRef.current = false
      setSubmitting(false)
    }
  }

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(registrationResult.key)
      setCopied(true)
    } catch (err) {
      console.error('Failed to copy:', err)
    }
  }

  // Auto-reset copied state with proper cleanup
  useEffect(() => {
    if (!copied) return
    const timeoutId = setTimeout(() => setCopied(false), 2000)
    return () => clearTimeout(timeoutId)
  }, [copied])

  const handleClose = useCallback(() => {
    if (registrationResult && acknowledged) {
      onSuccess()
    }
    onClose()
  }, [registrationResult, acknowledged, onSuccess, onClose])

  // Close on Escape key (only if no API key showing or already acknowledged)
  useEffect(() => {
    const handleEscape = (e) => {
      if (e.key === 'Escape' && (!registrationResult || acknowledged)) {
        handleClose()
      }
    }
    document.addEventListener('keydown', handleEscape)
    return () => document.removeEventListener('keydown', handleEscape)
  }, [registrationResult, acknowledged, handleClose])

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4"
      onClick={(e) => {
        if (e.target === e.currentTarget && (!registrationResult || acknowledged)) {
          handleClose()
        }
      }}
    >
      <div
        className="bg-white rounded-lg shadow-xl max-w-md w-full"
        role="dialog"
        aria-modal="true"
        aria-labelledby="register-dialog-title"
      >
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 id="register-dialog-title" className="text-xl font-bold text-gray-800">
            Register New Printer
          </h2>
        </div>

        {/* Content */}
        <div className="px-6 py-4">
          {registrationResult ? (
            // API Key Display (inline)
            <div>
              <div className="text-green-600 text-lg font-medium mb-4 text-center">
                Printer "{registrationResult.printerName}" registered!
              </div>

              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-4">
                <div className="flex items-center gap-2 text-yellow-800 mb-2">
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                  </svg>
                  <strong>Save this API key now</strong>
                </div>
                <p className="text-yellow-700 text-sm">
                  This key will only be shown once. You need it to configure the printer client.
                </p>
              </div>

              <div className="bg-gray-100 rounded-lg p-4 mb-4">
                <div className="flex items-center justify-between gap-2">
                  <code className="text-sm font-mono break-all flex-1">
                    {registrationResult.key}
                  </code>
                  <button
                    onClick={handleCopy}
                    className="p-2 hover:bg-gray-200 rounded-lg transition-colors shrink-0"
                    title="Copy to clipboard"
                  >
                    {copied ? (
                      <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                    ) : (
                      <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>

              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={acknowledged}
                  onChange={(e) => setAcknowledged(e.target.checked)}
                  className="w-4 h-4 rounded border-gray-300"
                />
                <span className="text-sm text-gray-700">
                  I have copied and saved this API key
                </span>
              </label>
            </div>
          ) : (
            // Registration Form
            <form onSubmit={handleSubmit}>
              <p className="text-gray-600 mb-4">
                Register a new printer for <strong>{eventName}</strong>
              </p>

              <div className="mb-4">
                <label htmlFor="printerName" className="block text-sm font-medium text-gray-700 mb-1">
                  Printer Name
                </label>
                <input
                  id="printerName"
                  type="text"
                  autoFocus
                  required
                  value={printerName}
                  onChange={(e) => setPrinterName(e.target.value)}
                  placeholder="e.g., Lobby-Printer-1"
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                />
              </div>

              {error && (
                <div className="p-3 bg-red-50 text-red-700 rounded-lg">
                  {error}
                </div>
              )}
            </form>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 flex justify-end gap-3">
          {registrationResult ? (
            <button
              onClick={handleClose}
              disabled={!acknowledged}
              className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors w-full"
            >
              Close
            </button>
          ) : (
            <>
              <button
                onClick={onClose}
                className="px-4 py-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleSubmit}
                disabled={!printerName.trim() || submitting}
                className="px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors flex items-center gap-2"
              >
                {submitting && (
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                )}
                {submitting ? 'Registering...' : 'Register'}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
