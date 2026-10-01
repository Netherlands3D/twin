using System;
using System.Collections;
using BitMiracle.LibTiff.Classic;
using UnityEngine;

namespace Netherlands3D.Functionalities.GeoTiff
{
    public static class GeoTiffTextureDecoder
    {
        public static IEnumerator Decode(string path, int maximumDimension, Action<Texture2D> completed,
            Action<Exception> failed)
        {
            Tiff tiff = null;
            var width = 0;
            var height = 0;
            var bitsPerSample = 0;
            var samplesPerPixel = 0;
            var photometric = (Photometric)(-1);
            var planarConfig = PlanarConfig.CONTIG;
            Exception initializationError = null;
            try
            {
                tiff = Tiff.Open(path, "r") ?? throw new InvalidOperationException("LibTiff could not open the image.");
                width = GetInt(tiff, TiffTag.IMAGEWIDTH);
                height = GetInt(tiff, TiffTag.IMAGELENGTH);
                bitsPerSample = GetInt(tiff, TiffTag.BITSPERSAMPLE, 8);
                samplesPerPixel = GetInt(tiff, TiffTag.SAMPLESPERPIXEL, 1);
                photometric = (Photometric)GetInt(tiff, TiffTag.PHOTOMETRIC, (int)Photometric.MINISBLACK);
                planarConfig = (PlanarConfig)GetInt(tiff, TiffTag.PLANARCONFIG, (int)PlanarConfig.CONTIG);

                if (bitsPerSample != 8)
                    throw new NotSupportedException($"This GeoTIFF uses {bitsPerSample}-bit samples. The importer currently supports 8-bit imagery.");
            }
            catch (Exception exception)
            {
                initializationError = exception;
            }
            if (initializationError != null)
            {
                failed(initializationError);
                tiff?.Dispose();
                yield break;
            }

            maximumDimension = Mathf.Clamp(maximumDimension, 32, SystemInfo.maxTextureSize);
            var sampleStep = Math.Max(1, Mathf.CeilToInt(Math.Max(width, height) / (float)maximumDimension));
            var outputWidth = Mathf.CeilToInt(width / (float)sampleStep);
            var outputHeight = Mathf.CeilToInt(height / (float)sampleStep);
            var pixels = new Color32[outputWidth * outputHeight];

            try
            {
                var supported = photometric == Photometric.RGB || photometric == Photometric.MINISBLACK ||
                                photometric == Photometric.MINISWHITE || photometric == Photometric.PALETTE;
                if (!supported)
                {
                    var rgbaDecoder = DecodeViaRgba(tiff, width, height, sampleStep, outputWidth, outputHeight, pixels)
                        .GetEnumerator();
                    while (true)
                    {
                        object current;
                        bool moveNext;
                        Exception moveError = null;
                        try
                        {
                            moveNext = rgbaDecoder.MoveNext();
                            current = moveNext ? rgbaDecoder.Current : null;
                        }
                        catch (Exception exception)
                        {
                            moveError = exception;
                            moveNext = false;
                            current = null;
                        }
                        if (moveError != null)
                        {
                            failed(moveError);
                            yield break;
                        }
                        if (!moveNext)
                            break;
                        yield return current;
                    }
                }
                else if (tiff.IsTiled())
                {
                    var tileDecoder = DecodeTiles(tiff, width, height, sampleStep, outputWidth, outputHeight, pixels,
                        samplesPerPixel, photometric, planarConfig).GetEnumerator();
                    while (true)
                    {
                        object current;
                        bool moveNext;
                        Exception moveError = null;
                        try
                        {
                            moveNext = tileDecoder.MoveNext();
                            current = moveNext ? tileDecoder.Current : null;
                        }
                        catch (Exception exception)
                        {
                            moveError = exception;
                            moveNext = false;
                            current = null;
                        }
                        if (moveError != null)
                        {
                            failed(moveError);
                            yield break;
                        }
                        if (!moveNext)
                            break;
                        yield return current;
                    }
                }
                else
                {
                    byte[] scanline = null;
                    byte[] redPlane = null;
                    byte[] greenPlane = null;
                    byte[] bluePlane = null;
                    byte[] alphaPlane = null;
                    ushort[] redMap = null;
                    ushort[] greenMap = null;
                    ushort[] blueMap = null;
                    Exception scanlineSetupError = null;
                    try
                    {
                        scanline = new byte[Math.Max(1, tiff.ScanlineSize())];
                        redPlane = planarConfig == PlanarConfig.SEPARATE ? new byte[scanline.Length] : null;
                        greenPlane = planarConfig == PlanarConfig.SEPARATE ? new byte[scanline.Length] : null;
                        bluePlane = planarConfig == PlanarConfig.SEPARATE ? new byte[scanline.Length] : null;
                        var colorSamples = photometric == Photometric.RGB ? 3 : 1;
                        alphaPlane = planarConfig == PlanarConfig.SEPARATE && samplesPerPixel > colorSamples
                            ? new byte[scanline.Length]
                            : null;
                        if (photometric == Photometric.PALETTE)
                        {
                            var colorMap = tiff.GetField(TiffTag.COLORMAP);
                            if (colorMap == null || colorMap.Length < 3)
                                throw new InvalidOperationException("Palette GeoTIFF has no color map.");
                            redMap = colorMap[0].ToUShortArray();
                            greenMap = colorMap[1].ToUShortArray();
                            blueMap = colorMap[2].ToUShortArray();
                        }
                    }
                    catch (Exception exception)
                    {
                        scanlineSetupError = exception;
                    }
                    if (scanlineSetupError != null)
                    {
                        failed(scanlineSetupError);
                        yield break;
                    }

                    var outputY = outputHeight - 1;
                    for (var sourceY = 0; sourceY < height; sourceY += sampleStep, outputY--)
                    {
                        Exception scanlineError = null;
                        try
                        {
                            if (planarConfig == PlanarConfig.SEPARATE)
                            {
                                if (photometric == Photometric.RGB)
                                {
                                    RequireScanline(tiff.ReadScanline(redPlane, sourceY, 0), sourceY);
                                    RequireScanline(tiff.ReadScanline(greenPlane, sourceY, 1), sourceY);
                                    RequireScanline(tiff.ReadScanline(bluePlane, sourceY, 2), sourceY);
                                }
                                else
                                {
                                    RequireScanline(tiff.ReadScanline(scanline, sourceY, 0), sourceY);
                                }
                                if (alphaPlane != null)
                                {
                                    var alphaSample = photometric == Photometric.RGB ? 3 : 1;
                                    RequireScanline(tiff.ReadScanline(alphaPlane, sourceY, (short)alphaSample), sourceY);
                                }
                            }
                            else
                            {
                                RequireScanline(tiff.ReadScanline(scanline, sourceY), sourceY);
                            }

                            var rowOffset = outputY * outputWidth;
                            var outputX = 0;
                            for (var sourceX = 0; sourceX < width; sourceX += sampleStep, outputX++)
                            {
                                pixels[rowOffset + outputX] = DecodePixel(sourceX * samplesPerPixel, sourceX,
                                    scanline, redPlane, greenPlane,
                                    bluePlane, alphaPlane, samplesPerPixel, photometric, planarConfig,
                                    redMap, greenMap, blueMap);
                            }
                        }
                        catch (Exception exception)
                        {
                            scanlineError = exception;
                        }
                        if (scanlineError != null)
                        {
                            failed(scanlineError);
                            yield break;
                        }

                        if ((outputY & 15) == 0)
                            yield return null;
                    }
                }

                try
                {
                    var texture = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, true, false)
                    {
                        name = System.IO.Path.GetFileNameWithoutExtension(path),
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    texture.SetPixels32(pixels);
                    texture.Apply(true, true);
                    completed(texture);
                }
                catch (Exception exception)
                {
                    failed(exception);
                }
            }
            finally
            {
                tiff.Dispose();
            }
        }

