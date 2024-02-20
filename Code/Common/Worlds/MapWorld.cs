using Godot;

namespace Lavender.Common.Worlds;

public partial class MapWorld : Node3D
{
    public override void _Ready()
    {
        base._Ready();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                CreateChunk(x, y);
            }
        }
    }

    private void CreateChunk(int chunkX, int chunkY)
    {
        
    }
}