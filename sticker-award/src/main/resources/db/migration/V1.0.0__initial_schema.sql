-- Initial schema for stickerlandia sticker-award service

-- Stickers table
CREATE TABLE stickers (
    sticker_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    image_url VARCHAR(255),
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

-- Initial seed data
INSERT INTO stickers (sticker_id, name, description, image_url, created_at)
VALUES 
('sticker-001', 'Debugging Hero', 'Awarded for exceptional debugging skills', 'https://stickerlandia.example.com/images/debugging-hero.png', CURRENT_TIMESTAMP),
('sticker-002', 'Code Review Champion', 'Awarded for thorough code reviews', 'https://stickerlandia.example.com/images/code-review-champion.png', CURRENT_TIMESTAMP),
('sticker-003', 'Performance Optimizer', 'Awarded for significant performance improvements', 'https://stickerlandia.example.com/images/performance-optimizer.png', CURRENT_TIMESTAMP);

-- Sample assignment
INSERT INTO sticker_assignments (user_id, sticker_id, assigned_at, reason)
VALUES ('user-001', 'sticker-001', CURRENT_TIMESTAMP, 'Found and fixed a critical production issue');