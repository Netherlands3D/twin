# Guide to Understanding the Floating Origin System

## Overview

The Floating Origin System is designed for managing precision concerns in Unity 3D worlds. It pivots around the
concept of objects retaining their real-world coordinates, even as the origin in the game world moves. Key players in
this system are the Origin, WorldTransform, and WorldTransformShifter components.

## Key Components

### Origin

The Origin class manages the point of origin in the game world. It ensures that the origin stays within a configured
distance from the camera, guaranteeing the precision of world objects.

### WorldTransform

WorldTransform represents an object's real-world location in Unity's game world. When the Origin moves, WorldTransform
reflects the "shift" while the Unity's Transform remains unchanged, creating two interlinked but separated coordinate
spaces.

### WorldTransformShifter

WorldTransformShifter is a base class used to manage what happens with GameObjects when the origin moves.
Implementations of WorldTransformShifter can dictate custom behaviors for origin shifts through the ShiftTo method.

### Specialized Shifters

Now, let's consider a scenario where you're not working with vanilla Unity GameObjects but with specific packages that
have unique behaviors. The Netherlands3D 3DTiles package is an example. WorldTransformShifter allows for custom shift
behaviors switching focus to specific sub-objects that need the shifting adjustment.

#### ThreeDTilesWorldTransformShifter

```csharp
public class ThreeDTilesWorldTransformShifter : WorldTransformShifter
{
    public override void ShiftTo(WorldTransform worldTransform, Coordinate from, Coordinate to)
    {
        var delta = CoordinateConverter.ConvertTo(from, CoordinateSystem.Unity).ToVector3() 
            - CoordinateConverter.ConvertTo(to, CoordinateSystem.Unity).ToVector3();

        var contentComponents = transform.GetComponentsInChildren<Content>();
        foreach (Content contentComponent in contentComponents)
        {
            foreach (Transform child in contentComponent.transform)
            {
                child.position += delta;
            }
        }
    }
}
```

In this WorldTransformShifter, we focus on moving child GameObjects associated with the Content component in the
Netherlands3D.Tiles3D package. When an origin shift happens, it scans through all `Content` components and updates the
positions of all their child GameObjects accordingly.

## Using Floating Origin System in Your Projects

1. Attach the Origin component to a GameObject that you want to act as the world origin.
2. For objects that should keep stable real-world positions, attach the WorldTransform component.
3. For objects from the Netherlands3D 3DTiles package, attach the ThreeDTilesWorldTransformShifter to accommodate their
   specialized shifting needs in the large world context.

## Wrapping Up

The Floating Origin System empowers high-precision control of object positions in expansive Unity 3D worlds. By
combining Origin, WorldTransform, and a tailored WorldTransformShifter, you can easily manage positions of objects from
the Netherlands3D 3DTiles package or any other package needing unique shifting behaviors.