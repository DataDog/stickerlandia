-- Copyright 2025 Datadog, Inc.
-- SPDX-License-Identifier: Apache-2.0

-- Remove seed test data
DELETE FROM assignments WHERE user_id IN ('user-001', 'user-002');