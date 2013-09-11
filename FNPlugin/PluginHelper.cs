using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    class PluginHelper {
        public const double FIXED_SAT_ALTITUDE = 13599840256;
        public const int REF_BODY_KERBOL = 0;
        public const int REF_BODY_KERBIN = 1;
        public const int REF_BODY_MUN = 2;
        public const int REF_BODY_MINMUS = 3;
        public const int REF_BODY_MOHO = 4;
        public const int REF_BODY_EVE = 5;
        public const int REF_BODY_DUNA = 6;
        public const int REF_BODY_IKE = 7;
        public const int REF_BODY_JOOL = 8;
        public const int REF_BODY_LAYTHE = 9;
        public const int REF_BODY_VALL = 10;
        public const int REF_BODY_BOP = 11;
        public const int REF_BODY_TYLO = 12;
        public const int REF_BODY_GILLY = 13;
        public const int REF_BODY_POL = 14;
        public const int REF_BODY_DRES = 15;
        public const int REF_BODY_EELOO = 16;
        public static string[] atomspheric_resources = {"Oxygen", "Hydrogen","Argon","Deuterium"};
        public static string[] atomspheric_resources_tocollect = { "Oxidizer", "LiquidFuel", "Argon","Deuterium"};

        public static string getPluginSaveFilePath() {
            return KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/WarpPlugin.cfg";
        }

		public static ConfigNode getPluginSaveFile() {
			ConfigNode config = ConfigNode.Load (PluginHelper.getPluginSaveFilePath ());
			if (config == null) {
				config = new ConfigNode ();
				config.AddValue("writtenat",DateTime.Now.ToString());
				config.Save(PluginHelper.getPluginSaveFilePath ());
			}
			return config;
		}

        public static bool lineOfSightToSun(Vessel vess) {
            Vector3d a = vess.transform.position;
            Vector3d b = FlightGlobals.Bodies[0].transform.position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
                if (referenceBody.flightGlobalsIndex == 0) { // the sun should not block line of sight to the sun
                    continue;
                }
                Vector3d refminusa = referenceBody.position - a;
                Vector3d bminusa = b - a;
                if (Vector3d.Dot(refminusa, bminusa) > 0) {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude) {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static FloatCurve getSatFloatCurve() {
            FloatCurve satcurve = new FloatCurve();
            //satcurve.Add(206000000000, 0,0,0);
            //satcurve.Add(68773560320, 0.5f, 0, 0);
            satcurve.Add(406000000000, 0, 0, 0);
            satcurve.Add(108798722048, 0.015625f, 0, 0);
            satcurve.Add(54399361024, 0.0625f, 0, 0);
            satcurve.Add(27199680512, 0.25f, 0, 0);
            satcurve.Add(13599840256, 1, 0, 0);
            satcurve.Add(6799920128, 4, 0, 0);
            satcurve.Add(3399960064, 16, 0, 0);
            satcurve.Add(1699980032, 64, 0, 0);
            satcurve.Add(849990016, 256, 0, 0);
            satcurve.Add(0, 4000, 0, 0);
            return satcurve;
        }

        public static float getAtmosphereResourceContent(int refBody, int resource) {
            float resourcecontent = 0;
            if (refBody == REF_BODY_KERBIN) {
                if (resource == 0) {
                    resourcecontent = 0.21f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0093f;
                }
            }

            if (refBody == REF_BODY_LAYTHE) {
                if (resource == 0) {
                    resourcecontent = 0.18f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0105f;
                }
            }

            if (refBody == REF_BODY_JOOL) {
                if (resource == 1) {
                    resourcecontent = 0.89f;
                }
				if (resource == 3) {
					resourcecontent = 0.00003f;
				}
            }

            if (refBody == REF_BODY_DUNA) {
                if (resource == 0) {
                    resourcecontent = 0.0013f;
                }
                if (resource == 2) {
                    resourcecontent = 0.0191f;
                }
            }

            if (refBody == REF_BODY_EVE) {
                if (resource == 2) {
                    resourcecontent = 0.00007f;
                }
            }

            return resourcecontent;
        }

        public static float getScienceMultiplier(int refbody) {
            float multiplier = 1;
            if (refbody == REF_BODY_DUNA || refbody == REF_BODY_EVE || refbody == REF_BODY_IKE || refbody == REF_BODY_GILLY) {
                multiplier = 5f;
            }else if (refbody == REF_BODY_MUN || refbody ==  REF_BODY_MINMUS) {
                multiplier = 2.5f;
            }else if (refbody == REF_BODY_JOOL || refbody == REF_BODY_TYLO || refbody == REF_BODY_POL || refbody == REF_BODY_BOP) {
                multiplier = 10f;
            }else if (refbody == REF_BODY_LAYTHE || refbody == REF_BODY_VALL) {
                multiplier = 12f;
            }else if (refbody == REF_BODY_EELOO || refbody == REF_BODY_MOHO) {
                multiplier = 20f;
            }else if (refbody == REF_BODY_DRES) {
                multiplier = 7.5f;
            }else if (refbody == REF_BODY_KERBIN) {
                multiplier = 1f;
            }else {
                multiplier = 0;
            }


            return multiplier;
        }

        

        
    }
}
