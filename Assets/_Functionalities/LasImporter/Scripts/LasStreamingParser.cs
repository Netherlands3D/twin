using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Streaming LAS reader for uncompressed LAS (not LAZ).
/// You create it once from a byte[], it keeps an internal BinaryReader,
/// and you call ReadNextPoints(count) to get the next chunk.
/// </summary>
public class LasStreamingParser : IDisposable
{
    public class LasHeader
    {
        public byte pointDataFormat;
        public ushort pointDataRecordLength;
        public uint numberOfPointRecords;
        public double xScale, yScale, zScale;
        public double xOffset, yOffset, zOffset;
        public uint offsetToPointData;
    }

    public struct LasPoint
    {
        public Vector3 position;
        public Color32 color;
    }

    private MemoryStream _ms;
    private BinaryReader _br;
    private LasHeader _header;
    private uint _pointsRead;

    public LasHeader Header => _header;
    public uint PointsRead => _pointsRead;
    public bool Finished => _pointsRead >= _header.numberOfPointRecords;

    public LasStreamingParser(byte[] lasBytes)
    {
        _ms = new MemoryStream(lasBytes);
        _br = new BinaryReader(_ms);
        _header = ReadHeader(_br);

        // jump to point data
        _br.BaseStream.Seek(_header.offsetToPointData, SeekOrigin.Begin);
        _pointsRead = 0;
    }

    public List<LasPoint> ReadNextPoints(int maxToRead)
    {
        var result = new List<LasPoint>(maxToRead);

        int consumedBase = 20; // base bytes for formats 0–3

        for (int i = 0; i < maxToRead; i++)
        {
            if (_pointsRead >= _header.numberOfPointRecords)
                break;

            // --- XYZ ---
            int X = _br.ReadInt32();
            int Y = _br.ReadInt32();
            int Z = _br.ReadInt32();

            float px = (float)(X * _header.xScale + _header.xOffset);
            float py = (float)(Y * _header.yScale + _header.yOffset);
            float pz = (float)(Z * _header.zScale + _header.zOffset);

            ushort intensity = _br.ReadUInt16();
            byte flags = _br.ReadByte();
            byte classification = _br.ReadByte();
            byte scanAngle = _br.ReadByte();
            byte userData = _br.ReadByte();
            ushort pointSourceId = _br.ReadUInt16();

            // optional
            double gpsTime = 0;
            ushort r = 255, g = 255, b = 255;

            int consumed = consumedBase;

            if (_header.pointDataFormat == 1 || _header.pointDataFormat == 3)
            {
                gpsTime = _br.ReadDouble();
                consumed += 8;
            }

            if (_header.pointDataFormat == 2 || _header.pointDataFormat == 3)
            {
                r = _br.ReadUInt16();
                g = _br.ReadUInt16();
                b = _br.ReadUInt16();
                consumed += 6;
            }

            // skip any extra bytes in the record
            int toSkip = _header.pointDataRecordLength - consumed;
            if (toSkip > 0)
                _br.BaseStream.Seek(toSkip, SeekOrigin.Current);

            // convert colors
            byte R8 = (byte)Mathf.Clamp(r / 256, 0, 255);
            byte G8 = (byte)Mathf.Clamp(g / 256, 0, 255);
            byte B8 = (byte)Mathf.Clamp(b / 256, 0, 255);

            result.Add(new LasPoint
            {
                // swap Y/Z to stand upright in Unity
                position = new Vector3(px, pz, py),
                color = new Color32(R8, G8, B8, 255)
            });

            _pointsRead++;
        }

        return result;
    }

    private LasHeader ReadHeader(BinaryReader br)
    {
        string sig = new string(br.ReadChars(4));
        if (sig != "LASF")
            throw new Exception("Not a LAS file");

        br.BaseStream.Seek(94, SeekOrigin.Begin);
        ushort headerSize = br.ReadUInt16();
        uint offsetToPointData = br.ReadUInt32();
        uint numVLRecords = br.ReadUInt32();
        byte pointDataFormat = br.ReadByte();
        ushort pointDataRecordLength = br.ReadUInt16();
        uint numberOfPointRecords = br.ReadUInt32();

        // skip number of points by return (we don't need detail)
        br.BaseStream.Seek(20, SeekOrigin.Current);

        double xScale = br.ReadDouble();
        double yScale = br.ReadDouble();
        double zScale = br.ReadDouble();
        double xOffset = br.ReadDouble();
        double yOffset = br.ReadDouble();
        double zOffset = br.ReadDouble();

        return new LasHeader
        {
            pointDataFormat = pointDataFormat,
            pointDataRecordLength = pointDataRecordLength,
            numberOfPointRecords = numberOfPointRecords,
            xScale = xScale,
            yScale = yScale,
            zScale = zScale,
            xOffset = xOffset,
            yOffset = yOffset,
            zOffset = zOffset,
            offsetToPointData = offsetToPointData
        };
    }

    public void Dispose()
    {
        _br?.Dispose();
        _ms?.Dispose();
    }
}
