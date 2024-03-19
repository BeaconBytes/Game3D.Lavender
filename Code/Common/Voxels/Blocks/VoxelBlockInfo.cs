using Godot;

namespace Lavender.Common.Voxels.Blocks;

public class VoxelBlockInfo
{
    public bool IsSolid { get; set; }

    public TextureFaceData TextureData { get; set; }

    public class TextureFaceData
    {
        public Vector2 Top { get; set; }
        public Vector2 Bottom { get; set; }
        public Vector2 Left { get; set; }
        public Vector2 Right { get; set; }
        public Vector2 Front { get; set; }
        public Vector2 Back { get; set; }
    }
}