-- Unless explicitly stated otherwise all files in this repository are licensed under the Apache License Version 2.0.
-- This product includes software developed at Datadog (https://www.datadoghq.com/).
-- Copyright 2025-Present Datadog, Inc.

-- Test data for stickerlandia sticker-catalogue tests

-- Test stickers
INSERT INTO stickers (sticker_id, name, description, image_key, sticker_quantity_remaining, created_at)
VALUES 
('sticker-001', 'Debugging Hero', 'Recognizes exceptional debugging skills and problem-solving abilities', 'debugging-hero.png', 100, CURRENT_TIMESTAMP),
('sticker-002', 'Code Review Champion', 'Celebrates thorough and constructive code review practices', 'code-review-champion.png', 100, CURRENT_TIMESTAMP),
('sticker-003', 'Performance Optimizer', 'Honors achievements in system performance improvements', 'performance-optimizer.png', -1, CURRENT_TIMESTAMP);
