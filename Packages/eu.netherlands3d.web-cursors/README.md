# Web Cursors

MonoBehaviour and plugin allowing to change system cursor on WebGL applications using jslib plugin

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add web-cursors
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## Usage

Add the ChangePointerStyleHandler on a UI or a GameObject ( if the object has a collider, and the main camera a Physics Raycaster)
