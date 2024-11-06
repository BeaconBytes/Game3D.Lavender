using Lavender.Common.Managers;
using Lavender.Common.Mapping;

namespace Lavender.Common.Harvestables;

public interface IHarvestableNode
{
    public uint NetId { get; }
    public GameMap Map { get; }

    public void Setup(uint netId, GameMap map);
}