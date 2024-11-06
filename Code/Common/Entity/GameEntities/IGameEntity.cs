using System.Collections.Generic;
using Godot;
using Lavender.Common.Buffs;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.Data;
using Lavender.Common.Managers;

namespace Lavender.Common.Entity.GameEntities;

public interface IGameEntity : INetNode
{
    /// <summary>
    /// Attempts to cleanly destroy this GameEntity
    /// </summary>
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
    /// Teleport this entity instantly to the given location and, if not null, rotation.
    /// Networking is automatic.
    /// </summary>
    public void Teleport(Vector3 position, Vector3? rotation = null);

    /// <summary>
    /// Used for networking to sync rotation to last known rotation. Snaps immediately to given rotation.
    /// Override for custom logic
    /// </summary>
    public void SyncRotationTo(Vector3 rotation);
    
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

    /// <summary>
    /// Gets the GlobalTransform of this node
    /// </summary>
    public Transform3D GetGlobalTransform();

    /// <summary>
    /// Sets the nav agent's targeted position
    /// </summary>
    public void SetNavTarget(Vector3 pos);

    /// <summary>
    /// Uses the set raycast to check for collisions with other game entities, returning them as a list. If none, returns null
    /// </summary>
    public List<IGameEntity>? RaycastEntityHit();
    
    
    public Vector3 WorldPosition { get; set; }
    public Vector3 WorldRotation { get; set; }
    
    public string DisplayName { get; }
    public bool Enabled { get; }
    public bool IsControlsFrozen { get; set; }
    public NavigationAgent3D NavAgent { get; }

    public List<IController> AttachedControllers { get; }
    public EntityStats Stats { get; }
    public GameManager Manager { get; }

    public List<IEntityBuff> AppliedBuffs { get; }
    public List<IEntityBuff> TickingAppliedBuffs { get; }
    
    
    public event GameManager.SimpleNetNodeEventHandler OnCompletedNavPathEvent;
}