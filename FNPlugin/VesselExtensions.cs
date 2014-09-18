using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public static class VesselExtensions
    {
        public static bool IsInAtmosphere(this Vessel vessel)
        {
            if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) return true;
            return false;
        }
    }
}
