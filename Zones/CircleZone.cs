using CommonZones.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.Zones;

public class CircleZone : Zone
{
    private readonly float _radius;
    /// <summary>
    /// Radius of the circle zone.
    /// </summary>
    public float Radius => _radius;
    private const float SPACING = 18f; // every 5 degrees
    /// <inheritdoc/>
    internal CircleZone(ref ZoneModel data) : base(ref data)
    {
        if (data.UseMapCoordinates)
        {
            _radius = data.ZoneData.Radius * ImageMultiplier;
        }
        else
        {
            _radius = data.ZoneData.Radius;
        }
        GetParticleSpawnPoints(out _, out _);
        float r2 = _radius * 2;
        _boundArea = r2 * r2;
        _bounds = new Vector4(Center.x - _radius, Center.y - _radius, Center.x + _radius, Center.y + _radius);
        SucessfullyParsed = true;
    }
    /// <inheritdoc/>
    public override Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center)
    {
        corners = Array.Empty<Vector2>();
        center = Center;
        if (_particleSpawnPoints != null) return _particleSpawnPoints;
        float pi2F = 2f * Mathf.PI;
        float circumference = pi2F * _radius;
        float spacing = SPACING;
        float answer = circumference / spacing;
        int remainder = (int)Mathf.Round((answer - Mathf.Floor(answer)) * spacing);
        int canfit = (int)Mathf.Floor(answer);
        if (remainder != 0)
        {
            if (remainder < SPACING / 2)     // extend all others
                spacing = circumference / canfit;
            else                                  //add one more and subtend all others
                spacing = circumference / (canfit + 1);
        }
        List<Vector2> rtnSpawnPoints = new List<Vector2>();
        float angleRad = spacing / _radius;
        for (float i = 0; i < pi2F; i += angleRad)
        {
            rtnSpawnPoints.Add(new Vector2(Center.x + (Mathf.Cos(i) * _radius), Center.y + (Mathf.Sin(i) * _radius)));
        }
        _particleSpawnPoints = rtnSpawnPoints.ToArray();
        return _particleSpawnPoints;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector2 location)
    {
        if (!IsInsideBounds(location)) return false;
        float difX = location.x - Center.x;
        float difY = location.y - Center.y;
        float sqrDistance = (difX * difX) + (difY * difY);
        return sqrDistance <= _radius * _radius;
    }
    /// <inheritdoc/>
    public override bool IsInside(Vector3 location)
    {
        if (!IsInsideBounds(location)) return false;
        float difX = location.x - Center.x;
        float difY = location.z - Center.y;
        float sqrDistance = (difX * difX) + (difY * difY);
        return sqrDistance <= _radius * _radius;
    }
    /// <inheritdoc/>
    public override string ToString() => $"{base.ToString()}. Radius: {_radius}";
}