// API configuration
// In production (S3/CloudFront): use relative URLs since CloudFront routes /api/* to API Gateway
// In local development: set VITE_API_BASE_URL=http://localhost:8080 in .env.local
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '';
