using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Enums.Types;
using Lavender.Common.Worlds.Blocks;
using Lavender.Common.Worlds.Chunks;

namespace Lavender.Common.Worlds;

public partial class MapWorld : Node3D
{
    public int ChunkSize { get; protected set; } = 32;
    public int WorldHeight { get; protected set; } = 64;

    public int WorldSize { get; protected set; } = 128;

    public float TerrainAmplitude = 8f;
    public int WorldSeed { get; protected set; } = 42069;
    public Vector2I ChunkAtlasSize { get; protected set; } = new(8, 3);

    public Dictionary<BlockType, MapBlockType> BlockReg { get; protected set; } = new()
    {
        {
            BlockType.Air,
            new MapBlockType()
            {
                IsSolid = false,
            }
        },
        {
          BlockType.Bedrock,
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
            BlockType.Dirt,
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
            BlockType.Grass,
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
            BlockType.Stone,
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
        int chunksCount = Mathf.CeilToInt( (float)WorldSize / ChunkSize);
        _chunkOffset = Mathf.RoundToInt(chunksCount / 2f);
        
        GenerateTerrain(chunksCount);
    }

    public void GenerateTerrain(int chunksAmt = 4)
    {
        int maxYChunks = Mathf.CeilToInt(WorldHeight / (float)ChunkSize);
        
        _loadedChunks = new MapChunk[chunksAmt, maxYChunks, chunksAmt];
        
        for (int x = 0; x < chunksAmt; x++)
        {
            for (int y = 0; y < maxYChunks; y++)
            {
                for (int z = 0; z < chunksAmt; z++)
                {
                    int chunkX = x - _chunkOffset;
                    int chunkY = y;
                    int chunkZ = z - _chunkOffset;
                    
                    MapChunk tmp = CreateChunk(chunkX, chunkY, chunkZ);
                    _loadedChunks[x, y, z] = tmp;
                }
            }
        }

        for (int x = 0; x < _loadedChunks.GetLength(0); x++)
        {
            for (int y = 0; y < _loadedChunks.GetLength(1); y++)
            {
                for (int z = 0; z < _loadedChunks.GetLength(2); z++)
                {
                    _loadedChunks[x,y,z].DisplayBlocks();
                }
            }
        }
        
    }

    /// <summary>
    /// Gets a block at given position in world coordinates
    /// </summary>
    public BlockType GetBlockAtPos(int x, int y, int z)
    {
        int chunkX = Mathf.FloorToInt(x / (float)ChunkSize) + (_chunkOffset);
        int chunkY = Mathf.FloorToInt(y / (float)ChunkSize);
        int chunkZ = Mathf.FloorToInt(z / (float)ChunkSize) + (_chunkOffset);

        float worldSizeHalf = WorldSize / 2f;

        int blockX = ModuloInt(x,ChunkSize);
        int blockY = ModuloInt(y,ChunkSize);
        int blockZ = ModuloInt(z,ChunkSize);
        
        return _loadedChunks[chunkX, chunkY, chunkZ].GetBlockTypeAtPos(blockX, blockY, blockZ);
    }

    private MapChunk CreateChunk(int chunkX, int chunkY, int chunkZ)
    {
        MapChunk chunk = new MapChunk();
        AddChild(chunk);
        chunk.GlobalPosition = new Vector3(chunkX * ChunkSize, chunkY * ChunkSize, chunkZ * ChunkSize);
        chunk.Setup(new Vector3I(chunkX, chunkY, chunkZ), this);

        return chunk;
    }
    
    int ModuloInt(float a,float b)
    {
        return Mathf.RoundToInt(a - b * Mathf.Floor(a / b));
    }
    
    [Export]
    public Material ChunkMaterial { get; protected set; }

    private MapChunk[,,] _loadedChunks;

    private int _chunkOffset = 2;
}