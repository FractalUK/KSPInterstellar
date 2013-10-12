using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
	[KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PluginHelper : MonoBehaviour {
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

		protected static bool plugin_init = false;

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

		public static float getMaxAtmosphericAltitude(CelestialBody body) {
			if (!body.atmosphere) {
				return 0;
			}
			return (float) -body.atmosphereScaleHeight * 1000.0f * Mathf.Log(1e-6f);
		}

        public static float getScienceMultiplier(int refbody, bool landed) {
			float multiplier = 1;

			if (refbody == REF_BODY_DUNA || refbody == REF_BODY_EVE || refbody == REF_BODY_IKE || refbody == REF_BODY_GILLY) {
				multiplier = 5f;
			} else if (refbody == REF_BODY_MUN || refbody == REF_BODY_MINMUS) {
				multiplier = 2.5f;
			} else if (refbody == REF_BODY_JOOL || refbody == REF_BODY_TYLO || refbody == REF_BODY_POL || refbody == REF_BODY_BOP) {
				multiplier = 10f;
			} else if (refbody == REF_BODY_LAYTHE || refbody == REF_BODY_VALL) {
				multiplier = 12f;
			} else if (refbody == REF_BODY_EELOO || refbody == REF_BODY_MOHO) {
				multiplier = 20f;
			} else if (refbody == REF_BODY_DRES) {
				multiplier = 7.5f;
			} else if (refbody == REF_BODY_KERBIN) {
				multiplier = 1f;
			} else if (refbody == REF_BODY_KERBOL) {
				multiplier = 15f;
			}else {
				multiplier = 0f;
			}

			if (landed) {
				if (refbody == REF_BODY_TYLO) {
					multiplier = multiplier*3f;
				} else if (refbody == REF_BODY_EVE) {
					multiplier = multiplier*2.5f;
				} else {
					multiplier = multiplier*2f;
				}
			}

            return multiplier;
        }



		public void Update() {
			if (!plugin_init) {
				plugin_init = true;

				List<AvailablePart> available_parts = PartLoader.LoadedPartsList;
				foreach (AvailablePart available_part in available_parts) {
					Part prefab_available_part = available_part.partPrefab;

					try {
						//String path11 = KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/Additions/" + available_part.name + ".cfg";
						//String path21 = KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/Replacements/" + available_part.name + ".cfg";
						String path1 = "WarpPlugin/Additions/" + available_part.name + "/" + available_part.name;
						String path2 = "WarpPlugin/Replacements/" + available_part.name + "/" + available_part.name;

						//ConfigNode config_addition = ConfigNode.Load (path1);
						//ConfigNode config_replacement = ConfigNode.Load(path2);
						ConfigNode config_addition = null;
						ConfigNode config_replacement  = null;

						if(GameDatabase.Instance.ExistsConfigNode(path1)) {
							config_addition = GameDatabase.Instance.GetConfigNode(path1);
						}
						if(GameDatabase.Instance.ExistsConfigNode(path2)) {
							config_replacement = GameDatabase.Instance.GetConfigNode(path2);
						}
						List<ConfigNode> config_nodes = new List<ConfigNode>();
						//ConfigNode.ConfigNodeList config_nodes = new ConfigNode.ConfigNodeList();

						if(config_replacement != null) {

							foreach(ConfigNode conf_node in config_replacement.nodes) {
								config_nodes.Add(conf_node);
							}

							foreach(PartModule pm in prefab_available_part.Modules) {
								prefab_available_part.Modules.Remove(pm);
							}

							available_part.moduleInfo = "";
							available_part.resourceInfo = "";
						}

						if(config_addition != null) {
							foreach(ConfigNode conf_node in config_addition.nodes) {
								config_nodes.Add(conf_node);
							}
						}

						if(config_nodes.Count > 0) {
							print ("[WarpPlugin] PartLoader making update to : " + prefab_available_part.name + " part");
						}

						foreach (ConfigNode config_part_item in config_nodes) {
							if(config_part_item.name == "RESOURCE") {
								PartResource pr = prefab_available_part.AddResource(config_part_item);

								if(available_part.resourceInfo.Length == 0) {
									available_part.resourceInfo = pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
								}else{
									available_part.resourceInfo = available_part.resourceInfo + "\n" + pr.resourceName + ":" + pr.amount + " / " + pr.maxAmount;
								}
							}else if(config_part_item.name == "MODULE") {
								PartModule pm = prefab_available_part.AddModule(config_part_item);

								if(available_part.moduleInfo.Length == 0) {
									if(pm.GetInfo() != null) {
										available_part.moduleInfo = pm.GetInfo();
									}
								}else{
									if(pm.GetInfo() != null) {
										available_part.moduleInfo = "\n" + pm.GetInfo();
									}
								}
							}
						}

					}catch(Exception ex) {
						print ("[WarpPlugin] Exception caught adding to: " + prefab_available_part.name + " part");
					}


				}
			}

			Destroy (this);
		}

		protected static bool warning_displayed = false;

		public static void showInstallationErrorMessage() {
			if (!warning_displayed) {
				PopupDialog.SpawnPopupDialog ("KSP Interstellar Installation Error", "KSP Interstellar is unable to detect files required for the functioning of this rocket.  Please make sure that this mod has been installed to [Base KSP directory]/GameData/WarpPlugin.", "OK", false, HighLogic.Skin);
				warning_displayed = true;
			}
		}

        

        
    }
}
