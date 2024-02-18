using Godot;

namespace Lavender.Common.Utils;

public static class MathUtils
{
    public static float Distance(Vector3 start, Vector3 end)
    {
        return Mathf.Sqrt(Mathf.Pow(start.X - end.X, 2) + Mathf.Pow(start.Y - end.Y, 2) + Mathf.Pow(start.Z - end.Z, 2));
    }

    public static float FastDistance(Vector3 start, Vector3 end)
    {
        
        return (Mathf.Pow(start.X - end.X, 2) + Mathf.Pow(start.Y - end.Y, 2) + Mathf.Pow(start.Z - end.Z, 2));
    }
}