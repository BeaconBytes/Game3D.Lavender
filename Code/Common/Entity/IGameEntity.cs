using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Buffs;
using Lavender.Common.Entity.Data;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity;

public interface IGameEntity : INetNode
{
    public void Destroy();
    
    /// <summary>
    /// Recalculates visibility of child nodes on this node for Server/Client usage
    /// </summary>
    public void RecalculateVisibility();
    
    public void HandleControllerInputs(RawInputs inputs);

    /// <summary>
    /// Teleports this entity to the given location & rotation
    /// </summary>
    public void Teleport(Vector3 position, Vector3? rotation = null);

    /// <summary>
    /// Adds given IController to list of applied IControllers
    /// </summary>
    public void AddController(IController controller, bool insertFirst = false);
    
    /// <summary>
    /// Removes given IController from list of applied IControllers
    /// </summary>
    /// <param name="controller"></param>
    public void RemoveController(IController controller);
    
    public Vector3 WorldPosition { get; set; }
    public Vector3 WorldRotation { get; set; }
    
    public string DisplayName { get; }
    public bool Enabled { get; }
    public bool AutomaticMoveAndSlide { get; }
    public bool IsControlsFrozen { get; set; }

    public EntityStats Stats { get; }

    public List<IEntityBuff> AppliedBuffs { get; }
    public List<IEntityBuff> TickingAppliedBuffs { get; }
}