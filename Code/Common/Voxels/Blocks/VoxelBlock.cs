using Godot;
using Lavender.Common.Voxels.Chunks;

namespace Lavender.Common.Voxels.Blocks;

public partial class VoxelBlock : Node3D
{
    public override void _Ready()
    {
        base._Ready();
        
    }

    public void Setup(VoxelWorld world, VoxelChunk chunk, Vector3 position)
    {
        
    }
}