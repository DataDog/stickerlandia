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