using System;
using Godot;
using VoxelWorld = Lavender.Common.Voxels.VoxelWorld;

namespace Lavender.Common.Managers;

public partial class MapManager : LoadableNode
{
	protected override void Load()
	{
		base.Load();
		
	}

	public Marker3D GetRandomPlayerSpawnPoint()
	{
		if (_playerSpawnPoints.Count == 0)
		{
			throw new Exception("_playerSpawnPoints is empty!");
			// return new Vector3(0f, 1f, 0f);
		}
		int val = Math.Abs((int)GD.Randi()) % _playerSpawnPoints.Count;
		return _playerSpawnPoints[val];
	}

	[Export]
	private Godot.Collections.Array<Marker3D> _playerSpawnPoints;

	public VoxelWorld VoxelWorld { get; protected set; }
}
