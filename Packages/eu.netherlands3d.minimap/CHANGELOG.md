# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.2]

### Removed

- Superfluous Runtime.meta file in the Runtime folder; this kept giving warnings in Unity because 
  there is no Runtime\Runtime folder.

## [1.1.1]

### Fixed

- Adding padding based on 50% of map to clamping behaviour solving flickering map issues on zoom levels where map was smaller than view

## [1.1.0]

### Added

- Added option to set target camera
- Added option to enable or disable moving of target camera after click

## [1.0.0]

### Added

- Initial minimap release