using UnityEngine;

namespace Phobos.Helpers;

public static class PathHelper
{
    public static Vector2 ClosestPointOnLine(Vector2 origin, Vector2 target, Vector2 point)
    {
        var vec1 = point - origin;
        var vec2 = (target - origin).normalized;

        var d = Vector3.Distance(origin, target);
        var t = Vector3.Dot(vec2, vec1);

        if (t <= 0) 
            return origin;

        if (t >= d) 
            return target;
 
        var vec3 = vec2 * t;

        return origin + vec3;
    }
    
    public static Vector3 ClosestPointOnLine(Vector3 origin, Vector3 target, Vector3 point)
    {
        var vec1 = point - origin;
        var vec2 = (target - origin).normalized;

        var d = Vector3.Distance(origin, target);
        var t = Vector3.Dot(vec2, vec1);

        if (t <= 0) 
            return origin;

        if (t >= d) 
            return target;
 
        var vec3 = vec2 * t;

        return origin + vec3;
    }

    public static float TotalLength(Vector3[] corners)
    {
        if (corners.Length < 2)
        {
            return 0f;
        }
        
        var length = 0f;

        for (var i = 1; i < corners.Length; i++)
        {
            var prevCorner = corners[i - 1];
            var nextCorner = corners[i];
            
            length += Vector3.Distance(prevCorner, nextCorner);
        }
        
        return length;
    }
    
    public static Vector3 CalcForwardPoint(Vector3[] corners, Vector3 position, int cornerIndex, float targetDistanceSqr)
    {
        if (cornerIndex >= corners.Length)
            return position;

        // NB: Squared distances aren't telescoping so the below implementation is incorrect, but good enough for now.
        // Example: (2^2 + 2^2) = 8 but (2 + 2)^2 = 16
        
        // Track squared distance remaining
        var remainingDistanceSqr = targetDistanceSqr;
        // Start from bot's current position
        var currentPoint = position;
        // Start checking from the next corner
        var currentIndex = cornerIndex;

        while (remainingDistanceSqr > 0 && currentIndex < corners.Length)
        {
            // Calculate vector and squared distance to the next corner
            var toCorner = corners[currentIndex] - currentPoint;
            var distanceToCornerSqr = toCorner.sqrMagnitude;

            // If the next corner is far enough, our target point is along this segment
            if (distanceToCornerSqr >= remainingDistanceSqr)
            {
                // Need actual distance for the final lerp/movement calculation
                var remainingDistance = Mathf.Sqrt(remainingDistanceSqr);
                return currentPoint + toCorner.normalized * remainingDistance;
            }

            // The corner is closer than our remaining distance, so "consume" this segment
            // Subtract squared distance covered
            remainingDistanceSqr -= distanceToCornerSqr;
            // Jump to this corner
            currentPoint = corners[currentIndex];
            // Move to next corner
            currentIndex++;
        }

        // We've run out of path - return the final corner as the furthest point
        return corners[^1];
    }
    
    public static float CalculatePathAngleJitter(Vector3[] path, int startIndex, int count = 2)
    {
        // TODO: Extend this so that we look X meters ahead instead of X corners
        // Clamp count to available corners
        count = Mathf.Min(count, path.Length - startIndex - 2);

        if (count <= 0)
            return 0f;
        
        var angleMax = 0f;

        // Calculate angles between consecutive segments
        for (var i = startIndex; i < startIndex + count; i++)
        {
            var pointA = path[i];
            var pointB = path[i + 1];
            var pointC = path[i + 2];

            // Calculate direction vectors
            var directionAb = (pointB - pointA).normalized;
            var directionBc = (pointC - pointB).normalized;

            // Calculate angle between the two direction vectors
            var angle = Vector3.Angle(directionAb, directionBc);
            if (angle > angleMax)
                angleMax = angle;
        }

        return angleMax;
    }
}