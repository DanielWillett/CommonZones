using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.Tags;
/// <summary>
/// Tags are used to mark the behavior for players inside or outsize zones:<br/>
/// Tags are defined like so:<br/><br/>
/// <code>
/// - #tagname[!][@group][!][$data]
/// -          ^ add this ! to affect people outside the zone instead.
/// -             ^ affects only this group.
/// -                     ^ add this ! to affect everyone but this group instead.
///                          ^ some tags require extra data which should be appended to the end after a dollar sign.
/// - For example:
///     #nodamagedeal@admin!   - In this zone, only admins can deal damage
///     #globalvc!             - Everyone outside the zone has a global vc affect.
///     #group$shopper         - Players in this zone will have the 'shopper' permission group.
///     #group!@admin!$shopper - Players out of this zone will be added to the 'shopper' permission group if they're not in the admin group.
/// </code>
/// </summary>
public static class Tags
{
    /// <summary>Players under this tag's effect can not deal damage to other players.</summary>
    public static readonly string NoDamageGive = "nodamagedeal";
    /// <summary>Players under this tag's effect can not take damage from other players.</summary>
    public static readonly string NoPvPDamageTake = "nopvpdamagetake";
    /// <summary>Players under this tag's effect can not take damage in any way (safezone basically).</summary>
    public static readonly string NoDamageTake = "nodamagetake";
    /// <summary>Players under this tag's effect can not place structures or barricades.</summary>
    /// <remarks>Recommended to use with <see cref="NoSalvaging"/>.</remarks>
    public static readonly string NoBuilding = "nobuilding";
    /// <summary>Players under this tag's effect can not salvage structures or barricades.</summary>
    /// <remarks>Recommended to use with <see cref="NoBuilding"/>.</remarks>
    public static readonly string NoSalvaging = "nosalvage";
    /// <summary>Players under this tag's effect can not use voice chat.</summary>
    public static readonly string NoVoiceChat = "novc";
    /// <summary>All players under this tag's effect can hear each other at full volume through voice chat.</summary>
    public static readonly string GlobalVoiceChat = "globalvc";
    /// <summary>When a player under this tag enters or leaves a zone, a permission group will be given or taken away from them.</summary>
    /// <remarks>Data: group name (string)</remarks>
    public static readonly string Group = "group";
    /// <summary>Players under this tag will have a set gravity multiplier (0+) while in the zone.</summary>
    /// <remarks>Data: gravity multiplier (float)</remarks>
    public static readonly string Gravity = "gravity";
    /// <summary>Players under this tag will have a set jump speed multiplier (0+) while in the zone.</summary>
    /// <remarks>Data: jump speed multiplier (float)</remarks>
    public static readonly string Jump = "jump";
    /// <summary>Players under this tag will have a set movement speed multiplier (0+) while in the zone.</summary>
    /// <remarks>Data: movement speed multiplier (float)</remarks>
    public static readonly string Speed = "speed";

    public static readonly string[] AllTags = typeof(Tags)
        .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
        .Where(x => x.FieldType == typeof(string))
        .Select(x => (string)x.GetValue(null))
        .ToArray();


    internal static unsafe TagData ParseTag(string tag)
    {
        char[] chars = tag.ToCharArray();
        int length = chars.Length;
        TagData rtn = default;
        rtn.Original = tag;
        int tagSt = 0;
        int tagEnd = -1;
        int groupSt = -1;
        int groupEnd = -1;
        int dataSt = -1;
        for (int i = 0; i < length; ++i)
        {
            char c = chars[i];
            if (i == 0 && c == '#')
            {
                tagSt = 1;
                continue;
            }
            if (c == '@' || c == '$' || c == '!')
            {
                bool isLast = i == length - 1;
                char next = isLast ? '\0' : chars[i + 1];
                if (i == tagSt)
                {
                    continue;
                }
                if (tagEnd == -1)
                {
                    tagEnd = i - 1;
                    if (c == '!')
                    {
                        if (groupSt == -1)
                            rtn.TagInverted = true;
                        else if (isLast || next == '$')
                            rtn.GroupInverted = true;
                        if (!isLast)
                        {
                            if (next == '@')
                                groupSt = i + 2;
                            else if (next == '$')
                                dataSt = i + 2;
                            else continue;
                            ++i;
                        }
                    }
                    else if (c == '@')
                    {
                        if (isLast || next == '$' || next == '@' || next == ' ') continue;
                        groupSt = i + 1;
                    }
                    else if (c == '$')
                    {
                        if (isLast || next == '$' || next == '@' || next == ' ') continue;
                        dataSt = i + 1;
                    }
                }
                else if (groupSt != -1 && c != '@' && groupEnd == -1)
                {
                    groupEnd = i - 1;
                    if (c == '!')
                    {
                        if (groupSt == -1)
                            rtn.TagInverted = true;
                        else if (isLast || next == '$')
                            rtn.GroupInverted = true;
                        if (!isLast)
                        {
                            if (next == '@')
                                groupSt = i + 2;
                            else if (next == '$')
                                dataSt = i + 2;
                            else continue;
                            ++i;
                        }
                    }
                    else if (c == '@')
                    {
                        if (isLast || next == '$' || next == '@' || next == ' ') continue;
                        groupSt = i + 1;
                    }
                    else if (c == '$')
                    {
                        if (isLast || next == '$' || next == '@' || next == ' ') continue;
                        dataSt = i + 1;
                    }
                }
            }
        }
        fixed (char* ptr = chars)
        {
            if (tagSt != -1)
            {
                if (tagEnd == -1)
                {
                    tagEnd = length - 1;
                }

                rtn.TagName = new string(ptr, tagSt, tagEnd - tagSt + 1);
            }
            if (groupSt != -1)
            {
                if (groupEnd == -1)
                {
                    groupEnd = length - 1;
                }

                rtn.TagGroup = new string(ptr, groupSt, groupEnd - groupSt + 1);
            }
            if (dataSt != -1)
            {
                rtn.DataString = new string(ptr, dataSt, length - dataSt + 1);
            }
        }
        return rtn;
    }
}
public struct TagData
{
    public string TagName;
    public string? TagGroup;
    public bool TagInverted;
    public bool GroupInverted;
    public string? DataString;
    public string Original;
    public override string ToString()
    {
        string str = $"Tag: {TagName}";
        if (TagInverted)
            str += " (inverted)";
        if (TagGroup != null)
        {
            str += ", Group: " + TagGroup;
            if (GroupInverted)
                str += " (inverted)";
        }
        if (DataString != null)
            str += ", Data: " + DataString;
        return str;
    }
    public override bool Equals(object obj) =>
        obj is TagData data &&
        data.Original.Equals(Original, StringComparison.Ordinal);
    public override int GetHashCode() => Original.GetHashCode();
    public static bool operator ==(TagData a, TagData b) =>
        a.Original.Equals(b.Original, StringComparison.Ordinal);
    public static bool operator !=(TagData a, TagData b) =>
        !a.Original.Equals(b.Original, StringComparison.Ordinal);
}
