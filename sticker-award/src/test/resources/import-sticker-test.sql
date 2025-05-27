-- Additional test data for sticker catalog API testing

-- Insert more stickers for testing pagination
INSERT INTO stickers (sticker_id, name, description, image_url, sticker_quantity_remaining, created_at)
VALUES 
('sticker-007', 'Bug Hunter', 'Awarded for finding critical bugs', 'https://stickerlandia.example.com/images/bug-hunter.png', 75, CURRENT_TIMESTAMP),
('sticker-008', 'Mentor', 'Awarded for helping other team members', 'https://stickerlandia.example.com/images/mentor.png', -1, CURRENT_TIMESTAMP),
('sticker-009', 'Documentation Expert', 'Awarded for excellent documentation', 'https://stickerlandia.example.com/images/documentation-expert.png', 30, CURRENT_TIMESTAMP),
('sticker-010', 'Security Champion', 'Awarded for security improvements', 'https://stickerlandia.example.com/images/security-champion.png', 40, CURRENT_TIMESTAMP);

-- Add more test assignments
INSERT INTO sticker_assignments (user_id, sticker_id, assigned_at, reason)
VALUES 
('user-002', 'sticker-002', CURRENT_TIMESTAMP, 'Excellent code review feedback'),
('user-003', 'sticker-003', CURRENT_TIMESTAMP, 'Optimized database queries'),
('user-002', 'sticker-004', CURRENT_TIMESTAMP, 'First to complete sprint tasks'),
('user-004', 'sticker-005', CURRENT_TIMESTAMP, 'Great collaboration on cross-team project'),
('user-003', 'sticker-007', CURRENT_TIMESTAMP, 'Found and fixed memory leak');