using System;
using System.IO;

public class EsriRasterParser
{
    /// <summary>
    /// Parses an ASCII raster file into a 2D array of doubles.
    /// Check https://desktop.arcgis.com/en/arcmap/latest/manage-data/raster-and-images/esri-ascii-raster-format.htm for more information.
    /// </summary>
    /// <param name="asciiString">The ASCII string content of the raster file</param>
    /// <returns></returns>
    public static double[,] Parse(string asciiString)
    {
        string[] lines = asciiString.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        // Skip the header lines
        int dataStartIndex = 6;

        // Get the number of columns and rows
        string[] sizeInfo = lines[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int numColumns = int.Parse(sizeInfo[1]);
        int numRows = int.Parse(sizeInfo[3]);

        // Create a 2D array to store the raster data
        double[,] rasterData = new double[numRows, numColumns];

        // Parse the data lines
        for (int i = dataStartIndex; i < lines.Length; i++)
        {
            string[] dataValues = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (int j = 0; j < dataValues.Length; j++)
            {
                rasterData[i - dataStartIndex, j] = double.Parse(dataValues[j]);
            }
        }

        return rasterData;
    }

    /// <summary>
    /// Get the pixel value at the given coordinates.
    /// </summary>
    /// <param name="rasterData">The 2D array of raster data</param>
    /// <param name="x">The x coordinate</param>
    /// <param name="y">The y coordinate</param>
    public static double GetPixelValue(double[,] rasterData, double x, double y)
    {
        // Get the number of columns and rows
        int numColumns = rasterData.GetLength(1);
        int numRows = rasterData.GetLength(0);

        // Calculate the pixel coordinates
        double xPixel = (x - 0) / (1 - 0) * (numColumns - 1);
        double yPixel = (y - 0) / (1 - 0) * (numRows - 1);

        // Get the pixel coordinates
        int xPixelInt = (int)Math.Floor(xPixel);
        int yPixelInt = (int)Math.Floor(yPixel);

        // Get the pixel value
        double pixelValue = rasterData[yPixelInt, xPixelInt];

        return pixelValue;
    }
}
