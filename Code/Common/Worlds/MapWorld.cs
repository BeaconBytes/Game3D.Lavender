using System.Collections.Generic;
using Godot;
using Lavender.Common.Worlds.Blocks;
using Lavender.Common.Worlds.Chunks;

namespace Lavender.Common.Worlds;

public partial class MapWorld : Node3D
{
    public int ChunkSize { get; protected set; } = 32;
    public int WorldHeight { get; protected set; } = 128;
    public int WorldSeed { get; protected set; } = 42069;
    public Vector2I ChunkAtlasSize { get; protected set; } = new(8, 3);

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
          BlockTypes.Bedrock,
          new MapBlockType()
          {
              IsSolid = true,
              TextureData = new()
              {
                  Top = new(0,1), Bottom = new(0,1), Left = new(0,1),
                  Right = new(0,1), Front = new(0,1), Back = new(0,1),
              }
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
                    Top = new(1,1), Bottom = new(1,1), Left = new(1,1),
                    Right = new(1,1), Front = new(1,1), Back = new(1,1),
                }
            }
        }
    };
    
    public override void _Ready()
    {
        base._Ready();

        int maxYChunks = Mathf.RoundToInt(WorldHeight / (float)ChunkSize);
        
        for (int x = -2; x <= 2; x++)
        {
            for (int y = 0; y <= maxYChunks; y++)
            {
                for (int z = -2; z <= 2; z++)
                {
                    CreateChunk(x, y, z);
                }
            }
        }
    }

    private void CreateChunk(int chunkX, int chunkY, int chunkZ)
    {
        MapChunk chunk = new MapChunk();
        AddChild(chunk);
        chunk.GlobalPosition = new Vector3(chunkX * ChunkSize, chunkY * ChunkSize, chunkZ * ChunkSize);
        chunk.Setup(new Vector3I(chunkX, chunkY, chunkZ), this);
    }
    
    
    public enum BlockTypes
    {
        Air = 0,
        Bedrock = 1,
        Dirt = 5,
        Grass = 6,
        Stone = 7,
    }
    
    [Export]
    public Material ChunkMaterial { get; protected set; }
    
}