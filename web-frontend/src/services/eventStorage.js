/*
 * Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
 * This product includes software developed at Datadog (https://www.datadoghq.com/).
 * Copyright 2025-Present Datadog, Inc.
 */

const LAST_ACTIVE_EVENT_KEY = 'stickerlandia-last-active-event'

export function getLastActiveEvent() {
  return localStorage.getItem(LAST_ACTIVE_EVENT_KEY) || null
}

export function setLastActiveEvent(eventName) {
  localStorage.setItem(LAST_ACTIVE_EVENT_KEY, eventName)
}
