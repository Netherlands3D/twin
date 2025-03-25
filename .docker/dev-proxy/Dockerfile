FROM alpine
RUN apk add brotli nginx nginx-mod-http-brotli

COPY nginx/nginx.conf /etc/nginx/http.d/default.conf
COPY nginx/htpasswd /etc/nginx/.htpasswd

COPY localFiles /usr/share/nginx/html/local_files

WORKDIR /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
