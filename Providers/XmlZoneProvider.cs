﻿using CommonZones.API;
using CommonZones.Models;
using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

namespace CommonZones.Providers;
internal class XmlZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public XmlZoneProvider(FileInfo file)
    {
        this._file = file;
        this._zones = new List<Zone>();
    }
    private void AddPluginZones()
    {
        if (API.Zones.PluginZonesHasSubscriptions)
        {
            ZoneBuilderCollection collection = new ZoneBuilderCollection(8);
            API.Zones.OnRegisterPluginsNeeded(collection);
            if (collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; ++i)
                    _zones.Add(collection[i].GetZone());
            }
        }
    }

    public void SaveZone(Zone zone) => Save();
    public void SaveZone(int index) => Save();
    public void Reload()
    {
        _zones.Clear();
        if (_file.Exists)
        {
            FileStream? rs = null;
            List<Exception>? exceptions = null;
            try
            {
                using (rs = new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    List<ZoneModel> zones = new List<ZoneModel>(_zones.Count);
                    XmlReader reader = XmlReader.Create(rs, XmlReaderOptions);
                    if (reader.Read())
                    {
                        while (reader.Read())
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Value == "Zones") continue;
                                ZoneModel zone = new ZoneModel();
                                // handles any parse exceptions in order to keep one zone from breaking the rest.
                                try
                                {
                                    ReadXml(ref zone, reader);
                                }
                                catch (Exception ex)
                                {
                                    if (exceptions == null)
                                        exceptions = new List<Exception>(1) { ex };
                                    else
                                        exceptions.Add(ex);
                                }
                                if (zone.IsValid)
                                    zones.Add(zone);
                            }
                        }
                    }
                    _zones.Capacity = zones.Count;
                    for (int i = 0; i < zones.Count; ++i)
                    {
                        _zones.Add(zones[i].GetZone());
                    }
                    reader.Close();
                    reader.Dispose();
                    rs.Close();
                    rs.Dispose();
                }
                AddPluginZones();
                return;
            }
            catch (Exception ex)
            {
                if (exceptions == null)
                    exceptions = new List<Exception>(1) { ex };
                else
                    exceptions.Insert(0, ex);
                if (rs != null)
                {
                    rs.Close();
                    rs.Dispose();
                }
            }
            AddPluginZones();
            if (exceptions != null)
            {
                L.LogError("Failed to deserialize zone data because of the following exceptions: ");
                for (int i = 0; i < exceptions.Count; ++i)
                {
                    L.LogError(exceptions[i]);
                }
                throw exceptions[0];
            }
        }
        else
        {
            AddPluginZones();
            Save();
        }
    }
    public void Save()
    {
        try
        {
            if (!_file.Exists)
                _file.Create()?.Close();

            using (FileStream rs = new FileStream(_file.FullName, FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                XmlWriter writer = XmlWriter.Create(rs, XmlWriterOptions);
                writer.WriteStartDocument();
                writer.WriteStartElement("Zones");
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Data.IsTemp) continue;
                    ZoneModel mdl = _zones[i].Data;
                    WriteXml(ref mdl, writer);
                }
                writer.WriteEndElement();
                writer.Flush();
                writer.Close();
                writer.Dispose();
                rs.Close();
                rs.Dispose();
            }
            return;
        }
        catch (Exception ex)
        {
            L.LogError("Failed to serialize zone");
            L.LogError(ex);
        }
    }
    public void ReadXml(ref ZoneModel mdl, XmlReader reader)
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
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = mdl };
                    else if (prop.Equals("name", StringComparison.Ordinal))
                        mdl.Name = reader.Value;
                    else if (prop.Equals("short-name", StringComparison.Ordinal))
                        mdl.ShortName = reader.Value;
                    else if (prop.Equals("x", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out mdl.X);
                    else if (prop.Equals("z", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out mdl.Z);
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal))
                        mdl.UseMapCoordinates = reader.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    else if (prop.Equals("min-height", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out mdl.MinimumHeight);
                    else if (prop.Equals("max-height", StringComparison.Ordinal))
                        float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out mdl.MaximumHeight);
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
                        mdl.Tags = tags.ToArray();
                    }
                    else
                    {
                        for (int i = 0; i < ZoneModel.ValidProperties.Length; ++i)
                        {
                            ref ZoneModel.PropertyData data = ref ZoneModel.ValidProperties[i];
                            if (data.Name.Equals(prop, StringComparison.Ordinal))
                            {
                                if (mdl.ZoneType == EZoneType.INVALID || mdl.ZoneType == data.ZoneType)
                                {
                                    mdl.ZoneType = data.ZoneType;
                                    if (reader.NodeType == XmlNodeType.Text && float.TryParse(reader.Value, System.Globalization.NumberStyles.Any, CommonZones.Locale, out float f))
                                    {
                                        ((ZoneModel.PropertyData.ModData<float>)data.Modifier)(ref mdl.ZoneData, f);
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
                                        ((ZoneModel.PropertyData.ModData<Vector2[]>)data.Modifier)(ref mdl.ZoneData, v2s.ToArray());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        mdl.ValidateRead();
    }
    public void WriteXml(ref ZoneModel mdl, XmlWriter writer)
    {
        writer.WriteStartElement("zone");
        if (!mdl.IsValid || mdl.ZoneType == EZoneType.INVALID)
        {
            writer.WriteElementString("error", "true");
            writer.WriteEndElement();
            return;
        }
        writer.WriteElementString("name", mdl.Name);
        if (mdl.ShortName != null)
            writer.WriteElementString("short-name", mdl.ShortName);
        writer.WriteElementString("x", mdl.X.ToString(CommonZones.Locale));
        writer.WriteElementString("z", mdl.Z.ToString(CommonZones.Locale));
        if (mdl.UseMapCoordinates)
            writer.WriteElementString("use-map-coordinates", mdl.UseMapCoordinates ? "true" : "false");
        if (!float.IsNaN(mdl.MinimumHeight))
            writer.WriteElementString("min-height", mdl.MinimumHeight.ToString(CommonZones.Locale));
        if (!float.IsNaN(mdl.MaximumHeight))
            writer.WriteElementString("max-height", mdl.MaximumHeight.ToString(CommonZones.Locale));
        if (mdl.Tags != null && mdl.Tags.Length > 0)
        {
            writer.WriteStartElement("tags");
            for (int i = 0; i < mdl.Tags.Length; ++i)
            {
                writer.WriteElementString("tag", mdl.Tags[i]);
            }
            writer.WriteEndElement();
        }
        switch (mdl.ZoneType)
        {
            case EZoneType.RECTANGLE:
                writer.WriteElementString("size-x", mdl.ZoneData.SizeX.ToString(CommonZones.Locale));
                writer.WriteElementString("size-z", mdl.ZoneData.SizeZ.ToString(CommonZones.Locale));
                break;
            case EZoneType.CIRCLE:
                writer.WriteElementString("radius", mdl.ZoneData.Radius.ToString(CommonZones.Locale));
                break;
            case EZoneType.POLYGON:
                Vector2[] v2 = mdl.ZoneData.Points;
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

    public void Dispose() { }

    internal static readonly XmlWriterSettings XmlWriterOptions;
    internal static readonly XmlReaderSettings XmlReaderOptions;
    static XmlZoneProvider()
    {
        XmlWriterOptions = new XmlWriterSettings() { Async = false, Encoding = System.Text.Encoding.UTF8, Indent = true, WriteEndDocumentOnClose = true };
        XmlReaderOptions = new XmlReaderSettings() { Async = false, ValidationType = ValidationType.None };
    }
}
