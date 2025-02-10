# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.3.1]

### Added

- Added events to the interaction class to keep track of the objectmappings being checked in and out

## [1.3.0]

### Added/Removed

- Removed function to delete ColorSetLayer by list index. this was confusing since it was not deleting by priorityIndex. Pass the colorset instead to remove a ColorSetLayer.
- Added return type to InsertColorSetLayer.
- Made list of custom colors public

### Fixed

- Colors are now applied after recalculating the priorities.

## [1.2.1]

### Fixed

- Removed print statements

## [1.2.0]

### Added

- Added function to remove colorSetLayer by direct reference

## [1.1.1]

### Fixed

- Fixed bug where loading 2 files would not correctly color according to the new list of prioritized colors

## [1.1.0]

### Added

- AddAndMergeCustomColorSet now returns the ColorSetLayer.

## [1.0.2]

### Fixed

- Do not calculate changed colors after invoking

## [1.0.1]

### Fixed

- Changed namespace
- made no override color a static readonly variable
- fixed issue where the ColorSetLayer would hold a reference to a temporary variable dictionary instead of a standalone copy\

## [1.0.0]

### Added

- Script that manages different user defined color maps and prioritizes them for object coloring. 
