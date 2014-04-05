extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_1::OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Radiation Status")]
	class FNModuleRadiation : PartModule	{
		[KSPField(isPersistant = false, guiActive = true, guiName = "Rad.")]
		public string radiationLevel = ":";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Accumulated Dose")]
        public string radiationLevel2 = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rad status")]
        public string radiationStatus = ":";

		public double rad_hardness = 1;

		protected double radiation_level = 0;

        public override void OnSave(ConfigNode node) {
            try {
                if (!vessel.isEVA) {
                    foreach (ProtoCrewMember crewmember in part.protoModuleCrew) {
                        if (VanAllen.crew_rad_exposure.ContainsKey(crewmember.name)) {
                            double current_rad = VanAllen.crew_rad_exposure[crewmember.name];
                            ConfigNode rad_node = new ConfigNode("KERBAL_RADIATION_" + crewmember.name);
                            rad_node.AddValue("lifetimeDose", current_rad);
                            node.AddNode(rad_node);
                        }
                    }
                } else {
                    if (VanAllen.crew_rad_exposure.ContainsKey(vessel.vesselName)) {
                        double current_rad = VanAllen.crew_rad_exposure[vessel.vesselName];
                        ConfigNode rad_node = new ConfigNode("KERBAL_RADIATION_" + vessel.vesselName);
                        rad_node.AddValue("lifetimeDose", current_rad);
                        node.AddNode(rad_node);
                    }
                }
            } catch (Exception ex) { }
        }

        public override void OnLoad(ConfigNode node) {
            try {
                if (!vessel.isEVA) {
                    foreach (ProtoCrewMember crewmember in part.protoModuleCrew) {
                        if (!VanAllen.crew_rad_exposure.ContainsKey(crewmember.name)) {
                            if (node.HasNode("KERBAL_RADIATION_" + crewmember.name)) {
                                ConfigNode rad_node = node.GetNode("KERBAL_RADIATION_" + crewmember.name);
                                if (rad_node.HasValue("lifetimeDose")) {
                                    VanAllen.crew_rad_exposure.Add(crewmember.name, double.Parse(rad_node.GetValue("lifetimeDose")));
                                }
                            }
                        }
                    }
                } else {
                    if (!VanAllen.crew_rad_exposure.ContainsKey(vessel.vesselName)) {
                        if (node.HasNode("KERBAL_RADIATION_" + vessel.vesselName)) {
                            ConfigNode rad_node = node.GetNode("KERBAL_RADIATION_" + vessel.vesselName);
                            if (rad_node.HasValue("lifetimeDose")) {
                                VanAllen.crew_rad_exposure.Add(vessel.vesselName, double.Parse(rad_node.GetValue("lifetimeDose")));
                            }
                        }
                    }
                }
            } catch (Exception ex) { }
        }

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }
            //if (!vessel.isEVA) {
            //    part.force_activate();
            //}
            print("[KSP Interstellar] Radiation Module Loaded.");
            Fields["radiationLevel"].guiActive = true;
		}

        public override void OnUpdate() {
            Fields["radiationLevel"].guiActive = true;
            Fields["radiationLevel2"].guiActive = vessel.isEVA;
            double rad_level_yr = radiation_level * 24 * 365.25;
            if (radiation_level >= 1000) {
                radiationLevel = (radiation_level / 1000).ToString("0.00") + " Sv/h";
            } else {
                if (radiation_level >= 1) {
                    radiationLevel = radiation_level.ToString("0.00") + " mSv/hr";
                } else {
                    if (radiation_level >= 0.001) {
                        radiationLevel = (radiation_level * 1000.0).ToString("0.00") + " uSv/h";
                    } else {
                        radiationLevel = (radiation_level * 1000000.0).ToString("0.00") + " nSv/h";
                    }
                }
            }

            
            if (rad_level_yr >= 1e9) {
                radiationLevel = radiationLevel + " " + (rad_level_yr / 1e9).ToString("0.00") + " MSv/yr";
            } else {
                if (rad_level_yr >= 1e6) {
                    radiationLevel = radiationLevel + " " + (rad_level_yr / 1e6).ToString("0.00") + " KSv/yr";
                } else {
                    if (rad_level_yr >= 1e3) {
                        radiationLevel = radiationLevel + " " + (rad_level_yr / 1e3).ToString("0.00") + " Sv/yr";
                    } else {
                        radiationLevel = radiationLevel + " " + (rad_level_yr).ToString("0.00") + " mSv/yr";
                    }
                }
            }

            if (VanAllen.crew_rad_exposure.ContainsKey(vessel.vesselName)) {
                double tot_rad_exp = VanAllen.crew_rad_exposure[vessel.vesselName];
                if (tot_rad_exp >= 1000) {
                    radiationLevel2 = (tot_rad_exp / 1000).ToString("0.00") + " Sv";
                } else {
                    if (tot_rad_exp >= 1) {
                        radiationLevel2 = tot_rad_exp.ToString("0.00") + " mSv";
                    } else {
                        if (tot_rad_exp >= 0.001) {
                            radiationLevel2 = (tot_rad_exp * 1000.0).ToString("0.00") + " uSv";
                        } else {
                            if (tot_rad_exp >= 1e-6) {
                                radiationLevel2 = (tot_rad_exp * 1000000.0).ToString("0.00") + " nSv";
                            } else {
                                radiationLevel2 = (tot_rad_exp * 1000000000.0).ToString("0.00") + " pSv";
                            }
                        }
                    }
                }
            }

            if (rad_level_yr <= 50) {
                radiationStatus = "Safe.";
            } else {
                if (rad_level_yr <= 200) {
                    radiationStatus = "Elevated.";
                } else if (rad_level_yr <= 2000) {
                    radiationStatus = "High.";
                } else if (radiation_level <= 100) {
                    radiationStatus = "Dangerous.";
                } else {
                    radiationStatus = "Deadly.";
                }
            }
            /*
            CelestialBody cur_ref_body = FlightGlobals.ActiveVessel.mainBody;
			CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            ORSPlanetaryResourcePixel res_pixel = ORSPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", cur_ref_body.GetLatitude(vessel.transform.position), cur_ref_body.GetLongitude(vessel.transform.position));
            double ground_rad = Math.Sqrt(res_pixel.getAmount()*9e6)/24/365.25 / Math.Max(vessel.altitude/870,1);
            double rad = VanAllen.getRadiationLevel(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
			double divisor = Math.Pow (cur_ref_body.Radius / crefkerbin.Radius, 2.0);
            double mag_field_strength = VanAllen.getBeltMagneticFieldMag(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
			if (cur_ref_body.flightGlobalsIndex == PluginHelper.REF_BODY_KERBOL) {
				rad = rad * 1e6;
			}
            
			double rad_level = rad/divisor;
            double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
            double solar_radiation = 0.19*inv_square_mult;
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
            solar_radiation = solar_radiation * Math.Exp(-73840.56456662708394321273809886 * mag_field_strength);
            radiation_level = (Math.Pow(rad_level / 3e-5, 3.0) * 3.2 + ground_rad + solar_radiation) / rad_hardness;
            //print(radiation_level);
            */
            radiation_level = VanAllen.getRadiationDose(vessel, rad_hardness);

            double rad_level_sec = radiation_level / 3600.0;
            List<ProtoCrewMember> crew_members = part.protoModuleCrew;
            if (!vessel.isEVA) {
                foreach (ProtoCrewMember crewmember in crew_members) {
                    if (VanAllen.crew_rad_exposure.ContainsKey(crewmember.name)) {
                        double current_rad = VanAllen.crew_rad_exposure[crewmember.name];
                        VanAllen.crew_rad_exposure[crewmember.name] = Math.Max(current_rad + rad_level_sec * TimeWarp.deltaTime - (50.0/31557600.0*TimeWarp.fixedDeltaTime),0);
                    } else {
                        VanAllen.crew_rad_exposure.Add(crewmember.name, Math.Max(rad_level_sec * TimeWarp.deltaTime - (50.0 / 31557600.0 * TimeWarp.fixedDeltaTime), 0));
                    }
                }
            } else {
                if (VanAllen.crew_rad_exposure.ContainsKey(vessel.vesselName)) {
                    double current_rad = VanAllen.crew_rad_exposure[vessel.vesselName];
                    VanAllen.crew_rad_exposure[vessel.vesselName] = Math.Max(current_rad + rad_level_sec * TimeWarp.deltaTime - (50.0 / 31557600.0 * TimeWarp.fixedDeltaTime), 0);
                } else {
                    VanAllen.crew_rad_exposure.Add(vessel.vesselName, Math.Max(rad_level_sec * TimeWarp.deltaTime - (50.0 / 31557600.0 * TimeWarp.fixedDeltaTime), 0));
                }
            }
            
		}

        public override string GetInfo() {
            return "Rad Hardness: " + rad_hardness.ToString("0.00");
        }

	}
}

