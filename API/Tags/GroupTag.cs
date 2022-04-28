using CommonZones.Tags;
using CommonZones.Zones;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.API.Tags;

[Tag("group", true, true, false, true)]
public class GroupTag : TagHandler
{
    private string group = null!;
    /// <summary>Initializes a tag that will give players inside the zone a rocket permission group, and will remove it when they leave the zone.</summary>
    /// <remarks/>
    /// <inheritdoc/>
    public GroupTag(Zone zone, TagData data) : base(zone, data) { }
    /// <inheritdoc/>
    protected override void OnPlayerEntered(Player player, Zone zone)
    {
        CheckState();
        if (IsZoneInverted)
            RemovePlayer(player);
        else
            AddPlayer(player);
    }
    /// <inheritdoc/>
    protected override void OnPlayerExited(Player player, Zone zone)
    {
        CheckState();
        if (IsZoneInverted)
            AddPlayer(player);
        else
            RemovePlayer(player);
    }

    /// <inheritdoc/>
    protected override bool Init()
    {
        if (string.IsNullOrEmpty(TagData.DataString))
        {
            L.LogWarning("Error with tag " + TagData.Original + " on zone " + Zone.Name + ". Group not specified (specify it in the data string like this: \"group$shopper\").");
            return false;
        }
        group = TagData.DataString!;
        RocketPermissionsGroup? grp = R.Permissions.GetGroup(group);
        if (grp == null)
        {
            RocketPermissionsProviderResult result = group.Equals("default", StringComparison.OrdinalIgnoreCase) ? RocketPermissionsProviderResult.DuplicateEntry : 
                R.Permissions.AddGroup(new RocketPermissionsGroup(group, group, "default", new List<string>(0), new List<Permission>(0)));
            if (result == RocketPermissionsProviderResult.Success)
                grp = R.Permissions.GetGroup(group);
            if (grp == null)
            {
                L.LogWarning("Error with tag " + TagData.Original + " on zone " + Zone.Name + ". Unable to find or create group " + TagData.DataString + ".");
                return false;
            }
        }
        return true;
    }
    private void AddPlayer(Player player)
    {
        if (group == null)
            throw new InvalidOperationException(NOT_INITIALIZED_ERROR);
        PlayerWrapper wrapper = new PlayerWrapper(player);
        if (!IsAffected(this, wrapper)) return;
        RocketPermissionsProviderResult res = R.Permissions.AddPlayerToGroup(group, wrapper);
        if (res == RocketPermissionsProviderResult.Success || res == RocketPermissionsProviderResult.DuplicateEntry) return;
        else if (res == RocketPermissionsProviderResult.GroupNotFound)
        {
            if (Init())
            {
                res = R.Permissions.AddPlayerToGroup(group, wrapper);
                if (res == RocketPermissionsProviderResult.Success) return;
                if (res == RocketPermissionsProviderResult.GroupNotFound)
                {
                    L.LogWarning("Error with tag " + TagData.Original + " on zone " + Zone.Name + ". Unable to find or create group " + TagData.DataString + ".");
                }
            }
        }
        else
        {
            L.LogWarning("Error adding player " + wrapper.DisplayName + " to group " + group + ": " + res.ToString());
        }
    }
    private void RemovePlayer(Player player)
    {
        if (group == null)
            throw new InvalidOperationException(NOT_INITIALIZED_ERROR);
        PlayerWrapper wrapper = new PlayerWrapper(player);
        if (!IsAffected(this, wrapper)) return;
        RocketPermissionsProviderResult res = R.Permissions.RemovePlayerFromGroup(group, wrapper);
        if (res == RocketPermissionsProviderResult.Success || res == RocketPermissionsProviderResult.DuplicateEntry) return;
        else if (res == RocketPermissionsProviderResult.GroupNotFound)
        {
            if (Init())
            {
                res = R.Permissions.RemovePlayerFromGroup(group, wrapper);
                if (res == RocketPermissionsProviderResult.Success) return;
                if (res == RocketPermissionsProviderResult.GroupNotFound)
                {
                    L.LogWarning("Error with tag " + TagData.Original + " on zone " + Zone.Name + ". Unable to find or create group " + TagData.DataString + ".");
                }
            }
        }
        else
        {
            L.LogWarning("Error removing player " + wrapper.DisplayName + " from group " + group + ": " + res.ToString());
        }
    }
}
