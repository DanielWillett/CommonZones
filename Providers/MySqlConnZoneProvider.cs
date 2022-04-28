using CommonZones.Zones;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones.Providers;
internal class MySqlConnZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public MySqlConnZoneProvider(FileInfo file)
    {
        this._file = file;
        this._zones = new List<Zone>();
    }

    public void Reload() => throw new NotImplementedException();
    public void Save() => throw new NotImplementedException();
    public void Dispose() => throw new NotImplementedException();
}
