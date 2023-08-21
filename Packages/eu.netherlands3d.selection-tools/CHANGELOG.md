# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0]

### Added
Separated Package from Netherlands 3D. Still has dependency on Netherlands 3D (core and Poly2Mesh) this will be removed in the next update

## [1.0.1]

### Fixed

* Removed dependency on Netherlands3D.Core. The extension methods used are added in a temporary script that should be removed when possible to avoid double code.
* Removed depandency on Poly2Mesh embedded package in Netherlands3D, replaced it with a depencancy on the OpenUPM version of this package.