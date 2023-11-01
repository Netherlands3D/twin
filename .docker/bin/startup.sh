#!/usr/bin/env sh

if [ "${NL3D_CONFIGURATION}" != "false" ]; then
  
  # Only set app-config.json if it ain't there; otherwise we overwrite a volume-added one
  if [ ! -f app.config.json ]; then

    echo "No pre-existing configuration file found, creating a configuration file based on environment variables"
    cat app.config.dist.json | /envcat -f j2 'NL3D_*' | jq "" > app.config.json

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

# Boot the application
echo "Starting Webserver"
nginx -g "daemon off;"