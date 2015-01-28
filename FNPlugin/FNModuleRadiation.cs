extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_4_3::OpenResourceSystem;

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

		public override void OnStart(PartModule.StartState state) 
        {
			if (state == StartState.Editor)
                return; 

            //if (!vessel.isEVA) {
            //    part.force_activate();
            //}

            if (PluginHelper.RadiationMechanicsDisabled)
            {
                Fields["radiationLevel"].guiActive = false;
                Fields["radiationLevel2"].guiActive = false;
                Fields["radiationStatus"].guiActive = false;
                return;
            }

            print("[KSP Interstellar] Radiation Module Loaded.");
            Fields["radiationLevel"].guiActive = true;
		}

        public override void OnUpdate() 
        {
            if (PluginHelper.RadiationMechanicsDisabled) return;

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

            RadiationDose dose = vessel.GetRadiationDose().GetDoseWithMaterialShielding(1.0 / rad_hardness);
            radiation_level = dose.TotalDose;
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

        public override string GetInfo() 
        {
            return "Rad Hardness: " + rad_hardness.ToString("0.00");
        }

	}
}

