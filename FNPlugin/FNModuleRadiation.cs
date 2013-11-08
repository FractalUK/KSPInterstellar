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
			this.part.force_activate();
		}

		public override void OnUpdate() {
			if (radiation_level >= 1000) {
				radiationLevel = (radiation_level/1000).ToString ("0.00") + " Sv/hour";
			}else{
				if (radiation_level >= 1) {
					radiationLevel = radiation_level.ToString ("0.00") + " mSv/hour";
				} else {
					if (radiation_level >= 0.001) {
						radiationLevel = (radiation_level * 1000.0).ToString ("0.00") + " uSv/hour";
					} else {
						radiationLevel = (radiation_level * 1000000.0).ToString ("0.00") + " nSv/hour";
					}
				}
			}
		}

		public override void OnLoad(ConfigNode node) {
			foreach (ProtoCrewMember crewmember in part.protoModuleCrew) {
				int kerbal_num = HighLogic.CurrentGame.CrewRoster.IndexOf (crewmember);
				print ("Loading Kerbal: " + kerbal_num);
				if (VanAllen.crew_rad_exposure.ContainsKey (crewmember)) {
					VanAllen.crew_rad_exposure [crewmember] = PluginHelper.getKerbalRadiationDose (kerbal_num);
				} else {
					VanAllen.crew_rad_exposure.Add (crewmember, PluginHelper.getKerbalRadiationDose (kerbal_num));
				}
			}
		}

		public override void OnSave(ConfigNode node) {
			foreach (ProtoCrewMember crewmember in part.protoModuleCrew) {
				int kerbal_num = HighLogic.CurrentGame.CrewRoster.IndexOf (crewmember);
				//HighLogic.CurrentGame.CrewRoster[1].KerbalRef.
				ConfigNode kerbal_node = PluginHelper.getKerbal (kerbal_num);
				if (kerbal_node != null) {
					if (kerbal_node.HasValue ("totalDose")) {
						if (VanAllen.crew_rad_exposure.ContainsKey (crewmember)) {
							//kerbal_node.SetValue ("totalValue", VanAllen.crew_rad_exposure [crewmember].ToString("E"));
							print ("Saving for Kerbal " + kerbal_num + " radiation: " + VanAllen.crew_rad_exposure [crewmember]);
							PluginHelper.saveKerbalRadiationdose (kerbal_num, (float)VanAllen.crew_rad_exposure [crewmember]);
                            
						} 
					} else {
						if (VanAllen.crew_rad_exposure.ContainsKey (crewmember)) {
							//kerbal_node.AddValue("totalValue", VanAllen.crew_rad_exposure [crewmember].ToString("E"));
							print ("Saving for Kerbal " + kerbal_num + " radiation: " + VanAllen.crew_rad_exposure [crewmember]);
							PluginHelper.saveKerbalRadiationdose (kerbal_num, (float)VanAllen.crew_rad_exposure [crewmember]);
						}
					}
					crewmember.Save (kerbal_node);
				}
			}
		}

		public override void OnFixedUpdate() {
			CelestialBody cur_ref_body = vessel.mainBody;
			CelestialBody crefkerbin = FlightGlobals.fetch.bodies[1];
			double rad = VanAllen.getBeltAntiparticles (cur_ref_body.flightGlobalsIndex, (float) vessel.altitude, (float)vessel.latitude);
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
				rad = VanAllen.getBeltAntiparticles (cur_ref_body.flightGlobalsIndex, (float) (Vector3d.Distance(vessel.transform.position,cur_ref_body.transform.position)-cur_ref_body.Radius), 0.0f);
				divisor = Math.Pow (cur_ref_body.Radius / crefkerbin.Radius, 2.0);
				if (cur_ref_body.flightGlobalsIndex == PluginHelper.REF_BODY_KERBOL) {
					rad = rad * 1e6;
				}
				rad_level += rad/divisor;
			}
			radiation_level = Math.Pow(rad_level / 2e-5,4.0)*130/rad_hardness;

			foreach (ProtoCrewMember crewmember in part.protoModuleCrew) {
				if (VanAllen.crew_rad_exposure.ContainsKey (crewmember)) {
					double current_rad = VanAllen.crew_rad_exposure [crewmember];
					VanAllen.crew_rad_exposure [crewmember] = current_rad + radiation_level * TimeWarp.fixedDeltaTime;
				} else {
					int kerbal_num = HighLogic.CurrentGame.CrewRoster.IndexOf (crewmember);
					VanAllen.crew_rad_exposure.Add (crewmember, PluginHelper.getKerbalRadiationDose (kerbal_num)+radiation_level * TimeWarp.fixedDeltaTime);
				}
			}
		}

	}
}

