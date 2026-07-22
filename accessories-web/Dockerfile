# Stage 1: Build the Angular application
FROM node:alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build --configuration=production

# Stage 2: Serve with Nginx
FROM nginx:alpine

RUN rm -rf /usr/share/nginx/html/*

# Note: Modern Angular outputs the client files into the /browser subfolder
COPY --from=build /app/dist/accessories-web/browser /usr/share/nginx/html

EXPOSE 80