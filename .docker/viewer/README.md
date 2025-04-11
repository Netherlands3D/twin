# Netherlands3D Digital Twin Docker Image

This Docker image is designed to facilitate the deployment of the Netherlands3D digital twin application along with an
Nginx web server configured for Brotli compression. The image allows you to easily host and serve the Netherlands3D
digital twin application while offering flexibility through a series of environment variables.

## Key Features

- **Netherlands3D Digital Twin Application:** The image includes the Netherlands3D digital twin application, enabling
  you to deploy a highly detailed and interactive 3D representation of the Netherlands.

- **Nginx Web Server with Brotli Compression:** Nginx is preconfigured to support Brotli compression, which
  significantly reduces the size of transmitted data, leading to faster load times and improved user experience.

- **Customization through Environment Variables:** The Docker image allows you to influence the configuration of the
  Netherlands3D digital twin application using a set of environment variables. You can tailor various aspects of the
  application to meet your specific requirements.

## Environment Variables

### Application Configuration

- `NL3D_ORIGIN_X`: *The starting position's X coordinate when starting the application, in Rijksdriehoekformaat*
- `NL3D_ORIGIN_Y`: *The starting position's Y coordinate when starting the application, in Rijksdriehoekformaat*
- `NL3D_ORIGIN_Z`: *The starting position's elevation of the camera when starting the application, ranging from 0 to 1400*
- `NL3D_ALLOW_USER_SETTINGS`: *true/false, allow viewer user to change settings*
- `NL3D_TERRAIN`: *true/false, is the Terrain feature enabled*
- `NL3D_BUILDINGS`: *true/false, is the Buildings feature enabled*
- `NL3D_TREES`: *true/false, is the Trees feature enabled*
- `NL3D_STREETS`: *true/false, is the Streets feature enabled*
- `NL3D_NEIGHBOURHOODS`: *true/false, is the Neighbourhoods feature enabled*
- `NL3D_DISTRICTS`: *true/false, is the Districts feature enabled*
- `NL3D_BUILDING_INFORMATION`: *true/false, is the Building information feature enabled*
- `NL3D_DOWNLOAD`: *true/false, is the Download area feature enabled*
- `NL3D_SCREENSHOT`: *true/false, is the screenshotting feature enabled*
- `NL3D_OBJECT_IMPORTER`: *true/false, is the importing of .obj files enabled*
- `NL3D_INDICATORS`: *true/false, is it possible to show Indicator for dossiers*
- `NL3D_INDICATORS_DOSSIER_ID`: *The default dossier id to show for the indicators feature, can be empty*
- `NL3D_INDICATORS_API_BASE_URI`: *The API endpoint's base URI used to retrieve the dossier and assets from, example: https://nl3d-backend-provincie-utrecht.azurewebsites.net/api/v1/indicators*
- `NL3D_INDICATORS_API_KEY`: *The API key used to connect to the API endpoints, this is passed as a "code" query parameter*
- `NL3D_CSV_COLORING`: *true/false, is the coloring of buildsings using CSV feature is enabled*
- `NL3D_3DTILES`: *true/false, is 3D Tiles support enabled*
- `NL3D_GOOGLE_REALITYMESH`: *true/false, can the Google 3D Tiles set be loaded, depends on NL3D_3DTILES*
- `NL3D_GOOGLE_REALITYMESH_API_KEY`: *The API key used to connect to the Google 3D Tiles endpoint*
- `NL3D_SUN`: *true/false, is the Sun and Shadows feature enabled*
- `NL3D_TIMELINE`: *true/false, is the Timeline feature enabled*
- `NL3D_TRAFFIC`: *true/false, is the Traffic Simulation feature enabled*

### CORS Proxy settings

By default, Netherlands3D does not make use of a CORS proxy. However, because quite a few GIS applications do not 
provide full support for CORS, it can be necessary to use a CORS proxy to route traffic through.

**Warning**: using a CORS Proxy does not come without risk or possible abuse. Please consult your infrastructure 
specialist and security officer before enabling this. It is recommended to only enable the local CORS Proxy if you 
have a good Web Application Firewall in place and cap the amount of spending that traffic can generate in your setup.

- `NL3D_CORS_PROXY_LOCAL`: *true/false, whether to start a local CORS Proxy on this server*
- `NL3D_CORS_PROXY_URL`: *String defining the URL, including port, of a CORS Proxy that can be used to route requests through, set to `http://<HOST OF WEBSITE>:8080` for the local proxy*
- `NL3D_CORS_PROXY_HOST`: *String defining the HOST/IP on which the local CORS Proxy should listen to. `0.0.0.0` by default*
- `NL3D_CORS_PROXY_PORT`: *String defining the port on which the local CORS Proxy should listen to. `8080` by default*

**Note:** For detailed information on available environment variables and their impact on the application's behavior,
refer to the subsequent section in the documentation.

## Getting Started

To get started with this Docker image, follow these steps:

1. **Docker Installation:** If you haven't already, install Docker on your system.

2. **Pull the Docker Image:** Run the following command to pull the Netherlands3D Digital Twin Docker image:

   ```bash
   docker pull ghcr.io/netherlands3d/twin
   ```

3. **Environment Variable Configuration**: Customize the behavior of the Netherlands3D digital twin application by
   setting the appropriate environment variables. Refer to the next section for a comprehensive list of available
   variables and their descriptions.

4. **Run the Docker Container**: Start the Docker container using the pulled image, making sure to expose the necessary
   ports and bind volumes as required.

   ```bash
   docker run --rm -d -p 80:80 ghcr.io/netherlands3d/twin:latest
   ```

