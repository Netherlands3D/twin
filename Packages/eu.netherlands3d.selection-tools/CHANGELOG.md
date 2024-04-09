# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.2.4]

### Fixed

- Fixed problem where dragging the end point of an open polygon (line) would also move the first point to the end position.

## [2.2.3]

### Fixed

- Used SetDrawMode when finishing a polygon to update pointer visibility

## [2.2.2]

### Fixed

- Fixed execution order issue where another system may disable the input action map of this system. By default the input action map will be enabled and no longer disabled to avoid interfering with other systems that may use the same input action map.

## [2.2.1]

### Fixed

- Auto draw is now disabled in edit mode


[2.2.0]

### Added

- Added a mode selection to allow only creating or only editing polygons. Doing both at once is still possible with the mode CreateAndEdit

[2.1.0]

### Added

- New method for returning last hovered GameObject by InputSystem module
- New method for returning the default UI/Click InputAction of the InputSystem module

## [2.0.0]

### Changed

- UI layer is now blocking the start of an area selection by default

## [1.0.0]

### Added
- Separated Package from Netherlands 3D. Still has dependency on Netherlands 3D (core and Poly2Mesh) this will be removed in the next update

## [1.0.1]

### Fixed

- Removed dependency on Netherlands3D.Core. The extension methods used are added in a temporary script that should be removed when possible to avoid double code.
- Removed depandency on Poly2Mesh embedded package in Netherlands3D, replaced it with a depencancy on the OpenUPM version of this package.

## [1.1.0]

### Added
- Unity functions are now overridable
- Added overridable function to calculate position to be added. 

### Fixed

- Fixed incorrect function calls with CloseLoop and FinishPolygon.
- Renamed some variables for clarity 
- MinPointDistance is now calculated based on handle size for spherical handles
