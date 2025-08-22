-- Add indexes for optimal query performance on assignments table

-- Index for finding assignments by user_id (most common query pattern)
CREATE INDEX idx_assignments_user_id ON assignments(user_id);

-- Index for finding assignments by sticker_id
CREATE INDEX idx_assignments_sticker_id ON assignments(sticker_id);

-- Composite index for user + active assignments (removed_at IS NULL)
CREATE INDEX idx_assignments_user_active ON assignments(user_id, removed_at) WHERE removed_at IS NULL;

-- Index for time-based queries on assigned_at
CREATE INDEX idx_assignments_assigned_at ON assignments(assigned_at);

-- Index for soft deletion queries on removed_at
CREATE INDEX idx_assignments_removed_at ON assignments(removed_at) WHERE removed_at IS NOT NULL;