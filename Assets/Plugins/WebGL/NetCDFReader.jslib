using System;
using System.Collections.Generic;
using System.Text;

namespace Netherlands3D.Functionalities.NetCDF
{
    /// <summary>
    /// Reads NetCDF "classic" format (v1) and "64-bit offset" format (v2) files —
    /// the CDF-prefixed binary format documented at:
    /// https://www.unidata.ucar.edu/software/netcdf/docs/file_format_specifications.html
    ///
    /// NOTE: This does NOT support NetCDF-4 (HDF5-based) files. Those start with
    /// the byte sequence 0x89 'H' 'D' 'F', not 'C' 'D' 'F'. If GetMagicBytes on
    /// your file doesn't start with "CDF", this reader cannot parse it and you'd
    /// need an HDF5-capable library instead.
    /// </summary>
    public class NetCDFReader
    {
        public int Version { get; private set; }
        public List<NetCDFDimension> Dimensions { get; private set; } = new();
        public List<NetCDFAttribute> GlobalAttributes { get; private set; } = new();
        public List<NetCDFVariable> Variables { get; private set; } = new();
        public NetCDFRecordDimension RecordDimension { get; private set; } = new();

        private readonly byte[] _data;

        public NetCDFReader(byte[] data)
        {
            _data = data;
            var buffer = new BigEndianReader(data);

            string magic = buffer.ReadChars(3);
            if (magic != "CDF")
                throw new FormatException($"Not a valid NetCDF v3.x file: should start with 'CDF' (got '{magic}'). " +
                                           "This may be a NetCDF-4/HDF5 file, which this reader does not support.");

            Version = buffer.ReadByte();
            if (Version > 2)
                throw new FormatException($"Not a valid NetCDF v3.x file: unknown version {Version}");

            ReadHeader(buffer);
        }

        private void ReadHeader(BigEndianReader buffer)
        {
            RecordDimension.Length = buffer.ReadUInt32();

            ReadDimensionsList(buffer);
            GlobalAttributes = ReadAttributesList(buffer);
            ReadVariablesList(buffer);
        }

        private void ReadDimensionsList(BigEndianReader buffer)
        {
            const uint ZERO = 0;
            const uint NC_DIMENSION = 10;
            const uint NC_UNLIMITED = 0;

            uint tag = buffer.ReadUInt32();
            if (tag == ZERO)
            {
                uint zeroCheck = buffer.ReadUInt32();
                if (zeroCheck != ZERO)
                    throw new FormatException("wrong empty tag for list of dimensions");
                return;
            }

            if (tag != NC_DIMENSION)
                throw new FormatException("wrong tag for list of dimensions");

            uint count = buffer.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                string name = ReadName(buffer);
                uint size = buffer.ReadUInt32();

                if (size == NC_UNLIMITED)
                {
                    RecordDimension.Id = i;
                    RecordDimension.Name = name;
                }

                Dimensions.Add(new NetCDFDimension { Name = name, Size = (int)size });
            }
        }

        private List<NetCDFAttribute> ReadAttributesList(BigEndianReader buffer)
        {
            const uint ZERO = 0;
            const uint NC_ATTRIBUTE = 12;

            var result = new List<NetCDFAttribute>();

            uint tag = buffer.ReadUInt32();
            if (tag == ZERO)
            {
                uint zeroCheck = buffer.ReadUInt32();
                if (zeroCheck != ZERO)
                    throw new FormatException("wrong empty tag for list of attributes");
                return result;
            }

            if (tag != NC_ATTRIBUTE)
                throw new FormatException("wrong tag for list of attributes");

            uint count = buffer.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                string name = ReadName(buffer);
                uint type = buffer.ReadUInt32();
                if (type < 1 || type > 6)
                    throw new FormatException($"non valid type {type}");

                uint size = buffer.ReadUInt32();
                object value = ReadTypedValue(buffer, (NetCDFType)type, (int)size);
                buffer.ApplyPadding();

                result.Add(new NetCDFAttribute { Name = name, Type = (NetCDFType)type, Value = value });
            }

