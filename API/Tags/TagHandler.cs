using CommonZones.Tags;
using CommonZones.Zones;
using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.API.Tags;
/// <summary>
/// Base class for all tag handlers, internal or 3rd party.
/// <para>Tag Handlers are created for each tag on each zone.<br/>They include <see langword="virtual"/> methods for <see cref="OnPlayerEntered(Player, Zone)"/> and <see cref="OnPlayerExited(Player, Zone)"/>.</para>
/// <para>Use this class along with the <see cref="TagAttribute"/> to define a tag. You must also call <see cref="Tags.RegisterPluginTags"/> once <see cref="CommonZones.OnLoaded"/> is called.</para>
/// </summary>
/// <remarks>They are not <see cref="MonoBehaviour"/>s because most tags will not need to be a component. Override <see cref="TagHandler{TComp}"/> to have an attached <see cref="MonoBehaviour"/>.</remarks>
public abstract class TagHandler : IDisposable
{
    protected const string NOT_INITIALIZED_ERROR = "This tag has not been properly initialized yet.";
    /// <summary>Use this constructor as <see langword="base"/> to create a tag handler.</summary>
    /// <param name="zone">Zone the tag is a part of.</param>
    /// <param name="data">Data about the zone.</param>
    /// <remarks><see cref="Init"/> runs before the parent constructor of this will, so do any initialization in <see cref="Init"/>, leave the constructor empty.</remarks>
    public TagHandler(Zone zone, TagData data)
    {
        Zone = zone;
        TagData = data;
        Initialized = Init();
        L.Log((Initialized ? "Successfully created " : " Failed to create ") + this.GetType().Name);
    }
    /// <summary>Zone that this tag interacts with.</summary>
    public readonly Zone Zone;
    /// <summary>Tag data provided to the <see cref="global::CommonZones.Zones.Zone"/> config file.</summary>
    public readonly TagData TagData;
    /// <summary>Tells whether the <see cref="Init"/> function return true in the constructor.</summary>
    internal readonly bool Initialized = false;
    /// <summary>Should the players *outside* the zone be affected by this tag instead?</summary>
    protected bool IsZoneInverted => TagData.TagInverted;
    /// <summary>Should the group filter be treated as a blacklist instead of a whitelist?</summary>
    protected bool IsGroupInverted => TagData.GroupInverted;
    /// <summary>Initialize the tag (one per zone).</summary>
    /// <returns><see langword="true"/> if the provided data was valid, otherwise <see langword="false"/>.</returns>
    protected abstract bool Init();
    /// <summary>
    /// Throws an error if the tag wasn't initialized properly.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the tag hasn't been successfully initialized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CheckState()
    {
        if (!Initialized) throw new InvalidOperationException(NOT_INITIALIZED_ERROR);
    }

    internal void InvokeOnPlayerEntered(Player player, Zone zone) => 
        OnPlayerEntered(player, zone);
    /// <summary>
    /// This function is called when a player enters the area of the zone, connects while in the zone, or teleports into the zone.
    /// </summary>
    /// <param name="player">The player that entered the zone.</param>
    /// <param name="zone">The zone the player entered.</param>
    protected virtual void OnPlayerEntered(Player player, Zone zone)
    {

    }
    /// <summary>
    /// This function is called when a player leaves the area of the zone, disconnects while in the zone, or teleports out of the zone.
    /// </summary>
    /// <param name="player">The player that left the zone.</param>
    /// <param name="zone">The zone the player left.</param>
    protected virtual void OnPlayerExited(Player player, Zone zone)
    {

    }
    /// <summary>Ensure you call <see langword="base"/> dispose function as it handles destroying <see cref="HelperComponent"/>. Override to unsubscribe from any events, clean up any leaks, etc.</summary>
    public virtual void Dispose()
    {
        L.Log("Disposing " + this.GetType().Name);
    }
    /// <summary>Checks whether the <paramref name="player"/> is affected (from the group filter) given the data in the <paramref name="handler"/>'s <see cref="TagData"/> field.</summary>
    /// <returns><see langword="true"/> if the player should be affected by the tag, otherwise <see langword="false"/>.</returns>
    protected static bool IsAffected(TagHandler handler, IRocketPlayer player)
    {
        return !(handler.TagData.TagGroup != null && !(R.Permissions.GetGroups(player, true).Exists(x => x.Id.Equals(handler.TagData.TagGroup)) ^ handler.IsGroupInverted));
    }
}

/// <summary>
/// Base class for any tag handlers that need a <see cref="MonoBehaviour"/>. The component must also implement <see cref="ITagHandlerComponent"/>.
/// </summary>
/// <remarks>All tag handler components are added to <see cref="CommonZones.I"/>'s <see cref="GameObject"/>.</remarks>
/// <typeparam name="TComp">Monobehaviour that will be automatically created when the tag is constructed.</typeparam>
public abstract class TagHandler<TComp> : TagHandler where TComp : MonoBehaviour, ITagHandlerComponent<TComp>
{
    private readonly TComp _comp;
    /// <summary>Associated <see cref="MonoBehaviour"/> to this handler.</summary>
    public TComp Component { get => _comp; }
    /// <summary>Must be ran from game thread.</summary>
    public TagHandler(Zone zone, TagData data) : base(zone, data)
    {
        ThreadUtil.assertIsGameThread();
        this._comp = CommonZones.I.gameObject.AddComponent<TComp>();
        this._comp.Handler = this;
    }
    public override void Dispose()
    {
        base.Dispose();
        if (_comp != null)
            UnityEngine.Object.Destroy(_comp);
    }
}

/// <summary>Implemented for use in <see cref="TagHandler{TComp}"/> descendants.</summary>
/// <typeparam name="TComp">Implementing class.</typeparam>
public interface ITagHandlerComponent<TComp> where TComp : MonoBehaviour, ITagHandlerComponent<TComp>
{
    /// <summary>The <see cref="TagHandler{TComp}"/> associated with this <see cref="MonoBehaviour"/>.</summary>
    public TagHandler<TComp> Handler { get; internal set; }
}