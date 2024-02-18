using Godot;

namespace Lavender.Common.Entity;

public interface IGameEntity : INetObject
{
    public void Teleport(Vector3 position);
    public void RotateTo(Vector3 rotation);
    public void Destroy();
    
    public Vector3 WorldPosition { get; }
    public Vector3 WorldRotation { get; }
    
    public void SetName(string name);
    public string DisplayName { get; }
    public bool Destroyed { get; }
}