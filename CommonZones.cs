using CommonZones.API;
using CommonZones.Providers;
using CommonZones.Zones;
using Rocket.Core;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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

    // Library support checks.
    internal bool HasNewtonsoft     = false;
    internal bool HasSysTextJson    = false;
    internal bool HasMySqlData      = false;
    internal bool HasMySqlConnector = false;
    internal bool HasSysXml         = false;
    internal Type? ZoneProviderType = null;

    /// <summary>Called after <see cref="Load"/> has been called. This is where <see cref="API.Tags.Tags.RegisterPluginTags"/> should be called.</summary>
    /// <remarks>If you're looking for where to register plugin zones, look at <see cref="API.Zones.OnRegisterPluginZones"/>.</remarks>
    public static event System.Action? OnLoaded;
    /// <summary>Called after all zones have been read (after level load).</summary>
    public static event System.Action? OnZonesLoaded;
    /// <summary>Called just before the plugin is unloaded.</summary>
    public static event System.Action? OnUnloading;

    protected override void Load()
    {
        I = this;
        AssemblyName = Assembly.GetName();
        Translation.InitTranslations();
        L.LoadColoredConsole();
        L.Log("Loading " + AssemblyName.Name + " by BlazingFlame#0001: " + AssemblyName.Version, ConsoleColor.Magenta);
        DataDirectory = System.Environment.CurrentDirectory + @"\Plugins\" + AssemblyName.Name + @"\";
        LibCheck();
        if (ZoneProviderType == null) return;
        SubscribeEvents();
        Tags.TagManager.RegisterTagsFromAssembly(Assembly);
        if (HasLoaded)
        {
            OnLevelLoaded(2); // rocket reload
        }
        IsLoaded = true;
        HasLoaded = true;
        OnLoaded?.Invoke();
    }
    private void LibCheck()
    {
        bool sTxtJson1 = false, newtonsoft1 = false, sqld1 = false, sqlc1 = false, xml1 = false;

        // private Dictionary<AssemblyName, string> RocketPluginManager.libraries from R.Plugins
        Dictionary<AssemblyName, string>? libs = (Dictionary<AssemblyName, string>?)typeof(RocketPluginManager)?.GetField("libraries", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(R.Plugins);
        bool v = libs != null;
        if (v)
        {
            // Rocket doesn't load libraries until a type from them are needed,
            // so we need to check all the discovered libraries that have yet to be loaded.
            // If they aren't there then we dont check for the type.
            foreach (KeyValuePair<AssemblyName, string> kvp in libs!)
            {
                string n = kvp.Key.FullName;
                if (n.IndexOf("System.Text.Json", StringComparison.OrdinalIgnoreCase) != -1)
                    sTxtJson1 = true;
                else if (n.IndexOf("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase) != -1)
                    newtonsoft1 = true;
                else if (n.IndexOf("MySql.Data", StringComparison.OrdinalIgnoreCase) != -1)
                    sqld1 = true;
                else if (n.IndexOf("MySqlConnector", StringComparison.OrdinalIgnoreCase) != -1)
                    sqlc1 = true;
                else if (n.IndexOf("System.Xml", StringComparison.OrdinalIgnoreCase) != -1)
                    xml1 = true;
            }
            // Also check if the library was already loaded by unturned or rocket's dependencies.
            if (!sTxtJson1 || !newtonsoft1 || !sqld1 || !sqlc1 || !xml1)
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; ++i)
                {
                    string n = assemblies[i].FullName;
                    if (n.IndexOf("System.Text.Json", StringComparison.OrdinalIgnoreCase) != -1)
                        sTxtJson1 = true;
                    else if (n.IndexOf("Newtonsoft.Json", StringComparison.OrdinalIgnoreCase) != -1)
                        newtonsoft1 = true;
                    else if (n.IndexOf("MySql.Data", StringComparison.OrdinalIgnoreCase) != -1)
                        sqld1 = true;
                    else if (n.IndexOf("MySqlConnector", StringComparison.OrdinalIgnoreCase) != -1)
                        sqlc1 = true;
                    else if (n.IndexOf("System.Xml", StringComparison.OrdinalIgnoreCase) != -1)
                        xml1 = true;
                }
            }
        }
        // If the libraries exist, try to get some random types to make sure the library is properly loaded and what we're expecting.
        Type? sysTextJson   =  !v || sTxtJson1   ? Type.GetType("System.Text.Json.Utf8JsonReader, System.Text.Json", false, false) : null;
        Type? newtonsoft    =  !v || newtonsoft1 ? Type.GetType("Newtonsoft.Json.JsonReader, Newtonsoft.Json", false, false) : null;
        Type? mySqlConn     = (!v || sqld1       ? Type.GetType("MySql.Data.MySqlClient.MySqlConnection, MySql.Data", false, false) : null)
                           ?? (!v || sqlc1       ? Type.GetType("MySqlConnector.MySqlClient.MySqlConnection, MySqlConnector", false, false) : null);
        Type? xml           =  !v || xml1        ? Type.GetType("System.Xml.XmlReader, System.Xml", false, false) : null;
        if (!v && (sysTextJson == null || newtonsoft == null || mySqlConn == null || xml == null))
            L.Log("You can ignore any dependency errors above.", ConsoleColor.DarkGray);

        HasSysTextJson      = sysTextJson != null && sysTextJson.Assembly.GetName().FullName.IndexOf("System.Text.Json", StringComparison.OrdinalIgnoreCase) != -1;
        HasNewtonsoft       = newtonsoft  != null && newtonsoft .Assembly.GetName().FullName.IndexOf("Newtonsoft.Json",  StringComparison.OrdinalIgnoreCase) != -1;
        HasSysXml           = xml         != null && xml        .Assembly.GetName().FullName.IndexOf("System.Xml",       StringComparison.OrdinalIgnoreCase) != -1;
        if (mySqlConn == null)
        {
            HasMySqlData        = false;
            HasMySqlConnector   = false;
        }
        else
        {
            HasMySqlData            = mySqlConn.Assembly.GetName().FullName.IndexOf("MySql.Data",     StringComparison.OrdinalIgnoreCase) != -1;
            if (!HasMySqlData)
                HasMySqlConnector   = mySqlConn.Assembly.GetName().FullName.IndexOf("MySqlConnector", StringComparison.OrdinalIgnoreCase) != -1;
        }
        if (HasSysTextJson)
            L.Log("Found System.Text.Json " + sysTextJson!.Assembly.GetName().Version, ConsoleColor.Magenta);
        if (HasNewtonsoft)
            L.Log("Found Newtonsoft.Json " + newtonsoft!.Assembly.GetName().Version, ConsoleColor.Magenta);
        if (HasMySqlData)
            L.Log("Found MySql.Data " + mySqlConn!.Assembly.GetName().Version, ConsoleColor.Magenta);
        else if (HasMySqlConnector)
            L.Log("Found MySqlConnector " + mySqlConn!.Assembly.GetName().Version, ConsoleColor.Magenta);
        if (HasSysXml)
            L.Log("Found System.Xml " + xml!.Assembly.GetName().Version, ConsoleColor.Magenta);

        if (!Enum.TryParse(Configuration.Instance.StorageType, true, out EZoneStorageType zoneStorage))
        {
            goto badzone;
        }

        switch (zoneStorage)
        {
            case EZoneStorageType.JSON:
                if (!HasSysTextJson)
                {
                    if (!HasNewtonsoft)
                    {
                        ZoneProviderType = null;
                        L.LogError("To read zone data with JSON one of the following libraries must be available in Rocket\\Libraries: `Newtonsoft.Json` or `System.Text.Json`.");
                        throw new ZoneAPIException();
                    }
                    else
                    {
                        ZoneProviderType = typeof(NewtonsoftJsonZoneProvider);
                    }
                }
                else
                {
                    ZoneProviderType = typeof(SysTextJsonZoneProvider);
                }
                break;
            case EZoneStorageType.MYSQL:
                if (!HasMySqlData)
                {
                    if (!HasMySqlConnector)
                    {
                        ZoneProviderType = null;
                        L.LogError("To read zone data from MySQL one of the following libraries must be available in Rocket\\Libraries: `MySql.Data` or `MySqlConnector`.");
                        throw new ZoneAPIException();
                    }
                    else
                    {
                        ZoneProviderType = typeof(MySqlConnZoneProvider);
                    }
                }
                else
                {
                    ZoneProviderType = typeof(MySqlDataZoneProvider);
                }
                break;
            case EZoneStorageType.XML:
                if (!HasSysXml)
                {
                    ZoneProviderType = null;
                    L.LogError("To read zone data from XML the library `System.Xml` must be available in Rocket\\Libraries.");
                    throw new ZoneAPIException();
                }
                else
                {
                    ZoneProviderType = typeof(XmlZoneProvider);
                }
                break;
            default: goto badzone;
        }
        if (ZoneProviderType != null)
        {
            if (ZoneProviderType.GetInterface(nameof(IZoneProvider)) == null)
            {
                L.LogError("Type " + ZoneProviderType.Name + " does not implement " + nameof(IZoneProvider) + "!");
                ZoneProviderType = null;
                throw new ZoneAPIException();
            }
            else if (ZoneProviderType.GetConstructor(new Type[] { typeof(FileInfo) }) == null)
            {
                L.LogError("Type " + ZoneProviderType.Name + " does not have a matching constructor: " + ZoneProviderType.Name + "(FileInfo)");
                ZoneProviderType = null;
                throw new ZoneAPIException();
            }
        }
        return;
        badzone:
        ZoneProviderType = null;
        L.LogError("Invalid zone storage type in config, unloading plugin. Options: JSON, MYSQL, XML");
        throw new ZoneAPIException();
    }
    protected override void Unload()
    {
        if (!IsLoaded) return;
        OnUnloading?.Invoke();
        IsLoaded = false;
        if (ZoneProvider != null)
        {
            ZoneProvider.Dispose();
            ZoneProvider = null!;
        }
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
        if (ZoneProviderType == null)
        {
            L.LogError("Failed to load because a provider type was not selected.");
            return;
        }
        Zone.OnLevelLoaded();
        try
        {
            ZoneProvider = (IZoneProvider)Activator.CreateInstance(ZoneProviderType, new FileInfo(DataDirectory + "zones.json"));
            ZoneProvider.Reload();
        }
        catch (ZoneAPIException)
        {
            UnloadPlugin(Rocket.API.PluginState.Failure);
            return;
        }
        ZonePlayerComponent.UIInit();
        OnZonesLoaded?.Invoke();
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
}
