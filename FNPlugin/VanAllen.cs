extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_1::OpenResourceSystem;

namespace FNPlugin {
    class VanAllen {
        const double B0 = 3.12E-5;
		public static Dictionary<string,double> crew_rad_exposure = new Dictionary<string, double> ();
                
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
            beltparticles = beltparticles * relmp * relrp / relrt*50.0;

            if (crefbody.flightGlobalsIndex == 0) {
                beltparticles = beltparticles / 1000;
            }

			beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * getSpecialMagneticFieldScaling(refBody);

            return (float) beltparticles;
        }

        public static double getRadiationLevel(int refBody, double altitude, double lat) {
            lat = lat / 180 * Math.PI;
            
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
            double atmosphere = FlightGlobals.getStaticPressure(altitude, crefbody);
            double atmosphere_height = PluginHelper.getMaxAtmosphericAltitude(crefbody);
            double atmosphere_scaling = Math.Exp(-atmosphere);
            
            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double peakbelt = 1.5 * crefkerbin.Radius * relrp;
            double peakbelt2 = 6 * crefkerbin.Radius * relrp;
            double altituded = altitude;
            double a = peakbelt / Math.Sqrt(2);
            double b = peakbelt2 / Math.Sqrt(2);
            double beltparticles = Math.Sqrt(2 / Math.PI) * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2.0 * Math.Pow(a, 2))) / (Math.Pow(a, 3)) + 0.9*Math.Sqrt(2 / Math.PI) * Math.Pow(altituded, 2) * Math.Exp(-Math.Pow(altituded, 2) / (2.0 * Math.Pow(b, 2))) / (Math.Pow(b, 3));
            beltparticles = beltparticles * relrp / relrt * 50.0;

            if (crefbody.flightGlobalsIndex == 0) {
                beltparticles = beltparticles / 1000;
            }

            beltparticles = beltparticles * Math.Abs(Math.Cos(lat)) * getSpecialMagneticFieldScaling(refBody)*atmosphere_scaling;

            return beltparticles;
        }

        public static double getRadiationDose(Vessel vessel, double rad_hardness) {
            double radiation_level = 0;
            CelestialBody cur_ref_body = FlightGlobals.ActiveVessel.mainBody;
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            ORSPlanetaryResourcePixel res_pixel = ORSPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", cur_ref_body.GetLatitude(vessel.transform.position), cur_ref_body.GetLongitude(vessel.transform.position));
            double ground_rad = Math.Sqrt(res_pixel.getAmount() * 9e6) / 24 / 365.25 / Math.Max(vessel.altitude / 870, 1);
            double rad = VanAllen.getRadiationLevel(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
            double divisor = Math.Pow(cur_ref_body.Radius / crefkerbin.Radius, 2.0);
            double mag_field_strength = VanAllen.getBeltMagneticFieldMag(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
           // if (cur_ref_body.flightGlobalsIndex == PluginHelper.REF_BODY_KERBOL) {
            //    rad = rad * 1e6;
            //}

            double rad_level = rad / divisor;
            double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
            double solar_radiation = 0.19 * inv_square_mult;
            while (cur_ref_body.referenceBody != null) {
                CelestialBody old_ref_body = cur_ref_body;
                cur_ref_body = cur_ref_body.referenceBody;
                if (cur_ref_body == old_ref_body) {
                    break;
                }
                //rad = VanAllen.getBeltAntiparticles (cur_ref_body.flightGlobalsIndex, (float) (Vector3d.Distance(FlightGlobals.ship_position,cur_ref_body.transform.position)-cur_ref_body.Radius), 0.0f);
                //rad = VanAllen.getRadiationLevel(cur_ref_body.flightGlobalsIndex, (Vector3d.Distance(FlightGlobals.ship_position, cur_ref_body.transform.position) - cur_ref_body.Radius), 0.0);
                mag_field_strength += VanAllen.getBeltMagneticFieldMag(cur_ref_body.flightGlobalsIndex, (float)(Vector3d.Distance(FlightGlobals.ship_position, cur_ref_body.transform.position) - cur_ref_body.Radius), (float)FlightGlobals.ship_latitude);
                //rad_level += rad;
            }
            if (cur_ref_body.flightGlobalsIndex != PluginHelper.REF_BODY_KERBOL) {
                solar_radiation = solar_radiation * Math.Exp(-73840.56456662708394321273809886 * mag_field_strength) * Math.Exp(-vessel.atmDensity * 4.5);
            } 
            radiation_level = (Math.Pow(rad_level / 3e-5, 3.0) * 3.2 + ground_rad + solar_radiation) / rad_hardness;
            return radiation_level;
        }

        public static double getPeakBeltAltitude(int refBody, double altitude, double lat) {
            lat = lat / 180 * Math.PI;
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
            double rp = crefbody.Radius;
            double relrp = rp / crefkerbin.Radius;
            double peakbelt = 1.5 * crefkerbin.Radius * relrp;
            return peakbelt;
        }

        public static float getBeltMagneticFieldMag(int refBody, float altitude, float lat) {
            double mlat = lat / 180 * Math.PI + Math.PI/2;
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;



            double altituded = ((double)altitude)+rp;
            double Bmag = B0 / relrt *relmp * Math.Pow((rp / altituded), 3) * Math.Sqrt(1 + 3 * Math.Pow(Math.Cos(mlat), 2)) * getSpecialMagneticFieldScaling(refBody);

            return (float)Bmag;
        }

        public static float getBeltMagneticFieldRadial(int refBody, float altitude, float lat) {
            double mlat = lat / 180 * Math.PI + Math.PI/2;
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
                        
            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
			double Bmag = -2 / relrt *relmp * B0 * Math.Pow((rp / altituded), 3) * Math.Cos(mlat)* getSpecialMagneticFieldScaling(refBody);

            return (float)Bmag;
        }

        public static float getBeltMagneticFieldAzimuthal(int refBody, float altitude, float lat) {
            double mlat = lat / 180 * Math.PI + Math.PI/2;
            CelestialBody crefbody = FlightGlobals.fetch.bodies[refBody];
            CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            double mp = crefbody.Mass;
            double rp = crefbody.Radius;
            double rt = crefbody.rotationPeriod;
            double relmp = mp / crefkerbin.Mass;
            double relrp = rp / crefkerbin.Radius;
            double relrt = rt / crefkerbin.rotationPeriod;

            double altituded = ((double)altitude) + rp;
			double Bmag = -relmp * B0 / relrt * Math.Pow((rp / altituded), 3) * Math.Sin(mlat)* getSpecialMagneticFieldScaling(refBody);

            return (float)Bmag;
        }

		public static double getSpecialMagneticFieldScaling(int refBody) {
			double special_scaling = 1;
			if (refBody == PluginHelper.REF_BODY_TYLO) {
				special_scaling = 7;
			}
			if (refBody == PluginHelper.REF_BODY_LAYTHE) {
				special_scaling = 5;
			}
			if (refBody == PluginHelper.REF_BODY_MOHO) {
				special_scaling = 2;
			}
			if (refBody == PluginHelper.REF_BODY_JOOL) {
				special_scaling = 3;
			}
			if (refBody == PluginHelper.REF_BODY_EVE) {
				special_scaling = 2;
			}
			if (refBody == PluginHelper.REF_BODY_MUN || refBody == PluginHelper.REF_BODY_IKE) {
				special_scaling = 0.2;
			}
			if (refBody == PluginHelper.REF_BODY_GILLY || refBody == PluginHelper.REF_BODY_BOP || refBody == PluginHelper.REF_BODY_POL) {
				special_scaling = 0.05;
			}
			return special_scaling;
		}

        
    }
}
