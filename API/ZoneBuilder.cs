using CommonZones.Models;
using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.API;
/// <summary>
/// Used to build a <see cref="ZoneModel"/> from the public API. Performs error checking.<br/>
/// Any fields and properties can also be safely set without using "With" methods.
/// </summary>
public class ZoneBuilder
{
    /// <summary>Name of the zone.</summary>
    public string? Name;
    /// <summary>Short name of the zone, unused in this plugin but other plugins using the API may opt to use it.</summary>
    public string? ShortName;
    /// <summary>X position of the center of the zone.</summary>
    public float X;
    /// <summary>Z position of the center of the zone.</summary>
    public float Z;
    /// <summary>Tells the deserializer whether or not to convert the coordinates from pixel coordinates on the Map.png image or just use world coordinates.</summary>
    public bool UseMapCoordinates;
    /// <summary>Lower limit of the Y of the zone.</summary>
    /// <remarks>Set to <see cref="float.NaN"/> to not have a lower limit.</remarks>
    public float MinHeight = float.NaN;
    /// <summary>Upper limit of the Y of the zone.</summary>
    /// <remarks>Set to <see cref="float.NaN"/> to not have an upper limit.</remarks>
    public float MaxHeight = float.NaN;
    /// <summary>
    /// Sets special properties of the zone that affect it's behavior and the behavior of players in and out of the zone.
    /// </summary>
    /// <remarks>See <see cref="CommonZones.Tags.Tags"/> for more information.</remarks>
    public string[]? Tags;
    internal ZoneModel.Data ZoneData;
    /// <summary>Sets the zone type to <see cref="CircleZone"/> and sets the radius to <see langword="value"/>.</summary>
    public float Radius 
    { 
        set
        {
            this.WithRadius(value);
        }
    }
    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to <see langword="value"/>: (<see cref="Vector2.x"/>, <see cref="Vector2.y"/>).</summary>
    public Vector2 RectSize
    { 
        set
        {
            this.WithRectSize(value.x, value.y);
        }
    }
    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to value: (<see langword="value"/>.x, <see langword="value"/>.z).</summary>
    public (float x, float z) RectSize2
    { 
        set
        {
            this.WithRectSize(value.x, value.z);
        }
    }
    /// <summary>Sets the zone type to <see cref="PolygonZone"/> and sets the corners of the rectangle to <see langword="value"/>.</summary>
    public Vector2[] Points
    {
        set
        {
            this.WithPoints(value);
        } 
    }
    private EZoneType ZoneType = EZoneType.INVALID;
    /// <summary>Set the name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithName(string name)
    {
        this.Name = name;
        return this;
    }
    /// <summary>Set the name and short name.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithName(string name, string shortName)
    {
        this.Name = name;
        this.ShortName = shortName;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(float X, float Z)
    {
        this.X = X;
        this.Z = Z;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(Vector2 position)
    {
        this.X = position.x;
        this.Z = position.y;
        return this;
    }
    /// <summary>Set the center position, required.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithPosition(Vector3 position)
    {
        this.X = position.x;
        this.Z = position.z;
        return this;
    }
    /// <summary>Already default, tells the deserializer to convert the coordinates from pixel coordinates on the Map.png image.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder FromMapCoordinates()
    {
        this.UseMapCoordinates = true;
        return this;
    }
    /// <summary>Already default, tells the deserializer to leave the coordinates as-is.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder FromWorldCoordinates()
    {
        this.UseMapCoordinates = false;
        return this;
    }
    /// <summary>Default <see cref="float.NaN"/>, sets a lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithMinHeight(float minHeight)
    {
        this.MinHeight = minHeight;
        return this;
    }
    /// <summary>Default <see cref="float.NaN"/>, sets an upper limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithMaxHeight(float maxHeight)
    {
        this.MaxHeight = maxHeight;
        return this;
    }
    /// <summary>Removes the lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithoutMinHeight() => WithMinHeight(float.NaN);

    /// <summary>Removes the lower limit to the Y of the zone.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithoutMaxHeight() => WithMaxHeight(float.NaN);

    /// <summary>Sets the tags of the zone, not required, leave blank to clear.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ZoneBuilder WithTags(params string[] tags)
    {
        this.Tags = tags;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="CircleZone"/> and sets the radius to <paramref name="radius"/>.</summary>
    public ZoneBuilder WithRadius(float radius)
    {
        this.ZoneType = EZoneType.CIRCLE;
        this.ZoneData.Radius = radius;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="RectZone"/> and sets the size of the rectangle to (<paramref name="sizeX"/>, <paramref name="sizeZ"/>).</summary>
    public ZoneBuilder WithRectSize(float sizeX, float sizeZ)
    {
        this.ZoneType = EZoneType.RECTANGLE;
        this.ZoneData.SizeX = sizeX;
        this.ZoneData.SizeZ = sizeZ;
        return this;
    }

    /// <summary>Sets the zone type to <see cref="PolygonZone"/> and sets the corners of the rectangle to <paramref name="corners"/>.</summary>
    public ZoneBuilder WithPoints(params Vector2[] corners)
    {
        this.ZoneType = EZoneType.POLYGON;
        this.ZoneData.Points = corners;
        return this;
    }

    /// <summary>
    /// Runs a check on all data.
    /// </summary>
    /// <returns><see cref="ZoneModel"/> representing the converted save for the zone.</returns>
    /// <exception cref="ZoneAPIException">Thrown if there are any problems with the data.</exception>
    /// <exception cref="ZoneReadException">Also thrown if there are any problems with the data.</exception>
    internal ZoneModel Build()
    {
        if (Name == null)
            throw new ZoneAPIException(Error.ERROR_NAME_NULL);
        if (ZoneType != EZoneType.CIRCLE && ZoneType != EZoneType.RECTANGLE && ZoneType != EZoneType.POLYGON)
            throw new ZoneAPIException(Error.ERROR_ZONE_TYPE);
        ZoneModel mdl = new ZoneModel()
        {
            IsValid = false,
            MaximumHeight = MaxHeight,
            MinimumHeight = MinHeight,
            Name = Name,
            ShortName = ShortName,
            Tags = Tags ?? new string[0],
            ZoneType = ZoneType,
            UseMapCoordinates = UseMapCoordinates,
            X = X,
            Z = Z,
            ZoneData = ZoneData,
            IsTemp = true
        };
        mdl.ValidateRead();
        return mdl;
    }
}