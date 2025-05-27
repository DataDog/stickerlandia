-- Initial schema for stickerlandia sticker-award service

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

-- Sticker assignments table
CREATE TABLE sticker_assignments (
    assignment_id BIGSERIAL PRIMARY KEY,
    user_id VARCHAR(50) NOT NULL,
    sticker_id VARCHAR(50) NOT NULL,
    assigned_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    removed_at TIMESTAMP,
    reason VARCHAR(500),
    CONSTRAINT fk_sticker FOREIGN KEY (sticker_id) REFERENCES stickers(sticker_id),
    CONSTRAINT uq_user_sticker UNIQUE (user_id, sticker_id, removed_at)
);

-- Indexes
CREATE INDEX idx_sticker_assignments_user_id ON sticker_assignments(user_id);
CREATE INDEX idx_sticker_assignments_active ON sticker_assignments(user_id, removed_at) 
WHERE removed_at IS NULL;

-- Add comment to explain the quantity field
COMMENT ON COLUMN stickers.sticker_quantity_remaining IS 'Quantity remaining (-1 for infinite)';

-- Initial seed data
INSERT INTO stickers (sticker_id, name, description, image_url, sticker_quantity_remaining, created_at)
VALUES 
('sticker-001', 'Debugging Hero', 'Awarded for exceptional debugging skills', 'https://stickerlandia.example.com/images/debugging-hero.png', 100, CURRENT_TIMESTAMP),
('sticker-002', 'Code Review Champion', 'Awarded for thorough code reviews', 'https://stickerlandia.example.com/images/code-review-champion.png', 100, CURRENT_TIMESTAMP),
('sticker-003', 'Performance Optimizer', 'Awarded for significant performance improvements', 'https://stickerlandia.example.com/images/performance-optimizer.png', -1, CURRENT_TIMESTAMP),
('sticker-004', 'Early Bird', 'Awarded for being first to complete tasks', 'https://stickerlandia.example.com/images/early-bird.png', 50, CURRENT_TIMESTAMP),
('sticker-005', 'Team Player', 'Awarded for excellent collaboration', 'https://stickerlandia.example.com/images/team-player.png', -1, CURRENT_TIMESTAMP),
('sticker-006', 'Innovation Award', 'Awarded for creative solutions', 'https://stickerlandia.example.com/images/innovation-award.png', 25, CURRENT_TIMESTAMP);

-- Sample assignment
INSERT INTO sticker_assignments (user_id, sticker_id, assigned_at, reason)
VALUES ('user-001', 'sticker-001', CURRENT_TIMESTAMP, 'Found and fixed a critical production issue');