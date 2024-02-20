using Godot;
using Lavender.Common.Worlds.Chunks;

namespace Lavender.Common.Worlds.Blocks;

public partial class MapBlock : Node3D
{
    public override void _Ready()
    {
        base._Ready();
        
    }

    public void Setup(MapWorld world, MapChunk chunk, Vector3 position)
    {
        
    }
}