/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

const KNOWN_EVENTS_KEY = 'stickerlandia-known-events'
const LAST_ACTIVE_EVENT_KEY = 'stickerlandia-last-active-event'

export function getKnownEvents() {
  try {
    const stored = localStorage.getItem(KNOWN_EVENTS_KEY)
    return stored ? JSON.parse(stored) : []
  } catch {
    return []
  }
}

export function addKnownEvent(eventName) {
  const events = getKnownEvents()
  if (!events.includes(eventName)) {
    events.push(eventName)
    localStorage.setItem(KNOWN_EVENTS_KEY, JSON.stringify(events))
  }
}

export function removeKnownEvent(eventName) {
  const events = getKnownEvents().filter(e => e !== eventName)
  localStorage.setItem(KNOWN_EVENTS_KEY, JSON.stringify(events))
}

export function getLastActiveEvent() {
  return localStorage.getItem(LAST_ACTIVE_EVENT_KEY) || null
}

export function setLastActiveEvent(eventName) {
  localStorage.setItem(LAST_ACTIVE_EVENT_KEY, eventName)
}
