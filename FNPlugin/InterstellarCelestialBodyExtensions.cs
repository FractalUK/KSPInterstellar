using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public static class InterstellarCelestialBodyExtensions
    {
        public static double GetBeltAntiparticles(this CelestialBody body, double altitude, double lat)
        {
            lat = (lat / 180 * Math.PI);
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];

            double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude(body);
            if (altitude <= atmosphere_height && body.flightGlobalsIndex != 0)  return 0;

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double peakbelt = 1.5 * crefkerbin.Radius * relrp;
            double altituded = ((double)altitude);
            double a = peakbelt / Math.Sqrt(2);
            double beltparticles = Math.Sqrt(2 / Math.PI) * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2.0 * Math.Pow(a, 2))) / (Math.Pow(a, 3));
            beltparticles = beltparticles * relmp * relrp / relrt * 50.0;

            if (body.flightGlobalsIndex == 0) beltparticles = beltparticles / 1000;

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * body.specialMagneticFieldScaling();
            return beltparticles;
        }


        public static double GetProtonRadiationLevel(this CelestialBody body, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];
            double atmosphere = FlightGlobals.getStaticPressure(altitude, body);
            double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude(body);
            double atmosphere_scaling = Math.Exp(-atmosphere);

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double peakbelt = body.GetPeakProtonBeltAltitude(altitude, lat);
            double altituded = altitude;
            double a = peakbelt / Math.Sqrt(2);
            double beltparticles = Math.Sqrt(2 / Math.PI) * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2.0 * Math.Pow(a, 2))) / (Math.Pow(a, 3));
            beltparticles = beltparticles * relrp / relrt * 50.0;

            if (body.flightGlobalsIndex == 0)
            {
                beltparticles = beltparticles / 1000;
            }

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * body.specialMagneticFieldScaling() *atmosphere_scaling;

            return beltparticles;
        }

        public static double GetPeakProtonBeltAltitude(this CelestialBody body, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
            double rp = body.Radius;
            double relrp = rp / crefkerbin.Radius;
            double peakbelt = 1.5 * crefkerbin.Radius * relrp;
            return peakbelt;
        }

        public static double GetElectronRadiationLevel(this CelestialBody body, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];
            double atmosphere = FlightGlobals.getStaticPressure(altitude, body);
            double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude(body);
            double atmosphere_scaling = Math.Exp(-atmosphere);

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double peakbelt2 = body.GetPeakElectronBeltAltitude(altitude, lat);
            double altituded = altitude;
            double b = peakbelt2 / Math.Sqrt(2);
            double beltparticles = 0.9 * Math.Sqrt(2 / Math.PI) * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2.0 * Math.Pow(b, 2))) / (Math.Pow(b, 3));
            beltparticles = beltparticles * relrp / relrt * 50.0;

            if (body.flightGlobalsIndex == 0)
            {
                beltparticles = beltparticles / 1000;
            }

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * body.specialMagneticFieldScaling() * atmosphere_scaling;

            return beltparticles;
        }

        public static double GetPeakElectronBeltAltitude(this CelestialBody body, double altitude, double lat)
        {
            lat = lat / 180 * Math.PI;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
            double rp = body.Radius;
            double relrp = rp / crefkerbin.Radius;
            double peakbelt = 6.0 * crefkerbin.Radius * relrp;
            return peakbelt;
        }

        public static double specialMagneticFieldScaling(this CelestialBody body)
        {
            double special_scaling = 1;
            switch (body.flightGlobalsIndex)
            {
                case (PluginHelper.REF_BODY_TYLO):
                    special_scaling = 7;
                    break;
                case(PluginHelper.REF_BODY_LAYTHE):
                    special_scaling = 5;
                    break;
                case(PluginHelper.REF_BODY_MOHO):
                case(PluginHelper.REF_BODY_EVE):
                    special_scaling = 2;
                    break;
                case(PluginHelper.REF_BODY_JOOL):
                    special_scaling = 3;
                    break;
                case(PluginHelper.REF_BODY_MUN):
                case(PluginHelper.REF_BODY_IKE):
                    special_scaling = 0.2;
                    break;
                case(PluginHelper.REF_BODY_GILLY):
                case(PluginHelper.REF_BODY_BOP):
                case(PluginHelper.REF_BODY_POL):
                    special_scaling = 0.05;
                    break;
                default:
                    special_scaling = 1.0;
                    break;
            }
            return special_scaling;
        }

        public static double GetBeltMagneticFieldMagnitude(this CelestialBody body, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
            double Bmag = VanAllen.B0 / relrt * relmp * Math.Pow((rp / altituded), 3) * Math.Sqrt(1 + 3 * Math.Pow(Math.Cos(mlat), 2)) * body.specialMagneticFieldScaling();

            return Bmag;
        }

        public static double GetBeltMagneticFieldRadial(this CelestialBody body, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
            double Bmag = -2 / relrt * relmp * VanAllen.B0 * Math.Pow((rp / altituded), 3) * Math.Cos(mlat) * body.specialMagneticFieldScaling();

            return Bmag;
        }

        public static double getBeltMagneticFieldAzimuthal(this CelestialBody body, double altitude, double lat)
        {
            double mlat = lat / 180 * Math.PI + Math.PI / 2;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[PluginHelper.REF_BODY_KERBIN];

            double mp = body.Mass;
            double rp = body.Radius;
            double rt = body.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
            double Bmag = -relmp * VanAllen.B0 / relrt * Math.Pow((rp / altituded), 3) * Math.Sin(mlat) * body.specialMagneticFieldScaling();

            return Bmag;
        }
    }
}
