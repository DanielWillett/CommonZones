using CommonZones.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.Zones;

public class RectZone : Zone
{
    private readonly Vector2 Size;
    private readonly Line[] lines;
    private readonly Vector2[] Corners;
    private const float SPACING = 10f;
    /// <inheritdoc/>
    internal RectZone(ref ZoneModel data) : base(ref data)
    {
        if (data.UseMapCoordinates)
        {
            Size = new Vector2(data.ZoneData.SizeX * ImageMultiplier, data.ZoneData.SizeZ * ImageMultiplier);
        }
        else
        {
            Size = new Vector2(data.ZoneData.SizeX, data.ZoneData.SizeZ);
        }
        Corners = new Vector2[4]
        {
            new Vector2(Center.x - Size.x / 2, Center.y - Size.y / 2), //tl
            new Vector2(Center.x + Size.x / 2, Center.y - Size.y / 2), //tr
            new Vector2(Center.x + Size.x / 2, Center.y + Size.y / 2), //br
            new Vector2(Center.x - Size.x / 2, Center.y + Size.y / 2)  //bl
        };
        _bounds = new Vector4(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
        _boundArea = Size.x * Size.y;
        lines = new Line[4]
        {
            new Line(Corners[0], Corners[1]), // tl -> tr
            new Line(Corners[1], Corners[2]), // tr -> br
            new Line(Corners[2], Corners[3]), // br -> bl
            new Line(Corners[3], Corners[0]), // bl -> tl
        };
        GetParticleSpawnPoints(out _, out _);
        SucessfullyParsed = true;
    }
    /// <inheritdoc/>
    public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center)
    {
        corners = Corners;
        center = Center;
        if (_particleSpawnPoints != null) return _particleSpawnPoints;
        List<Vector2> rtnSpawnPoints = new List<Vector2>();
        foreach (Line line in lines)
        {
            if (line.Length == 0) continue;
            float distance = line.NormalizeSpacing(SPACING);
            if (distance != 0) // prevent infinite loops
                for (float i = distance; i < line.Length; i += distance)
                {
                    rtnSpawnPoints.Add(line.GetPointFromP1(i));
                }
        }
        _particleSpawnPoints = rtnSpawnPoints.ToArray();
        return _particleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        return location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.y > Center.y - Size.y / 2 && location.y < Center.y + Size.y / 2;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        return (float.IsNaN(MinHeight) || location.y >= MinHeight) && (float.IsNaN(MaxHeight) || location.y <= MaxHeight) &&
               location.x > Center.x - Size.x / 2 && location.x < Center.x + Size.x / 2 && location.z > Center.y - Size.y / 2 && location.z < Center.y + Size.y / 2;
    }
    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()}. Size: {Size.x}x{Size.y}";
}
