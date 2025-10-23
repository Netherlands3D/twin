# Sun, Time of Day and Shadows

This package contains the means to add a near-realistic Day/Night cycle to your project. This will allow for 
shadow simulations that reflect the time of day and day in a specific year.

Please note that this package is specific for the Universal Render Pipeline (URP) as the shadow manipulation
modifies your render pipeline to make the shadow length as realistic as possible.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.sun
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

> Please note that for this package to work as expected, you need to have the Origin set up from the 
> Coordinates package.

The easiest path is to use the Sun prefab that is included and 

1. Hook up the Directional Light that is representing your sun to the Suntime script. 
2. Hook up your UniversalRenderPipeline to the DynamicShadowDistance script.

After this, the script will automatically alter the Sun's position according to the specified date and time. These  

When using real-world positions, please consult the Coordinates package how to configure the position of 0,0,0 in 
worldspace to match a specific point in the real world.