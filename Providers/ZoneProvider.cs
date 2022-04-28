using CommonZones.Models;
using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.Providers;
public interface IZoneProvider : IDisposable
{
    public List<Zone> Zones { get; }
    void Reload();
    void Save();
}
