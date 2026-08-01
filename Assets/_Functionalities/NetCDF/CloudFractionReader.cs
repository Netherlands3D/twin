using System;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Functionalities.NetCDF
{
    /// <summary>
    /// Wraps NetCDFReader to extract cloud fraction data as a 2D grid per time/level index.
    /// Assumes the variable's dimensions are ordered (time, y, x) — a 3D variable. If your
    /// file's variable is actually 4D (e.g. time, level, y, x), see the note in GetCloudLayer.
    /// </summary>
    public class CloudFractionReader
    {
        private readonly NetCDFReader _reader;
        private readonly string _variableName;

        public CloudFractionReader(byte[] ncBytes, string variableName = "effective-type-cloud-area-fraction-atm")
        {
            _reader = new NetCDFReader(ncBytes);
            _variableName = variableName;
        }

        /// <summary>
        /// Returns the raw dimension names/sizes for the target variable, useful for
        /// confirming the actual shape before trusting GetCloudLayer's reshape logic.
        /// </summary>
        public (string[] names, int[] sizes) GetVariableShape()
        {
            var variable = _reader.FindVariable(_variableName);
            if (variable == null)
                throw new ArgumentException($"Variable not found: {_variableName}. " +
                    $"Available variables: {string.Join(", ", _reader.Variables.Select(v => v.Name))}");

            var dims = variable.DimensionIds.Select(id => _reader.Dimensions[id]).ToArray();
            return (dims.Select(d => d.Name).ToArray(), dims.Select(d => d.Size).ToArray());
        }

        /// <summary>
        /// Returns a single time-slice of the cloud fraction data as a [y, x] grid.
        /// </summary>
        public float[,] GetCloudLayer(int timeIndex)
        {
            var variable = _reader.FindVariable(_variableName);
            if (variable == null)
                throw new ArgumentException($"Variable not found: {_variableName}. " +
                    $"Available variables: {string.Join(", ", _reader.Variables.Select(v => v.Name))}");

            var dims = variable.DimensionIds.Select(id => _reader.Dimensions[id]).ToArray();

            if (dims.Length != 3)
            {
                throw new NotSupportedException(
                    $"Expected a 3D variable (time, y, x) but '{_variableName}' has {dims.Length} dimensions: " +
                    $"[{string.Join(", ", dims.Select(d => $"{d.Name}={d.Size}"))}]. " +
                    "If this is 4D (e.g. time, level, y, x), use GetVolumetricLayer instead.");
            }

            int ySize = dims[1].Size;
            int xSize = dims[2].Size;

            float[] flat = _reader.GetDataVariable<float>(variable);

            var result = new float[ySize, xSize];
            long offset = (long)timeIndex * ySize * xSize;

            for (int y = 0; y < ySize; y++)
            for (int x = 0; x < xSize; x++)
                result[y, x] = flat[offset + y * xSize + x];

            return result;
        }

        /// <summary>
        /// Returns a single time-slice as a [level, y, x] volumetric grid, for variables
        /// with a 4th dimension (e.g. atmospheric level/height).
        /// </summary>
        public float[,,] GetVolumetricLayer(int timeIndex)
        {
            var variable = _reader.FindVariable(_variableName);
            if (variable == null)
                throw new ArgumentException($"Variable not found: {_variableName}");

            var dims = variable.DimensionIds.Select(id => _reader.Dimensions[id]).ToArray();

            if (dims.Length != 4)
            {
                throw new NotSupportedException(
                    $"Expected a 4D variable (time, level, y, x) but '{_variableName}' has {dims.Length} dimensions: " +
                    $"[{string.Join(", ", dims.Select(d => $"{d.Name}={d.Size}"))}]. " +
                    "If this is 3D (time, y, x), use GetCloudLayer instead.");
            }

            int levelSize = dims[1].Size;
            int ySize = dims[2].Size;
            int xSize = dims[3].Size;

            float[] flat = _reader.GetDataVariable<float>(variable);

            var result = new float[levelSize, ySize, xSize];
            long timeOffset = (long)timeIndex * levelSize * ySize * xSize;

            for (int l = 0; l < levelSize; l++)
            for (int y = 0; y < ySize; y++)
            for (int x = 0; x < xSize; x++)
                result[l, y, x] = flat[timeOffset + l * ySize * xSize + y * xSize + x];

            return result;
        }
    }
}