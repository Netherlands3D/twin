using System;

public class EsriRasterData{
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
            dataStartIndex ++;

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
    public double GetPixelValue(double x, double y)
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