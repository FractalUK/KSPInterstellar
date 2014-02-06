extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_1::OpenResourceSystem;

namespace FNPlugin {
	class FNModuleRadiation : PartModule	{
		[KSPField(isPersistant = false, guiActive = true, guiName = "Rad.")]
		public string radiationLevel = ":";

		public double rad_hardness = 1;

		protected double radiation_level = 0;

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
            
            CelestialBody cur_ref_body = FlightGlobals.ActiveVessel.mainBody;
			CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];

            ORSPlanetaryResourcePixel res_pixel = ORSPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", cur_ref_body.GetLatitude(vessel.transform.position), cur_ref_body.GetLongitude(vessel.transform.position));
            double ground_rad = Math.Sqrt(res_pixel.getAmount()*9e6)/24/365.25 / Math.Max(vessel.altitude/870,1);
            double rad = VanAllen.getRadiationLevel(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
			double divisor = Math.Pow (cur_ref_body.Radius / crefkerbin.Radius, 2.0);
			if (cur_ref_body.flightGlobalsIndex == PluginHelper.REF_BODY_KERBOL) {
				rad = rad * 1e6;
			}
            
			double rad_level = rad/divisor;
			while (cur_ref_body.referenceBody != null) {
				CelestialBody old_ref_body = cur_ref_body;
				cur_ref_body = cur_ref_body.referenceBody;
				if (cur_ref_body == old_ref_body) {
					break;
				}
				//rad = VanAllen.getBeltAntiparticles (cur_ref_body.flightGlobalsIndex, (float) (Vector3d.Distance(FlightGlobals.ship_position,cur_ref_body.transform.position)-cur_ref_body.Radius), 0.0f);
                rad = VanAllen.getRadiationLevel(cur_ref_body.flightGlobalsIndex, (Vector3d.Distance(FlightGlobals.ship_position, cur_ref_body.transform.position) - cur_ref_body.Radius), 0.0);
				if (cur_ref_body.flightGlobalsIndex == PluginHelper.REF_BODY_KERBOL) {
					rad = rad * 1e6;
				}
				rad_level += rad;
			}

            radiation_level = (Math.Pow(rad_level / 3e-5, 3.0) * 5.2 + ground_rad) / rad_hardness;
            //print(radiation_level);
            
            List<ProtoCrewMember> crew_members = part.protoModuleCrew;
            if (!vessel.isEVA) {
                foreach (ProtoCrewMember crewmember in crew_members) {
                    if (VanAllen.crew_rad_exposure.ContainsKey(crewmember.name)) {
                        double current_rad = VanAllen.crew_rad_exposure[crewmember.name];
                        VanAllen.crew_rad_exposure[crewmember.name] = current_rad + radiation_level * TimeWarp.deltaTime;
                    } else {
                        VanAllen.crew_rad_exposure.Add(crewmember.name, radiation_level * TimeWarp.deltaTime);
                    }
                }
            } else {
                if (VanAllen.crew_rad_exposure.ContainsKey(vessel.vesselName)) {
                    double current_rad = VanAllen.crew_rad_exposure[vessel.vesselName];
                    VanAllen.crew_rad_exposure[vessel.vesselName] = current_rad + radiation_level * TimeWarp.deltaTime;
                } else {
                    VanAllen.crew_rad_exposure.Add(vessel.vesselName, radiation_level * TimeWarp.deltaTime);
                }
            }
            
		}

	}
}

