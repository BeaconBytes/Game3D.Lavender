using System;
using Godot;
using VoxelWorld = Lavender.Common.Voxels.VoxelWorld;

namespace Lavender.Common.Managers;

public partial class MapManager : LoadableNode
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
