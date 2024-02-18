using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Lavender.Common.Data.Saving.Mapping;
using Lavender.Common.Utils;

namespace Lavender.Common.Managers;

public partial class PathManager : LoadableNode
{

    public async Task LoadMap(string mapFilePath)
    {
        VolumetricNavSave navSave = await Task.Run(() => VolumetricNavSave.FromFile(mapFilePath));
        await navSave.SetupPathManager(this);
    }

    public Vector3[] GetPathPoints(Vector3 fromPos, Vector3 toPos)
    {
        List<ulong> openIds = new();
        List<ulong> closedIds = new();
        Dictionary<ulong, ulong> parentsList = new();

        ulong fromId = GetClosestNodeId(fromPos);
        ulong toId = GetClosestNodeId(toPos);

        Vector3I? startPointPos = GetPositionFromId(fromId);
        Vector3I? endPointPos = GetPositionFromId(toId);

        if (startPointPos == null || endPointPos == null)
            return null;
        
        openIds.Add(fromId);

        ulong curId = 0;
        ulong iterationCount = 0;
        while (true)
        {
            iterationCount++;
            if (iterationCount > 6400)
            {
                GD.PrintErr("Aborting Pathfinding: Max Iterations Hit");
                return null;
            }
            float lowestHCost = float.PositiveInfinity;
            foreach (ulong searchId in openIds)
            {
                float cost = GetHCost(searchId, endPointPos.Value);
                if (cost < lowestHCost)
                {
                    lowestHCost = cost;
                    curId = searchId;
                }
            }

            openIds.Remove(curId);
            closedIds.Add(curId);

            if (curId == toId)
            {
                // FINISHED
                List<Vector3> pathList = new();

                ulong curSel = curId;
                while (curSel != fromId)
                {
                    pathList.Add(GetPositionFromId(curSel).Value);
                    curSel = parentsList[curSel];
                }

                pathList.Reverse();
                return pathList.ToArray();
            }

            Vector3I? selectedPos = GetPositionFromId(curId);
            
            if (selectedPos == null)
                return null;

            float currentNodeGCost = GetGCost(curId, startPointPos.Value);
            
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;
                        
                        Vector3I neighborPos = new Vector3I(x, y, z) + selectedPos.Value;
                        ulong neighborId = GetIdFromPosition(neighborPos);

                        if (neighborId == 0)
                            continue;

                        if (closedIds.Contains(neighborId))
                            continue;
                        
                        float neighborMovementCost = currentNodeGCost + MathUtils.FastDistance(selectedPos.Value, neighborPos);
                        float neighborGCost = GetGCost(neighborId, startPointPos.Value);

                        if (neighborMovementCost < neighborGCost || !openIds.Contains(neighborId))
                        {
                            parentsList[neighborId] = curId;
                            
                            if (!openIds.Contains(neighborId))
                                openIds.Add(neighborId);
                        }
                    }
                }
            }
            
            
        }
    }

    public void AddNode(ulong id, Vector3I pos)
    {
        PathNode pathNode = new PathNode(pos);
        _pathPositionNodes.Add(pos, id);
        _pathNodes.Add(id, pathNode);
    }

    public void AddConnection(ulong fromId, ulong toId)
    {
        if (!_pathNodes.TryGetValue(fromId, out PathNode fromNode))
            return;
        fromNode.AddConnection(toId);
    }

    public Vector3I? GetPositionFromId(ulong id)
    {
        if (_pathNodes.TryGetValue(id, out PathNode pathNode))
            return pathNode.Position;
        
        return null;
    }

    public ulong GetIdFromPosition(Vector3I position)
    {
        if (_pathPositionNodes.TryGetValue(position, out ulong foundId))
            return foundId;
        
        return 0;
    }

    public ulong GetClosestNodeId(Vector3 position)
    {
        ulong closestNode = 0;
        float closestDist = Mathf.Inf;

        foreach (KeyValuePair<ulong,PathNode> pair in _pathNodes)
        {
            float dist = MathUtils.FastDistance(position, pair.Value.Position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNode = pair.Key;
            }
        }

        return closestNode;
    }
    public ulong GetSnappedIdFromPosition(Vector3 position)
    {
        Vector3I roughPos = new Vector3I(Mathf.RoundToInt(position.X), Mathf.RoundToInt(position.Y),
            Mathf.RoundToInt(position.Z));
        return GetIdFromPosition(roughPos);
    }

    public bool ArePointsConnected(ulong fromId, ulong toId)
    {
        if (!_pathNodes.TryGetValue(fromId, out PathNode fromNode))
            return false;

        return fromNode.ConnectedNodes.Contains(toId);
    }

    private float GetGCost(ulong id, Vector3 startPos)
    {
        Vector3? nodePos = GetPositionFromId(id);
        if (nodePos == null)
            return Mathf.Inf;
        
        return MathUtils.FastDistance(nodePos.Value, startPos);
    }
    private float GetHCost(ulong id, Vector3 endPos)
    {
        Vector3? nodePos = GetPositionFromId(id);
        if (nodePos == null)
            return Mathf.Inf;
        
        return MathUtils.FastDistance(nodePos.Value, endPos);
    }

    private float GetFCost(float costG, float costH)
    {
        return costG + costH;
    }

    private Dictionary<Vector3I, ulong> _pathPositionNodes = new();
    private Dictionary<ulong, PathNode> _pathNodes = new();

    private class PathNode
    {
        public PathNode(Vector3I position)
        {
            Position = position;
        }

        public void AddConnection(ulong id)
        {
            ConnectedNodes.Add(id);
        }
        
        public Vector3I Position { get; protected set; }
        public List<ulong> ConnectedNodes { get; protected set; } = new();
    }
}