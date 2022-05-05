using CommonZones.API;
using CommonZones.Models;
using CommonZones.Zones;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CommonZones.Providers;
internal class NewtonsoftJsonZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public NewtonsoftJsonZoneProvider(FileInfo file)
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
                    TextReader reader2 = new StreamReader(rs, System.Text.Encoding.UTF8);
                    JsonReader reader = new JsonTextReader(reader2)
                    {
                        Culture = CommonZones.Locale,
                        FloatParseHandling = FloatParseHandling.Double
                    };
                    if (reader.Read() && reader.TokenType == JsonToken.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            if (reader.TokenType == JsonToken.StartObject)
                            {
                                ZoneModel zone = new ZoneModel();
                                // handles any parse exceptions in order to keep one zone from breaking the rest.
                                try
                                {
                                    ReadJsonZone(reader, ref zone);
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
                    reader2.Close();
                    reader2.Dispose();
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
                TextWriter writer2 = new StreamWriter(rs, System.Text.Encoding.UTF8);
                JsonWriter writer = new JsonTextWriter(writer2)
                {
                    Formatting = Formatting.Indented,
                    Culture = CommonZones.Locale,
                    FloatFormatHandling = FloatFormatHandling.Symbol,
                    StringEscapeHandling = StringEscapeHandling.Default
                };
                writer.WriteStartArray();
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Data.IsTemp) continue;
                    ZoneModel mdl = _zones[i].Data;
                    WriteJsonZone(writer, ref mdl);
                }

                writer.WriteEndArray();
                writer.Close();
                writer.Flush();
                writer2.Flush();
                writer2.Dispose();
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
    internal static void ReadJsonZone(JsonReader reader, ref ZoneModel mdl)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonToken.EndObject) break;
            if (reader.TokenType == JsonToken.PropertyName)
            {
#pragma warning disable IDE0019
                string? prop = reader.Value as string;
#pragma warning restore IDE0019
                if (reader.Read() && prop != null)
                {
                    if (prop.Equals("error"))
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = mdl };
                    else if (prop.Equals("name", StringComparison.Ordinal))
                    {
                        if (reader.Value is string str)
                            mdl.Name = str;
                    }
                    else if (prop.Equals("short-name", StringComparison.Ordinal))
                    {
                        if (reader.Value is string str)
                            mdl.ShortName = str;
                    }
                    else if (prop.Equals("x", StringComparison.Ordinal))
                    {
                        if (reader.Value is decimal d)
                            mdl.X = (float)d;
                    }
                    else if (prop.Equals("z", StringComparison.Ordinal))
                    {
                        if (reader.Value is decimal d)
                            mdl.Z = (float)d;
                    }
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal))
                    {
                        if (reader.TokenType != JsonToken.Boolean)
                            mdl.UseMapCoordinates = false;
                        else if (reader.Value is bool b)
                            mdl.UseMapCoordinates = b;
                    }
                    else if (prop.Equals("min-height", StringComparison.Ordinal))
                    {
                        if (reader.Value is decimal d)
                            mdl.MinimumHeight = (float)d;
                    }
                    else if (prop.Equals("max-height", StringComparison.Ordinal))
                    {
                        if (reader.Value is decimal d)
                            mdl.MinimumHeight = (float)d;
                    }
                    else if (prop.Equals("tags", StringComparison.Ordinal))
                    {
                        if (reader.TokenType == JsonToken.Null) continue;
                        List<string> tags = new List<string>();
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.EndArray) break;
                            if (reader.TokenType == JsonToken.String)
                            {
                                if (reader.Value is string str)
                                tags.Add(str);
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
                                    if (reader.TokenType == JsonToken.Float && reader.Value is float f)
                                    {
                                        ((ZoneModel.PropertyData.ModData<float>)data.Modifier)(ref mdl.ZoneData, f);
                                    }
                                    else if (reader.TokenType == JsonToken.StartArray)
                                    {
                                        List<Vector2> v2s = new List<Vector2>(16);
                                        Vector2 current = default;
                                        while (reader.Read())
                                        {
                                            if (reader.TokenType == JsonToken.EndObject)
                                            {
                                                v2s.Add(current);
                                                current = default;
                                            }
                                            else if (reader.TokenType == JsonToken.PropertyName)
                                            {
#pragma warning disable IDE0019
                                                string? prop2 = reader.Value as string;
#pragma warning restore IDE0019
                                                if (reader.Read() && prop2 != null)
                                                {
                                                    if (prop2.Equals("x", StringComparison.Ordinal))
                                                    {
                                                        if (reader.Value is decimal d)
                                                            current.x = (float)d;
                                                    }
                                                    else if (prop2.Equals("z", StringComparison.Ordinal))
                                                    {
                                                        if (reader.Value is decimal d)
                                                            current.y = (float)d;
                                                    }
                                                }
                                            }
                                            else if (reader.TokenType == JsonToken.EndArray) break;
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
    internal static void WriteJsonZone(JsonWriter writer, ref ZoneModel mdl)
    {
        writer.WriteStartObject();
        if (!mdl.IsValid || mdl.ZoneType == EZoneType.INVALID)
        {
            writer.WritePropertyName("error");
            writer.WriteValue(true);
            writer.WriteEndObject();
            return;
        }
        writer.WritePropertyName("name");
        writer.WriteValue(mdl.Name);
        if (mdl.ShortName != null)
        {
            writer.WritePropertyName("short-name");
            writer.WriteValue(mdl.ShortName);
        }
        writer.WritePropertyName("x");
        writer.WriteValue(mdl.X);
        writer.WritePropertyName("z");
        writer.WriteValue(mdl.Z);
        if (mdl.UseMapCoordinates)
        {
            writer.WritePropertyName("use-map-coordinates");
            writer.WriteValue(mdl.UseMapCoordinates);
        }
        if (!float.IsNaN(mdl.MinimumHeight))
        {
            writer.WritePropertyName("min-height");
            writer.WriteValue(mdl.MinimumHeight);
        }
        if (!float.IsNaN(mdl.MaximumHeight))
        {
            writer.WritePropertyName("max-height");
            writer.WriteValue(mdl.MaximumHeight);
        }
        if (mdl.Tags != null && mdl.Tags.Length > 0)
        {
            writer.WritePropertyName("tags");
            writer.WriteStartArray();
            for (int i = 0; i < mdl.Tags.Length; ++i)
            {
                writer.WriteValue(mdl.Tags[i]);
            }
            writer.WriteEndArray();
        }
        switch (mdl.ZoneType)
        {
            case EZoneType.RECTANGLE:
                writer.WritePropertyName("size-x");
                writer.WriteValue(mdl.ZoneData.SizeX);
                writer.WritePropertyName("size-z");
                writer.WriteValue(mdl.ZoneData.SizeZ);
                break;
            case EZoneType.CIRCLE:
                writer.WritePropertyName("radius");
                writer.WriteValue(mdl.ZoneData.Radius);
                break;
            case EZoneType.POLYGON:
                writer.WritePropertyName("points");
                writer.WriteStartArray();
                for (int i = 0; i < mdl.ZoneData.Points.Length; ++i)
                {
                    Vector2 v = mdl.ZoneData.Points[i];
                    writer.WriteStartObject();
                    writer.WritePropertyName("x");
                    writer.WriteValue(v.x);
                    writer.WritePropertyName("z");
                    writer.WriteValue(v.y);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                break;
        }
        writer.WriteEndObject();
    }

    public void Dispose() { }
}
