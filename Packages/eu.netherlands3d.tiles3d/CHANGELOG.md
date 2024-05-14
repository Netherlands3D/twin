# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.4.1] - 14-05-2024

### Removed

- Validation whether the filename for the tileset is `tileset.json`, Google -and possibly others- use `root.json`.

## [1.4.0] - 29-04-2024

### Added

- The usedExtensions node in the root node are now parsed and checked against supported extensions

## [1.3.0] - 24-04-2024

### Added

- Added function to refresh the tiles, for example after the url changes.
- Added rudimentary check if URL is valid. 

### Fixed

- move all loaded scenes inside gltf to coordinate instead of just scene index 0


## [1.2.4] - 23-04-2024

### Fixed

- Removed forced assignment to layer 11 of the tile objects. Tile objects will take the parent object's layer by default, and will allow for manual assignment thereafter.

## [1.2.3] - 08-04-2024

### Fixed

- fixed calculation of bounds for items that do not have a boundingVolume available
- added public RecalculateBounds method to recalculate all tilebounds in tileset
- removed private legacy method 

## [1.2.1] - 16-02-2024

### Fixed

- fix calculating unitybounds from boudingVolumeType Box.
- fix reading subdivisionSceme

## [1.2.0] - 16-02-2024

### Added

- added support for boudingVolumeType Box in combination with Implicit Tiling

## [1.1.0] - 28-11-2023

### Added

- Added ability to read meshfeatures and store them using the subobjects-package
- Added support for implicit Tiling 

## [1.0.2] - 19-09-2023

### Changed

- Added unity keyword and value in package.json for discoverability in the package manager

## [1.0.1] - 15-09-2023

### Changed

- moved to version 1.0.1. of dependency eu.netherlands3d.coordinates. the bugfix makes sure the 3dTiles are allways correctly orientated.

## [1.0.0]

### Added

- First release
