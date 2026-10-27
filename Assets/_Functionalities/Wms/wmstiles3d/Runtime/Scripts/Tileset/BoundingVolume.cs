using Netherlands3D.Coordinates;

namespace Netherlands3D.WMSTiles3D{
    [System.Serializable]
    public class BoundingVolume
    {
        public BoundingVolumeType boundingVolumeType;
        public double[] values;

        public void ApplyTileTransform(TileTransform tiletransform)
        {
            switch (boundingVolumeType)
            {
                case BoundingVolumeType.Box:
                    Coordinate center = new Coordinate(CoordinateSystem.Undefined, values[0], values[1], values[2]);
                    Coordinate newCenter = tiletransform.MultiplyPoint3x4(center);
                    values[0] = newCenter.value1;
                    values[1] = newCenter.value2;
                    values[2] = newCenter.value3;
                    Coordinate xAxis = new Coordinate(CoordinateSystem.Undefined, values[3], values[4], values[5]);
                    Coordinate newXAxis = tiletransform.MultiplyVector(xAxis);
                    values[3] = newXAxis.value1;
                    values[4] = newXAxis.value2;
                    values[5] = newXAxis.value3;
                    Coordinate yAxis = new Coordinate(CoordinateSystem.Undefined, values[6], values[7], values[8]);
                    Coordinate newYAxis = tiletransform.MultiplyVector(yAxis);
                    values[6] = newYAxis.value1;
                    values[7] = newYAxis.value2;
                    values[8] = newYAxis.value3;
                    Coordinate zAxis = new Coordinate(CoordinateSystem.Undefined, values[9], values[10], values[11]);
                    Coordinate newZAxis = tiletransform.MultiplyVector(zAxis);
                    values[9] = newZAxis.value1;
                    values[10] = newZAxis.value2;
                    values[11] = newZAxis.value3;
                    break;
                case BoundingVolumeType.Sphere:
                    break;
                case BoundingVolumeType.Region:
                    // is explicitly in EPSG:4979 coordinates. so no transformation to apply
                    break;
                default:
                    break;
            }
        }

        public BoundingVolume GetChildBoundingVolume(int childIndex, SubdivisionScheme subdivisionScheme)
        {
            BoundingVolume newBoundingVolume = new BoundingVolume();
            switch (boundingVolumeType)
            {
                case BoundingVolumeType.Region:

                    double[] newvalues = new double[values.Length];
                    double lonMin = values[0];
                    double lonMax = values[2];
                    double lonMid = (lonMin + lonMax) / 2f;

                    double latMin = values[1];
                    double latMax = values[3];
                    double latMid = (latMin + latMax) / 2f;

                    if (childIndex % 2 == 0)
                    {
                        newvalues[0] = lonMin;
                        newvalues[2] = lonMid;
                    }
                    else
                    {
                        newvalues[0] = lonMid;
                        newvalues[2] = lonMax;
                    }
                    if (childIndex < 2)
                    {
                        newvalues[1] = latMin;
                        newvalues[3] = latMid;
                    }
                    else
                    {
                        newvalues[1] = latMid;
                        newvalues[3] = latMax;
                    }
                    newvalues[4] = values[4];
                    newvalues[5] = values[5];
                    newBoundingVolume.values = newvalues;
                    newBoundingVolume.boundingVolumeType = BoundingVolumeType.Region;
                    break;
                case BoundingVolumeType.Box:
                    
                    newBoundingVolume.boundingVolumeType = BoundingVolumeType.Box;
                    newvalues = new double[values.Length];
                    
                    //X-axis size and direction
                    newvalues[3] = values[3] / 2d;
                    newvalues[4] = values[4] / 2d;
                    newvalues[5] = values[5] / 2d;
                    //Y-axis size and direction
                    newvalues[6] = values[6] / 2d;
                    newvalues[7] = values[7] / 2d;
                    newvalues[8] = values[8] / 2d;
                    //Z-axis size and direction
                    double heightDivision = 1d;
                    if (subdivisionScheme==SubdivisionScheme.Octree)
                    {
                        heightDivision = 2d;
                    }
                    newvalues[9] = values[9] / heightDivision;
                    newvalues[10] = values[10] / heightDivision;
                    newvalues[11] = values[11] / heightDivision;

                    

                    double centerX = values[0]; //start at center
                    double centerY = values[1];
                    double centerZ = values[2];
                    
                    //X-offset
                    double leftRightMultiplier = 1;
                    if (childIndex%2==0) //left from center
                    {
                        leftRightMultiplier = -1;
                    }
                    centerX += (leftRightMultiplier * newvalues[3]);
                    centerY += (leftRightMultiplier * newvalues[4]);
                    centerZ += (leftRightMultiplier * newvalues[5]);
                    //y-offset
                    double frontBackMultiplier = 1;
                    if (childIndex%4>1)
                    {
                        frontBackMultiplier = -1;
                    }
                    centerX += (frontBackMultiplier * newvalues[6]);
                    centerY += (frontBackMultiplier * newvalues[7]);
                    centerZ += (frontBackMultiplier * newvalues[8]);

                    if (subdivisionScheme==SubdivisionScheme.Octree)
                    {
                        double topBottomMultiplier = -1;
                        if (childIndex > 3)
                        {
                            topBottomMultiplier = 1;
                        }
                    
                   
                    centerX += topBottomMultiplier * newvalues[9];
                    centerY += topBottomMultiplier * newvalues[10];
                    centerZ += topBottomMultiplier * newvalues[11];
                    }
                    //calculate center
                    newvalues[0] = centerX;
                    newvalues[1] = centerY;
                    newvalues[2] = centerZ;
                    newBoundingVolume.values = newvalues;
                    
                    break;
                default:
                    break;
            }

            return newBoundingVolume;
        }
    }
}