            return result;
        }

        private void ReadVariablesList(BigEndianReader buffer)
        {
            const uint ZERO = 0;
            const uint NC_VARIABLE = 11;

            uint tag = buffer.ReadUInt32();
            int recordStep = 0;

            if (tag == ZERO)
            {
                uint zeroCheck = buffer.ReadUInt32();
                if (zeroCheck != ZERO)
                    throw new FormatException("wrong empty tag for list of variables");
                RecordDimension.RecordStep = 0;
                return;
            }

            if (tag != NC_VARIABLE)
                throw new FormatException("wrong tag for list of variables");

            uint count = buffer.ReadUInt32();
            for (int v = 0; v < count; v++)
            {
                string name = ReadName(buffer);

                uint dimensionality = buffer.ReadUInt32();
                var dimIds = new int[dimensionality];
                for (int d = 0; d < dimensionality; d++)
                    dimIds[d] = (int)buffer.ReadUInt32();

                var attributes = ReadAttributesList(buffer);

                uint type = buffer.ReadUInt32();
                uint varSize = buffer.ReadUInt32();

                long offset = buffer.ReadUInt32();
                if (Version == 2)
                {
                    if (offset > 0)
                        throw new FormatException("offsets larger than 4GB not supported");
                    offset = buffer.ReadUInt32();
                }

                bool isRecord = false;
                if (RecordDimension.Id.HasValue && dimIds.Length > 0 && dimIds[0] == RecordDimension.Id.Value)
                {
                    recordStep += (int)varSize;
                    isRecord = true;
                }

                Variables.Add(new NetCDFVariable
                {
                    Name = name,
                    DimensionIds = dimIds,
                    Attributes = attributes,
                    Type = (NetCDFType)type,
                    Size = (int)varSize,
                    Offset = offset,
                    IsRecord = isRecord
                });
            }

            RecordDimension.RecordStep = recordStep;
        }

        private string ReadName(BigEndianReader buffer)
        {
            uint length = buffer.ReadUInt32();
            string name = buffer.ReadChars((int)length);
            buffer.ApplyPadding();
            return name;
        }

        private static object ReadTypedValue(BigEndianReader buffer, NetCDFType type, int size)
        {
            switch (type)
            {
                case NetCDFType.Byte:
                    return buffer.ReadBytes(size);
                case NetCDFType.Char:
                    return TrimNull(buffer.ReadChars(size));
                case NetCDFType.Short:
                    return ReadNumberArray(size, buffer.ReadInt16);
                case NetCDFType.Int:
                    return ReadNumberArray(size, buffer.ReadInt32);
                case NetCDFType.Float:
                    return ReadNumberArray(size, buffer.ReadFloat32);
                case NetCDFType.Double:
                    return ReadNumberArray(size, buffer.ReadFloat64);
                default:
                    throw new FormatException($"non valid type {type}");
            }
        }

        private static object ReadNumberArray<T>(int size, Func<T> reader)
        {
            if (size == 1) return reader();
            var arr = new T[size];
            for (int i = 0; i < size; i++) arr[i] = reader();
            return arr;
        }

        private static string TrimNull(string value)
        {
            if (value.Length > 0 && value[^1] == '\0')
                return value[..^1];
            return value;
        }

        /// <summary>
        /// Finds a variable by name (case-sensitive, matching the file's variable name exactly).
        /// </summary>
        public NetCDFVariable FindVariable(string name)
        {
            return Variables.Find(v => v.Name == name);
        }

        /// <summary>
        /// Reads the full flat data for a variable as an array of the given numeric type.
        /// Caller is responsible for knowing/handling the variable's dimension shape to
        /// reinterpret the flat array (see NetCDFVariable.DimensionIds + Dimensions).
        /// </summary>
        public T[] GetDataVariable<T>(string variableName) where T : struct
        {
            var variable = FindVariable(variableName);
            if (variable == null)
                throw new ArgumentException($"Variable not found: {variableName}");

            return GetDataVariable<T>(variable);
        }

        public T[] GetDataVariable<T>(NetCDFVariable variable) where T : struct
        {
            var buffer = new BigEndianReader(_data);
            buffer.Seek(variable.Offset);

            if (variable.IsRecord)
                return ReadRecordData<T>(buffer, variable);

            return ReadNonRecordData<T>(buffer, variable);
        }

        private T[] ReadNonRecordData<T>(BigEndianReader buffer, NetCDFVariable variable) where T : struct
        {
            int bytesPerElement = NumTypeBytes(variable.Type);
            int size = variable.Size / bytesPerElement;
            var data = new T[size];

            for (int i = 0; i < size; i++)
                data[i] = ReadSingleValue<T>(buffer, variable.Type);

            return data;
        }

        private T[] ReadRecordData<T>(BigEndianReader buffer, NetCDFVariable variable) where T : struct
        {
            int bytesPerElement = NumTypeBytes(variable.Type);
            int width = variable.Size > 0 ? variable.Size / bytesPerElement : 1;
            int size = RecordDimension.Length;
            int step = RecordDimension.RecordStep;

            if (step == 0)
                throw new FormatException("recordDimension.RecordStep is undefined");

            var data = new T[size * width];
            for (int i = 0; i < size; i++)
            {
                long currentOffset = buffer.Position;
                for (int w = 0; w < width; w++)
                    data[i * width + w] = ReadSingleValue<T>(buffer, variable.Type);
                buffer.Seek(currentOffset + step);
            }

            return data;
        }

        private static T ReadSingleValue<T>(BigEndianReader buffer, NetCDFType type) where T : struct
        {
            object raw = type switch
            {
                NetCDFType.Byte => (object)buffer.ReadByte(),
                NetCDFType.Short => buffer.ReadInt16(),
                NetCDFType.Int => buffer.ReadInt32(),
                NetCDFType.Float => buffer.ReadFloat32(),
                NetCDFType.Double => buffer.ReadFloat64(),
                _ => throw new FormatException($"unsupported data type for numeric read: {type}")
            };

            return (T)Convert.ChangeType(raw, typeof(T));
        }

        private static int NumTypeBytes(NetCDFType type) => type switch
        {
            NetCDFType.Byte => 1,
            NetCDFType.Char => 1,
            NetCDFType.Short => 2,
            NetCDFType.Int => 4,
            NetCDFType.Float => 4,
            NetCDFType.Double => 8,
            _ => throw new FormatException($"unknown type {type}")
        };
    }

    public enum NetCDFType
    {
        Byte = 1,
        Char = 2,
        Short = 3,
        Int = 4,
        Float = 5,
        Double = 6
    }

    public class NetCDFDimension
    {
        public string Name;
        public int Size;
    }

    public class NetCDFAttribute
    {
        public string Name;
        public NetCDFType Type;
        public object Value;
    }

    public class NetCDFVariable
    {
        public string Name;
        public int[] DimensionIds;
        public List<NetCDFAttribute> Attributes;
        public NetCDFType Type;
        public int Size;
        public long Offset;
        public bool IsRecord;
    }

    public class NetCDFRecordDimension
    {
        public int Length;
        public int? Id;
        public string Name;
        public int RecordStep;
    }

    /// <summary>
    /// Minimal big-endian binary reader over a byte[], mirroring the subset of
    /// IOBuffer functionality netcdfjs relies on.
    /// </summary>
    internal class BigEndianReader
    {
        private readonly byte[] _data;
        public long Position { get; private set; }

        public BigEndianReader(byte[] data)
        {
            _data = data;
            Position = 0;
        }

        public void Seek(long offset) => Position = offset;

        public void ApplyPadding()
        {
            long rem = Position % 4;
            if (rem != 0) Position += 4 - rem;
        }

        public byte ReadByte() => _data[Position++];

        public byte[] ReadBytes(int n)
        {
            var result = new byte[n];
            Array.Copy(_data, Position, result, 0, n);
            Position += n;
            return result;
        }

        public string ReadChars(int n)
        {
            var chars = new char[n];
            for (int i = 0; i < n; i++)
                chars[i] = (char)_data[Position + i];
            Position += n;
            return new string(chars);
        }

        public ushort ReadUInt16()
        {
            ushort v = (ushort)((_data[Position] << 8) | _data[Position + 1]);
            Position += 2;
            return v;
        }

        public short ReadInt16()
        {
            short v = (short)((_data[Position] << 8) | _data[Position + 1]);
            Position += 2;
            return v;
        }

        public uint ReadUInt32()
        {
            uint v = ((uint)_data[Position] << 24) | ((uint)_data[Position + 1] << 16) |
                     ((uint)_data[Position + 2] << 8) | _data[Position + 3];
            Position += 4;
            return v;
        }

        public int ReadInt32()
        {
            int v = (_data[Position] << 24) | (_data[Position + 1] << 16) |
                    (_data[Position + 2] << 8) | _data[Position + 3];
            Position += 4;
            return v;
        }

        public float ReadFloat32()
        {
            var bytes = new byte[4];
            for (int i = 0; i < 4; i++) bytes[i] = _data[Position + 3 - i]; // reverse to little-endian for BitConverter
            Position += 4;
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadFloat64()
        {
            var bytes = new byte[8];
            for (int i = 0; i < 8; i++) bytes[i] = _data[Position + 7 - i];
            Position += 8;
            return BitConverter.ToDouble(bytes, 0);
        }
    }
}
