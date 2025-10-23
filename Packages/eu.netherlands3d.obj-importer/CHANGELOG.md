# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2025-09-30

### Changed
* stopped using depracated functions from coordinates-package in StreamreadOBJ.cs

## [1.2.3] - 2025-07-15

### Added

* Instead of allocating new arrays for every object, a single allocation is done and this is re-used for all objects.

## [1.2.2] - 2025-01-13

### Fixed

* Fixed gameobject referencing bug. When parsing multiple objs the objs were mixed up

## [1.2.1] - 2024-12-10

### Fixed

* Fixed importing RD geo-referenced obj models to no longer have issues with floating point precision.

## [1.2.0] - 2024-12-05

### Added

* Added events for OBJ and MTL import success

## [1.1.1] - 2023-10-17

### Fixed

* Fixed data file names to allow parsing multiple files at once.

## [1.1.0] - 2023-08-08

### Added

* made events public

## [1.0.0] - 2023-08-07

### Added

* Extracted https://github.com/Amsterdam/Netherlands3D/tree/main/Packages/Netherlands3D/ModelParsing 
  into its own package
