using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace FNPlugin {
    class VanAllen {
        const double B0 = 3.12E-5;
                
        public static float getBeltAntiparticles(int refBody, float altitude, float lat) {
            lat = (float) (lat/180*Math.PI);
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

			double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude (crefbody);
            if (altitude <= atmosphere_height && crefbody.flightGlobalsIndex != 0) {
                return 0;
            }

            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double peakbelt = 1.5 * crefkerbin.Radius * relrp;
            double altituded = ((double)altitude);
            double a = peakbelt / Math.Sqrt(2);
            double beltparticles = Math.Sqrt(2 / Math.PI)*Math.Pow(altituded,2)*Math.Exp(-Math.Pow(altituded,2)/(2.0*Math.Pow(a,2)))/(Math.Pow(a,3));
            beltparticles = beltparticles * relmp * relrp * relrt*50.0;

            if (crefbody.flightGlobalsIndex == 0) {
                beltparticles = beltparticles / 10000;
            }

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat));

            return (float) beltparticles;
        }

        public static float getBeltMagneticFieldMag(int refBody, float altitude, float lat) {
            lat = (float)(lat / 180 * Math.PI);
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude)+rp;
            double Bmag = B0 * relrt * Math.Pow((rp / altituded), 3) * Math.Sqrt(1 + 3 * Math.Pow(Math.Cos(lat), 2));

            return (float)Bmag;
        }

        public static float getBeltMagneticFieldRadial(int refBody, float altitude, float lat) {
            lat = (float)(lat / 180 * Math.PI);
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
                        
            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
            double Bmag = -2 * relrt * B0 * Math.Pow((rp / altituded), 3) * Math.Cos(lat);

            return (float)Bmag;
        }

        public static float getBeltMagneticFieldAzimuthal(int refBody, float altitude, float lat) {
            lat = (float)(lat / 180 * Math.PI);
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
            double Bmag = -relrt * B0 * Math.Pow((rp / altituded), 3) * Math.Sin(lat);

            return (float)Bmag;
        }

        
    }
}
