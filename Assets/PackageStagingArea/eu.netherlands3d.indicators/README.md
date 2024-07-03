# Quality indicators for Area Development

This package enables users to display quality indicator scores for one or more project area's within a bound, and
to provide explainability for these indicators through the use of map overlays and references to documentation
explaining the indicator's source.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.indicators
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d` and
- `com.virgis.geojson.net`

### WebGL

When you want to use this package in a build intended for WebGL, do not forgot to add the following to `Assets/link.xml`
in your project. This package relies on (de)serialisation using JSON.net and when Code Stripping is enabled, the linker
will otherwise remove all JsonConverters and elements involved in the (de)serialisation of the code.

```xml
<linker>
    <!-- Anything that is already in your link.xml -->
	<assembly fullname="GeoJSON.NET" preserve="all"></assembly>
	<assembly fullname="eu.netherlands3d.json.Runtime" preserve="all"></assembly>
</linker>
```

If you do not have a `link.xml` file in the root of your `Assets` folder, you can create one with the contents above.

## Usage

To make use of this package, you can follow these steps:

1. Drag the Indicators prefab into your scene
2. Connect your camera to the `Center On Feature Collection`'s Target Game Object

After that you can start the `Open` coroutine on the Dossier Scriptable Object attached to the `Dossier Listener` with 
the dossier id of your Indicator provider.

Once the Dossier is loaded, the GeoJSON FeatureCollection describing the dossier's project area boundaries can be 
fetched using the Dossier Scriptable Object's `LoadProjectAreaGeometry` coroutine. This coroutine will want a variant of 
the Dossier for which to retrieve the geometry.

## Configuring a Provider

This package depends on a web service that exposes dossiers. In the Dossier scriptable object you can define a template
URI with a field `{id}` where to fetch dossiers from. 

## Exposing Dossiers through a Provider

In order to expose your own indicator dossiers, you need an HTTP endpoint that can expose a JSON document with a 
set structure.

TODO: Define a JSON-LD schema and reference it here, possibly include an example