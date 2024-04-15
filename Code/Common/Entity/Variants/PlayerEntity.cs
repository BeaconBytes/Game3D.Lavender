using Godot;
using Lavender.Client.Menus;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity.Variants;

public partial class PlayerEntity : HumanoidEntity
{
    public override void _Ready()
    {
        base._Ready();

        EnableAutoMoveSlide = true;
        
        _pauseMenuRootNode.Visible = false;
        
        if (IsClient)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            Register.Packets.Subscribe<ForceSyncEntityPacket>(OnForceSyncEntityPacket);
        }
        else
        {
            Manager.SendPacketToClient(new ForceSyncEntityPacket()
            {
                CurrentTick = CurrentTick,
                CurrentPos = GlobalPosition,
                CurrentRotation = GetRotationWithHead(),
            }, this);
        }
    }

    public override void Setup(uint netId, GameManager manager)
    {
        base.Setup(netId, manager);
        if (_clientHudRootNode == null)
        {
            GD.PrintErr($"[PlayerEntity#Setup]: _clientHudRootNode is not set in the editor!");
            return;
        }

        _clientHudRootNode.Setup(this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Manager.IsServer)
            return;

        if (Manager.ClientNetId != NetId)
        {
            GlobalPosition = GlobalPosition.Lerp(_targetedLerpPosition, GetMoveSpeed() * (float)delta);
        }
        
        HandleMovementInputs();

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

        if (Input.IsActionJustPressed("debug_action"))
        {
            Manager.SendPacketToServer(new DebugActionPacket()
            {
                Message = "debug",
                Augment = 0,
            });
        }
    }

    protected override void HandleTick()
    {
        base.HandleTick();

        if (!Enabled)
            return;
        
        if (Manager.IsClient)
        {
            if (Manager.ClientNetId == NetId)
            {
                if (!LatestServerState.Equals(default(StatePayload)) && 
                    (LastProcessedState.Equals(default(StatePayload)) || !LatestServerState.Equals(LastProcessedState)))
                {
                    HandleServerReconciliation();
                }
                
                uint bufferIndex = CurrentTick % GameManager.NET_BUFFER_SIZE;

                Vector3 realMoveDirection = _moveInput.Rotated( Vector3.Up, GlobalTransform.Basis.GetEuler( ).Y ).Normalized( );

                if (IsControlsFrozen && !_flagsInput.HasFlag(EntityMoveFlags.Frozen))
                {
                    _flagsInput |= EntityMoveFlags.Frozen;
                }
                
                InputPayload inputPayload = new()
                {
                    tick  = CurrentTick,
                    moveInput = realMoveDirection,
                    lookInput = _lookInput,
                    flagsInput = _flagsInput,
                };
                _lookInput = Vector3.Zero;
                
                InputBuffer[bufferIndex] = inputPayload;
                StateBuffer[bufferIndex] = ProcessMovement(inputPayload);
        
                _flagsInput = EntityMoveFlags.None;
		
                Manager.SendPacketToServer(new EntityInputPayloadPacket()
                {
                    NetId = NetId,
                    InputPayload = inputPayload,
                });
            }
            else
            {
                LastProcessedState = LatestServerState;
                _targetedLerpPosition = LatestServerState.position;
                RotateHead(LatestServerState.rotation);
            }
        }
        else
        {
            uint bufferIndex = 0;
            bool foundBufferIndex = ( InputQueue.Count > 0 );
            
            while (InputQueue.Count > 0)
            {
                InputPayload inputPayload = InputQueue.Dequeue();
                bufferIndex = inputPayload.tick % GameManager.NET_BUFFER_SIZE;

                StatePayload statePayload = ProcessMovement(inputPayload);
                StateBuffer[bufferIndex] = statePayload;
            }

            if (foundBufferIndex)
            {
                Manager.BroadcastPacketToClients(new EntityStatePayloadPacket()
                {
                    NetId = NetId,
                    StatePayload = StateBuffer[bufferIndex],
                });
            }
        }
    }
    
    
    
    protected virtual void HandleMovementInputs()
    {
        _moveInput = Vector3.Zero;
		
        if (Input.IsActionPressed("move_forward"))
            _moveInput.Z = -1f;
        else if (Input.IsActionPressed("move_backward"))
            _moveInput.Z = 1f;
        if (Input.IsActionPressed("move_left"))
            _moveInput.X = -1f;
        else if (Input.IsActionPressed("move_right"))
            _moveInput.X = 1f;

        if (Input.IsActionJustPressed("move_jump"))
            _flagsInput |= EntityMoveFlags.Jump;
    }
	
    public override void _UnhandledInput( InputEvent @event )
    {
		
        if ( @event is InputEventMouseMotion eventMouseMotion )
        {
            if (!_isPaused)
            {
                _lookInput.X += (-eventMouseMotion.Relative.X * _mouseSensitivity);
                _lookInput.Y += (-eventMouseMotion.Relative.Y * _mouseSensitivity);
            }
        }
    }

    public void TogglePause(bool forcePaused = false)
    {
        _isPaused = !_isPaused;
        if (forcePaused)
            _isPaused = true;

        if (_isPaused)
        {
            // Now paused
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _pauseMenuRootNode.Visible = true;
        }
        else
        {
            // NOT paused anymore
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _pauseMenuRootNode.Visible = false;
        }
    }
    
    
    private void OnForceSyncEntityPacket(ForceSyncEntityPacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        CurrentTick = packet.CurrentTick;
        GlobalPosition = packet.CurrentPos;
        RotateHead(packet.CurrentRotation);
    }
    
    

    private bool _isPaused = false;
    private float _mouseSensitivity = 0.045f;

    private Vector3 _targetedLerpPosition = Vector3.Zero;
    
    [Export]
    private Control _pauseMenuRootNode;

    [Export]
    private ClientHud _clientHudRootNode;

    [Export]
    private Camera3D _camera;

	
    private Vector3 _lookInput = Vector3.Zero;
    private Vector3 _moveInput = Vector3.Zero;
    private EntityMoveFlags _flagsInput = EntityMoveFlags.None;
}