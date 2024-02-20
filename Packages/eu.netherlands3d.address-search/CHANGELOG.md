# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1]

### Fixed

- `onCoordinateFound` event reported the coordinate to be RD, but it was in the Unity space; it now has the correct CRS.

## [1.1.0]

### Added

- Made moving/animating of main camera optional via serialized boolean
- New event containing RD coordinate of selected search item

## [1.0.0]

### Added

- Initial release
