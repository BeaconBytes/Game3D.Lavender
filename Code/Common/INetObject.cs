using Lavender.Common.Managers;

namespace Lavender.Common;

public interface INetObject
{
    public uint NetId { get; }
    public void Setup(uint netId, GameManager manager);
}