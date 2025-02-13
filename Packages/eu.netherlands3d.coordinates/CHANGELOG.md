# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.9.1] 13-02-2025

### Fixed
* Fixed some errors in Coordinate struct regarding to missing operator coordinatesystem types

## [1.9.0] 10-02-2025

### Added
* Added Multiply operator (using scalars)
* Added Divide operator (using scalars)

## [1.8.0] 27-01-2025

### Added
* Added ToString Method to allow Coordinate logging

## [1.7.2] 09-01-2025

### Changed
* Now using value1, value2 and value3 instead of Points array (performance improvement)

### Deprecated
* Coordinate constructors using param double[] points

## [1.7.1] 01-08-2024

### Fixed
* Added backing field for coordinate system to fix webgl deserialization issue. This means the coordinateSystem is no longer readonly.

## [1.7.0] 30-07-2024

### Added
* Added attributes for JSON serialization of Coordinate class if Newtonsoft.JSON is present in the project.
* Added JSON constructor for the Coordinate class.


## [1.6.3] 09-07-2024

### Fixed
* Coordinate.RotationToLocalGravityUp() had a small misalignment


## [1.6.2] 31-05-2024

### Fixed
* several coordainteSystems had incorrect conversions to and from WGS84_Lat_Lon_Height

## [1.6.1] 31-05-2024

### Fixed
* lattitude an longitude 2D coordinatesystems return a wgs84_LatLonHeight-coordinate without elevation

## [1.6.0] 31-05-2024

### Added
* Added CoordinateSystem CRS84_LonLat
* Added constructor to Coordinate, setting only the coordinateSystem.
* Added variables to Coordinate to set/get Easting, Northing and Height. these put the values in the appropriate position

### Fixed
* RDNAP_Operations returned the wrong name.

## [1.5.0] 17-05-2024

### Added
* Added function CoordinateSytems.FindCoordinateSystem to find the coordaintesystem-enum by name.

### Fixed
* WGS84_ECEF_Operations and ETRS89_ECEF_Operations didn't return corrent extraLattitude and extraLongitude values.

## [1.4.3] 05-05-2024

### Fixed
* many functions using the deprecated ConvertCoordinates.Convert-function convert to CoordinateSystem.RD, expecting a CoordinateSystem.RDNAP to be returned. 



## [1.4.2] 05-05-2024

### Fixed
* CoordinateConverter.ConvertTo gave an error when converting to or from CoordinateSystem.Unity


## [1.4.1] 05-05-2024

### Changed
* update epsg4936.relativeCenter
* improved calculation for RotationToLocalGravityUp()

## [1.4.0] 26-04-2024

### Added
* static CoodinateSystems-class for connecting a epsg-coordinatesystem to Unity and connecting a epsg-coordinate to the Unity-Origin
* enum CoordinateSystem, exposing all the available coordinateSystems
* Coordinate-struct for storing a coordinate in a specified coordaintesystem
  - convert function for converting between coordinateSystems
  - ToUnity function to convert to a unity Vector3
  - Constructor to create a coordinate from a unity Vector3
  - RotationToLocalGravityUp function to get the required rotation to align geometry defined in a coordaintesystem to the geometry defined in the coordinatesystem that is connected to Unity.
  - test to see if the coordinate is valid

### Deprecated
* CoordinateConverter class with all its functions
* EPSG3857-class
* EPSG4326-class
* EPSG4936-class
* EPSG7415-class
* MovingOrigin-class
* MovingOriginFollower-class
* Unity-class
* Vector2RD-struct
* Vector3ECEF-struct
* Vector3RD-struct

## [1.3.0] 16-02-2024

### Added

* Addition and subtraction operators for Coordinate

### Fixed

* implementation of gridshift for EPSG7415.ToWGS84

## [1.2.2] 25-10-2023

### Fixed

* Proper english name for coordinate variable

## [1.2.1] 25-10-2023

### Fixed

* EPSG7415.isValid()'s range determination was flipped; causing it to always return false, resulting in false-positives

## [1.2.0]

### Added

* Added CoordinateSetup.cs to manipulate the RD-coordinates that correspond to the unity-origin.

## [1.1.1] 15-09-2023
### Fixed
* ecefRotationToUp gave wrong results when GRoundLevelY in SetGlobalOrigin.cs was not set to 0.

## [1.1.0]

### Added

* New converter for EPSG:3857

### Fixed

* The converter formerly known as WGS84 had the wrong EPSG code, it is EPSG:4326 instead of EPSG:3857

## [1.0.1]

### Fixed

* Meta file got the package.json was missing
* EPSG7415.isValid()'s range determination was flipped; causing it to always return false

## [1.0.0]

### Added

* Extracted https://github.com/Amsterdam/Netherlands3D/tree/main/Packages/Netherlands3D/Core/Runtime/Scripts/Coordinates 
  into its own package
