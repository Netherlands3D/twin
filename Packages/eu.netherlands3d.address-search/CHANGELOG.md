# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.1] - 2025-04-01

### Fixed

- Removed the to unity coordinate conversion

## [2.0.0] - 2024-06-03

### Added

- update coordinate conversion package dependency to specific 1.6.2 version
- changed code so new coordinate conversion methods are used, and lat lon order is used,- fixing potential bugs with applications using latest coordinate conversion package

## [1.1.3] - 2024-02-20

### Fixed

- Fixed unintentional scaling of result buttons

## [1.1.2] - 2024-02-20

### Fixed

- `onCoordinateFound` event mixed the y and z of the coordinate.

## [1.1.1] - 2024-02-20

### Fixed

- `onCoordinateFound` event reported the coordinate to be RD, but it was in the Unity space; it now has the correct CRS.

## [1.1.0] - 2024-01-29

### Added

- Made moving/animating of main camera optional via serialized boolean
- New event containing RD coordinate of selected search item

## [1.0.0] - 2023-06-26

### Added

- Initial release