        private static IEnumerable DecodeViaRgba(Tiff tiff, int width, int height, int sampleStep,
            int outputWidth, int outputHeight, Color32[] pixels)
        {
            if ((long)width * height > 32_000_000)
                throw new NotSupportedException("This large GeoTIFF uses a color layout that requires full-image decoding. Convert it to an 8-bit RGB(A) GeoTIFF first.");

            var raster = new int[width * height];
            if (!tiff.ReadRGBAImageOriented(width, height, raster, Orientation.TOPLEFT))
                throw new InvalidOperationException("LibTiff could not decode the GeoTIFF pixels.");

            var outputY = outputHeight - 1;
            for (var sourceY = 0; sourceY < height; sourceY += sampleStep, outputY--)
            {
                var outputX = 0;
                for (var sourceX = 0; sourceX < width; sourceX += sampleStep, outputX++)
                {
                    var value = raster[sourceY * width + sourceX];
                    pixels[outputY * outputWidth + outputX] = new Color32(
                        (byte)Tiff.GetR(value), (byte)Tiff.GetG(value), (byte)Tiff.GetB(value), (byte)Tiff.GetA(value));
                }
                if ((outputY & 15) == 0)
                    yield return null;
            }
        }

        private static IEnumerable DecodeTiles(Tiff tiff, int width, int height, int sampleStep,
            int outputWidth, int outputHeight, Color32[] pixels, int samplesPerPixel, Photometric photometric,
            PlanarConfig planarConfig)
        {
            var tileWidth = GetInt(tiff, TiffTag.TILEWIDTH);
            var tileHeight = GetInt(tiff, TiffTag.TILELENGTH);
            var tileRowSize = tiff.TileRowSize();
            if (tileWidth <= 0 || tileHeight <= 0 || tileRowSize <= 0)
                throw new InvalidOperationException("The tiled GeoTIFF has invalid tile dimensions.");

            var bufferSize = Math.Max(1, tiff.TileSize());
            var tileData = new byte[bufferSize];
            var redPlane = planarConfig == PlanarConfig.SEPARATE ? new byte[bufferSize] : null;
            var greenPlane = planarConfig == PlanarConfig.SEPARATE && photometric == Photometric.RGB ? new byte[bufferSize] : null;
            var bluePlane = planarConfig == PlanarConfig.SEPARATE && photometric == Photometric.RGB ? new byte[bufferSize] : null;
            var colorSamples = photometric == Photometric.RGB ? 3 : 1;
            var alphaPlane = planarConfig == PlanarConfig.SEPARATE && samplesPerPixel > colorSamples
                ? new byte[bufferSize]
                : null;

            ushort[] redMap = null;
            ushort[] greenMap = null;
            ushort[] blueMap = null;
            if (photometric == Photometric.PALETTE)
            {
                var colorMap = tiff.GetField(TiffTag.COLORMAP);
                if (colorMap == null || colorMap.Length < 3)
                    throw new InvalidOperationException("Palette GeoTIFF has no color map.");
                redMap = colorMap[0].ToUShortArray();
                greenMap = colorMap[1].ToUShortArray();
                blueMap = colorMap[2].ToUShortArray();
            }

            for (var tileY = 0; tileY < height; tileY += tileHeight)
            {
                for (var tileX = 0; tileX < width; tileX += tileWidth)
                {
                    if (planarConfig == PlanarConfig.SEPARATE)
                    {
                        if (photometric == Photometric.RGB)
                        {
                            RequireTile(tiff.ReadTile(redPlane, 0, tileX, tileY, 0, 0), tileX, tileY);
                            RequireTile(tiff.ReadTile(greenPlane, 0, tileX, tileY, 0, 1), tileX, tileY);
                            RequireTile(tiff.ReadTile(bluePlane, 0, tileX, tileY, 0, 2), tileX, tileY);
                        }
                        else
                        {
                            RequireTile(tiff.ReadTile(tileData, 0, tileX, tileY, 0, 0), tileX, tileY);
                        }
                        if (alphaPlane != null)
                            RequireTile(tiff.ReadTile(alphaPlane, 0, tileX, tileY, 0,
                                (short)(photometric == Photometric.RGB ? 3 : 1)), tileX, tileY);
                    }
                    else
                    {
                        RequireTile(tiff.ReadTile(tileData, 0, tileX, tileY, 0, 0), tileX, tileY);
                    }

                    var firstSampleY = ((tileY + sampleStep - 1) / sampleStep) * sampleStep;
                    var firstSampleX = ((tileX + sampleStep - 1) / sampleStep) * sampleStep;
                    var endY = Math.Min(height, tileY + tileHeight);
                    var endX = Math.Min(width, tileX + tileWidth);
                    for (var sourceY = firstSampleY; sourceY < endY; sourceY += sampleStep)
                    {
                        var localY = sourceY - tileY;
                        for (var sourceX = firstSampleX; sourceX < endX; sourceX += sampleStep)
                        {
                            var localX = sourceX - tileX;
                            var separateOffset = localY * tileRowSize + localX;
                            var contiguousOffset = localY * tileRowSize + localX * samplesPerPixel;
                            var outputX = sourceX / sampleStep;
                            var outputY = outputHeight - 1 - sourceY / sampleStep;
                            pixels[outputY * outputWidth + outputX] = DecodePixel(contiguousOffset, separateOffset,
                                tileData, redPlane, greenPlane, bluePlane, alphaPlane, samplesPerPixel, photometric,
                                planarConfig, redMap, greenMap, blueMap);
                        }
                    }
                    yield return null;
                }
            }
        }

