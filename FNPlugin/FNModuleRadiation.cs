using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	public class FNModuleRadiation : PartModule	{
		[KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Level")]
		public string radiationLevel = ":";

		public double rad_hardness = 1;

		protected double radiation_level = 0;

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }
            //if (!vessel.isEVA) {
            //    part.force_activate();
            //}
		}

        public override void OnUpdate() {
            if (radiation_level >= 1000) {
                radiationLevel = (radiation_level / 1000).ToString("0.00") + " Sv/hour";
            } else {
                if (radiation_level >= 1) {
                    radiationLevel = radiation_level.ToString("0.00") + " mSv/hour";
                } else {
                    if (radiation_level >= 0.001) {
                        radiationLevel = (radiation_level * 1000.0).ToString("0.00") + " uSv/hour";
                    } else {
                        radiationLevel = (radiation_level * 1000000.0).ToString("0.00") + " nSv/hour";
                    }
                }
            }

            CelestialBody cur_ref_body = FlightGlobals.ActiveVessel.mainBody;
			CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
            
            double rad = VanAllen.getBeltAntiparticles(cur_ref_body.flightGlobalsIndex, (float)FlightGlobals.ship_altitude, (float)FlightGlobals.ship_latitude);
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
            
			radiation_level = Math.Pow(rad_level / 3e-5,3.0)*130/rad_hardness;
            
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

