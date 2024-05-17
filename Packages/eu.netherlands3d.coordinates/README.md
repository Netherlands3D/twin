Coordinates
===========

Using this package it is possible to 

1. Convert between the following coordinate systems:
   - [World Geodetic System 1984 Lattitude, Longitude, EllipsoidalHeight (EPSG:3857, WGS84_LatLonH)](https://epsg.io/3857)
   - [World Geodetic System 1984 Lattitude, Longitude (EPSG:4326, WGS84_LatLon)](https://epsg.io/4326)
   - [World Geodetic System 1984 EartCentered-EarthFixed (EPSG:4978, WGS84_ECEF)](https://epsg.io/4978)
   - [European Terrestrial Reference System 1989 Lattitude, Longitude, EllipsoidalHeight (EPSG:4937, ERTS89_LatLonH)](https://epsg.io/4937)
   - [European Terrestrial Reference System 1989 Lattitude, Longitude (EPSG:4258, ERTS89_LatLon)](https://epsg.io/4258)
   - European Terrestrial Reference System 1989 EartCentered-EarthFixed (EPSG:4936, ERTS89_ECEF)](https://epsg.io/4936)
   - [Rijksdriehoekscoördinaten](https://nl.wikipedia.org/wiki/Rijksdriehoeksco%C3%B6rdinaten) (https://en.wikipedia.org/wiki/Amsterdam_Ordnance_Datum) (RD / [EPSG:28992](https://epsg.io/28992))
   - [Rijksdriehoekscoördinaten](https://nl.wikipedia.org/wiki/Rijksdriehoeksco%C3%B6rdinaten) + [NAP height](https://en.wikipedia.org/wiki/Amsterdam_Ordnance_Datum) (RD / [EPSG:7415](https://epsg.io/7415))
   - WGS 84 / Pseudo-Mercator (EPSG:3857, WGS84_PseudoMercator)](https://epsg.io/3857)


2. convert between each of these coordinateSystems and Unity Vector3

# Usage

## Using coordinates

### Creating a Coordinate from known values

Example, describing longitude 10.02, latitude 20.01 in EPSG:4326, Coordinate Reference System.

```
$coordinate = new Coordinate(CoordinateSystem.WGS84LatLon, 20.01, 10.02);
```
### Testing the validity of a coordinate

you can test is a coordinate is valid using the function IsValid():

```
$bool isValid = CoordinateToTest.isValid();
```
returns true if:
	- the number of axis is correct AND
	- the AxisValues fall within the bounds of the valid area for the coordinatesystem

### Converting to another CoordinateSystem

$rdCoordinate = originalCoordinate.Convert(CoordinateSystem.RDNAP);

### find a CoordinateSystem by name

$bool CanHandleTheCoordainteSystem = CoordinateSytems.FindCoordinateSystem("coordinatesystemName", out NewCoordinateSystem)

each coordinateSystem has a unique name (in most case the epsg-code). the first found coordinateSystem who's name ins contained in the searchterm will be returned.
If a coordinateSystem is found, the function returns "True"
If a coordinateSystem is not found, the function return "False" and the coordainteSystem is set to CoordinateSystem.Undefined

## Connecting Coordinates to Unity

### assigning a CoordinateSystem to Unity

$CoordinateSystems.connectedCoordinateSystem = CoordinateSystem.RDNAP;

### assigning a location to the Unity Origin

$CoordinateSystems.SetOrigin(Coordinate that has to be at the Unity-Origin)

this coordinate does not have to be in the coordinateSystem that is assigned to Unity.

### Creating a coordinate from an Unity Vector3

$newCoordinate = new Coordinate(UnityVector3)

### Converting to a Vector3

$Vector3 = CoordinateToConvert.ToUnity()

### Rotating geometry
When a geocentric coordinateSystem is attached to Unity, the geometry defined in this coordinatesystem has to be rotated so that the gravity-Updirection at the UnityOrigin aligns with UnityUp and the north-direction aligns with the Unity Z-axis.

different coordinatesystems use different Up- and East-directions.
When coordinateSystem A is connected to Unity, but you have geometry (for example a mesh) that is defined in coordinateSystem B, a rotation might have to be applied to ensure that the geometry aligns nicely with the other objects in unity.

$Quaternion rotationInUnity = CoordinateAtOrigin.RotationToLocalGravityUp()
$transform.rotation = rotationInUnity

* (for now) we assume that the mesh itself is defined in a left-handed, Y-up style. (even though most official coordinatesystems are righthanded and Z-up), most geometry-parsers in unity already change this definition


## Backwards compatibility

In Netherlands3D, we used to make use of conversion methods on the CoordinateConverter -such as RDtoWGS84- and
Vector3 classes per Coordinate System. This architecture is not scalable to support the plethora of CRS out there,
and as such these are all deprecated and replaced by Coordinate.Convert(targetCRS)