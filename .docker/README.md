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

- `NL3D_ORIGIN_X`: *The starting position's X coordinate when starting the application, in Rijksdriehoekformaat*
- `NL3D_ORIGIN_Y`: *The starting position's Y coordinate when starting the application, in Rijksdriehoekformaat*
- `NL3D_ORIGIN_Z`: *The starting position's elevation of the camera when starting the application, ranging from 0 to 1400*
- `NL3D_TERRAIN`: *true/false, is the Terrain feature enabled*
- `NL3D_BUILDINGS`: *true/false, is the Buildings feature enabled*
- `NL3D_STREETS`: *true/false, is the Streets feature enabled*
- `NL3D_TREES`: *true/false, is the Trees feature enabled*
- `NL3D_SUN`: *true/false, is the Sun and Shadows feature enabled*
- `NL3D_TIMELINE`: *true/false, is the Timeline feature enabled*
- `NL3D_TRAFFIC`: *true/false, is the Traffic Simulation feature enabled*
- `NL3D_SCREENSHOT`: *true/false, is the screenshotting feature enabled*
- `NL3D_OBJECT_IMPORTER`: *true/false, is the importing of .obj files enabled*
- `NL3D_INDICATORS`: *true/false, is it possible to show Indicator for dossiers*
- `NL3D_INDICATORS_DOSSIER_ID`: *The default dossier id to show for the indicators feature, can be empty*
- `NL3D_INDICATORS_API_BASE_URI`: *The API endpoint's base URI used to retrieve the dossier and assets from, example: https://nl3d-backend-provincie-utrecht.azurewebsites.net/api/v1/indicators*
- `NL3D_INDICATORS_API_KEY`: *The API key used to connect to the API endpoints, this is passed as a "code" query parameter*

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

### NL3D_TERRAIN

*true/false, is the Terrain feature enabled*

### NL3D_BUILDINGS

*true/false, is the Buildings feature enabled*

### NL3D_STREETS

*true/false, is the Streets feature enabled*

### NL3D_TREES`

*true/false, is the Trees feature enabled*

### NL3D_SUN`

*true/false, is the Sun and Shadows feature enabled*

### NL3D_TIMELINE`

*true/false, is the Timeline feature enabled*

### NL3D_TRAFFIC`

*true/false, is the Traffic Simulation feature enabled*

### NL3D_SCREENSHOT`

*true/false, is the screenshotting feature enabled*

### NL3D_OBJECT_IMPORTER`

*true/false, is the importing of .obj files enabled*

### NL3D_INDICATORS`

*true/false, is it possible to show Indicator for dossiers*

### NL3D_INDICATORS_DOSSIER_ID`

*The default dossier id to show for the indicators feature, can be empty*
