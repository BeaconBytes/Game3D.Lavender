using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Controllers.Enemies;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.States;
using Lavender.Common.Enums.Types;
using Lavender.Common.Networking.Packets.Variants.Mapping;
using Lavender.Server.Managers;

namespace Lavender.Common.Managers.World;

public partial class WaveManager : LoadableNode
{
    private const float WAVE_STARTUP_TIME = 8f;

    public void Setup(GameManager gameManager)
    {
        Manager = gameManager;
        CurrentWaveState = WaveState.Stopped;

        Manager.NodeSpawnedEvent += OnSpawnedNode;
        Manager.NodeDestroyedEvent += OnDestroyedNode;
    }


    protected override void Unload()
    {
        if (Manager == null)
            return;

        Manager.NodeSpawnedEvent -= OnSpawnedNode;
        Manager.NodeDestroyedEvent -= OnDestroyedNode;

        foreach (KeyValuePair<uint, BasicControllerBase> pair in _spawnedEnemyControllers)
        {
            Manager.DestroyNode(pair.Value);
        }
    }

    private void OnCompletedBotPath(BrainControllerBase brainController)
    {
        Manager.DestroyNode(brainController);
    }


    protected virtual void HandleTick()
    {
        if (CurrentWaveState is WaveState.Stopped)
        {
            SetWaveState(WaveState.Starting);
        }
        else if (CurrentWaveState == WaveState.Starting)
        {
            if (_waveStartupCooldown > 0)
            {
                _waveStartupCooldown -= GameManager.NET_TICK_TIME;
                if (_waveStartupCooldown <= 0)
                {
                    // STARTUP COUNTDOWN FINISHED!
                    // Start the wave.
                    SetWaveState(WaveState.Active);
                }
                else if(Manager.IsServer)
                {
                    int curCountdownSecond = Mathf.FloorToInt(_waveStartupCooldown);
                    if (curCountdownSecond < _lastNotifiedCountdownSecond)
                    {
                        _lastNotifiedCountdownSecond = curCountdownSecond;
                        Manager.BroadcastPacketToClients(new MapNotificationPacket()
                        {
                            Message = $"Starting in {curCountdownSecond}",
                            TimeLengthSeconds = 1f,
                        });
                    }
                }
            }
        }
        else if (CurrentWaveState == WaveState.Active)
        {
            // Wave currently running/active

            // If there are enemies left to spawn, AND its been at least 4 seconds(equivalently, in ticks) then:
            if (_enemiesToSpawnCount > 0 && (_currentTick % (GameManager.SERVER_TICK_RATE * 4f) == 0 || _spawnedEnemyControllers.Count < 1))
            {
                BuddyController enemyController = Manager.SpawnController<BuddyController>(ControllerType.Buddy);
                _spawnedEnemyControllers.Add(enemyController.NetId, enemyController);

                // TODO: Better way to convert botPathPoints into a System Array
                //enemyController.SetupWave(new List<Marker3D>(botPathPoints).ToArray(), this);

                _enemiesToSpawnCount--;
            }
            else if (_enemiesToSpawnCount <= 0 && _spawnedEnemyControllers.Count == 0)
            {
                SetWaveState(WaveState.Finished);
            }
        }
    }


    public void StartWave()
    {
        if (CurrentWaveState == WaveState.Stopped)
            SetWaveState(WaveState.Starting);
        else if (CurrentWaveState == WaveState.Starting)
            SetWaveState(WaveState.Active);
        else
            throw new Exception("Invalid call order for WaveState in WaveManager#StartWave()");
    }

    public void StopWave()
    {
        SetWaveState(WaveState.Stopped);
    }

    private void SetWaveState(WaveState newState)
    {
        if (newState == CurrentWaveState)
            return;

        if (newState is WaveState.Stopped or WaveState.Finished && CurrentWaveState == WaveState.Active)
        {
            // Stop the wave
            foreach (KeyValuePair<uint, BasicControllerBase> pair in _spawnedEnemyControllers)
            {
                Manager.DestroyNode(pair.Value);
            }
            newState = WaveState.Stopped;
        }
        else if (newState is WaveState.Starting && CurrentWaveState is WaveState.Stopped or WaveState.Finished)
        {
            // Start the wave countdown to enemies arriving
            _waveStartupCooldown = WAVE_STARTUP_TIME;
            _lastNotifiedCountdownSecond = Mathf.CeilToInt(WAVE_STARTUP_TIME) + 1;
        }
        else if (newState is WaveState.Active && CurrentWaveState is WaveState.Starting)
        {
            // Wave countdown completed. Start spawning enemies!
            _waveStartupCooldown = -1f;

            _currentLevel++;
            _enemiesToSpawnCount = _currentLevel * 12;
        }
        else
        {
            GD.PrintErr($"[ERROR]WaveManager.SetWaveState({newState.ToString()}); where `CurrentWaveStart`='{CurrentWaveState.ToString()}' was called in an incorrect order!");
        }

        GD.Print($"WaveManager.SetWaveState({newState.ToString()}); where `CurrentWaveStart`='{CurrentWaveState.ToString()}'");
        CurrentWaveState = newState;

        WaveStateChangedEvent?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        if (Manager is not ServerManager)
            return;

        _deltaTimer += (float)delta;

        while (_deltaTimer >= GameManager.NET_TICK_TIME)
        {
            _deltaTimer -= GameManager.NET_TICK_TIME;
            HandleTick();
            _currentTick++;
        }
    }

    private void OnSpawnedNode(INetNode target)
    {
        if (target is EnemyEntityBase enemyEntity)
        {
            if (enemyEntity.GetMasterController() is BrainControllerBase brainController)
            {
                brainController.OnCompletedPathEvent += OnCompletedBotPath;
            }
        }
    }
    private void OnDestroyedNode(INetNode target)
    {
        if (target is EnemyEntityBase enemyEntity)
        {
            enemyEntity.OnCompletedPathEvent -= OnCompletedBotPath;
        }
    }

    private double _waveStartupCooldown = WAVE_STARTUP_TIME;

    public WaveState CurrentWaveState { get; protected set; } = WaveState.Stopped;

    private double _deltaTimer = 0;
    private uint _currentTick = 0;

    private uint _currentLevel = 0;
    private uint _enemiesToSpawnCount = 0;
    
    
    private int _lastNotifiedCountdownSecond = 0;

    private Dictionary<uint, BasicControllerBase> _spawnedEnemyControllers = new();
    public GameManager Manager { get; protected set; }


    [Export]
    private Godot.Collections.Array<Marker3D> botPathPoints;


    // EVENT HANDLERS //
    public delegate void WaveStateChangedHandler(WaveManager waveManager);
    

    // EVENTS //
    public event WaveStateChangedHandler WaveStateChangedEvent;
}