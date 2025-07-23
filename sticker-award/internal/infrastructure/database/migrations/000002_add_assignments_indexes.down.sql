-- Remove indexes from assignments table
DROP INDEX IF EXISTS idx_assignments_removed_at;
DROP INDEX IF EXISTS idx_assignments_assigned_at;
DROP INDEX IF EXISTS idx_assignments_user_active;
DROP INDEX IF EXISTS idx_assignments_sticker_id;
DROP INDEX IF EXISTS idx_assignments_user_id;