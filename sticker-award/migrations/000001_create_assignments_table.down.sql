-- Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
-- This product includes software developed at Datadog (https://www.datadoghq.com/).
-- Copyright 2025-Present Datadog, Inc.

-- Drop indexes in reverse order
DROP INDEX IF EXISTS idx_assignments_unique_active;
DROP INDEX IF EXISTS idx_assignments_assigned_at;
DROP INDEX IF EXISTS idx_assignments_removed_at;
DROP INDEX IF EXISTS idx_assignments_active;
DROP INDEX IF EXISTS idx_assignments_user_sticker;
DROP INDEX IF EXISTS idx_assignments_user_id;

-- Drop assignments table
DROP TABLE IF EXISTS assignments;