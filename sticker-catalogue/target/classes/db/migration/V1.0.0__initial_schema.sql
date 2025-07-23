-- Initial schema for stickerlandia sticker-catalogue service

-- Stickers table
CREATE TABLE stickers (
    sticker_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    image_url VARCHAR(255),
    sticker_quantity_remaining INTEGER NOT NULL DEFAULT -1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Add comment to explain the quantity field
COMMENT ON COLUMN stickers.sticker_quantity_remaining IS 'Quantity remaining (-1 for infinite)';

-- Initial seed data
INSERT INTO stickers (sticker_id, name, description, image_url, sticker_quantity_remaining, created_at)
VALUES 
('sticker-001', 'Debugging Hero', 'Recognizes exceptional debugging skills and problem-solving abilities', 'https://stickerlandia.example.com/images/debugging-hero.png', 100, CURRENT_TIMESTAMP),
('sticker-002', 'Code Review Champion', 'Celebrates thorough and constructive code review practices', 'https://stickerlandia.example.com/images/code-review-champion.png', 100, CURRENT_TIMESTAMP),
('sticker-003', 'Performance Optimizer', 'Honors achievements in system performance improvements', 'https://stickerlandia.example.com/images/performance-optimizer.png', -1, CURRENT_TIMESTAMP),
('sticker-004', 'Early Bird', 'Acknowledges promptness and proactive task completion', 'https://stickerlandia.example.com/images/early-bird.png', 50, CURRENT_TIMESTAMP),
('sticker-005', 'Team Player', 'Highlights excellent collaboration and teamwork skills', 'https://stickerlandia.example.com/images/team-player.png', -1, CURRENT_TIMESTAMP),
('sticker-006', 'Innovation Award', 'Recognizes creative and innovative solution development', 'https://stickerlandia.example.com/images/innovation-award.png', 25, CURRENT_TIMESTAMP);
