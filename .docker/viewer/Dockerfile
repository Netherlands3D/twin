FROM alpine
ARG SOURCE_PATH=Build
RUN apk add jq brotli nginx nginx-mod-http-brotli npm

# Install envcat (linux-x86_64): https://github.com/busyloop/envcat
ARG ENVCAT_VERSION=1.1.0
ARG ENVCAT_SHA256=1113adbd687393b0353fe93f77b8313d9842e1850875d8dea4fd461de5d53b94
ADD https://github.com/busyloop/envcat/releases/download/v${ENVCAT_VERSION}/envcat-${ENVCAT_VERSION}.linux-x86_64 /envcat
RUN chmod +x /envcat

# We assume the context/path to be the project root and not this folder 
COPY .docker/viewer/nginx/nginx.conf /etc/nginx/http.d/default.conf
ADD $SOURCE_PATH /usr/share/nginx/html
COPY .docker/viewer/app.config.dist.json /usr/share/nginx/html

# Inject startup script
COPY .docker/viewer/bin/startup.sh /startup.sh
RUN chmod +x /startup.sh

WORKDIR /usr/share/nginx/html

# needed to run the local cors proxy in proxy.js
RUN npm install cors-anywhere
COPY .docker/viewer/proxy.js /usr/share/nginx/html/

CMD ["/startup.sh"]
EXPOSE 80
EXPOSE 8080