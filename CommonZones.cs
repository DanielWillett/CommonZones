using CommonZones.API;
using CommonZones.Models;
using CommonZones.Providers;
using CommonZones.Zones;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using UnityEngine;

namespace CommonZones;

public partial class CommonZones : RocketPlugin<CommonZonesConfig>
{
    internal static CommonZones I            = null!;
    internal bool IsLoaded                   = false;
    internal static CultureInfo Locale       = new CultureInfo("en-US");
    public static IZoneProvider ZoneProvider = null!;
    internal static string DataDirectory     = null!;
    private static AssemblyName AssemblyName = null!;
    private bool HasLoaded                   = false;
    protected override void Load()
    {
        I = this;
        AssemblyName = Assembly.GetName();
        Translation.InitTranslations();
        L.LoadColoredConsole();
        L.Log("Loading " + AssemblyName.Name + " by BlazingFlame#0001: " + AssemblyName.Version, ConsoleColor.Magenta);
        DataDirectory = Environment.CurrentDirectory + @"\Plugins\" + AssemblyName.Name + @"\";
        SubscribeEvents();
        if (HasLoaded)
            OnLevelLoaded(2); // rocket reload
        IsLoaded = true;
        HasLoaded = true;
    }
    protected override void Unload()
    {
        IsLoaded = false;
        ZoneProvider = null!;
        L.Log("Unloading CommonZones by BlazingFlame#0001.");
        UnsubscribeEvents();
    }
    private void SubscribeEvents()
    {
        Level.onLevelLoaded += OnLevelLoaded;
        //API.Zones.OnRegisterPluginZones += OnRegisterZones;
        Provider.onServerConnected += OnPlayerConnected;
    }


    private void UnsubscribeEvents()
    {
        Level.onLevelLoaded -= OnLevelLoaded;
        //API.Zones.OnRegisterPluginZones -= OnRegisterZones;
        Provider.onServerConnected -= OnPlayerConnected;
    }
    private void OnLevelLoaded(int level)
    {
        Zone.OnLevelLoaded();
        ZoneProvider = new JsonZoneProvider(new FileInfo(DataDirectory + "zones.json"));
        ZoneProvider.Reload();
        ZonePlayerComponent.UIInit();
    }

    private void OnPlayerConnected(CSteamID playerid)
    {
        Player? player = null;
        for (int i = 0; i < Provider.clients.Count; ++i)
        {
            if (Provider.clients[i].playerID.steamID.m_SteamID == playerid.m_SteamID)
            {
                player = Provider.clients[i].player;
                break;
            }
        }
        if (player == null)
        {
            L.LogWarning("Failed to resolve " + playerid.ToString() + " as a steam64 id.");
            return;
        }
        L.Log(player.name + " connected.", ConsoleColor.Cyan);
        player.gameObject.AddComponent<ZonePlayerComponent>().Init(player);
    }

    private void OnRegisterZones(ZoneBuilderCollection zones)
    {
        ZoneBuilder zone = new ZoneBuilder()
        {
            Name = "O'Leary Prison",
            ShortName = "Prison",
            UseMapCoordinates = true,
            X = 750,
            Z = 1005,
            Radius = 133
        };
        zones.Add(zone);
        zone = new ZoneBuilder()
        {
            Name = "Summerside Military Base",
            ShortName = "Summerside",
            UseMapCoordinates = true,
            X = 532,
            Z = 292.5f
        };
        zone.WithRectSize(122, 123);
        zones.Add(zone);
        zone = new ZoneBuilder()
        {
            Name = "Montague",
            UseMapCoordinates = true,
            X = 1341,
            Z = 1081,
            Points = new Vector2[]
            {
                new Vector2(1345, 1045),
                new Vector2(1345, 1100),
                new Vector2(1264, 1100),
                new Vector2(1264, 1117),
                new Vector2(1418, 1117),
                new Vector2(1418, 1100),
                new Vector2(1362, 1100),
                new Vector2(1362, 1045)
            }
        };
        zones.Add(zone);
    }




    private static readonly JavaScriptEncoder jsEncoder;
    public static readonly JsonSerializerOptions JsonSerializerSettings;
    public static readonly JsonWriterOptions JsonWriterOptions;
    public static readonly JsonReaderOptions JsonReaderOptions;
    static CommonZones()
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
}
