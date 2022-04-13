using CommonZones.API;
using CommonZones.Zones;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using UnityEngine;

namespace CommonZones.Models;
internal struct ZoneModel : IXmlSerializable
{
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
    private static readonly PropertyData[] ValidProperties = new PropertyData[]
    {
        new PropertyData("size-x", EZoneType.RECTANGLE, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeX  = v)),
        new PropertyData("size-z", EZoneType.RECTANGLE, (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.SizeZ  = v)),
        new PropertyData("radius", EZoneType.CIRCLE,    (PropertyData.ModData<float>)    ((ref Data d, float v)     => d.Radius = v)),
        new PropertyData("points", EZoneType.POLYGON,   (PropertyData.ModData<Vector2[]>)((ref Data d, Vector2[] v) => d.Points = v))
    };
    private readonly struct PropertyData
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
    internal void Read(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? prop = reader.GetString();
                if (reader.Read() && prop != null)
                {
                    if (prop.Equals("error"))
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = this };
                    else if (prop.Equals("name", StringComparison.Ordinal))
                        Name = reader.GetString() ?? string.Empty;
                    else if (prop.Equals("short-name", StringComparison.Ordinal))
                        ShortName = reader.GetString();
                    else if (prop.Equals("x", StringComparison.Ordinal))
                        reader.TryGetSingle(out X);
                    else if (prop.Equals("z", StringComparison.Ordinal))
                        reader.TryGetSingle(out Z);
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal))
                        UseMapCoordinates = reader.TokenType == JsonTokenType.True;
                    else if (prop.Equals("min-height", StringComparison.Ordinal))
                        reader.TryGetSingle(out MinimumHeight);
                    else if (prop.Equals("max-height", StringComparison.Ordinal))
                        reader.TryGetSingle(out MaximumHeight);
                    else if (prop.Equals("tags", StringComparison.Ordinal))
                    {
                        if (reader.TokenType == JsonTokenType.Null) continue;
                        List<string> tags = new List<string>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.EndArray) break;
                            if (reader.TokenType == JsonTokenType.String)
                            {
                                tags.Add(reader.GetString()!);
                            }
                        }
                        Tags = tags.ToArray();
                    }
                    else
                    {
                        for (int i = 0; i < ValidProperties.Length; ++i)
                        {
                            ref PropertyData data = ref ValidProperties[i];
                            if (data.Name.Equals(prop, StringComparison.Ordinal))
                            {
                                if (ZoneType == EZoneType.INVALID || ZoneType == data.ZoneType)
                                {
                                    ZoneType = data.ZoneType;
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out float f))
                                    {
                                        ((PropertyData.ModData<float>)data.Modifier)(ref ZoneData, f);
                                    }
                                    else if (reader.TokenType == JsonTokenType.StartArray)
                                    {
                                        List<Vector2> v2s = new List<Vector2>(16);
                                        Vector2 current = default;
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonTokenType.EndObject)
                                            {
                                                v2s.Add(current);
                                                current = default;
                                            }
                                            else if (reader.TokenType == JsonTokenType.PropertyName)
                                            {
                                                string? prop2 = reader.GetString();
                                                if (reader.Read() && prop2 != null)
                                                {
                                                    if (prop2.Equals("x", StringComparison.Ordinal))
                                                    {
                                                        reader.TryGetSingle(out current.x);
                                                    }
                                                    else if (prop2.Equals("z", StringComparison.Ordinal))
                                                    {
                                                        reader.TryGetSingle(out current.y);
                                                    }
                                                }
                                            }
                                            else if (reader.TokenType == JsonTokenType.EndArray) break;
                                        }
                                        ((PropertyData.ModData<Vector2[]>)data.Modifier)(ref ZoneData, v2s.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        ValidateRead();
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
            throw new ZoneReadException("Zones are required to define: name (string), and optionally short-name (string).") { Data = this };
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
    internal void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (!IsValid || ZoneType == EZoneType.INVALID)
        {
            writer.WriteBoolean("error", true);
            writer.WriteEndObject();
            return;
        }
        writer.WriteString("name", Name);
        if (ShortName != null)
            writer.WriteString("short-name", ShortName);
        writer.WriteNumber("x", X);
        writer.WriteNumber("z", Z);
        if (UseMapCoordinates)
            writer.WriteBoolean("use-map-coordinates", UseMapCoordinates);
        if (!float.IsNaN(MinimumHeight))
            writer.WriteNumber("min-height", MinimumHeight);
        if (!float.IsNaN(MaximumHeight))
            writer.WriteNumber("max-height", MaximumHeight);
        if (Tags != null && Tags.Length > 0)
        {
            writer.WritePropertyName("tags");
            writer.WriteStartArray();
            for (int i = 0; i < Tags.Length; ++i)
            {
                writer.WriteStringValue(Tags[i]);
            }
            writer.WriteEndArray();
        }
        switch (ZoneType)
        {
            case EZoneType.RECTANGLE:
                writer.WriteNumber("size-x", ZoneData.SizeX);
                writer.WriteNumber("size-z", ZoneData.SizeZ);
                break;
            case EZoneType.CIRCLE:
                writer.WriteNumber("radius", ZoneData.Radius);
                break;
            case EZoneType.POLYGON:
                writer.WritePropertyName("points");
                writer.WriteStartArray();
                for (int i = 0; i < ZoneData.Points.Length; ++i)
                {
                    Vector2 v = ZoneData.Points[i];
                    writer.WriteStartObject();
                    writer.WriteNumber("x", v.x);
                    writer.WriteNumber("z", v.y);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
        }
        writer.WriteEndObject();
    }

    public XmlSchema GetSchema() => null!;
    public void ReadXml(XmlReader reader)
    {
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement) break;
            if (reader.NodeType == XmlNodeType.Comment) continue;
            if (reader.NodeType == XmlNodeType.Element)
            {
                string prop = reader.Value;
                if (reader.Read())
                {
                    if (prop.Equals("error"))
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = this };
                    else if (prop.Equals("name", StringComparison.Ordinal))
                        Name = reader.Value;
                    else if (prop.Equals("short-name", StringComparison.Ordinal))
                        ShortName = reader.Value;
                    else if (prop.Equals("x", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out X);
                    else if (prop.Equals("z", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out Z);
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal))
                        UseMapCoordinates = reader.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (prop.Equals("min-height", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out MinimumHeight);
                    else if (prop.Equals("max-height", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out MaximumHeight);
                    else if (prop.Equals("tags", StringComparison.Ordinal))
                    {
                        List<string> tags = new List<string>();
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.EndElement) break;
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                reader.Read();
                                if (reader.NodeType == XmlNodeType.Text)
                                {
                                    tags.Add(reader.Value);
                                    reader.Read();
                                }
                                else if (reader.NodeType != XmlNodeType.EndElement)
                                    reader.Read();
                            }
                        }
                        Tags = tags.ToArray();
                    }
                    else
                    {
                        for (int i = 0; i < ValidProperties.Length; ++i)
                        {
                            ref PropertyData data = ref ValidProperties[i];
                            if (data.Name.Equals(prop, StringComparison.Ordinal))
                            {
                                if (ZoneType == EZoneType.INVALID || ZoneType == data.ZoneType)
                                {
                                    ZoneType = data.ZoneType;
                                    if (reader.NodeType == XmlNodeType.Text && float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out float f))
                                    {
                                        ((PropertyData.ModData<float>)data.Modifier)(ref ZoneData, f);
                                    }
                                    else if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        List<Vector2> v2s = new List<Vector2>(16);
                                        Vector2 current = default;
                                        while (reader.Read())
                                        {
                                            if (reader.NodeType == XmlNodeType.EndElement)
                                            {
                                                if (reader.Value.Equals("points")) break;
                                                v2s.Add(current);
                                                current = default;
                                            }
                                            else if (reader.NodeType == XmlNodeType.Attribute)
                                            {
                                                string prop2 = reader.Value;
                                                if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                                                {
                                                    if (prop2.Equals("x", StringComparison.Ordinal))
                                                    {
                                                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out current.x);
                                                    }
                                                    else if (prop2.Equals("z", StringComparison.Ordinal))
                                                    {
                                                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out current.y);
                                                    }
                                                }
                                            }
                                        }
                                        ((PropertyData.ModData<Vector2[]>)data.Modifier)(ref ZoneData, v2s.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element)
                {
                        
                }
            }
        }
        ValidateRead();
    }
    public void WriteXml(XmlWriter writer)
    {
        writer.WriteStartElement("zone");
        if (!IsValid || ZoneType == EZoneType.INVALID)
        {
            writer.WriteElementString("error", "true");
            writer.WriteEndElement();
            return;
        }
        writer.WriteElementString("name", Name);
        if (ShortName != null)
            writer.WriteElementString("short-name", ShortName);
        writer.WriteElementString("x", X.ToString(CommonZones.Locale));
        writer.WriteElementString("z", Z.ToString(CommonZones.Locale));
        if (UseMapCoordinates)
            writer.WriteElementString("use-map-coordinates", UseMapCoordinates ? "true" : "false");
        if (!float.IsNaN(MinimumHeight))
            writer.WriteElementString("min-height", MinimumHeight.ToString(CommonZones.Locale));
        if (!float.IsNaN(MaximumHeight))
            writer.WriteElementString("max-height", MaximumHeight.ToString(CommonZones.Locale));
        if (Tags != null && Tags.Length > 0)
        {
            writer.WriteStartElement("tags");
            for (int i = 0; i < Tags.Length; ++i)
            {
                writer.WriteElementString("tag", Tags[i]);
            }
            writer.WriteEndElement();
        }
        switch (ZoneType)
        {
            case EZoneType.RECTANGLE:
                writer.WriteElementString("size-x", ZoneData.SizeX.ToString(CommonZones.Locale));
                writer.WriteElementString("size-z", ZoneData.SizeZ.ToString(CommonZones.Locale));
                break;
            case EZoneType.CIRCLE:
                writer.WriteElementString("radius", ZoneData.Radius.ToString(CommonZones.Locale));
                break;
            case EZoneType.POLYGON:
                Vector2[] v2 = ZoneData.Points;
                writer.WriteStartElement("points");
                for (int i = 0; i < v2.Length; ++i)
                {
                    Vector2 v = v2[i];
                    writer.WriteStartElement("point");
                    writer.WriteAttributeString("x", v.x.ToString(CommonZones.Locale));
                    writer.WriteAttributeString("z", v.y.ToString(CommonZones.Locale));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                break;
        }
        writer.WriteEndElement();
    }
}
