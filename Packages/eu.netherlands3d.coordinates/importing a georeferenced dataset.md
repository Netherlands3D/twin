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
