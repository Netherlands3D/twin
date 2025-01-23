# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.3] - 2025-01-23

### Fixed

- reduced garbage creation

## [1.1.2] - 2024-03-04

### Fixed

- Added null checks to callbacks in TextLayer, fixing null reference errors

## [1.1.1] - 2024-02-29

### Fixed

- GameObjects that were created after a tile had been removed are now properly destroyed
- When changing LOD levels, prior game objects are now destroyed
- Code readability improvements
- A performance improvement in determining whether a Tile needs to be destroyed when out of view

## [1.1.0] - 2024-02-24

### Added

- Serialized property for createMeshcollider bool, in order to change bool as Dynamic Bool from UnityEvents

## [1.0.1] - 2023-11-01

### Fixed

- changed namespace to work with changed namespace of SubObjects package

## [1.0.0] - 2023-10-18

### Added

- moved tileHandler and layers to its own repo
