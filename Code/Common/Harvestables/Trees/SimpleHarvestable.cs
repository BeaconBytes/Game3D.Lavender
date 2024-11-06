using Godot;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Mapping;

namespace Lavender.Common.Harvestables.Trees;

public partial class SimpleHarvestable : Node3D, IHarvestableNode
{
    [Export]
    public bool AllowRespawning { get; set; } = true;
    [Export]
    public float MaxRespawnTimeSecs { get; protected set; } = 360f;
    public float RespawnTimerSecs { get; protected set; } = 0f;
    private bool _tickRespawning = false;

    [Export]
    public Node MeshRootNode { get; private set; }
    [Export]
    public CollisionObject3D ColliderRootNode { get; private set; }

    public uint NetId { get; private set; } = (uint)StaticNetId.Null;
    public GameMap Map { get; private set; }

    public void Setup(uint netId, GameMap map)
    {
        NetId = netId;
        Map = map;
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_tickRespawning)
        {
            RespawnTimerSecs -= (float)delta;
            if (RespawnTimerSecs < 0)
            {
                _tickRespawning = false;
                SetHidden(true);
            }
        }
    }

    public void SetHidden(bool isHidden)
    {
        
    }
}