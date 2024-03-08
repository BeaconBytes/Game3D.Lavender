using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Worlds.Blocks;

namespace Lavender.Common.Worlds.Chunks;

public partial class MapChunk : Node3D
{
    public void Setup(Vector2I chunkPosition, MapWorld mapWorld)
    {
        ChunkPosition = chunkPosition;
        
        _mapWorld = mapWorld;
        ChunkSize = _mapWorld.ChunkSize;

        Noise.Seed = _mapWorld.WorldSeed;
        Noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
        Noise.Offset = new Vector3(ChunkPosition.X * ChunkSize, ChunkPosition.Y * ChunkSize, 0);
        
        GenerateBlocks();
        UpdateBlocks();
    }

    public void UpdateBlocks()
    {
        if (chunkMeshInstance != null)
        {
            chunkMeshInstance.CallDeferred("queue_free");
            chunkMeshInstance = null;
        }

        chunkMesh = new ArrayMesh();
        chunkMeshInstance = new MeshInstance3D();
        st.Begin(Mesh.PrimitiveType.Triangles);
        st.SetSmoothGroup(UInt32.MaxValue);
        
        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    CreateBlock(x, y, z);
                }
            }
        }

        st.GenerateNormals();
        st.SetMaterial(_mapWorld.ChunkMaterial);
        st.Commit(chunkMesh);
        chunkMeshInstance.Mesh = chunkMesh;

        AddChild(chunkMeshInstance);
        chunkMeshInstance.CreateTrimeshCollision();
    }

    public void GenerateBlocks()
    {
        _blocks = new MapWorld.BlockTypes[_mapWorld.ChunkSize, _mapWorld.ChunkSize, _mapWorld.ChunkSize];

        for (int j = 0; j < _blocks.GetLength(0); j++)
        {
            for (int k = 0; k < _blocks.GetLength(1); k++)
            {
                
                for (int l = 0; l < _blocks.GetLength(2); l++)
                {
                    MapWorld.BlockTypes blockType = MapWorld.BlockTypes.Air;

                    if (k < 16)
                        blockType = MapWorld.BlockTypes.Stone;
                    else if (k < 31)
                        blockType = MapWorld.BlockTypes.Dirt;
                    else if (k == 31)
                        blockType = MapWorld.BlockTypes.Grass;

                    _blocks[j, k, l] = blockType;
                }
            }
        }
    }

    private void CreateBlock(int x, int y, int z)
    {
        MapWorld.BlockTypes blockType = _blocks[x, y, z];
        if (blockType == MapWorld.BlockTypes.Air)
            return;

        MapBlockType.TextureFaceData blockFaceData = _mapWorld.BlockReg[blockType].TextureData;
        
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

        Vector2 uvOffset = texAtlasOffset / _mapWorld.ChunkAtlasSize;
        float height = 1f / _mapWorld.ChunkAtlasSize.Y;
        float width = 1f / _mapWorld.ChunkAtlasSize.X;

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
        int chunkSize = _mapWorld.ChunkSize;
        if (x >= 0 && x < chunkSize &&
            y >= 0 && y < chunkSize &&
            z >= 0 && z < chunkSize)
            return !(_mapWorld.BlockReg[_blocks[x, y, z]].IsSolid);
        return true;
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
    

    public Vector2I ChunkPosition { get; private set; } = Vector2I.Zero;
    public int ChunkSize { get; protected set; }
    

    public FastNoiseLite Noise { get; protected set; } = new();

    private MapWorld.BlockTypes[,,] _blocks;

    private MapWorld _mapWorld;
}