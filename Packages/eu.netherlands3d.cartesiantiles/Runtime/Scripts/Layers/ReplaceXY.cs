
using System.Text;


namespace Netherlands3D.CartesianTiles
{
    public static class StringExtensions
    {
        /// <summary>
        /// Replace the template string and fill in the x and y values
        /// </summary>
        /// <param name="template">The template string</param>
        /// <param name="x">double value x</param>
        /// <param name="y">double value y</param>
        /// <returns>The replaced template string</returns>
        public static string ReplaceXY(this string template, double x, double y)
        {
            StringBuilder sb = new StringBuilder(template);
            sb.Replace("{x}", $"{x}");
            sb.Replace("{y}", $"{y}");
            return sb.ToString();
        }
    }
}
