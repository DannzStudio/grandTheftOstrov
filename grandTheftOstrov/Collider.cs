using Microsoft.Xna.Framework;
using System;

public class Collider
{
    public Vector2[] Corners = new Vector2[4];

    public void Update(Vector2 position, int width, int height, float rotation)
    {
        Vector2 origin = new Vector2(width / 2f, height / 2f);

        Vector2[] localCorners =
        {
            new Vector2(-origin.X, -origin.Y),
            new Vector2( origin.X, -origin.Y),
            new Vector2( origin.X,  origin.Y),
            new Vector2(-origin.X,  origin.Y)
        };

        float cos = (float)Math.Cos(rotation);
        float sin = (float)Math.Sin(rotation);

        for (int i = 0; i < 4; i++)
        {
            float rx = localCorners[i].X * cos - localCorners[i].Y * sin;
            float ry = localCorners[i].X * sin + localCorners[i].Y * cos;
            Corners[i] = new Vector2(position.X + rx, position.Y + ry);
        }
    }

    public bool Intersects(Collider other, out Vector2 pushNormal, out float pushDepth)
    {
        pushNormal = Vector2.Zero;
        pushDepth = float.MaxValue;

        // 4 kolmé osy (2 z tohoto obdélníku, 2 z druhého)
        Vector2[] axes =
        {
            GetEdgeNormal(Corners[0], Corners[1]),
            GetEdgeNormal(Corners[1], Corners[2]),
            GetEdgeNormal(other.Corners[0], other.Corners[1]),
            GetEdgeNormal(other.Corners[1], other.Corners[2])
        };

        Vector2 centerA = GetCenter(Corners);
        Vector2 centerB = GetCenter(other.Corners);

        foreach (Vector2 axis in axes)
        {
            if (axis.LengthSquared() < 0.0001f)
                continue;

            Vector2 unitAxis = Vector2.Normalize(axis);

            Project(Corners, unitAxis, out float minA, out float maxA);
            Project(other.Corners, unitAxis, out float minB, out float maxB);

            // Mezera na této ose -> žádné prolínání
            if (maxA < minB || maxB < minA)
            {
                pushNormal = Vector2.Zero;
                pushDepth = 0f;
                return false;
            }

            float overlap = Math.Min(maxA, maxB) - Math.Max(minA, minB);

            if (overlap < pushDepth)
            {
                pushDepth = overlap;
                pushNormal = unitAxis;

                // Zajistíme, aby pushNormal ukazovala OD objektu 'other' (budovy) pryč k tomuto objektu (autu)
                Vector2 directionFromOther = centerA - centerB;
                if (Vector2.Dot(directionFromOther, pushNormal) < 0f)
                {
                    pushNormal = -pushNormal;
                }
            }
        }

        return true;
    }

    private static Vector2 GetEdgeNormal(Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        return new Vector2(-edge.Y, edge.X);
    }

    private static Vector2 GetCenter(Vector2[] corners)
    {
        return (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
    }

    private static void Project(Vector2[] corners, Vector2 axis, out float min, out float max)
    {
        min = float.MaxValue;
        max = float.MinValue;

        foreach (Vector2 c in corners)
        {
            float proj = Vector2.Dot(c, axis);
            if (proj < min) min = proj;
            if (proj > max) max = proj;
        }
    }
}