using CommonZones.API.Tags;
using CommonZones.Zones;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.Tags;
internal static class TagManager
{
    private static readonly List<TagAttributeData> _tags = new List<TagAttributeData>();
    internal static void RegisterTagsFromAssembly(Assembly assembly)
    {
        Type[] types = assembly.GetTypes();
        int old = _tags.Count;
        for (int i = 0; i < types.Length; ++i)
        {
            if (Attribute.GetCustomAttribute(types[i], typeof(TagAttribute)) is TagAttribute attr)
            {
                Type type = types[i];
                ConstructorInfo[] ctors = type.GetConstructors();
                bool exists = false;
                for (int j = 0; j < ctors.Length; ++j)
                {
                    ParameterInfo[] @params = ctors[j].GetParameters();
                    if (@params.Length != 2) continue;
                    ParameterInfo p1 = @params[0];
                    ParameterInfo p2 = @params[1];
                    if (!p1.ParameterType.Equals(typeof(Zone)) || p1.Attributes != ParameterAttributes.None)
                        continue;
                    if (!p2.ParameterType.Equals(typeof(TagData)) || p2.Attributes != ParameterAttributes.None)
                        continue;
                    exists = true;
                    break;
                }

                if (!exists)
                {
                    L.LogError("Tag Handler class \"" + type.Name + "\" from plugin \"" + type.Assembly.GetName().Name +
                                 "\" does not have a constructor with the arguments: " + type.Name + "(" + nameof(Zone) + ", " + nameof(TagData) + ")");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(attr.TagName))
                {
                    L.LogError("Tag Handler class \"" + type.Name + "\" from plugin \"" + type.Assembly.GetName().Name + " is missing a valid tag name.");
                    continue;
                }
                if (type.Assembly.Equals(CommonZones.I.Assembly))
                    attr.IsInternal = true;

                string t = attr.TagName;
                int index = -1;
                for (int j = 0; j < _tags.Count; ++j)
                {
                    if (_tags[j].AttrData.TagName.Equals(t, StringComparison.OrdinalIgnoreCase))
                    {
                        index = j;
                        break;
                    }
                }
                if (index != -1)
                {
                    L.LogWarning("Found duplicate tag " + t + " from plugin " + assembly.GetName().Name + ", overriding tag from " + _tags[index].Type.Assembly.GetName().Name + ".");
                    DeregisterAny(_tags[index].AttrData.TagName);
                    _tags[index] = new TagAttributeData(attr, type);
                    RegisterAny(t);
                }
                else
                {
                    _tags.Add(new TagAttributeData(attr, type));
                    RegisterAny(t);
                }
            }
        }
        int num = _tags.Count - old;
        if (num > 0)
            L.Log("Plugin " + assembly.GetName().Name + " registered " + num.ToString(CommonZones.Locale) + (num == 1 ? string.Empty : "s") + " tags.", ConsoleColor.Magenta);
        else
            L.LogWarning("Found no tags in plugin " + assembly.GetName().Name);
    }
    /// <summary>Deregister any registered tag of name <paramref name="tag"/>.</summary>
    private static void DeregisterAny(string tag)
    {
        if (CommonZones.ZoneProvider != null && CommonZones.ZoneProvider.Zones.Count > 0)
        {
            List<Zone> zones = CommonZones.ZoneProvider.Zones;
            for (int i = 0; i < zones.Count; ++i)
            {
                Zone zone = zones[i];
                for (int t = 0; t < zone.Tags.Count; ++t)
                {
                    if (zone.Tags[t].TagName.Equals(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        TagHandler? th = zone._tagHandlers[t];
                        if (th == null) continue;
                        else
                        {
                            th.Dispose();
                            zone._tagHandlers[t] = null;
                        }
                    }
                }
            }
        }
    }
    /// <summary>Register any unregistered tag of name <paramref name="tag"/>.</summary>
    private static void RegisterAny(string tag)
    {
        if (CommonZones.ZoneProvider != null && CommonZones.ZoneProvider.Zones.Count > 0)
        {
            List<Zone> zones = CommonZones.ZoneProvider.Zones;
            for (int i = 0; i < zones.Count; ++i)
            {
                Zone zone = zones[i];
                for (int t = 0; t < zone.Tags.Count; ++t)
                {
                    if (zone.Tags[t].TagName.Equals(tag, StringComparison.OrdinalIgnoreCase))
                    {
                        TagHandler? th = zone._tagHandlers[t];
                        if (th != null)
                            th.Dispose();

                        LoadTag(zone, t);
                    }
                }
            }
        }
    }

    internal static void OnZoneLoaded(Zone zone)
    {
        zone._tagHandlers = new TagHandler[zone.Tags.Count];
        for (int i = 0; i < zone.Tags.Count; ++i)
        {
            LoadTag(zone, i);
        }
    }
    internal static bool LoadTag(Zone zone, int tagIndex)
    {
        int index = -1;
        for (int j = 0; j < _tags.Count; ++j)
        {
            if (_tags[j].AttrData.TagName.Equals(zone.Tags[tagIndex].TagName, StringComparison.OrdinalIgnoreCase))
            {
                index = j;
                break;
            }
        }
        if (index == -1)
        {
            zone._tagHandlers[tagIndex] = null;
            L.LogWarning("Unrecognized tag " + zone.Tags[tagIndex].TagName + " in zone " + zone.Name);
            return false;
        }
        TagAttributeData tad = _tags[index];
        try
        {
            // constructors are checked on load.
            zone._tagHandlers[tagIndex] = (TagHandler)Activator.CreateInstance(tad.Type, new object[] { zone, zone.Tags[tagIndex] });
            TagHandler? th = zone._tagHandlers[tagIndex];
            if (th == null || !th.Initialized)
            {
                zone._tagHandlers[tagIndex] = null;
                L.LogWarning("Failed to set up tag " + zone.Tags[tagIndex].ToString() + " for zone " + zone.Name + " from plugin " + tad.Type.Assembly.GetName().Name + ". Check for error messages above.");
                return false;
            }
            return true;
        }
        catch (MissingMethodException)
        {
            zone._tagHandlers[tagIndex] = null;
            L.LogWarning("Tag Handler class \"" + tad.Type.Name + "\" from plugin \"" + tad.Type.Assembly.GetName().Name +
                         "\" does not have a constructor with the arguments: " + tad.Type.Name + "(" + nameof(Zone) + ", " + nameof(TagData) + ")");
            return false;
        }
        catch (Exception ex)
        {
            zone._tagHandlers[tagIndex] = null;
            L.LogWarning("Tag " + zone.Tags[tagIndex].TagName + " in plugin " + tad.Type.Assembly.GetName().Name + " failed to initialize: \n" + ex.ToString());
            return false;
        }
    }
    private readonly struct TagAttributeData
    {
        public readonly TagAttribute AttrData;
        public readonly Type Type;

        public TagAttributeData(TagAttribute attrData, Type type)
        {
            AttrData = attrData;
            Type = type;
        }
    }
}
