# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.2] - 2024-03-28

### fxied

- Fixed weird issue where Unity's input system's wasReleasedThisFrame doesn't work consistently. The RuntimeTransformHandles will now calculate these states itself. 


## [1.1.1] - 2024-03-11

### Added

- Option to choose handle colors

## [1.0.1] - 2024-02-07

### Removed

- Superfluous meta file in the Runtime\Scripts folder, which caused Unity to give warnings.

## [1.0.0] - 2023-08-08

### Added

- Forked from https://github.com/pshtif/RuntimeTransformHandle and adapted to work with the new input system, added some events and general improvements