using Godot;
using Lavender.Common.Entity.Variants;

namespace Lavender.Common.Managers.Variants.Map;

public partial class DebugMapManager : MapManager
{
    protected override void Initialize()
    {
        base.Initialize();
    }

    private void OnDeathBoxTriggered(Node3D body)
    {
        if (body is PlayerEntity playerEntity)
        {
            playerEntity.Teleport(GetRandomPlayerSpawnPoint());
        }
    }
}