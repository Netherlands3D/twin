# importing a Dataset
when we have a list of coordinates and we want to use them in Unity3D, we need to know the coordinateSystem the coordinates are in. 
if we don't know the coordinate we need to find out which it is using [Determining the coordinatesystem](determining%20the%20coordinatesystem%20of%20a%20dataset.md). 

## getting positions for gameobjects
if we just need a Unity3d-position from the dataset to place a prefab we create a Coordinate from the data
```
Coordinate prefabCoordinate = new Coordinate(CoordinateSystem.DatasetCRS, coordinateValue1, CoordinateValue2, CoordinateValue3)
```
If whe have established that the Axis-order has been flipped in the dataset, we unflip it here:
```
Coordinate prefabCoordinate = new Coordinate(CoordinateSystem.DatasetCRS, coordinateValue1, CoordinateValue3, CoordinateValue2)
```

### when using the FloatingOrigin-package
After we created the gameObject we add a WorldTransform-Component to the gameObject and let that component move the gameObject to the right location
```
WorldTransform newWorldTransform = newGameObject.AddComponent<WorldTransForm>();
newWorldTransform.MoveToCoordinate(prefabCoordinate);
```
### when not using the FloatingOrigin-package
After we have created the gameObject we need to give it the right position and orientation.
```
newGameobject.Transform.postion = prefabCoordinate.toUnity();
newGameObject.transform.rotation = prefabCoordinate.RotationToLocalGravityUp();
```
## using multiple coordinates contained in one GameObject
if we want to use multiple coordinates from a dataset to create geometry contained in a single gameobject we take the following steps:
* define a local origin.
  Coordinates are usually too big to be contained in a vector3 without loss of accuracy. to make sure the values are small enough we select one of the coordinates inthe dataset to be the local Origin. which will later be applied to the gameobject.
  ```
  Coordinate datasetLocalOrigin = new Coordinate(DatasetCoordinateSystem, coordinateValue1, CoordinateValue2, CoordinateValue3)
  ```
  don't forget to flip the Axes if neccessary.
* check the coordinateSystemType.
  Some coordinatesystems are defined in degrees. coordinates in these coordinateSystems can't directly be used in our Unity environment, which is based on meters.
  if this is the case for our dataset coordinatesystem, we have to convert te coordinates before we can use them.
  ```
  bool needConversion = CoordinateSystems.getCoordinateSystemType(DatasetCoordinateSystem) = CoordinateSystemType.Geographic;
  ```
  if we need to convert the coordinates, we can convert the coordinates to the connectedCoordinateSystem.
  ```
  CoordinateSystem gameObjectCoordinateSystem = CoordinateSystems.connectedCoordinateSytem;
  CoordinateSystem GameObjectLocalOrigin = datasetLocalOrigin.Convert(gameObjectCoordinateSystem);
  ```
* Convert all the coordinates to local coordinates. 
  We can now convert all the coordinates of the dataset into Vector3's by first creating a coordinate in the datasetCoordinateSystem
  ```
  Coordinate datasetCoordinate = new Coordinate(DatasetCoordinateSystem, coordinateValue1, CoordinateValue2, CoordinateValue3);
  ```
  don't forget to flip the axes if necessary.
  next we transform the coordinate into the gameObjectCoordinateSystem
  ```
  Coordinate gameObjectCoordinate = datasetCoordinate.Convert(gameObjectCoordinateSystem);
  ```
  `
  the convert-function does a check to see if the datasetCoordinate.Coordinatesystem and the gameObjectCoordinateSystem are the same, if they are, it wont do any calculations and simply return the coordinate. so there is no need to do if-statements, you can just allways do this conversion.`
  
  now we can turn the coordinate into a local coordinate and turn it into a unity-Vector3 with the correct axis-order. we can use these Vector3's to create meshes or whatever we want to create.
  ```
  Coordinate localCoordinate = gameObjectCoordinate-GameObjectLocalOrigin;
  Vector3 localposition = new Vector3(localCoordinate.easting, localCoordinate.height, localCoordinate.northing);
  ```
* set the gameObject to the correct position and orientation
  finally, we have the get the gameobject at the right position and orientation, using the method described above. the input for this method is GameObjectLocalOrigin.
  ```
  WorldTransform newWorldTransform = newGameObject.AddComponent<WorldTransForm>();
  newWorldTransform.MoveToCoordinate(prefabCoordinate);
  ```
