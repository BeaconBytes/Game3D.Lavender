using System.Collections.Generic;
using Godot;

namespace Lavender.Common.Worlds.Chunks;

public partial class MapChunk : Node3D
{
    public void Setup(Vector2I chunkPosition, uint chunkSize)
    {
        ChunkPosition = chunkPosition;
        ChunkSize = chunkSize;
        
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

        st.GenerateNormals(false);
        st.Commit(chunkMesh);
        chunkMeshInstance.Mesh = chunkMesh;

        AddChild(chunkMeshInstance);
        chunkMeshInstance.CreateTrimeshCollision();
    }

    public void RenderBlocks()
    {
        
    }

    private void CreateBlock(int x, int y, int z)
    {
        CreateFace(_faceTop, x, y, z);
        CreateFace(_faceBottom, x, y, z);
        CreateFace(_faceLeft, x, y, z);
        CreateFace(_faceRight, x, y, z);
        CreateFace(_faceFront, x, y, z);
        CreateFace(_faceBack, x, y, z);
    }

    private void CreateFace(ushort[] i, int x, int y, int z)
    {
        Vector3 offset = new Vector3(x, y, z);
        Vector3 a = _blockVertices[i[0]] + offset;
        Vector3 b = _blockVertices[i[1]] + offset;
        Vector3 c = _blockVertices[i[2]] + offset;
        Vector3 d = _blockVertices[i[3]] + offset;
        
        st.AddTriangleFan(new Vector3[] {a, b, c});
        st.AddTriangleFan(new Vector3[] {a, c, d});
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
    private static ushort[] _faceBack = new ushort[] { 2, 0, 1, 4 };
    

    public Vector2I ChunkPosition { get; private set; } = Vector2I.Zero;
    public uint ChunkSize { get; protected set; }
}