using System;
using Godot;
using Lavender.Common.Worlds;

namespace Lavender.Common.Managers;

public partial class MapManager : LoadableNode
{
	protected override void Load()
	{
		base.Load();
		MapWorld = new MapWorld();
		AddChild(MapWorld);
	}

	public Vector3 GetRandomPlayerSpawnPoint()
	{
		if (_playerSpawnPoints.Count == 0)
		{
			throw new Exception("_playerSpawnPoints is empty!");
			// return new Vector3(0f, 1f, 0f);
		}
		int val = Math.Abs((int)GD.Randi()) % _playerSpawnPoints.Count;
		return _playerSpawnPoints[val].GlobalPosition;
	}

	[Export]
	private Godot.Collections.Array<Node3D> _playerSpawnPoints;

	public MapWorld MapWorld { get; protected set; }
}
