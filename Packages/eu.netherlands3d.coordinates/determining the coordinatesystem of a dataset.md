

# determining the coordinatesystem for a dataset
to get the data from a dataset into untiy at the correct location we need to know the coordinatesystem of the dataset.
## coordinatesystem is known
if the dataset uses a default-coordinatesystem, we set it up, if not we start with an undefined coordinatesystem

```
CoordinateSystem datasetCoordinateSystem = CoordinateSystem.Undefined;
```
if the dataset contains a field in which the coordinatesystem is declared, we can try to find the corresponding enum using FindCoordinateSystem()
```
 if (CoordinateSystems.FindCoordinateSystem(Dataset.coordinatesystemname,out datasetCoordinateSystem)==false)
 {
     // a coordiantesystem is not found
 }
```
If a Coordinatesystem is not found, we probably don't support the coordinateSystem, or the name of the coordinatesystem is not correct.
When we have found a coordinateSystem to work with, we need to do a couple of checks.

### first: check axiscount  
Many coordinateSystems have 2D- and 3D-variants. not everyone is hyper-aware of this. as a result it is possible that de dataset declares a 2D-coordinatesystem,
while the dataset contains 3D-coordinates. 
Because the third dimension of the coordinate will be lost when creating a Coordinate from the package, we have to make sure the coordinatesystem is the one for the 3D-version of the coordinatesystem if the coordinates in the dataset contain 3 dimensions.
```
datasetCoordinateSystem = CoordinateSystems.To3D(datasetCoordinateSystem);
```
### second: check axisOrder
The axis-order of a coordinatesystem is not always respected. in some cases the second and third axis will be flipped.
To be able to detect this and flip the axis-order back we can do the following, using a coordinate from the dataset:
get the values for the 3 axes of a coordinate in the dataset
```
        double Axis1Value = Dataset.coordinates[0, 0];
        double Axis2Value = Dataset.coordinates[0, 1];
        double Axis3Value = Dataset.coordinates[0, 2];
```
check if the coordinate is within the validity-range of the coordinatesystem
```
        bool standardIsValid;
        Coordinate standardCoordinate = new Coordinate(datasetCoordinateSystem, Axis1Value, Axis2Value, Axis3Value);
        standardIsValid = standardCoordinate.IsValid();
```
if the standardCoordinate is valid we can continue.
if the standardCoordinate is not valid we can check if the second and third axis are flipped.
```
        bool flippedIsValid;
        Coordinate flippedCoordinate = new Coordinate(datasetCoordinateSystem, Axis1Value, Axis3Value, Axis2Value);
        flippedIsValid = flippedCoordinate.IsValid();
```
If the flipped coordinate is valid, we need te remember to flip the second and third axis for all coordaintes from the dataset.
If the flipped coordinate is also invalid we might want to stop the code and tell the user something is very wrong.
## coordinatesystem is known
if the coordinatesystem of the dataset is not known we can check to see which of the supported coordinatesystems it can be, using a coordinate from the dataset.  
Get the values for the 3 axes of a coordinate in the dataset
```
        double Axis1Value = Dataset.coordinates[0, 0];
        double Axis2Value = Dataset.coordinates[0, 1];
        double Axis3Value = Dataset.coordinates[0, 2];
```
Using CoordinateSystems.TryFindValidCoordinates we can get a list of valid coordinates that can be created from the given values
```
 List<Coordinate> possibleCoordinates;
 if (CoordinateSystems.TryFindValidCoordinates(Axis1Value, Axis2Value, Axis3Value, out possibleCoordinates))
 {
     //one or more possible coordinatesystems have been found. if it is more then one you might want to ask the user which one to use.
     //if only one has been found we can set the datasetCoordinateSystem to the coordaintesystem of the first coordinate in the list
     datasetCoordinateSystem = (CoordinateSystem)possibleCoordinates[0].CoordinateSystem;
 }
```
If this doesn't result in a possible coordinatesystem, we can try it with a flipped second and third axis
```
if (CoordinateSystems.TryFindValidCoordinates(Axis1Value, Axis3Value, Axis2Value, out possibleCoordinates))
{
    //one or more possible coordinatesystems have been found. if it is more then one you might want to ask the user which one to use.
    //if only one has been found we can set the datasetCoordinateSystem to the coordaintesystem of the first oordinateinthe list
    datasetCoordinateSystem = (CoordinateSystem)possibleCoordinates[0].CoordinateSystem;
    //we need te remember to flip the second and third axis for all coordaintes from the dataset.
}
```
If we still can't find a possible coordinatesystem then
- the dataset is in a coordinatesystem the coordinatepackage doesn't support or,
- the dataset is in an undefined local coordinatesytem
