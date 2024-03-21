using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Enums.Types;
using Lavender.Common.Networking.Packets;
using Lavender.Common.Networking.Packets.Variants.Entity;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Networking.Packets.Variants.Other;
using Lavender.Common.Networking.Packets.Variants.Protocol;
using LiteNetLib.Utils;
using Environment = System.Environment;

namespace Lavender.Common.Registers.Variants;

public class PacketRegistry
{
    public void LoadDefaults()
    {
        // OTHER
        Register<DebugActionPacket>(PacketType.DebugAction);
        
        // Protocol
        Register<AuthMePacket>(PacketType.AuthMe);
        Register<IdentifyPacket>(PacketType.Identify);
        
        // Setup
        Register<WorldSetupPacket>(PacketType.WorldSetup);
        
        // Entity
        Register<SpawnEntityPacket>(PacketType.SpawnEntity);
        Register<DestroyEntityPacket>(PacketType.DestroyEntity);
        Register<ForceSyncEntityPacket>(PacketType.ForceSyncEntity);
        
        // Entity Updates
        Register<EntityRotatePacket>(PacketType.EntityRotate);
        Register<EntityMoveToPacket>(PacketType.EntityMoveTo);
        Register<EntityTeleportPacket>(PacketType.EntityTeleport);
        Register<EntityInputPayloadPacket>(PacketType.EntityInputPayload);
        Register<EntityStatePayloadPacket>(PacketType.EntityStatePayload);
    }
    
    public void Register<T>(PacketType packetType) where T : GamePacket
    {
        if (packetType == PacketType.Unknown)
        {
            throw new Exception($"Invalid PacketRegistry.Register<{typeof(T).Namespace}.{typeof(T).Name}>(PacketType.Unknown)");
        }
        _entries.Add(packetType, (T)Activator.CreateInstance(typeof(T)));
    }

    [Obsolete("TriggerHandler() is deprecated. Please use GetSubscriberCallback() instead.", true)]
    public void TriggerHandler(PacketType packetType, NetDataReader reader, uint srcNetId)
    {
        GetPacketCached(packetType).TriggerHandler(reader, srcNetId);
    }
    
    //[Obsolete("GetPacketCached() is deprecated, please use Subscribe<T>(Action<T> onReceive) instead.", true)]
    private GamePacket GetPacketCached(PacketType packetType)
    {
        if (packetType == PacketType.Unknown)
            throw new Exception($"PacketType.Unknown is not allowed here.");

        GamePacket basePacket;
        if (!_entries.TryGetValue(packetType, out basePacket))
        {
            throw new Exception($"PacketType.{packetType} doesn't have a class registered to it!");
        }
        return basePacket;
    }

    public void Subscribe<T>(Action<T, uint> onReceive) where T : GamePacket, new()
    {
        PacketType packetType = PacketType.Unknown;
        foreach (KeyValuePair<PacketType,GamePacket> pair in _entries)
        {
            if (pair.Value.GetType() == typeof(T))
            {
                packetType = pair.Key;
                break;
            }
        }

        if (packetType == PacketType.Unknown)
            throw new Exception($"Failed to Subscribe to Packet: PacketType not registered to given class.");

        _callbacks[(packet, srcNetId) =>
        {
            onReceive((T)packet, srcNetId);
        }] = packetType;
    }

    internal List<SubscribeDelegate> GetSubscriberCallback(PacketType packetType)
    {
        List<SubscribeDelegate> callbackReturns = new();
        foreach (KeyValuePair<SubscribeDelegate,PacketType> pair in _callbacks)
        {
            if (pair.Value == packetType)
            {
                callbackReturns.Add(pair.Key);
            }
        }

        return callbackReturns;
    }

    internal void InvokeSubscriberEvent(PacketType packetType, NetDataReader reader, uint sourceNetId)
    {
        GamePacket packet = GetPacketCached(packetType);
        packet.Deserialize(reader);
        
        List<SubscribeDelegate> callbackList = GetSubscriberCallback(packetType);
        foreach (SubscribeDelegate subscribeDelegate in callbackList)
        {
            try
            {
                subscribeDelegate.Invoke(packet, sourceNetId);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"InvokeSubscriberEvent() Error!{Environment.NewLine}{ex}");
                _callbacks.Remove(subscribeDelegate);
            }
        }
    }

    public PacketType GetPacketType(GamePacket netPacket)
    {
        PacketType result = PacketType.Unknown;
        foreach (KeyValuePair<PacketType, GamePacket> pair in _entries)
        {
            if (pair.Value.GetType() == netPacket.GetType())
            {
                result = pair.Key;
                break;
            }
        }

        return result;
    }

    private readonly Dictionary<PacketType, GamePacket> _entries = new();
    
    private readonly Dictionary<SubscribeDelegate, PacketType> _callbacks = new();
    public delegate void SubscribeDelegate(GamePacket packet, uint sourceNetId);
}