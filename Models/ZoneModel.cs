﻿using CommonZones.API;
using CommonZones.Zones;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

namespace CommonZones.Models;
internal struct ZoneModel
{
    internal int OptPrimaryKey = -1;
    internal string Name = null!;
    internal string? ShortName;
    internal float X;
    internal float Z;
    internal bool UseMapCoordinates;
    internal float MinimumHeight = float.NaN;
    internal float MaximumHeight = float.NaN;
    internal string[] Tags = Array.Empty<string>();
    internal EZoneType ZoneType = EZoneType.INVALID;
    internal Data ZoneData = new Data();
    internal List<Vector2>? TempPointList = null;
    internal List<string>? TempTagList = null;
    /// <summary>Plugin zones are temporary and are not saved.</summary>
    internal bool IsTemp = false;
    internal struct Data
    {
        internal Vector2[] Points = null!;
        internal float SizeX = float.NaN;
        internal float SizeZ = float.NaN;
        internal float Radius = float.NaN;
        public Data() { }
    }
    internal bool IsValid = false;
    internal static readonly PropertyData[] ValidProperties = new PropertyData[]
    {
        new PropertyData("size-x", EZoneType.RECTANGLE, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeX  = v)),
        new PropertyData("size-z", EZoneType.RECTANGLE, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeZ  = v)),
        new PropertyData("radius", EZoneType.CIRCLE,    (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.Radius = v)),
        new PropertyData("points", EZoneType.POLYGON,   (PropertyData.ModData<Vector2[]>)((ref Data d, Vector2[] v) => d.Points = v))
    };
    internal readonly struct PropertyData
    {
        public readonly string Name;
        public readonly EZoneType ZoneType;
        public readonly Delegate Modifier;
        public PropertyData(string name, EZoneType zoneType, Delegate modifier)
        {
            Name = name;
            ZoneType = zoneType;
            Modifier = modifier;
        }

        public delegate void ModData<T>(ref Data d, T v);
    }
    public ZoneModel()
    {
        ShortName = null;
        X = float.NaN;
        Z = float.NaN;
        UseMapCoordinates = false;
        OptPrimaryKey = -1;
    }

    internal Zone GetZone()
    {
        if (IsValid)
        {
            switch (ZoneType)
            {
                case EZoneType.RECTANGLE:
                    return new RectZone(ref this);
                case EZoneType.CIRCLE:
                    return new CircleZone(ref this);
                case EZoneType.POLYGON:
                    return new PolygonZone(ref this);
            }
        }
        throw new ZoneReadException("Failure when creating a zone object. This JSONZoneData was not read properly.");
    }
    /// <returns><see langword="false"/> if <paramref name="fl"/> is <see cref="float.NaN"/> or <see cref="float.PositiveInfinity"/> or <see cref="float.NegativeInfinity"/>.</returns>
    private bool IsBadFloat(float fl) => float.IsNaN(fl) || float.IsInfinity(fl);
    /// <summary>
    /// Validates all data in this model.
    /// </summary>
    /// <exception cref="ZoneReadException">If data is invalid.</exception>
    internal void ValidateRead()
    {
        if (float.IsNaN(X) || float.IsNaN(Z))
            throw new ZoneReadException("Zones are required to define: x (float), z (float).") { Data = this };
        if (string.IsNullOrEmpty(Name))
            throw new ZoneReadException("Zones are required to define: name (string, max. 128 char), and optionally short-name (string, max. 64 char).") { Data = this };
        else if (Name.Length > 128)
            throw new ZoneReadException("Name must be 128 characters or less.") { Data = this };
        if (ShortName != null && ShortName.Length > 64)
            throw new ZoneReadException("Short name must be 64 characters or less.") { Data = this };
        if (ZoneType == EZoneType.INVALID)
        {
            throw new ZoneReadException("Zone JSON data should have at least one valid data property: " + string.Join(", ", ValidProperties.Select(x => x.Name))) { Data = this };
        }
        if (ZoneType == EZoneType.RECTANGLE)
        {
            if (IsBadFloat(ZoneData.SizeX) || IsBadFloat(ZoneData.SizeZ) || ZoneData.SizeX <= 0 || ZoneData.SizeZ <= 0)
                throw new ZoneReadException("Rectangle zones are required to define: size-x (float, > 0), size-z (float, > 0), and optionally angle (float, degrees).") { Data = this };
        }
        else if (ZoneType == EZoneType.CIRCLE)
        {
            if (IsBadFloat(ZoneData.Radius) || ZoneData.Radius <= 0)
                throw new ZoneReadException("Circle zones are required to define: radius (float, > 0).") { Data = this };
        }
        else if (ZoneType == EZoneType.POLYGON)
        {
            if (ZoneData.Points == null || ZoneData.Points.Length < 3)
                throw new ZoneReadException("Polygon zones are required to define at least 3 points: points ({ \"x\", \"z\" } array).") { Data = this };
        }
        else
        {
            throw new ZoneReadException("Zone JSON data should have at least one valid data property: " + string.Join(", ", ValidProperties.Select(x => x.Name))) { Data = this };
        }
        IsValid = true;
    }
}
