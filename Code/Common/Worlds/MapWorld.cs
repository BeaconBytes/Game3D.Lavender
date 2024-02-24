using Godot;
using Lavender.Common.Worlds.Chunks;

namespace Lavender.Common.Worlds;

public partial class MapWorld : Node3D
{
    public static uint ChunkSize { get; protected set; } = 16;

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
        MapChunk chunk = new MapChunk();
        chunk.GlobalPosition = new Vector3(chunkX * ChunkSize, 0, chunkY * ChunkSize);
        chunk.Setup(new Vector2I(chunkX, chunkY), ChunkSize);
        AddChild(chunk);
    }
}