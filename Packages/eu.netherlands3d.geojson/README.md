# GeoJSON

The GeoJSON package for Netherlands3D exposes the GeoJSON.net package and an additional suite of features
to work with it.


## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.geojson
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`
- `com.virgis`

## Usage

Before we start, in your project's `Assets` folder you need a file called `link.xml` -if it doesn't exist- with the
following content:

```xml
<linker>
	<assembly fullname="GeoJSON.NET" preserve="all"></assembly>
</linker>
```

This is needed because GeoJSON.net provides additional JSON.net converters, which use reflection to be detected. Without
this exception, the linker of Unity will filter these converters away because it thinks it is dead code.

### Deserializing from GeoJSON

Since GeoJSON.net is a wrapper around JSON.net, deserializing a GeoJSON json file is a matter of the following:

```csharp
    var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(jsonString);
```

For more information on the usage of GeoJSON.net, see their repository: https://github.com/ViRGIS-Team/GeoJSON.Net

### Getting a Feature's identifier

The GeoJSON specification mentions that when a Feature has an identifier, you should add a field `id`. In practice, 
some providers do not do this but instead add a property `id`, `ID`, `objectid` or `OBJECTID`.

To make it easier for consumers to get the identifier of a feature, an extension method `TryGetIdentifier()` is 
introduced that checks each potential source of identifiers in order. A parameter can be provided as a default, for when
no identifying property was present.

Example:

```csharp
    string id = feature.TryGetIdentifier("Feature" + featureIndex);
```

### Getting the EPSG identifier

The Coordinate System for GeoJSON is, by default, the EPSG:4326 variant of WSG-84. But a GeoJSON extension allows
providers to add their own CRS specifiers. 

if a feature has a Named CRS specified using an EPSG urn, you can use the method `EPSGId()` to attempt to get the EPSG
numeric code that is specified. If none is specified, `4326` is returned.

Example:

```csharp
    var epsgId = featureCollection.EPSGId();
```

### Getting the Bounding Box when absent

The `bbox` property in GeoJSON is optional, but sometimes you still need the bounding box of all features combined
in a FeatureCollection.

To do this, an extension method is introduced called `DerivedBoundingBoxes()`, that will calculate a total bounding box
based on the features' extents.

Example:

```csharp
    double[] boundingBox = featureCollection.BoundingBoxes ?? featureCollection.DerivedBoundingBoxes();
```

### Moving a GameObject to the center of a FeatureCollection

Sometimes you need to move an object, such as a Camera or DecalProjector, to the location of a feature collection. The
`MoveToCenterOfFeatureCollection` MonoBehaviour is can do that with a given gameobject, or the current when none
is specified.