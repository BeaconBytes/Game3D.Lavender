using System;
using Godot;
using Lavender.Common.Data.Saving.Mapping;
using Lavender.Common.Entity.Data;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Utils;
using Lavender.Server.Managers;

namespace Lavender.Common.Entity;

public partial class FlyingBrainEntity : LivingEntity
{

    public override void _Ready()
    {
        base._Ready();
        _currentPathPoints = null;
    }


    protected override void HandleTick()
    {
        base.HandleTick();

        if (Manager.IsClient)
        {
            if (LatestServerState.Equals(default(StatePayload)) ||
                (!LastProcessedState.Equals(default(StatePayload)) && LatestServerState.Equals(LastProcessedState)))
            {
                HandleServerReconciliation();
            }
            
            LastProcessedState = LatestServerState;
            GlobalPosition = LatestServerState.position;
            GlobalRotation = LatestServerState.rotation;
        }
        else
        {
            Vector3 moveDirVec = DoAiMovementLogic();

            InputPayload inputPayload = new InputPayload()
            {
                tick = CurrentTick,
                lookInput = new Vector3(0f, 0f, 0f),
                moveInput = moveDirVec,
                flagsInput = EntityMoveFlags.None,
            };
            uint bufferIndex = inputPayload.tick % BUFFER_SIZE;

            StatePayload statePayload = ProcessMovement(inputPayload);
            StateBuffer[bufferIndex] = statePayload;
            
            Manager.BroadcastPacketToClients(new EntityStatePayloadPacket()
            {
                NetId = NetId,
                StatePayload = StateBuffer[bufferIndex],
            });
        }
    }

    /// <summary>
    /// Process AI movement and return a Vector3 representing normalized inputs on x,y,z axis
    /// </summary>
    protected virtual Vector3 DoAiMovementLogic()
    {
        Vector3 curPos = GlobalPosition;

        if (_currentPathPoints == null || _currentPathPoints.Length == 0)
        {
            return Vector3.Zero;
        }
        Vector3 targetPathPos = _currentPathPoints[_currentPointIndex];
        float nextStepDistance = MathUtils.Distance(curPos, targetPathPos);
        
        if (nextStepDistance <= CollisionSize)
        {
            _currentPointIndex++;
            
            if (_currentPointIndex >= _currentPathPoints.Length)
            {
                _currentTargetPos = GlobalPosition;
                _currentPathPoints = null;

                OnReachedGoal();
                
                return Vector3.Zero;
            }
            targetPathPos = _currentPathPoints[_currentPointIndex];
        }
        
        return (targetPathPos - curPos).Normalized();
    }

    public PlayerEntity SetTargetToRandomPlayer()
    {
        PlayerEntity[] players = Manager.GetPlayers();
        if (players == null)
            return null;

        PlayerEntity tarPlr = players[GD.Randi() % players.Length];
        SetDesiredPathLocation(tarPlr.GlobalPosition);
        
        GD.Print($"Targeting random player with id of {tarPlr.NetId}");

        return tarPlr;
    }

    public void SetDesiredPathLocation(Vector3 pos)
    {
        if (_currentTargetPos == pos)
        {
            GD.PrintErr("SetDesiredPathLocation(): Already at path pos");
            return;
        }
        _currentPathPoints = Manager.PathManager.GetPathPoints(GlobalPosition, pos);

        if (_currentPathPoints == null)
        {
            GD.PrintErr("Failed finding path");
            return;
        }
        
        _currentTargetPos = pos;
        _currentPointIndex = 0;
        
        if(_currentPathPoints.Length > 0)
            GD.Print($"Pathing from ({_currentPathPoints[0]}) to ({_currentPathPoints[_currentPathPoints.Length-1]}) through {_currentPathPoints.Length} nodes.");
    }

    public void StopPathing()
    {
        _currentPathPoints = null;
        _currentTargetPos = null;
    }

    protected virtual void OnReachedGoal() { }

    /// <summary>
    /// Increases the currently selected PathPoint by 1
    /// </summary>
    /// <returns>Returns true if successful, false if not</returns>
    private bool IncrementCurrentPathPoint()
    {
        if (_currentPathPoints == null || _currentPathPoints.Length == 0)
            return false;
        _currentPointIndex++;
        if (_currentPointIndex >= _currentPathPoints.Length)
        {
            _currentPathPoints = null;
            _currentPointIndex = -1;
            return false;
        }

        return true;
    }
    
    private int _currentPointIndex = -1;
    private Vector3[] _currentPathPoints;
    private Vector3? _currentTargetPos;

    protected float CollisionSize = 1.5f;

    public bool HasPath => _currentPathPoints != null;
    
    /// <summary>
    /// Gets current number of pathfinding points. Returns -1 if CurrentPathPoints is null/unset
    /// </summary>
    public int PathPointsCount => _currentPathPoints?.Length ?? -1;

    /// <summary>
    /// Get the currently targeted path point. Returns null if CurrentPathPoints is null/unset
    /// </summary>
    public Vector3? CurrentPathPoint => _currentPathPoints?[_currentPointIndex] ?? null;

    public Vector3? CurrentTargetPosition => _currentTargetPos;
}