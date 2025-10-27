# JSON Extra's for Netherlands3D

## Installing

This package is provided through OpenUPM, to install it using the CLI you can perform the following:

```bash
$ openupm add eu.netherlands3d.json
```

or, you have to add `https://package.openupm.com` as a scoped registry with, at least, the following scopes:

- `eu.netherlands3d`

## JSON Converters

## Color

A JSON Converter for JSON.net is provided that can interpret hexadecimal or CSS-named color codes in JSON, and convert 
them into UnityEngine.Color objects. To use this, ensure the type of a field is UnityEngine.Color and add the following 
annotation:

```csharp
using UnityEngine;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

class MyJson {
    [JsonConverter(typeof(ColorConverter))]
    public Color $color;
}
```

Accepted color variants are every variant supported by [ColorUtility.TryParseHtmlString](https://docs.unity3d.com/ScriptReference/ColorUtility.TryParseHtmlString.html). 
When serializing to a JSON file, it will always output as `#RRGGBBAA`.

## System.URI

A JSON Converter for JSON.net is provided that can interpret URIs in JSON and convert them into URI objects. To use 
this, ensure the type of a field is System.URI and add the following annotation:

```csharp
using System;
using Netherlands3D.Json.JsonConverters;
using Newtonsoft.Json;

class MyJson {
    [JsonConverter(typeof(UriConverter))]
    public Uri $uri;
}
```