using CommonZones.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.API;
/// <summary>Used to add temporary zones to the zone providers.</summary>
public class ZoneBuilderCollection
{
    private readonly List<ZoneModel> _list;
    internal ZoneBuilderCollection() 
    {
        _list = new List<ZoneModel>();
    }
    internal ZoneBuilderCollection(int capacity)
    {
        _list = new List<ZoneModel>(capacity);
    }
    internal ZoneBuilderCollection(IEnumerable<ZoneModel> collection) 
    {
        _list = new List<ZoneModel>(collection);
    }
    public int Count => ((ICollection<ZoneModel>)_list).Count;
    public void Add(ZoneBuilder item)
    {
        for (int i = 0; i < _list.Count; ++i)
        {
            if (_list[i].Name == null)
                throw new ZoneAPIException(Error.ERROR_NAME_NULL);
            if (_list[i].Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ZoneAPIException(Error.ERROR_NAME_TAKEN);
            }
        }
        for (int i = 0; i < CommonZones.ZoneProvider.Zones.Count; ++i)
        {
            if (CommonZones.ZoneProvider.Zones[i].Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ZoneAPIException(Error.ERROR_NAME_TAKEN);
            }
        }
        ZoneModel mdl = item.Build();
        _list.Add(mdl);
    }
    internal bool Contains(ZoneModel item) => ((ICollection<ZoneModel>)_list).Contains(item);
    internal void CopyTo(ZoneModel[] array, int arrayIndex) => ((ICollection<ZoneModel>)_list).CopyTo(array, arrayIndex);
    internal IEnumerator<ZoneModel> GetEnumerator() => ((IEnumerable<ZoneModel>)_list).GetEnumerator();
    internal int IndexOf(ZoneModel item) => ((IList<ZoneModel>)_list).IndexOf(item);
    public void Insert(int index, ZoneBuilder item)
    {
        for (int i = 0; i < _list.Count; ++i)
        {
            if (_list[i].Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ZoneAPIException(Error.ERROR_NAME_TAKEN);
            }
        }
        for (int i = 0; i < CommonZones.ZoneProvider.Zones.Count; ++i)
        {
            if (CommonZones.ZoneProvider.Zones[i].Name.Equals(item.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ZoneAPIException(Error.ERROR_NAME_TAKEN);
            }
        }
        ZoneModel mdl = item.Build();
        _list.Insert(index, mdl);
    }
    internal ZoneModel this[int index] => _list[index];
    internal void Merge(ZoneBuilderCollection collection)
    {
        this._list.AddRange(collection._list);
    }
    internal bool Remove(ZoneModel item) => ((ICollection<ZoneModel>)_list).Remove(item);
    internal void RemoveAt(int index) => ((IList<ZoneModel>)_list).RemoveAt(index);
}