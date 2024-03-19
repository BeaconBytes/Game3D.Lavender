using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Lavender.Common.Managers;

namespace Lavender.Common.Data.Saving.Mapping;

public class VolumetricNavSave
{
    private VolumetricNavSave(string filePath)
    {
        GD.Print($"Loading NavSave from path '{filePath}'");
        byte[] readBytes = File.ReadAllBytes(filePath);
        _dataBuffer = new RawDataBuffer(readBytes);
        _origin = _dataBuffer.ReadVector3I();
        _size = _dataBuffer.ReadVector3I();
        int unwalkableEntryCount = _dataBuffer.ReadInt();
        List<Vector3I> unwalkablesList = new();

        for (int i = 0; i < unwalkableEntryCount; i++)
        {
            unwalkablesList.Add(_dataBuffer.ReadVector3I());
        }

        _pointsMap = new bool[_size.X,_size.Y,_size.Z];
        _offsets = new Vector3I(Mathf.RoundToInt(_size.X / 2f), Mathf.RoundToInt(_size.Y / 2f), Mathf.RoundToInt(_size.Z / 2f));

        _minMapPos = new Vector3I(_origin.X - _offsets.X, _origin.Y - _offsets.Y, _origin.Z - _offsets.Z);
        _maxMapPos = new Vector3I(_origin.X + _offsets.X, _origin.Y + _offsets.Y, _origin.Z + _offsets.Z);

        for (int x = 0; x < _size.X; x++)
        {
            for (int y = 0; y < _size.Y; y++)
            {
                for (int z = 0; z < _size.Z; z++)
                {
                    Vector3I searchPos = new Vector3I(x - _offsets.X, y - _offsets.Y, z - _offsets.Z);
                    _pointsMap[x,y,z] = !(unwalkablesList.Contains(searchPos));
                }
            }
        }
        
        GD.Print($"PointsMap loaded with length of {_pointsMap.Length}");
    }

    public void AddUnwalkablePoint(Vector3I point)
    {
        _unwalkablesBuffer.Add(point);
    }

    public void SetupPathManager(PathManager pathManager)
    {
        ulong curId = 0;
        // Add the points
        for (int x = 0; x < _size.X; x++)
        {
            for (int y = 0; y < _size.Y; y++)
            {
                for (int z = 0; z < _size.Z; z++)
                {
                    // If this point isn't walkable, skip it.
                    if (!_pointsMap[x, y, z])
                        continue;
                    
                    Vector3I searchPos = new Vector3I(x - _offsets.X, y - _offsets.Y, z - _offsets.Z);
                    pathManager.AddNode(curId, searchPos);
                    
                    curId++;
                }
            }
        }

        long pathConnections = 0;
        // Connect the now-entered points
        for (int x = 0; x < _size.X; x++)
        {
            for (int y = 0; y < _size.Y; y++)
            {
                for (int z = 0; z < _size.Z; z++)
                {
                    // If this point isn't walkable, skip it.
                    if (!_pointsMap[x, y, z])
                        continue;
                    
                    Vector3I searchPos = new Vector3I(x - _offsets.X, y - _offsets.Y, z - _offsets.Z);
                    curId = pathManager.GetIdFromPosition(searchPos);
                    if (curId != 0)
                    {
                        for (int mX = -1; mX < 1; mX++)
                        {
                            for (int mY = -1; mY < 1; mY++)
                            {
                                for (int mZ = -1; mZ < 1; mZ++)
                                {
                                    
                                    if (mX == 0 && mY == 0 && mZ == 0)
                                        continue;
                                    if (mX + x >= _size.X || mY + y >= _size.Y || mZ + z >= _size.Z)
                                        continue;
                                    if (mX + x < 0 || mY + y < 0 || mZ + z < 0)
                                        continue;
                                    // If this point isn't walkable, skip it.
                                    if (!_pointsMap[x + mX, y + mY, z + mZ])
                                        continue;
                                    Vector3I selectedPos = new Vector3I(mX + searchPos.X, mY + searchPos.Y, mZ + searchPos.Z);
                                    // if (cachedPoints.TryGetValue(selectedPos, out long selectedId))
                                    ulong selectedId = pathManager.GetIdFromPosition(selectedPos);
                                    if(selectedId != 0)
                                    {
                                        if (curId != selectedId && !pathManager.ArePointsConnected(curId, selectedId))
                                        {
                                            pathManager.AddConnection(curId, selectedId);
                                            pathConnections++;
                                        }
                                    }
                                    
                                    
                                }
                            }
                        }
                        
                        
                    }
                }
            }
        }
        
        GD.Print($"Setup Pathfinder! ({pathConnections} pathConnections)");
    }

    public VolumetricNavSave(Vector3I origin, Vector3I size)
    {
        _dataBuffer = new RawDataBuffer();
        _origin = origin;
        _size = size;
    }

    public void SaveToFile(string filePath)
    {
        _dataBuffer.Clear();
        
        _dataBuffer.Write(_origin);
        _dataBuffer.Write(_size);
        _dataBuffer.Write(_unwalkablesBuffer.Count);

        for (int i = 0; i < _unwalkablesBuffer.Count; i++)
        {
            _dataBuffer.Write(_unwalkablesBuffer[i]);
        }

        string dir = new FileInfo(filePath).Directory.FullName;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(filePath, _dataBuffer.ReadAll());
    }
    
    public static VolumetricNavSave FromFile(string filePath)
    {
        return new VolumetricNavSave(filePath);
    }
    
    private readonly RawDataBuffer _dataBuffer;

    private List<Vector3I> _unwalkablesBuffer = new();

    private readonly Vector3I _origin;
    private readonly Vector3I _size;

    private Vector3I _offsets;
    private Vector3I _minMapPos;
    private Vector3I _maxMapPos;
    
    
    private bool[,,] _pointsMap = null;

}