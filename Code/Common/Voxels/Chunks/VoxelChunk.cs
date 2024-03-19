using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Enums.Types;
using Lavender.Common.Voxels.Blocks;

namespace Lavender.Common.Voxels.Chunks;

public partial class VoxelChunk : Node3D
{
    public void Setup(Vector3I chunkPosition, VoxelWorld voxelWorld, bool generateNewBlocks = false)
    {
        ChunkPosition = chunkPosition;
        
        _voxelWorld = voxelWorld;

        Noise.Seed = _voxelWorld.WorldSeed;
        Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        Noise.Offset = new Vector3(ChunkPosition.X * _voxelWorld.ChunkSize, ChunkPosition.Z * _voxelWorld.ChunkSize, 0);
        Noise.FractalOctaves = 7;
        Noise.FractalLacunarity = 0.33f;
        Noise.Frequency = 0.033f;
        
        if(generateNewBlocks)
            GenerateNewBlocks();
        // DisplayBlocks();
    }

    public void DisplayBlocks()
    {
        if (chunkMeshInstance != null)
        {
            chunkMeshInstance.CallDeferred("queue_free");
            chunkMeshInstance = null;
        }

        if (_isAllAir)
            return;

        chunkMesh = new ArrayMesh();
        chunkMeshInstance = new MeshInstance3D();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetSmoothGroup(UInt32.MaxValue);
        
        for (int x = 0; x < _voxelWorld.ChunkSize; x++)
        {
            for (int y = 0; y < _voxelWorld.ChunkSize; y++)
            {
                for (int z = 0; z < _voxelWorld.ChunkSize; z++)
                {
                    CreateBlock(x, y, z);
                }
            }
        }

        st.GenerateNormals();
        st.SetMaterial(_voxelWorld.ChunkMaterial);
        st.Commit(chunkMesh);
        chunkMeshInstance.Mesh = chunkMesh;

        AddChild(chunkMeshInstance);
        chunkMeshInstance.CreateTrimeshCollision();
    }

    public void GenerateNewBlocks()
    {
        _isAllAir = true;
        _blocks = new BlockType[_voxelWorld.ChunkSize, _voxelWorld.ChunkSize, _voxelWorld.ChunkSize];

        int bedrockHeight = 0;
        int stoneHeight = Mathf.RoundToInt(_voxelWorld.TerrainAmplitude * 0.66f);
        int dirtHeight = Mathf.RoundToInt(_voxelWorld.TerrainAmplitude * 0.85f);
        
        for (int j = 0; j < _blocks.GetLength(0); j++)
        {
            for (int l = 0; l < _blocks.GetLength(2); l++)
            {

                float resultHeight = ((Noise.GetNoise2D(j, l) + 1) / 2f) * _voxelWorld.TerrainAmplitude;
                int aproxHeight = Mathf.FloorToInt(resultHeight);
                
                for (int k = 0; k < _blocks.GetLength(1); k++)
                {
                    int globalHeightPos = (ChunkPosition.Y * _voxelWorld.ChunkSize) + k;
                    
                    BlockType blockType = BlockType.Air;

                    if (globalHeightPos <= bedrockHeight)
                    {
                        blockType = BlockType.Bedrock;
                    }
                    else if (globalHeightPos <= resultHeight)
                    {
                        if (globalHeightPos <= stoneHeight)
                            blockType = BlockType.Stone;
                        else if (globalHeightPos <= dirtHeight)
                            blockType = BlockType.Dirt;
                        else if (globalHeightPos == aproxHeight)
                            blockType = BlockType.Grass;
                        else
                            blockType = BlockType.Bedrock;
                    }
                
                    if (blockType != BlockType.Air)
                        _isAllAir = false;
                    
                    _blocks[j, k, l] = blockType;
                }
                
            }
        }
    }

    private bool _isAllAir = true;

    private void CreateBlock(int x, int y, int z)
    {
        BlockType blockType = _blocks[x, y, z];
        if (blockType == BlockType.Air)
            return;

        VoxelBlockInfo.TextureFaceData blockFaceData = _voxelWorld.BlockReg[blockType].TextureData;
        
        if(CheckTransparentBlock(x, y+1, z))
            CreateFace(_faceTop, x, y, z, blockFaceData.Top);
        if(CheckTransparentBlock(x, y-1, z))
            CreateFace(_faceBottom, x, y, z, blockFaceData.Bottom);
        if(CheckTransparentBlock(x-1, y, z))
            CreateFace(_faceLeft, x, y, z, blockFaceData.Left);
        if(CheckTransparentBlock(x+1, y, z))
            CreateFace(_faceRight, x, y, z, blockFaceData.Right);
        if(CheckTransparentBlock(x, y, z-1))
            CreateFace(_faceBack, x, y, z, blockFaceData.Back);
        if(CheckTransparentBlock(x, y, z+1))
            CreateFace(_faceFront, x, y, z, blockFaceData.Front);
    }

