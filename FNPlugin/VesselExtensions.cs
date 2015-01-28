extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        public static RadiationDose GetRadiationDose(this Vessel vessel)
        {
            CelestialBody cur_ref_body = vessel.mainBody;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];

            ORSPlanetaryResourcePixel res_pixel = ORSPlanetaryResourceMapData.getResourceAvailability(
                vessel.mainBody.flightGlobalsIndex, 
                InterstellarResourcesConfiguration.Instance.ThoriumTetraflouride, 
                cur_ref_body.GetLatitude(vessel.transform.position), 
                cur_ref_body.GetLongitude(vessel.transform.position));

            double ground_rad = Math.Sqrt(res_pixel.getAmount() * 9e6) / 24 / 365.25 / Math.Max(vessel.altitude / 870, 1);
            
            double proton_rad = cur_ref_body.GetProtonRadiationLevel(FlightGlobals.ship_altitude, FlightGlobals.ship_latitude);
            double electron_rad = cur_ref_body.GetElectronRadiationLevel(FlightGlobals.ship_altitude, FlightGlobals.ship_latitude);
            double divisor = Math.Pow(cur_ref_body.Radius / crefkerbin.Radius, 2.0);
            double proton_rad_level = proton_rad / divisor;
            double electron_rad_level = electron_rad / divisor;

            double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
            double solar_radiation = 0.19 * inv_square_mult;
            double mag_field_strength = cur_ref_body.GetBeltMagneticFieldMagnitude(FlightGlobals.ship_altitude, FlightGlobals.ship_latitude);
            while (cur_ref_body.referenceBody != null)
            {
                CelestialBody old_ref_body = cur_ref_body;
                cur_ref_body = cur_ref_body.referenceBody;
                if (cur_ref_body == old_ref_body)break;
                mag_field_strength += cur_ref_body.GetBeltMagneticFieldMagnitude(Vector3d.Distance(FlightGlobals.ship_position, cur_ref_body.transform.position) - cur_ref_body.Radius, FlightGlobals.ship_latitude);
            }
            if (vessel.mainBody != FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBOL])
                solar_radiation = solar_radiation * Math.Exp(-73840.5645666 * mag_field_strength) * Math.Exp(-vessel.atmDensity * 4.5);
            RadiationDose dose = new RadiationDose(Math.Pow(electron_rad_level / 3e-5, 3.0) * 3.2, ground_rad, solar_radiation + Math.Pow(proton_rad_level / 3e-5, 3.0) * 3.2, 0.0);
            return dose;
        }
    }
}
