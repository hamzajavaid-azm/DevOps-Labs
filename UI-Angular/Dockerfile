# syntax=docker/dockerfile:1
# Multi-stage build for the Todo Angular UI.
# Build context is the UI-Angular/ folder (this directory).

# ---- Stage 1: build the Angular app ----
FROM node:22-alpine AS build
WORKDIR /app

# Copy manifests first and install deps — cached unless package*.json changes.
COPY package.json package-lock.json ./
RUN npm ci

# Copy the rest of the source and produce the production build.
COPY . .
RUN npm run build

# ---- Stage 2: serve the static files with nginx ----
FROM nginx:alpine AS final

# Angular's application builder outputs to dist/<project>/browser.
COPY --from=build /app/dist/Lab-01-UI/browser /usr/share/nginx/html

# SPA routing: fall back to index.html for client-side routes.
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
# nginx:alpine already runs CMD ["nginx", "-g", "daemon off;"]
