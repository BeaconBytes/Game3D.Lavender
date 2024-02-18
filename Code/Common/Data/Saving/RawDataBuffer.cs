using System;
using System.Collections.Generic;
using Godot;

namespace Lavender.Common.Data.Saving;

public class RawDataBuffer
{
    public RawDataBuffer()
    {
        
    }

    public RawDataBuffer(byte[] data)
    {
        _buffer = new List<byte>(data);
    }
    
    private List<byte> _buffer = new();
    private int _readPos = 0;

    public void ResetRead()
    {
        _readPos = 0;
    }

    public void Clear()
    {
        _buffer.Clear();
        ResetRead();
    }

    public byte[] ReadAll()
    {
        return _buffer.ToArray();
    }


    public void Write(byte[] values)
    {
        _buffer.AddRange(values);
    }

    public void Write(int value)
    {
        Write(BitConverter.GetBytes(value));
    }
    public void Write(float value)
    {
        Write(BitConverter.GetBytes(value));
    }

    public void Write(Vector3 value)
    {
        Write(BitConverter.GetBytes(value.X));
        Write(BitConverter.GetBytes(value.Y));
        Write(BitConverter.GetBytes(value.Z));
    }

    public void Write(Vector3I value)
    {
        Write(BitConverter.GetBytes(value.X));
        Write(BitConverter.GetBytes(value.Y));
        Write(BitConverter.GetBytes(value.Z));
    }

    public void Write(bool value)
    {
        Write(BitConverter.GetBytes(value));
    }

    public byte[] ReadBytes(int count)
    {
        byte[] val = _buffer.GetRange(_readPos, count).ToArray();
        _readPos += count;
        return val;
    }

    public int ReadInt()
    {
        return BitConverter.ToInt32(ReadBytes(4));
    }
    public float ReadFloat()
    {
        return BitConverter.ToSingle(ReadBytes(4));
    }

    public Vector3 ReadVector3()
    {
        float x = ReadFloat();
        float y = ReadFloat();
        float z = ReadFloat();

        return new Vector3(x, y, z);
    }

    public Vector3I ReadVector3I()
    {
        int x = ReadInt();
        int y = ReadInt();
        int z = ReadInt();

        return new Vector3I(x, y, z);
    }

    public bool ReadBool()
    {
        return BitConverter.ToBoolean(ReadBytes(1));
    }
}