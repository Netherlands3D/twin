# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-03-18

### Added

- Unload Text assets after using them.
- Added option to cache Geo database for quicker access if time zones change often, at the cost of more memory usage.

## [1.0.0] - 2025-03-14

### Added

- Initial release, based on https://github.com/mattjohnsonpint/GeoTimeZone/ but adapted to work in Unity WebGL.
- Because WebGL has no access to TimeZoneInfo.FindSystemTimeZoneById(String) a custom implementation had to be built to calculate the offsets from UTC. The information was gotten from https://github.com/rxaviers/iana-tz-data/blob/master/iana-tz-data.json. This is IANA time zone data converted to json for easier parsing. In the future this should be preferably gotten from IANA directly

