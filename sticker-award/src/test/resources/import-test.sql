-- Test data for stickerlandia sticker-award tests

-- Test stickers
INSERT INTO stickers (sticker_id, name, description, image_url, sticker_quantity_remaining, created_at)
VALUES 
('sticker-001', 'Debugging Hero', 'Awarded for exceptional debugging skills', 'https://stickerlandia.example.com/images/debugging-hero.png', 100, CURRENT_TIMESTAMP),
('sticker-002', 'Code Review Champion', 'Awarded for thorough code reviews', 'https://stickerlandia.example.com/images/code-review-champion.png', 100, CURRENT_TIMESTAMP),
('sticker-003', 'Performance Optimizer', 'Awarded for significant performance improvements', 'https://stickerlandia.example.com/images/performance-optimizer.png', -1, CURRENT_TIMESTAMP);

-- Sample assignment for test user
INSERT INTO sticker_assignments (user_id, sticker_id, assigned_at, reason)
VALUES ('user-001', 'sticker-001', CURRENT_TIMESTAMP, 'For test purposes');