#!/usr/bin/env sh

# if you ever get the message `': No such file or directory` after building and starting the docker container; this file
# line endings changed from LF to CRLF, the line endings MUST be LF only.

# If there is a folder "Projects" with with a file called "ProjectTemplate.nl3d", overwrite the default
# project template; some cloud services (Azure) cannot mount a single file, only folders. This will enable
# that behaviour if they mount their blob storage onto `/usr/share/nginx/html/StreamingAssets/Projects/`.
if [ -f StreamingAssets/Projects/ProjectTemplate.nl3d ]; then
  cp StreamingAssets/Projects/ProjectTemplate.nl3d StreamingAssets/ProjectTemplate.nl3d
fi

if [ "${NL3D_CONFIGURATION}" != "false" ]; then
  
  # Only set app-config.json if it ain't there; otherwise we overwrite a volume-added one
  if [ ! -f app.config.json ]; then

    echo "No pre-existing configuration file found, creating a configuration file based on environment variables"
    cat app.config.dist.json | /envcat -f j2 'NL3D_*' | jq > app.config.json

  fi
  
  # Summarize the end-result for debugging
  echo "The following configuration is used"
  echo "-----------------------------------"
  cat app.config.json
  echo "-----------------------------------"

else
  # No configuration file should be rendered
  echo "No configuration files are generated upon start; any mounted configuration files are still used"
fi

# Always delete the dist file
rm app.config.dist.json

# Start a CORS Proxy in the background if one is set
if [ "${NL3D_CORS_PROXY_LOCAL}" == "true" ]; then
echo "Proxy URL provided, starting Proxy in the background"
node -r cors-anywhere proxy.js &
fi

# Boot the application
echo "Starting Webserver" 
nginx -g "daemon off;"