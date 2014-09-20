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

        public static double GetTemperatureofColdestThermalSource(this Vessel vess)
        {
            List<IThermalSource> active_reactors = vess.FindPartModulesImplementing<IThermalSource>().Where(ts => ts.IsActive).ToList();
            return active_reactors.Any() ? active_reactors.Min(ts => ts.CoreTemperature) : double.MaxValue;
        }

        public static bool HasAnyActiveThermalSources(this Vessel vess) {
            return vess.FindPartModulesImplementing<IThermalSource>().Where(ts => ts.IsActive).Any();
        }
    }
}
