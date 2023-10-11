# Object Coloring

Package to process color maps and color geometry.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add object-coloring
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

- Create color maps as Dictionaries<string, Color>. add a prioritization index and add them to the GeometryColorizer.