using Godot;

namespace Lavender.Common.Entity;

public interface IGameEntity : INetObject
{
    public void Destroy();
    
    public Vector3 WorldPosition { get; }
    public Vector3 WorldRotation { get; }
    
    public string DisplayName { get; }
    public bool Destroyed { get; }
}