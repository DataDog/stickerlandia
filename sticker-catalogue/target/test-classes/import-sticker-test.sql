-- Additional test data for sticker catalog API testing

-- Insert more stickers for testing pagination
INSERT INTO stickers (sticker_id, name, description, image_url, sticker_quantity_remaining, created_at)
VALUES 
('sticker-007', 'Bug Hunter', 'Recognizes skill in identifying and resolving critical bugs', 'https://stickerlandia.example.com/images/bug-hunter.png', 75, CURRENT_TIMESTAMP),
('sticker-008', 'Mentor', 'Celebrates dedication to helping and guiding team members', 'https://stickerlandia.example.com/images/mentor.png', -1, CURRENT_TIMESTAMP),
('sticker-009', 'Documentation Expert', 'Honors excellence in creating clear and comprehensive documentation', 'https://stickerlandia.example.com/images/documentation-expert.png', 30, CURRENT_TIMESTAMP),
('sticker-010', 'Security Champion', 'Acknowledges contributions to system security and best practices', 'https://stickerlandia.example.com/images/security-champion.png', 40, CURRENT_TIMESTAMP);
