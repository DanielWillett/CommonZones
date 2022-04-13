using CommonZones.Zones;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.API;
public static class Zones
{
    /// <summary>
    /// Use this event to register zones from code at the right time.
    /// <code>
    /// // Load(): 
    /// Zones.OnRegisterPluginZones += RegisterZones;
    /// 
    /// // register a circular zone around PEI's O'Leary Prison
    /// private void RegisterZones(List&lt;ZoneBuidler&gt; zones)
    /// {
    ///     ZoneBuilder zone = new ZoneBuilder();
    ///     zone.WithName("O'Leary Prison", "Prison");  // Optionally declare a short name. Unused in this plugin but could be used in plugins using the API.
    ///     zone.FromMapCoordinates();                  // tells the deserializer to convert the coordinates from pixel coordinates on the Map.png image.
    ///     zone.WithPosition(750, 1005);
    ///     zone.WithRadius(133);                       // automatically sets the zone to be a circle zone.
    ///     zone.WithTags("nodamagegive!");             // makes this the only PvP zone.
    ///     
    ///     zones.Add(zone.Finalize());                 // register the zone
    /// }
    /// </code>
    /// </summary>
    public static event RegisterPluginZones? OnRegisterPluginZones;
    internal static bool PluginZonesHasSubscriptions => OnRegisterPluginZones != null;
    internal static void OnRegisterPluginsNeeded(ZoneBuilderCollection zones)
    {
        if (OnRegisterPluginZones == null) return;
        Delegate[] dels = OnRegisterPluginZones.GetInvocationList();
        for (int i = 0; i < dels.Length; ++i)
        {
            try
            {
                int ct = zones.Count;
                ((RegisterPluginZones)dels[i])(zones);
                if (ct != zones.Count)
                {
                    System.Reflection.MethodInfo method = dels[i].Method;
                    if (method != null)
                    {
                        L.Log("Plugin " + (method.Module?.Assembly?.GetName()?.Name ?? "null") + " registered " + (zones.Count - ct) + " zones.", ConsoleColor.Magenta);
                    }
                    else
                        L.Log("Unknown plugin registered " + (zones.Count - ct) + " zones.", ConsoleColor.Magenta);
                }
            }
            catch (Exception ex)
            {
                System.Reflection.MethodInfo method = dels[i].Method;
                if (method != null)
                    L.LogError("Error adding plugin zones for " + method.Module.Assembly.GetName().Name + ": ");
                else
                    L.LogError("Error adding plugin zones: ");
                L.LogError(ex);
            }
        }
    }
    internal static void OnEnter(Player player, Zone zone)
    {
        player.SendChat("enter_zone_test", zone.Name);
        L.Log("Enter: " + zone.Name + " " + player.name);
    }
    internal static void OnExit(Player player, Zone zone)
    {
        player.SendChat("exit_zone_test", zone.Name);
        L.Log("Exit: " + zone.Name + " " + player.name);
    }
}

public delegate void RegisterPluginZones(ZoneBuilderCollection zones);
