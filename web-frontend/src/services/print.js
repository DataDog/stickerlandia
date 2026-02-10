/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

import { API_BASE_URL } from '../config'
import AuthService from './AuthService'

const BASE_URL = `${API_BASE_URL}/api/print/v1`

const getHeaders = () => {
  const tokenData = AuthService.getStoredToken()
  if (!tokenData?.access_token || !AuthService.isTokenValid()) {
    throw new Error('Session expired. Please log in again.')
  }

  return {
    'Authorization': `Bearer ${tokenData.access_token}`,
    'Content-Type': 'application/json'
  }
}

// Detect responses that were redirected to a non-API page (e.g. login page returning HTML)
const assertJsonResponse = (response) => {
  const contentType = response.headers.get('content-type') || ''
  if (!contentType.includes('application/json') && !contentType.includes('application/problem+json')) {
    if (response.redirected) {
      throw new Error('Your session has expired. Please log in again.')
    }
    throw new Error('Unexpected response from the print service. Please try again.')
  }
}

// Map HTTP errors to user-friendly messages
const getErrorMessage = (response, fallbackDetail) => {
  if (response.status === 401 || response.status === 403) {
    return 'You do not have permission to access this. Please log in again.'
  }
  if (response.status === 404) {
    return 'The requested resource was not found.'
  }
  if (response.status >= 500) {
    return 'The print service is currently unavailable. Please try again later.'
  }
  return fallbackDetail || 'An unexpected error occurred.'
}

export const getPrintersWithStatus = async (eventName) => {
  const encodedEventName = encodeURIComponent(eventName)
  const [printersRes, statusesRes] = await Promise.all([
    fetch(`${BASE_URL}/event/${encodedEventName}`, {
      headers: getHeaders(),
      credentials: 'include'
    }),
    fetch(`${BASE_URL}/event/${encodedEventName}/printers/status`, {
      headers: getHeaders(),
      credentials: 'include'
    })
  ])

  assertJsonResponse(printersRes)

  if (!printersRes.ok) {
    const error = await printersRes.json().catch(() => ({}))
    throw new Error(getErrorMessage(printersRes, error.detail))
  }

  const printers = await printersRes.json()
  const statuses = statusesRes.ok && statusesRes.headers.get('content-type')?.includes('json')
    ? await statusesRes.json()
    : { data: { printers: [] } }

  // Map statuses by printerId
  const statusMap = {}
  ;(statuses.data?.printers || []).forEach(s => {
    statusMap[s.printerId] = s
  })

  // Combine printers with their statuses
  return (printers.data || []).map(printer => ({
    ...printer,
    ...statusMap[printer.printerId],
    isOnline: statusMap[printer.printerId]?.status === 'Online'
  }))
}

export const submitPrintJob = async (eventName, printerName, printJob) => {
  console.log(printJob);
  const response = await fetch(
    `${BASE_URL}/event/${encodeURIComponent(eventName)}/printer/${encodeURIComponent(printerName)}/jobs`,
    {
      method: 'POST',
      headers: getHeaders(),
      credentials: 'include',
      body: JSON.stringify(printJob)
    }
  )

  assertJsonResponse(response)

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(getErrorMessage(response, error.detail))
  }

  return response.json()
}

export const registerPrinter = async (eventName, printerName) => {
  const response = await fetch(`${BASE_URL}/event/${encodeURIComponent(eventName)}`, {
    method: 'POST',
    headers: getHeaders(),
    credentials: 'include',
    body: JSON.stringify({ eventName, printerName })
  })

  assertJsonResponse(response)

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(getErrorMessage(response, error.detail))
  }

  return response.json()
}
