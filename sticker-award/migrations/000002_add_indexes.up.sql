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