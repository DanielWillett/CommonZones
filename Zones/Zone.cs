using CommonZones.Models;
using CommonZones.Tags;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CommonZones.Zones;
public abstract class Zone
{
    private static bool isReady = false;
    /// <summary>
    /// For converting between image sources and coordinate sources.
    /// </summary>
    protected static float ImageMultiplier;
    private static ushort lvlSize;
    private static ushort lvlBrdr;
    internal static void OnLevelLoaded()
    {
        lvlSize = Level.size;
        lvlBrdr = Level.border;
        ImageMultiplier = (lvlSize - lvlBrdr * 2) / (float)lvlSize;
        isReady = true;
    }
    /// <summary>
    /// Convert 2 <see langword="float"/> that was gotten from the Map image to world coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static (float x, float y) FromMapCoordinates(float x, float y)
    {
        return ((x - lvlSize / 2) * ImageMultiplier, (y - lvlSize / 2) * -ImageMultiplier);
    }
    /// <summary>
    /// Convert a <see cref="Vector2"/> that was gotten from the Map image to world coordinates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static Vector2 FromMapCoordinates(Vector2 v2)
    {
        return new Vector2((v2.x - lvlSize / 2) * ImageMultiplier, (v2.y - lvlSize / 2) * -ImageMultiplier);
    }
    internal readonly bool UseMapCoordinates;
    /// <summary>
    /// The highest Y level where the zone takes effect.
    /// </summary>
    public readonly float MaxHeight;
    /// <summary>
    /// The lowest Y level where the zone takes effect.
    /// </summary>
    public readonly float MinHeight;
    /// <summary>
    /// Zone shape definition type.
    /// </summary>
    public readonly EZoneType Type;
    /// <summary>
    /// The 2D center of the zone (x = x, y = z)
    /// </summary>
    public readonly Vector2 Center;
    private readonly Vector3 _center;
    private Vector3? _c3d;
    /// <summary>
    /// The 3D center of the zone (at terrain height).
    /// </summary>
    public Vector3 Center3D
    {
        get
        {
            if (_c3d.HasValue) return _c3d.Value;
            if (Level.isLoaded)
            {
                _c3d = new Vector3(Center.x, Util.GetHeight(_center, MinHeight), Center.y);
                return _c3d.Value;
            }
            else return _center;
        }
    }
    protected bool SucessfullyParsed = false;
    internal readonly ZoneModel Data;
    protected Vector2[]? _particleSpawnPoints;
    /// <summary>
    /// Display name for the zone.
    /// </summary>
    public readonly string Name;
    /// <summary>
    /// Shorter display name for the zone. Optional.
    /// </summary>
    public readonly string? ShortName;
    protected Vector4 _bounds;
    protected float _boundArea;
    /// <summary>
    /// Square area of the bounds rectangle, good for sorting layers.
    /// </summary>
    /// <remarks>Cached</remarks>
    public float BoundsArea => _boundArea;
    /// <summary>
    /// Rectangular bounds of the zone. (x = left, y = bottom, z = right, w = top)
    /// </summary>
    /// <remarks>Cached</remarks>
    public Vector4 Bounds => _bounds;
    /// <summary>
    /// Check if a 2D <paramref name="location"/> is inside the zone. Doesn't take height into account.
    /// </summary>
    public abstract bool IsInside(Vector2 location);
    /// <summary>
    /// Check if a 3D <paramref name="location"/> is inside the zone. Takes height into account.
    /// </summary>
    public abstract bool IsInside(Vector3 location);
    private TagData[] _tags;
    /// <summary>
    /// Array of all tags applied to this zone. Check <see cref="CommonZones.Tags.Tags"/> for more info.
    /// </summary>
    public TagData[] Tags => _tags;
    internal bool AddTag(string tagData)
    {
        TagData tag = global::CommonZones.Tags.Tags.ParseTag(tagData);
        if (!string.IsNullOrEmpty(tag.TagName))
        {
            Util.AddToArrayManaged(ref _tags, tag);
            return true;
        }
        return false;
    }
    internal bool RemoveTag(string tagData)
    {
        for (int i = 0; i < Tags.Length; ++i)
        {
            if (Tags[i].Original.Equals(tagData, StringComparison.Ordinal))
            {
                Util.RemoveFromArrayManaged(ref _tags, i);
                return true;
            }
        }
        return false;
    }
    internal bool RemoveTag(TagData tagData)
    {
        for (int i = 0; i < Tags.Length; ++i)
        {
            if (Tags[i].Equals(tagData))
            {
                Util.RemoveFromArrayManaged(ref _tags, i);
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Enumerate through all players currently in the zone. Checks on move next.
    /// </summary>
    public IEnumerator<SteamPlayer> EnumerateClients()
    {
        for (int i = 0; i < Provider.clients.Count; i++)
        {
            SteamPlayer player = Provider.clients[i];
            if (IsInside(player.player.transform.position))
                yield return player;
        }
    }
    /// <inheritdoc/>
    public override string ToString() => $"{Name}: {Type.ToString().ToLower()}. ({Center})." +
        $"{(!float.IsNaN(MaxHeight) ? $" Max Height: {MaxHeight}." : string.Empty)}{(!float.IsNaN(MinHeight)? $" Min Height: {MinHeight}." : string.Empty)}";
    /// <summary>
    /// Get the spawnpoints for the border preview.
    /// </summary>
    public abstract Vector2[] GetParticleSpawnPoints(out Vector2[] corners, out Vector2 center);
    /// <summary>
    /// Check if a 2D <paramref name="location"/> is inside the zone's rectangular bounds. Doesn't take height into account.
    /// </summary>
    public bool IsInsideBounds(Vector2 location)
    {
        return location.x >= Bounds.x && location.x <= Bounds.z && location.y >= Bounds.y && location.y <= Bounds.w;
    }
    /// <summary>
    /// Check if a 3D <paramref name="location"/> is inside the zone. Takes height into account.
    /// </summary>
    public bool IsInsideBounds(Vector3 location)
    {
        return location.x >= Bounds.x && location.x <= Bounds.z && location.z >= Bounds.y && location.z <= Bounds.w && (float.IsNaN(MinHeight) || location.y >= MinHeight) && (float.IsNaN(MaxHeight) || location.y <= MaxHeight);
    }
    /// <summary>
    /// Check if the <paramref name="tag"/> is included in <see cref="Tags"/>.
    /// </summary>
    /// <returns>Index of the tag in <see cref="Tags"/>, or -1.</returns>
    public int IndexOfTag(string tag)
    {
        for (int i = 0; i < _tags.Length; ++i)
        {
            ref TagData d = ref _tags[i];
            if (d.TagName.Equals(tag, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }
    private Zone()
    { 
        throw new NotImplementedException();
    }
    /// <summary>
    /// Zones must set <see cref="SucessfullyParsed"/> to <see langword="true"/>.
    /// </summary>
    internal Zone(ref ZoneModel data)
    {
        this.Data = data;
        this.UseMapCoordinates = data.UseMapCoordinates;
        this.Type = data.ZoneType;
        this.ShortName = data.ShortName;
        this.Name = data.Name;
        this.MinHeight = data.MinimumHeight;
        this.MaxHeight = data.MaximumHeight;
        if (data.Tags == null)
        {
            this._tags = new TagData[0];
        }
        else
        {
            this._tags = new TagData[data.Tags.Length];
            for (int i = 0; i < _tags.Length; ++i)
            {
                this._tags[i] = global::CommonZones.Tags.Tags.ParseTag(data.Tags[i]);
            }
            for (int i = _tags.Length - 1; i >= 0; --i)
            {
                // remove any bad tags (unlikely to happen so not very effecient)
                // bad tags can be caused by empty strings, stuff like "#@!", etc
                if (string.IsNullOrEmpty(this._tags[i].TagName))
                {
                    L.LogWarning("Invalid tag in zone " + Name + ": \"" + (_tags[i].TagName ?? "null") + "\"");
                    Util.RemoveFromArrayManaged(ref _tags, i);
                }
            }
        }
        if (data.UseMapCoordinates)
        {
            this.Center = FromMapCoordinates(new Vector2(data.X, data.Z));
            this._center = new Vector3(Center.x, 0f, Center.y);
        }
        else
        {
            this._center = new Vector3(data.X, 0f, data.Z);
            this.Center = new Vector2(_center.x, _center.z);
        }
    }
}
public enum EZoneType : byte
{
    INVALID = 0,
    CIRCLE = 1,
    RECTANGLE = 2,
    POLYGON = 4
}