5. **Access the Application**: Open a web browser and navigate to http://localhost or the appropriate server address to
   access the Netherlands3D digital twin application.

## Environment Variable Configuration

The Netherlands3D Digital Twin Docker image provides a range of environment variables that allow you to customize
various aspects of the application. To configure the application to meet your specific needs, please refer to the
following section detailing each environment variable and its influence on the application.

### NL3D_ORIGIN_X

*The starting position's X coordinate when starting the application, in Rijksdriehoekformaat*

### NL3D_ORIGIN_Y

*The starting position's Y coordinate when starting the application, in Rijksdriehoekformaat*

### NL3D_ORIGIN_Z

*The starting position's elevation of the camera when starting the application, ranging from 0 to 1400*

### NL3D_ALLOW_USER_SETTINGS

*true/false, allow viewer user to change settings*

### NL3D_TERRAIN

*true/false, is the Terrain feature enabled*

### NL3D_BUILDINGS

*true/false, is the Buildings feature enabled*

### NL3D_TREES

*true/false, is the Trees feature enabled*

### NL3D_STREETS

*true/false, is the Streets feature enabled*

### NL3D_NEIGHBOURHOODS

*true/false, is the Neighbourhoods feature enabled*

### NL3D_DISTRICTS

*true/false, is the Districts feature enabled*

### NL3D_BUILDING_INFORMATION

*true/false, is the Building BAG information feature enabled*

### NL3D_SCREENSHOT

*true/false, is the screenshotting feature enabled*

### NL3D_OBJECT_IMPORTER

*true/false, is the importing of .obj files enabled*

### NL3D_INDICATORS

*true/false, is it possible to show Indicator for dossiers*

### NL3D_INDICATORS_DOSSIER_ID

*The default dossier id to show for the indicators feature, can be empty*

### NL3D_CSV_COLORING

*true/false, is the building coloring by csv import feature enabled*

### NL3D_3DTILES

*true/false, is 3D Tiles support enabled*

### NL3D_GOOGLE_REALITYMESH
*true/false, can the Google 3D Tiles set be loaded, depends on NL3D_3DTILES*

### NL3D_GOOGLE_REALITYMESH_API_KEY
*The API key used to connect to the Google 3D Tiles endpoint, can be acquired from https://developers.google.com/maps/documentation/tile/3d-tiles*

### NL3D_SUN

*true/false, is the Sun and Shadows feature enabled*

### NL3D_TIMELINE

*true/false, is the Timeline feature enabled*

### NL3D_TRAFFIC

*true/false, is the Traffic Simulation feature enabled*

## Testing the docker build

When you make changes to the docker setup, the easiest way to test this is to locally
build and run your container.

This can be done, after performing a successful WebGL build of the application in the 
Build folder, by executing the following command:

```bash
docker build -f .docker/viewer/Dockerfile -t nl3d:test .
```

In the example above you may want to replace `nl3d:test` with any image name that you
like, this will stay on your computer.

After successful build, you can try it out using the following command:

```bash
docker run --rm -ti --name nl3d -p 8011:80 nl3d:test
```

### Testing with the local proxy on

When you want to test with the local CORS proxy enabled, you need to start with different settings:

1. Environment variable `NL3D_CORS_PROXY_LOCAL` must be passed with the value `true`
2. Environment variable `NL3D_CORS_PROXY_URL` must be passed with the value `http://localhost:8080` - note the port number, this must be `8080`
3. The local port on which the webserver is running must be equal to the port in the docker container - ie. `80:80`
4. The local port on which the proxy is running must be equal to the port in the docker container - ie. `8080:8080`

Thus, the following would work:

```bash
docker run --rm -ti --name nl3d -p 80:80 -p 8080:8080 -e "NL3D_CORS_PROXY_LOCAL=true" -e "NL3D_CORS_PROXY_URL=http://localhost:8080"  nl3d:test
```

The reason for this is due to the lack of DNS. When retrieving the default `ProjectTemplate.nl3d` file it will query 
the 'localhost'. Because the proxy runs from _within_ the docker container, its `localhost` entry is not the same as
your browser. The CORS Proxy's localhost will point to the docker container itself. This means that if a URL with 
`localhost` is called, it will need the port number as known inside the container -80 by default for the webserver, 8080
for the proxy-. 

In effect, if you have different port numbers exposed to your host machine than the docker container uses internally, it
will try to load the ProjectTemplate from the wrong port; as such: the exposed and internal port must be the same when
testing with the local cors proxy enabled.

This will not be an issue when testing without a cors proxy. When testing with a remote CORS proxy, your test build must
have a DNS address that it reachable from that proxy, thus: on the internet.

## Using a customized Project file as default

The project uses a file called `StreamingAssets/ProjectTemplate.nl3d` as a default project file,
if you want to configure your own; use a bind mount to mount your own file on top of it:

```bash
docker run --rm -ti --name nl3d -p 8011:80 --mount type=bind,source="C:\Users\mike\Projecten\zuidoost.nl3d",target=/usr/share/nginx/html/StreamingAssets/ProjectTemplate.nl3d nl3d:test 
```

Unfortunately some cloud providers, such as Azure, are unable to mount individual files, but instead
they can only mount folders. To circumvent this issue, a special folder is designated ("Projects"); if that contains a 
file called `ProjectTemplate.nl3d`, the startup script will copy that on top of the default ProjectTemplate.

As an example, this will work as well:

```bash
docker run --rm -ti --name nl3d -p 8011:80 --mount type=bind,source="C:\Users\mike\Projecten\zuidoost.nl3d",target=/usr/share/nginx/html/StreamingAssets/Projects/ProjectTemplate.nl3d nl3d:test 
```
