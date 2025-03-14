GeoTimeZone
===========

Provides an IANA time zone identifier from latitude and longitude coordinates.

Convert DateTime from local time to UTC using an IANA time zone identifier through a lookup table since TimeZoneInfo.FindSystemTimeZoneById(String) is not suported on WebGL.

## Supported Environments

As of version 1.0.0, *GeoTimeZone* works with WebGL in Unity 2022.3

- .NETStandard 2.1

Older version may be supported, but this is not tested.

## Example Usage

```csharp
string tz = TimeZoneLookup.GetTimeZone(52.4372, 4.5559).Result;  // "Europe/Amsterdam"
DateTime utcTime = TimeZoneConverter.ConvertToUTC(DateTime.Now, timeZoneId); //current time converted to UTC
```

## Usage Notes

This library returns [IANA time zone IDs](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones).

This library uses the time zone border definitions from the [Timezone Boundary Builder][1] project,
which in-turn derive from [Open Street Map][2].  As some international borders are the subject of dispute,
the results may or may not align with your worldview.  Use at your own risk.

The Time zone data is stored as GZ files, but since Unity's Resources.Load cannot recognize these, a .txt extension is added as a workaround.

## Acknowledgements

Huge thank you to the following people:
- Evan Siroky, who tirelessly maintains the Time Zone Boundary Builder project, which we use for our source data.
- Eric Muller, who authored the original tz_world data set (now deprecated in favor of TBB).
- Simon Bartlett, who contributed all the polygon indexing and lookup bits to this library.
- Sharon Lourduraj, who wrote GeoHash-net that we used for our original implementation.
- David Troy, who wrote Geohash-js that Sharon later ported to .NET
- Nick Johnson, who's [excellent blog post](http://blog.notdot.net/2009/11/Damn-Cool-Algorithms-Spatial-indexing-with-Quadtrees-and-Hilbert-Curves) has been an inspiration to this project and so many others!
- Jonas Nyrup, who has helped with performance optimizations.
-  Rafael Xavier de Souza for creating the json of IANA time zone data.

## License

This library is provided free of charge, under the terms of the [MIT license][3].


[1]: https://github.com/evansiroky/timezone-boundary-builder
[2]: https://www.openstreetmap.org/
[3]: https://github.com/mattjohnsonpint/GeoTimeZone/blob/master/LICENSE
