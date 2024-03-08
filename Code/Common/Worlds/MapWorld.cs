using System.Collections.Generic;
using Godot;
using Lavender.Common.Worlds.Blocks;
using Lavender.Common.Worlds.Chunks;

namespace Lavender.Common.Worlds;

public partial class MapWorld : Node3D
{
    public int ChunkSize { get; protected set; } = 32;
    public Vector2I ChunkAtlasSize { get; protected set; } = new(3, 2);
    public int WorldSeed { get; protected set; } = 42069;

    public Dictionary<BlockTypes, MapBlockType> BlockReg { get; protected set; } = new()
    {
        {
            BlockTypes.Air,
            new MapBlockType()
            {
                IsSolid = false,
            }
        },
        {
            BlockTypes.Dirt,
            new MapBlockType()
            {
                IsSolid = true,
                TextureData = new MapBlockType.TextureFaceData()
                {
                    Top = new(2,0), Bottom = new(2,0), Left = new(2,0),
                    Right = new(2,0), Front = new(2,0), Back = new(2,0),
                }
            }
        },
        {
            BlockTypes.Grass,
            new MapBlockType()
            {
                IsSolid = true,
                TextureData = new MapBlockType.TextureFaceData()
                {
                    Top = new(0,0), Bottom = new(2,0), Left = new(1,0),
                    Right = new(1,0), Front = new(1,0), Back = new(1,0),
                }
            }
        },
        {
            BlockTypes.Stone,
            new MapBlockType()
            {
                IsSolid = true,
                TextureData = new MapBlockType.TextureFaceData()
                {
                    Top = new(0,1), Bottom = new(0,1), Left = new(0,1),
                    Right = new(0,1), Front = new(0,1), Back = new(0,1),
                }
            }
        }
    };
    
    public override void _Ready()
    {
        base._Ready();
        for (int x = -1; x < 3; x++)
        {
            for (int y = -1; y < 3; y++)
            {
                CreateChunk(x, y);
            }
        }
    }

    private void CreateChunk(int chunkX, int chunkY)
    {
        MapChunk chunk = new MapChunk();
        AddChild(chunk);
        chunk.GlobalPosition = new Vector3(chunkX * ChunkSize, 0, chunkY * ChunkSize);
        chunk.Setup(new Vector2I(chunkX, chunkY), this);
    }
    
    
    public enum BlockTypes
    {
        Air = 0,
        Dirt = 1,
        Grass = 2,
        Stone = 3,
    }
    
    [Export]
    public Material ChunkMaterial { get; protected set; }
    
}