        private static Color32 DecodePixel(int contiguousOffset, int separateOffset, byte[] scanline,
            byte[] redPlane, byte[] greenPlane,
            byte[] bluePlane, byte[] alphaPlane, int samplesPerPixel, Photometric photometric,
            PlanarConfig planarConfig, ushort[] redMap, ushort[] greenMap, ushort[] blueMap)
        {
            if (photometric == Photometric.RGB)
            {
                if (planarConfig == PlanarConfig.SEPARATE)
                    return new Color32(redPlane[separateOffset], greenPlane[separateOffset], bluePlane[separateOffset],
                        alphaPlane == null ? (byte)255 : alphaPlane[separateOffset]);
                return new Color32(scanline[contiguousOffset], scanline[contiguousOffset + 1],
                    scanline[contiguousOffset + 2], samplesPerPixel > 3 ? scanline[contiguousOffset + 3] : (byte)255);
            }

            var valueOffset = planarConfig == PlanarConfig.CONTIG ? contiguousOffset : separateOffset;
            var value = scanline[valueOffset];
            if (photometric == Photometric.PALETTE)
                return new Color32((byte)(redMap[value] >> 8), (byte)(greenMap[value] >> 8), (byte)(blueMap[value] >> 8), 255);

            var gray = photometric == Photometric.MINISWHITE ? (byte)(255 - value) : value;
            var alpha = planarConfig == PlanarConfig.CONTIG && samplesPerPixel > 1
                ? scanline[valueOffset + 1]
                : alphaPlane == null ? (byte)255 : alphaPlane[separateOffset];
            return new Color32(gray, gray, gray, alpha);
        }

        private static int GetInt(Tiff tiff, TiffTag tag, int fallback = 0)
        {
            var value = tiff.GetField(tag);
            return value == null || value.Length == 0 ? fallback : value[0].ToInt();
        }

        private static void RequireScanline(bool succeeded, int row)
        {
            if (!succeeded)
                throw new InvalidOperationException($"LibTiff could not decode scanline {row}.");
        }

        private static void RequireTile(int bytesRead, int x, int y)
        {
            if (bytesRead < 0)
                throw new InvalidOperationException($"LibTiff could not decode the tile at ({x}, {y}).");
        }
    }
}
