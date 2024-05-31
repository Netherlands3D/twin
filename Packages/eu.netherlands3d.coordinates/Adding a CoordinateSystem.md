Adding a CoordinateSystem
=========================

Create a new Class using CoordinateSystemOperation as a baseClass

### for converting coordinates
```
public Override Coordinate ConvertFromWGS84LatLonH(Coordinate coordinate)
{

}
public Override Coordinate ConvertToWGS84LatLonH(Coordinate coordinate)
{

}
```

### for creating a coordinate
```
public Override int EastingIndex();
public Override int NorthingIndex();
public Override int AxisCount();
```

### for testing a coordinate
```
public Override bool CoordinateIsValid(Coordinate coordinate);
```

### for finding the coordainteSystem by epsg-code
```
public Override string Code();
```

### for aligning geometry to another coordinateSystem
```
public Override Vector3WGS Orientation();
public Override Vector3WGS GlobalUpDirection(Coordinate coordinate);
public Override Vector3WGS LocalUpDirection(Coordinate coordinate);
```