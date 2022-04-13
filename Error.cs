using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones;
internal static class Error
{
    internal const string ERROR_NAME_TAKEN = "There is already a zone with that name.";
    internal const string ERROR_NAME_NULL =  "All zones must declare a unique name.";
    internal const string ERROR_ZONE_TYPE =  "Zones must declare at least one type-specific property (Radius, SizeX & SizeY, Points).";
}
