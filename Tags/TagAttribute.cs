using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.Tags;
/// <summary>Defines the behavior of a tag handler descendant.</summary>

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TagAttribute : Attribute
{
    private readonly string tagName;
    private readonly bool canTakeGroup;
    private readonly bool canTakeData;
    private readonly bool requiresGroup;
    private readonly bool requiresData;
    /// <summary>Will be set on review, is <see langword="true"/> if the tag is defined within the <see cref="global::CommonZones"/> assembly.</summary>
    internal bool IsInternal;
    /// <summary>
    /// Defines the behavior of a tag handler descendant.
    /// </summary>
    /// <param name="tagName">Name of the tag, this is the first section of the tag.</param>
    /// <param name="canTakeGroup">Whether the tag can utilize a provided group filter.</param>
    /// <param name="canTakeData">Whether the tag can utilize custom data.</param>
    /// <param name="requiresGroup">Whether the tag must have a provided group to function.</param>
    /// <param name="requiresData">Whether the tag must have custom data to function.</param>
    /// <remarks>If <paramref name="canTakeGroup"/> == <see langword="false"/> then <paramref name="requiresGroup"/> will be set to <see langword="false"/>.<br/>
    /// If <paramref name="canTakeData"/> == <see langword="false"/> then <paramref name="requiresData"/> will be set to <see langword="false"/>.</remarks>
    public TagAttribute(string tagName, bool canTakeGroup = true, bool canTakeData = true, bool requiresGroup = false, bool requiresData = false)
    {
        this.tagName = tagName;
        this.canTakeGroup = canTakeGroup;
        this.canTakeData = canTakeData;
        this.requiresGroup = canTakeGroup && requiresGroup;
        this.requiresData = canTakeData && requiresData;
        this.IsInternal = false;
    }
    /// <summary>Name of the tag.</summary>
    /// <remarks>This is the first section of the tag.</remarks>
    public string TagName => tagName;
    /// <summary>Whether the tag can utilize a provided group filter.</summary>
    public bool CanTakeGroup => canTakeGroup;
    /// <summary>Whether the tag can utilize custom data.</summary>
    public bool CanTakeData => canTakeData;
    /// <summary>Whether the tag must have a provided group to function.</summary>
    public bool RequiresGroup => requiresGroup;
    /// <summary>Whether the tag must have custom data to function.</summary>
    public bool RequiresData => requiresData;
}