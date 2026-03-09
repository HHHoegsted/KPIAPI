# ---- build stage ----
FROM node:20-alpine AS build
WORKDIR /app

# Install deps first (better caching)
COPY package.json package-lock.json ./
RUN npm ci

# Copy source and build
COPY . .
RUN npm run build

# ---- runtime stage ----
FROM nginx:alpine AS runtime

# SPA + client-side routing + /api proxy config will be added in the next file
# (we'll copy it to /etc/nginx/conf.d/default.conf)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
