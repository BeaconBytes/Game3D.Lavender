using System;
using Godot;
using Lavender.Common.Voxels;

namespace Lavender.Common.Mapping;

public partial class GameMap : LoadableNode
{
    public Marker3D GetRandomPlayerSpawnPoint()
    {
        if (_playerSpawnPoints.Length == 0)
        {
            throw new Exception("_playerSpawnPoints is empty!");
        }
        int val = Math.Abs((int)GD.Randi()) % _playerSpawnPoints.Length;
        return _playerSpawnPoints[val];
    }

    [Export]
    private Marker3D[] _playerSpawnPoints;

    public VoxelWorld VoxelWorld { get; protected set; }
}