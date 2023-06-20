# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
