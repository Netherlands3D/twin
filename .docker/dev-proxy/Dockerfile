FROM nginx:stable-alpine
RUN apk add brotli nginx-mod-http-brotli

COPY nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY nginx/htpasswd /etc/nginx/.htpasswd

COPY localFiles /usr/share/nginx/html

WORKDIR /usr/share/nginx/html