    private void CreateFace(ushort[] i, int x, int y, int z, Vector2 texAtlasOffset)
    {
        Vector3 offset = new Vector3(x, y, z);
        Vector3 a = _blockVertices[i[0]] + offset;
        Vector3 b = _blockVertices[i[1]] + offset;
        Vector3 c = _blockVertices[i[2]] + offset;
        Vector3 d = _blockVertices[i[3]] + offset;

        Vector2 uvOffset = texAtlasOffset / _voxelWorld.ChunkAtlasSize;
        float height = 1f / _voxelWorld.ChunkAtlasSize.Y;
        float width = 1f / _voxelWorld.ChunkAtlasSize.X;

        // Correspond to 4 corners of mesh in UV co-ords:
        // uvA - TopLeft corner
        // uvB - BottomLeft corner
        // uvC - BottomRight corner
        // uvD - TopRight corner
        Vector2 uvA = uvOffset + new Vector2(0, 0);
        Vector2 uvB = uvOffset + new Vector2(0, height);
        Vector2 uvC = uvOffset + new Vector2(width, height);
        Vector2 uvD = uvOffset + new Vector2(width, 0);
        
        st.AddTriangleFan(new Vector3[] {a, b, c}, new Vector2[]{uvA, uvB, uvC});
        st.AddTriangleFan(new Vector3[] {a, c, d}, new Vector2[]{uvA, uvC, uvD});
    }

    private bool CheckTransparentBlock(int x, int y, int z)
    {
        // Assuming symmetrical chunk sizing/bounds here.
        int chunkSize = _voxelWorld.ChunkSize;
        if (x >= 0 && x < chunkSize &&
            y >= 0 && y < chunkSize &&
            z >= 0 && z < chunkSize)
            return !(_voxelWorld.BlockReg[_blocks[x, y, z]].IsSolid);
        
        // Need to check world pos to see if other chunks have air blocks around this block
        int worldX = (ChunkPosition.X * chunkSize) + x;
        int worldY = (ChunkPosition.Y * chunkSize) + y;
        int worldZ = (ChunkPosition.Z * chunkSize) + z;

        float worldSizeHalf = _voxelWorld.WorldSize / 2f;
        
        if (worldX > -worldSizeHalf && worldX < worldSizeHalf &&
            worldY >= 0 && worldY < _voxelWorld.WorldHeight &&
            worldZ > -worldSizeHalf && worldZ < worldSizeHalf)
        {
            return !(_voxelWorld.BlockReg[_voxelWorld.GetBlockAtPos(worldX, worldY, worldZ)].IsSolid);
            // bool val = !(_mapWorld.BlockReg[_mapWorld.GetBlockAtPos(worldX, worldY, worldZ)].IsSolid);
            // if (val)
            //     throw new Exception("FOUND VAL");
            // return val;
        }
        
        return true;
    }

    /// <summary>
    /// Gets the blockType at given position in Local/Block coordinates
    /// </summary>
    public BlockType GetBlockTypeAtPos(int x, int y, int z)
    {
        if (!IsPosInBounds(x, y, z))
            throw new Exception("GetBlockTypePos() @ OutOfBounds position given!");

        return _blocks[x, y, z];
    }

    private bool IsPosInBounds(int x, int y, int z)
    {
        int chunkSize = _voxelWorld.ChunkSize;
        return (x >= 0 && x < chunkSize &&
                y >= 0 && y < chunkSize &&
                z >= 0 && z < chunkSize);
    }

    private SurfaceTool st = new SurfaceTool();
    private ArrayMesh chunkMesh = null;
    private MeshInstance3D chunkMeshInstance = null;

    private List<Vector3> _blockVertices = new List<Vector3>()
    {
        new Vector3(0,0,0), //0
        new Vector3(1,0,0), //1
        new Vector3(0,1,0), //2
        new Vector3(1,1,0), //3
        new Vector3(0,0,1), //4
        new Vector3(1,0,1), //5
        new Vector3(0,1,1), //6
        new Vector3(1,1,1), //7
    };
    private static ushort[] _faceTop = new ushort[] { 2, 3, 7, 6 };
    private static ushort[] _faceBottom = new ushort[] { 0, 4, 5, 1 };
    private static ushort[] _faceLeft = new ushort[] { 6, 4, 0, 2 };
    private static ushort[] _faceRight = new ushort[] { 3, 1, 5, 7 };
    private static ushort[] _faceFront = new ushort[] { 7, 5, 4, 6 };
    private static ushort[] _faceBack = new ushort[] { 2, 0, 1, 3 };

    public Vector3I ChunkPosition { get; private set; } = Vector3I.Zero;
    

    public FastNoiseLite Noise { get; protected set; } = new();

    private BlockType[,,] _blocks;

    private VoxelWorld _voxelWorld;
}