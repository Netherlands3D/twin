importing a georeferenced dataset:

if the dataset uses a default-coordinatesystem, we set it up, if not we start with an undefined coordinatesystem

```
CoordinateSystem datasetCoordinateSystem = CoordinateSystem.Undefined;
```
if the dataset contains a field in which the coordinatesystem is declared, we can try to find the corresponding enum using FindCoordinateSystem()
```
 if (CoordinateSystems.FindCoordinateSystem(Dataset.coordinatesystemname,out datasetCoordinateSystem)==false)
 {
     // if a corresponding enum is not found, we probably don't support the coordinateSystem, or the name is not correct.
 }
```
when we have found a coordinateSystem to work with, we need to do a couple of checks.

first:   many coordinateSystems have 2D- and 3D-variants. not everyone is hyper-aware of this. as a result it is possible that de dataset declares a 2D-coordinatesystem, while the dataset containes 3d-coordinates.because the third dimension of the coordinate will be lost when creating a Coordinate form the package, we have to make sure the coordinatesystem-enum is the one for the 3D-version of the coordinatesystemif the coordinates in the dataset contain 3 dimensions.
