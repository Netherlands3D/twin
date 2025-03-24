# Dev Proxy Server

This NGINX proxy server provides access to various WMS and WFS services using different authentication methods. Below is
an overview of the available endpoints and the required credentials for each.

To start the server from the root of the project, use the command:

```bash
docker-compose up --build dev-proxy
```

This will run a local server on port 8100, so all endpoints listed below start with `http://localhost:8100/`.

**Note:** The trailing `/` in the path is mandatory; it will not work without it.

## Endpoints

### WMS (Web Map Service)

1. **Basic Authentication**
    - **Endpoint**: `/wms/basic/`
    - **Description**: Access the WMS service using Basic Authentication.
    - **Username**: `username`
    - **Password**: `password`

2. **Query Parameter Authentication (code)**
    - **Endpoint**: `/wms/code/`
    - **Description**: Access the WMS service using a query parameter `code`.
    - **Query Parameter**: `code=my_secret_code`

3. **Query Parameter Authentication (key)**
    - **Endpoint**: `/wms/key/`
    - **Description**: Access the WMS service using a query parameter `key`.
    - **Query Parameter**: `key=my_secret_key`

4. **Query Parameter Authentication (token)**
    - **Endpoint**: `/wms/token/`
    - **Description**: Access the WMS service using a query parameter `token`.
    - **Query Parameter**: `token=my_secret_token`

### WFS (Web Feature Service)

1. **Basic Authentication**
    - **Endpoint**: `/wfs/basic/`
    - **Description**: Access the WFS service using Basic Authentication.
    - **Username**: `username`
    - **Password**: `password`

2. **Query Parameter Authentication (code)**
    - **Endpoint**: `/wfs/code/`
    - **Description**: Access the WFS service using a query parameter `code`.
    - **Query Parameter**: `code=my_secret_code`

3. **Query Parameter Authentication (key)**
    - **Endpoint**: `/wfs/key/`
    - **Description**: Access the WFS service using a query parameter `key`.
    - **Query Parameter**: `key=my_secret_key`

4. **Query Parameter Authentication (token)**
    - **Endpoint**: `/wfs/token/`
    - **Description**: Access the WFS service using a query parameter `token`.
    - **Query Parameter**: `token=my_secret_token`

### Local Files

1. **Basic Authentication**
    - **Endpoint**: `/files/`
    - **Description**: Access local files using Basic Authentication.
    - **Username**: `username`
    - **Password**: `password`

## Usage

- **Basic Authentication**: When using an endpoint with Basic Authentication, you will be prompted to enter a username
  and password. Use the provided credentials to gain access.
- **Query Parameter Authentication**: Add the appropriate query parameter to the URL to access the service. For example:
  `/wms/code/?code=my_secret_code`.

## Examples

- **WMS with Basic Authentication**:
  ```
  http://localhost:8100/wms/basic/
  ```

- **WFS with Query Parameter Authentication (token)**:
  ```
  http://localhost:8100/wfs/token/?token=my_secret_token
  ```
