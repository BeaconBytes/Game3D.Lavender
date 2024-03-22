using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.States;
using Lavender.Common.Enums.Types;
using Lavender.Common.Interfaces.Management.Waves;
using Lavender.Server.Managers;

namespace Lavender.Common.Managers;

public partial class WaveManager : LoadableNode
{
    private const float WAVE_STARTUP_TIME = 3f;
    private const float MIN_TICK_TIME = 1f / EnvManager.SERVER_TICK_RATE;
    
    public void Setup(GameManager gameManager)
    {
        Manager = gameManager;
        CurrentWaveState = WaveState.Stopped;

        Manager.EntitySpawnedEvent += OnSpawnedEntity;
        Manager.EntityDestroyedEvent += OnDestroyedEntity;
    }

    
    private void OnSpawnedEntity(IGameEntity gameEntity)
    {
        if (gameEntity is BoomerEntity boomerEntity)
        {
            boomerEntity.OnCompletedPathEvent += OnCompletedBotPath;
        }
    }
    private void OnDestroyedEntity(IGameEntity gameEntity)
    {
        if (gameEntity is BoomerEntity boomerEntity)
        {
            boomerEntity.OnCompletedPathEvent -= OnCompletedBotPath;
        }
    }
    private void OnCompletedBotPath(BoomerEntity boomerEntity)
    {
        Manager.DestroyEntity(boomerEntity);
    }
    


    protected override void Unload()
    {
        if (Manager == null)
            return;
        
        foreach (KeyValuePair<uint,LivingEntity> pair in _spawnedEnemies)
        {
            Manager.DestroyEntity(pair.Value);
        }
    }

    
    protected virtual void HandleTick()
    {
        if (CurrentWaveState == WaveState.Starting)
        {
            if (_waveStartupCooldown > 0)
            {
                _waveStartupCooldown -= MIN_TICK_TIME;
                if (_waveStartupCooldown <= 0)
                {
                    // STARTUP COUNTDOWN FINISHED!
                    // Start the wave.
                    SetWaveState(WaveState.Active);
                }
            }
        }
        else if (CurrentWaveState == WaveState.Active)
        {
            // Wave currently running/active
            
            // If there are enemies left to spawn, AND its been at least 4 seconds(equivalently, in ticks) then:
            if (_enemiesToSpawnCount > 0 && _currentTick % (EnvManager.SERVER_TICK_RATE * 4f) == 0)
            {
                BoomerEntity boomerEntity = Manager.SpawnEntity<BoomerEntity>(EntityType.Boomer);
                _spawnedEnemies.Add(boomerEntity.NetId, boomerEntity);
                
                // TODO: Better way to convert botPathPoints into a System Array
                boomerEntity.WaveSetup(new List<Marker3D>(botPathPoints).ToArray(), this);
                
                _enemiesToSpawnCount--;
            }
            else if (_enemiesToSpawnCount <= 0 && _spawnedEnemies.Count == 0)
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
            foreach (KeyValuePair<uint,LivingEntity> pair in _spawnedEnemies)
            {
                Manager.DestroyEntity(pair.Value);
            }
            newState = WaveState.Stopped;
        }
        else if (newState is WaveState.Starting && CurrentWaveState is WaveState.Stopped or WaveState.Finished)
        {
            // Start the wave countdown to enemies arriving
            _waveStartupCooldown = WAVE_STARTUP_TIME;
        }
        else if (newState is WaveState.Active && CurrentWaveState is WaveState.Starting)
        {
            // Wave countdown completed. Start spawning enemies!
            // TODO: Spawn enemies
            _waveStartupCooldown = -1f;
            
            _currentLevel++;
            _enemiesToSpawnCount = 1;
        }
        else
        {
            GD.PrintErr($"[ERROR]WaveManager.SetWaveState({newState.ToString()}); where `CurrentWaveStart`='{CurrentWaveState.ToString()}' was called in an incorrect order!");
        }
        
        GD.Print($"WaveManager.SetWaveState({newState.ToString()}); where `CurrentWaveStart`='{CurrentWaveState.ToString()}'");
        CurrentWaveState = newState;
        
        WaveStateChanagedEvent?.Invoke(this);
    }

    public override void _Process(double delta)
    {
        if (Manager is not ServerManager)
            return;
        
        _deltaTimer += (float)delta;

        while (_deltaTimer >= MIN_TICK_TIME)
        {
            _deltaTimer -= MIN_TICK_TIME;
            HandleTick();
            _currentTick++;
        }
    }

    private float _waveStartupCooldown = WAVE_STARTUP_TIME;

    public WaveState CurrentWaveState { get; protected set; } = WaveState.Stopped;

    private float _deltaTimer = 0;
    private uint _currentTick = 0;

    private uint _currentLevel = 0;
    private uint _enemiesToSpawnCount = 0;
    
    private Dictionary<uint, LivingEntity> _spawnedEnemies = new();
    public GameManager Manager { get; protected set; }


    [Export]
    private Godot.Collections.Array<Marker3D> botPathPoints;
    
    
    // EVENT HANDLERS //
    public delegate void WaveStateChangedHandler(WaveManager waveManager);
    
    
    // EVENTS //
    public event WaveStateChangedHandler WaveStateChanagedEvent;
}