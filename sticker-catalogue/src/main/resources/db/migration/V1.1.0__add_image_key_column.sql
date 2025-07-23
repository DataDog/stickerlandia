-- Add image_key column to stickers table for S3 object storage
ALTER TABLE stickers ADD COLUMN image_key VARCHAR(255);