# syntax=docker/dockerfile:1

# --- Build stage --------------------------------------------------------------
FROM node:20-alpine AS build
WORKDIR /app

# Install deps from lockfile when available for reproducible builds
COPY package.json package-lock.json* ./
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

# Copy the rest of the source and build static assets
COPY . ./
RUN npm run build

# --- Runtime stage ------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

# Custom nginx config: serve the SPA and reverse-proxy /albums to the API container
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static assets produced by `vite build`
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
