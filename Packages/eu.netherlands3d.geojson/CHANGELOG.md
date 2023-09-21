# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2023-09-21

### Added

- `GeoJSONStreamReader`, a specialized way of parsing GeoJSON to reduce the memory footprint for large files.

## [1.0.0] - 2023-09-19

### Added

- `EPSGId()` extension method
- `DerivedBoundingBoxes()` extension method
- `TryGetIdentifier` extension method
- Moving game objects to the center of a feature collection: `MoveToCenterOfFeatureCollection`