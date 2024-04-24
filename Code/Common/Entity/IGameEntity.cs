using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity.Buffs;

namespace Lavender.Common.Entity;

public interface IGameEntity : INetObject
{
    public void Destroy();
    
    public Vector3 WorldPosition { get; }
    public Vector3 WorldRotation { get; }
    
    public string DisplayName { get; }
    public bool Enabled { get; }

    public List<IEntityBuff> AppliedBuffs { get; }
    public List<IEntityBuff> TickingAppliedBuffs { get; }
}