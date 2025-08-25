-- Initial schema for stickerlandia sticker-catalogue service
-- Creates stickers table with all current fields and performance indexes

-- Create stickers table
CREATE TABLE stickers (
    sticker_id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    image_key VARCHAR(255),
    sticker_quantity_remaining INTEGER NOT NULL DEFAULT -1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- Add performance indexes
CREATE INDEX idx_stickers_created_at ON stickers(created_at DESC);
CREATE INDEX idx_stickers_name ON stickers(name);

-- Add comment to explain the quantity field
COMMENT ON COLUMN stickers.sticker_quantity_remaining IS 'Quantity remaining (-1 for infinite)';

-- Sample sticker data is now seeded via StickerDataSeeder startup service
-- This approach provides better flexibility and traceability