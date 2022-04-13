using CommonZones.API;
using CommonZones.Models;
using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CommonZones.Providers;
internal class JsonZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public JsonZoneProvider(FileInfo file)
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
                    Utf8JsonReader reader = new Utf8JsonReader(buffer.AsSpan(), CommonZones.JsonReaderOptions);
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
                                    zone.Read(ref reader);
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
                Utf8JsonWriter writer = new Utf8JsonWriter(rs, CommonZones.JsonWriterOptions);
                writer.WriteStartArray();
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Data.IsTemp) continue;
                    _zones[i].Data.Write(writer);
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
}
