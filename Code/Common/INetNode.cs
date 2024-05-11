using Lavender.Common.Managers;

namespace Lavender.Common;

public interface INetNode
{
    public uint NetId { get; }
    
    /// <summary>
    /// Called immediately after node is spawned and added to scene
    /// </summary>
    public void Setup(uint netId, GameManager manager);
}