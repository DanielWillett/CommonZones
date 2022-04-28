using CommonZones.API;
using CommonZones.Models;
using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.Providers;
/// <summary>
/// Uses the System.Text.Json library.
/// </summary>
internal class SysTextJsonZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public SysTextJsonZoneProvider(FileInfo file)
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
                    long len = rs.Length;
                    if (len > int.MaxValue)
                    {
                        L.LogError("File " + _file.FullName + " is too large.");
                        return;
                    }
                    byte[] buffer = new byte[len];
                    rs.Read(buffer, 0, (int)len);
                    List<ZoneModel> zones = new List<ZoneModel>(_zones.Count);
                    Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), JsonReaderOptions);
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.StartObject)
                            {
                                ZoneModel zone = new ZoneModel();
                                // handles any parse exceptions in order to keep one zone from breaking the rest.
                                try
                                {
                                    ReadJsonZone(ref reader, ref zone);
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
                Utf8JsonWriter writer = new Utf8JsonWriter(rs, JsonWriterOptions);
                writer.WriteStartArray();
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Data.IsTemp) continue;
                    ZoneModel mdl = _zones[i].Data;
                    WriteJsonZone(writer, ref mdl);
                }

                writer.WriteEndArray();
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

    private static readonly JavaScriptEncoder jsEncoder;
    internal static readonly JsonSerializerOptions JsonSerializerSettings;
    internal static readonly JsonWriterOptions JsonWriterOptions;
    internal static readonly JsonReaderOptions JsonReaderOptions;
    static SysTextJsonZoneProvider()
    {
        jsEncoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        JsonSerializerSettings = new JsonSerializerOptions()
        {
            WriteIndented = true,
            IncludeFields = true,
            AllowTrailingCommas = true,
            Encoder = jsEncoder
        };
        JsonWriterOptions = new JsonWriterOptions() { Indented = true, Encoder = jsEncoder };
        JsonReaderOptions = new JsonReaderOptions() { AllowTrailingCommas = true };
    }

    internal static void ReadJsonZone(ref Utf8JsonReader reader, ref ZoneModel mdl)
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
                        throw new ZoneReadException("The zone being read was corrupted on write.") { Data = mdl };
                    else if (prop.Equals("name", StringComparison.Ordinal))
                        mdl.Name = reader.GetString() ?? string.Empty;
                    else if (prop.Equals("short-name", StringComparison.Ordinal))
                        mdl.ShortName = reader.GetString();
                    else if (prop.Equals("x", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.X);
                    else if (prop.Equals("z", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.Z);
                    else if (prop.Equals("use-map-coordinates", StringComparison.Ordinal))
                        mdl.UseMapCoordinates = reader.TokenType == JsonTokenType.True;
                    else if (prop.Equals("min-height", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.MinimumHeight);
                    else if (prop.Equals("max-height", StringComparison.Ordinal))
                        reader.TryGetSingle(out mdl.MaximumHeight);
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
                                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetSingle(out float f))
                                    {
                                        ((ZoneModel.PropertyData.ModData<float>)data.Modifier)(ref mdl.ZoneData, f);
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
    internal static void WriteJsonZone(Utf8JsonWriter writer, ref ZoneModel mdl)
    {
        writer.WriteStartObject();
        if (!mdl.IsValid || mdl.ZoneType == EZoneType.INVALID)
        {
            writer.WriteBoolean("error", true);
            writer.WriteEndObject();
            return;
        }
        writer.WriteString("name", mdl.Name);
        if (mdl.ShortName != null)
            writer.WriteString("short-name", mdl.ShortName);
        writer.WriteNumber("x", mdl.X);
        writer.WriteNumber("z", mdl.Z);
        if (mdl.UseMapCoordinates)
            writer.WriteBoolean("use-map-coordinates", mdl.UseMapCoordinates);
        if (!float.IsNaN(mdl.MinimumHeight))
            writer.WriteNumber("min-height", mdl.MinimumHeight);
        if (!float.IsNaN(mdl.MaximumHeight))
            writer.WriteNumber("max-height", mdl.MaximumHeight);
        if (mdl.Tags != null && mdl.Tags.Length > 0)
        {
            writer.WritePropertyName("tags");
            writer.WriteStartArray();
            for (int i = 0; i < mdl.Tags.Length; ++i)
            {
                writer.WriteStringValue(mdl.Tags[i]);
            }
            writer.WriteEndArray();
        }
        switch (mdl.ZoneType)
        {
            case EZoneType.RECTANGLE:
                writer.WriteNumber("size-x", mdl.ZoneData.SizeX);
                writer.WriteNumber("size-z", mdl.ZoneData.SizeZ);
                break;
            case EZoneType.CIRCLE:
                writer.WriteNumber("radius", mdl.ZoneData.Radius);
                break;
            case EZoneType.POLYGON:
                writer.WritePropertyName("points");
                writer.WriteStartArray();
                for (int i = 0; i < mdl.ZoneData.Points.Length; ++i)
                {
                    Vector2 v = mdl.ZoneData.Points[i];
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

    public void Dispose() { }
}
