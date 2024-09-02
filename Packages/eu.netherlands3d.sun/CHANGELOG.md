# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2023-09-03

### Added
- Added event and property for animation state.

### Fixed
- Fixed initial sun position.

## [1.2.1] - 2023-08-27

### Fixed
- Fixed max day clamp value for months that don't have 31 days when setting the date.

## [1.2.0] - 2023-08-27

### Added
- Added function to set timeSpeed with an absolute value
- Increased the max speed to max 12 hours per second

## [1.1.0] - 2023-08-26

### Added
- A DateTime object is now the main source of time instead of multiple ints.
- Added event to keep track of if the current time is used.

## [1.0.2] - 2023-08-21

### Fixed

- Fixed calculation of origin coordinate, added function to recalculate origin and update position if the origin changes.

## [1.0.1] - 2023-08-19

### Fixed

- When Applying a new Time, the correct new time is being sent by the event instead of the old time.

## [1.0.0] - 2023-07-27

### Added

- Initial release
