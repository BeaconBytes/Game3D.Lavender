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

    public override void ServerRespawnReceiver()
    {
        base.ServerRespawnReceiver();
        
        Marker3D spawnPointSelected = MapManager.GetRandomPlayerSpawnPoint();
        ReceiverEntity.Teleport(spawnPointSelected.GlobalPosition, spawnPointSelected.GlobalRotation);

        if (ReceiverEntity is PlayerEntity)
        {
            ShowNotification("Respawning...", 2);
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

        if (Manager.ClientNetId != NetId || (!IsClient && ReceiverEntity == null))
            return;
        
        MoveInput = Vector3.Zero;
        LookInput = Vector3.Zero;

        // If we just did a jump, remove its flags from our input flags,
        // so we don't repeatedly jump forever
        if (MoveFlagsInput.HasFlag(EntityMoveFlags.Jump))
            MoveFlagsInput &= ~EntityMoveFlags.Jump;
        if (MoveFlagsInput.HasFlag(EntityMoveFlags.PrimaryAttack))
            MoveFlagsInput &= ~EntityMoveFlags.PrimaryAttack;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Manager.IsServer || Manager.ClientNetId != NetId)
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
        Vector3 tmpMoveInput = Vector3.Zero;
		
        if (Input.IsActionPressed("move_forward"))
            tmpMoveInput.Z = -1f;
        else if (Input.IsActionPressed("move_backward"))
            tmpMoveInput.Z = 1f;
        if (Input.IsActionPressed("move_left"))
            tmpMoveInput.X = -1f;
        else if (Input.IsActionPressed("move_right"))
            tmpMoveInput.X = 1f;

        MoveInput = tmpMoveInput;
        

        if (Input.IsActionJustPressed("move_jump"))
            MoveFlagsInput |= EntityMoveFlags.Jump;

        if (Input.IsActionJustPressed("attack_primary"))
            MoveFlagsInput |= EntityMoveFlags.PrimaryAttack;
    }
	
    public override void _UnhandledInput( InputEvent @event )
    {
        if (!IsClient || Manager.ClientNetId != NetId)
            return;
        if ( @event is InputEventMouseMotion eventMouseMotion )
        {
            if (!_isPaused)
            {
                Vector3 tmpLookInput = LookInput;
                tmpLookInput.X += (-eventMouseMotion.Relative.X * _mouseSensitivity);
                tmpLookInput.Y += (-eventMouseMotion.Relative.Y * _mouseSensitivity);
                LookInput = tmpLookInput;
            }
        }
    }

    private void TogglePause(bool forcePaused = false)
    {
        if (!IsClient || Manager.ClientNetId != NetId)
            return;
        
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
}