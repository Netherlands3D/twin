FROM alpine
ARG SOURCE_PATH=Build
RUN apk add brotli nginx nginx-mod-http-brotli

# We assume the context/path to be the project root and not this folder 
COPY .docker/nginx/nginx.conf /etc/nginx/http.d/default.conf
ADD $SOURCE_PATH /usr/share/nginx/html

CMD ["nginx", "-g", "daemon off;"]
EXPOSE 80