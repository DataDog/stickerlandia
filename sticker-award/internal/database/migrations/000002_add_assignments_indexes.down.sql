-- Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
-- This product includes software developed at Datadog (https://www.datadoghq.com/).
-- Copyright 2025-Present Datadog, Inc.

-- Remove indexes from assignments table
DROP INDEX IF EXISTS idx_assignments_removed_at;
DROP INDEX IF EXISTS idx_assignments_assigned_at;
DROP INDEX IF EXISTS idx_assignments_user_active;
DROP INDEX IF EXISTS idx_assignments_sticker_id;
DROP INDEX IF EXISTS idx_assignments_user_id;