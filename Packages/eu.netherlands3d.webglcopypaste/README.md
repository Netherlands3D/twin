# WebGL Copy Paste

A unity package containing the copied plugin from:
https://github.com/Trisibo/unity-webgl-copy-and-paste

This scripts adds copy-paste support by injecting scripts onto input fields and TextMeshPro input fields.

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.webglcopypaste
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

The script will automatically initialize using the RuntimeInitializeOnLoadMethod attribute.