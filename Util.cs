using Rocket.API;
using SDG.NetTransport;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones;
internal static class Util
{
    public static void AddToArrayManaged<T>(ref T[] array, T obj)
    {
        T[] old = array;
        array = new T[old.Length + 1];
        if (old.Length > 0)
        {
            Array.Copy(old, 0, array, 0, old.Length);
            array[array.Length - 1] = obj;
        }
        else
        {
            array[0] = obj;
        }
    }
    public static unsafe void AddToArrayUnmanaged<T>(ref T[] array, T obj) where T : unmanaged
    {
        T[] old = array;
        array = new T[old.Length + 1];
        if (old.Length > 0)
        {
            Buffer.BlockCopy(old, 0, array, 0, old.Length * sizeof(T));
            array[array.Length - 1] = obj;
        }
        else
        {
            array[0] = obj;
        }
    }
    internal static float GetHeight(Vector3 point, float minHeight)
    {
        float height;
        if (Physics.Raycast(new Ray(new Vector3(point.x, Level.HEIGHT, point.z), Vector3.down), out RaycastHit hit, Level.HEIGHT, RayMasks.BLOCK_COLLISION))
        {
            height = hit.point.y;
            if (!float.IsNaN(minHeight))
                return Mathf.Max(height, minHeight);
            return height;
        }
        else
        {
            height = LevelGround.getHeight(point);
            if (!float.IsNaN(minHeight))
                return Mathf.Max(height, minHeight);
            else return height;
        }
    }
    public static bool RemoveFromArrayManaged<T>(ref T[] array, int index)
    {
        int ol = array.Length;
        if (ol == 0) return false;
        T[] old = array;
        array = new T[ol - 1];
        if (ol == 1) return true;
        if (index != 0)
            Array.Copy(old, 0, array, 0, index);
        Array.Copy(old, index + 1, array, index, ol - index - 1);
        return true;
    }
    public unsafe static bool RemoveFromArrayUnmanaged<T>(ref T[] array, int index) where T : unmanaged
    {
        int ol = array.Length;
        if (ol == 0) return false;
        T[] old = array;
        array = new T[ol - 1];
        if (ol == 1) return true;
        int size = sizeof(T);
        if (index != 0)
            Buffer.BlockCopy(old, 0, array, 0, index * size);
        Buffer.BlockCopy(old, index + 1, array, index, (ol - index - 1) * size);
        return true;
    }
    /// <summary>
    /// There's a rare error that causes a NRE when calling GameObject.get_Transform. This trys the position call and kicks the player if it's unable to be gotten.
    /// </summary>
    public static Vector3 GetPosition(this Player player)
    {
        try
        {
            return player.transform.position;
        }
        catch (NullReferenceException)
        {
            Provider.kick(player.channel.owner.playerID.steamID, Translation.Translate("kick_position_error"));
            return Vector3.zero;
        }
    }
    public static void TriggerEffectReliable(ushort ID, CSteamID player, Vector3 position)
    {
        TriggerEffectParameters p = new TriggerEffectParameters(ID)
        {
            position = position,
            reliable = true,
            relevantPlayerID = player
        };
        EffectManager.triggerEffect(p);
    }
    public static void TriggerEffectReliable(EffectAsset asset, ITransportConnection connection, Vector3 position)
    {
        EffectManager.sendEffectReliable(asset.id, connection, position);
    }
    public static string? GetStringOrNull(this DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
            return null;
        return reader.GetString(ordinal);
    }
}
/// <summary>
/// Struct wrapper for <see cref="IRocketPlayer"/>.
/// </summary>
internal readonly struct PlayerWrapper : IRocketPlayer
{
    public string Id => _id;
    private readonly string _id;
    public readonly Player player;
    public string DisplayName => player.channel.owner.playerID.characterName;
    public bool IsAdmin => player.channel.owner.isAdmin;
    public int CompareTo(object obj) => obj is IRocketPlayer rp ? rp.Id.CompareTo(Id) : 0;
    internal PlayerWrapper(Player player)
    {
        this.player = player;
        this._id = player.channel.owner.playerID.steamID.m_SteamID.ToString(CommonZones.Locale);
    }
}