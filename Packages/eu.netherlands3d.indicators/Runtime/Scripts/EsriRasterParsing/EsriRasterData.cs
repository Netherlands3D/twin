using System;
using UnityEngine;

namespace Netherlands3D.Indicators.Esri
{
    public class EsriRasterData
    {
        public int numColumns = 0;
        public int numRows = 0;
        public double xllcorner = 0;
        public double yllcorner = 0;
        public double dx = 0;
        public double dy = 0;
        public double cellsize = 0;
        public double nodataValue = 0;

        public double[,] rasterData;

        public void ParseASCII(string asciiString)
        {
            string[] lines = asciiString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int dataStartIndex = 0;

            // Parse the header lines
            foreach (string line in lines)
            {
                dataStartIndex++;

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    string key = parts[0].ToLower();
                    string value = parts[1];

                    //If key is a number, we have reached the data
                    if (double.TryParse(key, out double result))
                    {
                        break;
                    }

                    switch (key)
                    {
                        case "ncols":
                            numColumns = int.Parse(value);
                            break;
                        case "nrows":
                            numRows = int.Parse(value);
                            break;
                        case "xllcorner":
                            xllcorner = double.Parse(value);
                            break;
                        case "yllcorner":
                            yllcorner = double.Parse(value);
                            break;
                        case "dx":
                            dx = double.Parse(value);
                            break;
                        case "dy":
                            dy = double.Parse(value);
                            break;
                        case "cellsize":
                            cellsize = double.Parse(value);
                            break;
                        case "nodata_value":
                            nodataValue = double.Parse(value);
                            break;
                    }
                }
            }

            // Create a 2D array to store the raster data
            rasterData = new double[numRows, numColumns];

            // Parse the data lines
            for (int i = dataStartIndex; i < lines.Length; i++)
            {
                string[] dataValues = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < dataValues.Length; j++)
                {
                    rasterData[i - dataStartIndex, j] = double.Parse(dataValues[j]);
                }
            }
        }

        /// <summary>
        /// Get the pixel value at the given coordinates.
        /// </summary>
        /// <param name="rasterData">The 2D array of raster data</param>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        public double GetPixelValue(int x, int y)
        {
            return rasterData[x, y];
        }

        /// <summary>
        /// Return the grid value at a location using normalised (0 to 1) coordinates based on Unity's forward Z facing north
        /// </summary>
        /// <param name="x">Normalised x position</param>
        /// <param name="y">Normalised y position</param>
        /// <returns>The value of the pixel</returns>
        public double GetValueAtNormalisedLocation(float x, float y)
        {
            var targetPixelX = Mathf.RoundToInt(numRows * (1 - y));
            var targetPixelY = Mathf.RoundToInt(numColumns * x);
            return GetPixelValue(targetPixelX, targetPixelY);
        }
    }
}