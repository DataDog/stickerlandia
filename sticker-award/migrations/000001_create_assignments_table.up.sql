-- Copyright 2025 Datadog, Inc.
-- SPDX-License-Identifier: Apache-2.0

-- Create assignments table
-- This table stores sticker assignments to users with soft deletion support

CREATE TABLE assignments (
    id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(255) NOT NULL,
    sticker_id VARCHAR(255) NOT NULL,
    assigned_at TIMESTAMP WITH TIME ZONE NOT NULL,
    removed_at TIMESTAMP WITH TIME ZONE,
    reason VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Add comments for documentation
COMMENT ON TABLE assignments IS 'Stores sticker assignments to users with soft deletion support';
COMMENT ON COLUMN assignments.id IS 'Primary key, auto-incrementing assignment ID';
COMMENT ON COLUMN assignments.user_id IS 'ID of the user who received the sticker assignment';
COMMENT ON COLUMN assignments.sticker_id IS 'ID of the sticker that was assigned (references sticker catalogue)';
COMMENT ON COLUMN assignments.assigned_at IS 'Timestamp when the sticker was assigned to the user';
COMMENT ON COLUMN assignments.removed_at IS 'Timestamp when assignment was removed (NULL for active assignments)';
COMMENT ON COLUMN assignments.reason IS 'Optional reason for the assignment (max 500 characters)';
COMMENT ON COLUMN assignments.created_at IS 'Record creation timestamp';
COMMENT ON COLUMN assignments.updated_at IS 'Record last update timestamp';

-- Create indexes for optimal query performance

-- Index for querying assignments by user (most common query pattern)
CREATE INDEX idx_assignments_user_id ON assignments(user_id);

-- Composite index for user + sticker queries (checking duplicates, specific assignments)
CREATE INDEX idx_assignments_user_sticker ON assignments(user_id, sticker_id);

-- Index for active assignments only (partial index for better performance)
CREATE INDEX idx_assignments_active ON assignments(user_id, sticker_id) 
WHERE removed_at IS NULL;

-- Index for querying by removal status
CREATE INDEX idx_assignments_removed_at ON assignments(removed_at);

-- Index for time-based queries (assigned_at)
CREATE INDEX idx_assignments_assigned_at ON assignments(assigned_at);

-- Unique constraint to prevent duplicate active assignments
-- This ensures a user cannot have the same sticker assigned multiple times concurrently
CREATE UNIQUE INDEX idx_assignments_unique_active 
ON assignments(user_id, sticker_id) 
WHERE removed_at IS NULL;