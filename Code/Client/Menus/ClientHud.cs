using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity;
using Lavender.Common.Enums.Net;
using Lavender.Common.Managers;
using Lavender.Common.Networking.Packets.Variants.Mapping;
using Lavender.Common.Registers;
using Lavender.Common.Utils;

namespace Lavender.Client.Menus;

public partial class ClientHud : Control
{
    public void Setup(PlayerController owner)
    {
        _owner = owner;
        if (_notificationLabelNode == null)
        {
            GD.PrintErr($"[ClientHud#Setup()]: _notificationLabelNode is unset in the editor!");
            return;
        }
        Register.Packets.Subscribe<MapNotificationPacket>(OnMapNotificationPacket);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_notificationLabelNode == null)
            return;
        
        
        if (_msgTimeRemaining > 0)
            _msgTimeRemaining -= (float)delta;
        
        if (_msgTimeRemaining <= 0f)
        {
            if (_notificationMsgQueue.Count > 0)
            {
                NotificationMsgData msgData = _notificationMsgQueue.Dequeue();
                _notificationLabelNode.Text = StringUtils.Sanitize(msgData.Message, 64);
                _msgTimeRemaining = msgData.TimeLengthSeconds;
            }
            else if(!string.IsNullOrEmpty(_notificationLabelNode.Text))
            {
                _notificationLabelNode.Text = string.Empty;
            }
        }
    }

    private void OnMapNotificationPacket(MapNotificationPacket packet, uint sourceNetId)
    {
        if (sourceNetId != (uint)StaticNetId.Server)
            return;
        QueueNotification(packet.Message, packet.TimeLengthSeconds);
    }

    public void QueueNotification(string message, float showTime = 4f)
    {
        _notificationMsgQueue.Enqueue(new NotificationMsgData()
        {
            Message = message,
            TimeLengthSeconds = showTime,
        });
    }

    private PlayerController _owner;
    private float _msgTimeRemaining = 0f;

    private readonly Queue<NotificationMsgData> _notificationMsgQueue = new Queue<NotificationMsgData>();
    private struct NotificationMsgData
    {
        public string Message;
        public float TimeLengthSeconds;
    }

    [Export]
    private Label _notificationLabelNode;
}