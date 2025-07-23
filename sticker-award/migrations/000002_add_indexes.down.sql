-- Drop indexes in reverse order

DROP INDEX IF EXISTS idx_assignments_unique_active;
DROP INDEX IF EXISTS idx_assignments_assigned_at;
DROP INDEX IF EXISTS idx_assignments_removed_at;
DROP INDEX IF EXISTS idx_assignments_active;
DROP INDEX IF EXISTS idx_assignments_user_sticker;
DROP INDEX IF EXISTS idx_assignments_user_id;