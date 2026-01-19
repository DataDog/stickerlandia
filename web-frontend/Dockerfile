FROM node:20-alpine as build

WORKDIR /app

# Copy package files
COPY package.json package-lock.json ./

# Install dependencies
RUN npm install 

# Copy source code
COPY . .

# Build the app
RUN npm run build

# Production stage
FROM nginx:alpine

ARG DD_GIT_REPOSITORY_URL
ARG DD_GIT_COMMIT_SHA
ENV DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL}
ENV DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA}

# Copy nginx config for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built app to nginx (includes vite-envs.sh script)
COPY --from=build /app/dist /usr/share/nginx/html

# Make the vite-envs script executable
RUN chmod +x /usr/share/nginx/html/vite-envs.sh

EXPOSE 80

# Change to the directory containing the built files and run vite-envs script
WORKDIR /usr/share/nginx/html
ENTRYPOINT sh -c "./vite-envs.sh && nginx -g 'daemon off;'"
