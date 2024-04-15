using Godot;
using Lavender.Common.Interfaces.Management.Waves;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity;

public partial class EnemyEntity : BrainEntity, IWaveEnemy
{
    // Called immediately after spawning the entity but before any logic(or children) are processed
    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        OnCompletedPathEvent += OnCompletedPath;
    }

    public virtual void SetupWave(Marker3D[] botPathPoints, WaveManager waveManager)
    {
        WaveManager = waveManager;
        _botPathPointsCache = botPathPoints;

        Teleport(_botPathPointsCache[0].GlobalPosition);
        _targetedPointIndex = 0;
        TargetNextPoint();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        OnCompletedPathEvent -= OnCompletedPath;
    }

    private void TargetNextPoint()
    {
        if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
        {
            return;
        }

        _targetedPointIndex++;
        SetDesiredPathLocation(_botPathPointsCache[_targetedPointIndex].GlobalPosition);
    }
    
    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;

        if (IsClient) 
            return;
        
        if (NavAgent.IsTargetReached())
        {
            // Reached the targeted path position/point
            if (_targetedPointIndex + 1 >= _botPathPointsCache.Length)
            {
                // Finished the path.
                TriggerPathCompletedEvent();
            }
            TargetNextPoint();
        }

    }

    
    // EVENT HANDLERS //
    private void OnCompletedPath(BrainEntity brainEntity)
    {
        Enabled = false;
    }
    

    private int _targetedPointIndex = 1;
    
    private Vector3 _targetedLerpPosition = Vector3.Zero;

    public WaveManager WaveManager { get; protected set; }
    private Marker3D[] _botPathPointsCache;
}