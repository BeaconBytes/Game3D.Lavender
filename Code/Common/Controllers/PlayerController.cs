using Godot;
using Lavender.Client.Menus;
using Lavender.Common.Entity.Data;
using Lavender.Common.Enums.Entity;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Mapping;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using Lavender.Common.Registers;
using PlayerEntity = Lavender.Common.Entity.GameEntities.PlayerEntity;

namespace Lavender.Common.Controllers;

public partial class PlayerController : BasicControllerBase
{
    public override void Setup(uint netId, GameManager gameManager)
    {
        base.Setup(netId, gameManager);
        
        if (ClientHud == null)
        {
            GD.PrintErr($"[PlayerController#Setup]: ClientHud node is not set in the editor!");
            return;
        }

        ClientHud.Setup(this);
        
        _pauseMenuRootNode.Visible = false;
        
        if (IsClient)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            Manager.SendPacketToServer(new AcknowledgePacket());
        }
        else
        {
            Register.Packets.Subscribe<AcknowledgePacket>(OnAcknowledgePacket);
        }
    }

    public override void RespawnReceiver()
    {
        base.RespawnReceiver();
        
        ReceiverEntity.IsControlsFrozen = true;
        
        Marker3D spawnPointSelected = MapManager.GetRandomPlayerSpawnPoint();
        ReceiverEntity.Teleport(spawnPointSelected.GlobalPosition, spawnPointSelected.GlobalRotation);

        if (ReceiverEntity is PlayerEntity)
        {
            ShowNotification("Respawning...", 5);
        }

        ReceiverEntity.IsControlsFrozen = false;
    }

    public void ShowNotification(string msg, float shownForTime = 3.0f)
    {
        Manager.SendPacketToClient(new MapNotificationPacket()
        {
            Message = msg,
            TimeLengthSeconds = shownForTime,
        }, NetId);
    }

    public override void NetworkProcess(double delta)
    {
        base.NetworkProcess(delta);

        if (!IsClient && ReceiverEntity == null)
            return;

        if (ReceiverEntity.IsControlsFrozen && !_flagsInput.HasFlag(EntityMoveFlags.Frozen))
        {
            _flagsInput |= EntityMoveFlags.Frozen;
        }
        else if (!ReceiverEntity.IsControlsFrozen && _flagsInput.HasFlag(EntityMoveFlags.Frozen))
        {
            _flagsInput &= ~EntityMoveFlags.Frozen;
        }
        
        ReceiverEntity.HandleControllerInputs(this, new RawInputs()
        {
            MoveInput = _moveInput,
            LookInput = _lookInput,
            FlagsInput = _flagsInput,
        });
        
        _moveInput = Vector3.Zero;
        _lookInput = Vector3.Zero;

        // If we just did a jump, remove its flags from our input flags,
        // so we don't repeatedly jump forever
        if (_flagsInput.HasFlag(EntityMoveFlags.Jump))
            _flagsInput &= ~EntityMoveFlags.Jump;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Manager.IsServer)
            return;
        
        ReadMovementInputs();

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            _isPaused = !_isPaused;

            Input.MouseMode = _isPaused ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
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
    protected virtual void ReadMovementInputs()
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

    private void TogglePause(bool forcePaused = false)
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
    
    // EVENT HANDLERS //
    private void OnAcknowledgePacket(AcknowledgePacket packet, uint sourceNetId)
    {
        if (sourceNetId != NetId || IsServerPlayerInitialized || IsClient)
            return;
        
        IsServerPlayerInitialized = true;
    }
    
    
    public bool IsServerPlayerInitialized { get; protected set; } = false;

    [Export]
    public ClientHud ClientHud { get; protected set; }
    [Export]
    private Control _pauseMenuRootNode;
	
    private bool _isPaused = false;
    private float _mouseSensitivity = 0.045f;
    
    private Vector3 _lookInput = Vector3.Zero;
    private Vector3 _moveInput = Vector3.Zero;
    private EntityMoveFlags _flagsInput = EntityMoveFlags.None;
}