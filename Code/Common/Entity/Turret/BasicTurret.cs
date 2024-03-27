using Godot;
using Lavender.Common.Entity.Turret.Data;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity.Turret;

public partial class BasicTurret : BasicEntity
{
    public virtual void SetupWave(WaveManager waveManager)
    {
        WaveManager = waveManager;
    }

    protected override void HandleTick()
    {
        base.HandleTick();
        
    }

    [Export]
    public Node3D TurretHeadRoot;


    public TurretStats TurretStats { get; protected set; } = new();
    public WaveManager WaveManager { get; private set; }
}