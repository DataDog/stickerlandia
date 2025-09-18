-- Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
-- This product includes software developed at Datadog (https://www.datadoghq.com/).
-- Copyright 2025-Present Datadog, Inc.

-- Seed test data for development and testing
-- This migration adds sample assignment data for testing purposes

-- Insert sample assignments (matching test data from Java service)
INSERT INTO assignments (user_id, sticker_id, assigned_at, reason, created_at, updated_at)
VALUES 
    -- Active assignment for user-001
    (
        'user-001', 
        'sticker-001', 
        '2024-01-01 10:00:00+00:00',
        'For exceptional debugging skills during the Q4 release',
        '2024-01-01 10:00:00+00:00',
        '2024-01-01 10:00:00+00:00'
    ),
    
    -- Another active assignment for user-001
    (
        'user-001', 
        'sticker-002', 
        '2024-01-15 14:30:00+00:00',
        'Outstanding code review contributions',
        '2024-01-15 14:30:00+00:00',
        '2024-01-15 14:30:00+00:00'
    ),
    
    -- Assignment for user-002
    (
        'user-002', 
        'sticker-001', 
        '2024-01-10 09:15:00+00:00',
        'Quick resolution of critical production issue',
        '2024-01-10 09:15:00+00:00',
        '2024-01-10 09:15:00+00:00'
    ),
    
    -- Removed assignment example (soft deleted)
    (
        'user-002', 
        'sticker-003', 
        '2024-01-05 16:45:00+00:00',
        'Performance optimization work',
        '2024-01-05 16:45:00+00:00',
        '2024-01-20 11:30:00+00:00'
    );

-- Update the removed assignment to have a removed_at timestamp
UPDATE assignments 
SET removed_at = '2024-01-20 11:30:00+00:00',
    updated_at = '2024-01-20 11:30:00+00:00'
WHERE user_id = 'user-002' AND sticker_id = 'sticker-003';