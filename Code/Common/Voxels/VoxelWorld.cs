using System.Collections.Generic;
using Godot;
using Lavender.Common.Enums.Types;
using Lavender.Common.Voxels.Blocks;
using Lavender.Common.Voxels.Chunks;

namespace Lavender.Common.Voxels;

public partial class VoxelWorld : Node3D
{
    public int ChunkSize { get; protected set; } = 32;
    public int WorldHeight { get; protected set; } = 64;

    public int WorldSize { get; protected set; } = 128;

    public float TerrainAmplitude = 8f;
    public int WorldSeed { get; protected set; } = 42069;
    public Vector2I ChunkAtlasSize { get; protected set; } = new(8, 3);

    public Dictionary<BlockType, VoxelBlockInfo> BlockReg { get; protected set; } = new()
    {
        {
            BlockType.Air,
            new VoxelBlockInfo()
            {
                IsSolid = false,
            }
        },
        {
          BlockType.Bedrock,
          new VoxelBlockInfo()
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
            new VoxelBlockInfo()
            {
                IsSolid = true,
                TextureData = new VoxelBlockInfo.TextureFaceData()
                {
                    Top = new(2,0), Bottom = new(2,0), Left = new(2,0),
                    Right = new(2,0), Front = new(2,0), Back = new(2,0),
                }
            }
        },
        {
            BlockType.Grass,
            new VoxelBlockInfo()
            {
                IsSolid = true,
                TextureData = new VoxelBlockInfo.TextureFaceData()
                {
                    Top = new(0,0), Bottom = new(2,0), Left = new(1,0),
                    Right = new(1,0), Front = new(1,0), Back = new(1,0),
                }
            }
        },
        {
            BlockType.Stone,
            new VoxelBlockInfo()
            {
                IsSolid = true,
                TextureData = new VoxelBlockInfo.TextureFaceData()
                {
                    Top = new(1,1), Bottom = new(1,1), Left = new(1,1),
                    Right = new(1,1), Front = new(1,1), Back = new(1,1),
                }
            }
        }
    };
    
    public override void _Ready()
    {
        // GenerateNewTerrain();
    }

    public void GenerateNewTerrain()
    {
        int chunksAmt = Mathf.CeilToInt( (float)WorldSize / ChunkSize);
        int maxYChunks = Mathf.CeilToInt(WorldHeight / (float)ChunkSize);
        
        _chunkOffset = Mathf.RoundToInt(chunksAmt / 2f);
        
        
        _loadedChunks = new VoxelChunk[chunksAmt, maxYChunks, chunksAmt];
        
        for (int x = 0; x < chunksAmt; x++)
        {
            for (int y = 0; y < maxYChunks; y++)
            {
                for (int z = 0; z < chunksAmt; z++)
                {
                    int chunkX = x - _chunkOffset;
                    int chunkY = y;
                    int chunkZ = z - _chunkOffset;
                    
                    VoxelChunk tmp = CreateChunk(chunkX, chunkY, chunkZ);
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

    private VoxelChunk CreateChunk(int chunkX, int chunkY, int chunkZ)
    {
        VoxelChunk chunk = new VoxelChunk();
        AddChild(chunk);
        chunk.GlobalPosition = new Vector3(chunkX * ChunkSize, chunkY * ChunkSize, chunkZ * ChunkSize);
        chunk.Setup(new Vector3I(chunkX, chunkY, chunkZ), this);

        return chunk;
    }

    private VoxelChunk CreateEmptyChunk(int chunkX, int chunkY, int chunkZ)
    {
        VoxelChunk chunk = new();
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

    private VoxelChunk[,,] _loadedChunks;

    private int _chunkOffset = 2;
}