using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Buffs;
using Lavender.Common.Entity.Data;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity.GameEntities;

public interface IGameEntity : INetNode
{
    public void Destroy();
    
    /// <summary>
    /// Called once every Network Tick
    /// </summary>
    public void NetworkProcess(double delta);
    
    /// <summary>
    /// Recalculates visibility of child nodes on this node for Server/Client usage
    /// </summary>
    public void RecalculateVisibility();
    
    /// <summary>
    /// Handle input from an attached IController
    /// </summary>
    public void HandleControllerInputs(IController source, RawInputs inputs);

    /// <summary>
    /// Teleport this entity instantly to the given location and, if not null, rotation.
    /// Networking is automatic.
    /// </summary>
    public void Teleport(Vector3 position, Vector3? rotation = null);

    /// <summary>
    /// Forces given controller into slot 0/master on this entity's AppliedControllers list
    /// </summary>
    public void SetMasterController(IController controller);

    /// <summary>
    /// Gets the primary/active controller(slot 0) for this entity
    /// </summary>
    public IController GetMasterController();
    /// <summary>
    /// Adds given IController to list of applied IControllers
    /// </summary>
    public void AddController(IController controller, bool insertFirst = false);
    
    /// <summary>
    /// Removes given IController from list of applied IControllers
    /// </summary>
    public void RemoveController(IController controller);
    
    public Vector3 WorldPosition { get; set; }
    public Vector3 WorldRotation { get; set; }
    
    public string DisplayName { get; }
    public bool Enabled { get; }
    public bool AutomaticMoveAndSlide { get; }
    public bool IsControlsFrozen { get; set; }
    public NavigationAgent3D NavAgent { get; }

    public List<IController> AttachedControllers { get; }
    public EntityStats Stats { get; }

    public List<IEntityBuff> AppliedBuffs { get; }
    public List<IEntityBuff> TickingAppliedBuffs { get; }
    
    
    public event GameManager.SimpleNetNodeEventHandler OnCompletedNavPathEvent;
}