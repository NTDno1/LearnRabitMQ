# Build Stage
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build -- --configuration production

# Run Stage
FROM nginx:alpine
COPY --from=build /app/dist/signalr-angular-frontend/browser /usr/share/nginx/html
# Copy custom nginx config if needed, but default is fine for simple SPA usually.
# However, for Angular routing, we often need a custom config to redirect 404 to index.html.
# I'll add a simple config command to overwrite default.conf or just rely on default for now.
# Better to add a simple config for SPA.
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
