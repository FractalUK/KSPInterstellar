using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
	class VistaEngineController : FNResourceSuppliableModule {
		[KSPField(isPersistant = true)]
		bool IsEnabled;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Hazard To")]
		public string radhazardstr;
		[KSPField(isPersistant = true)]
		bool rad_safety_features = true;

		protected bool radhazard = false;

		protected double minISP = 0;
		protected double standard_megajoule_rate = 0;
		protected double standard_deut_rate = 0;
		protected double standard_lith_rate = 0;



		[KSPEvent(guiActive = true, guiName = "Disable Radiation Safety", active = true)]
		public void DeactivateRadSafety() {
			rad_safety_features = false;
		}

		[KSPEvent(guiActive = true, guiName = "Activate Radiation Safety", active = false)]
		public void ActivateRadSafety() {
			rad_safety_features = true;
		}

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) {return;}

			ModuleEngines curEngineT = (ModuleEngines)this.part.Modules ["ModuleEngines"];
			minISP = curEngineT.atmosphereCurve.Evaluate(0);

            standard_deut_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio;
            standard_lith_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio;

		}

		public override void OnUpdate() {
			Events ["DeactivateRadSafety"].active = rad_safety_features;
			Events ["ActivateRadSafety"].active = !rad_safety_features;

			ModuleEngines curEngineT = (ModuleEngines)this.part.Modules ["ModuleEngines"];
			if (curEngineT.isOperational && !IsEnabled) {
				IsEnabled = true;
				part.force_activate ();
			}

			List<Vessel> vessels = FlightGlobals.Vessels;
			int kerbal_hazard_count = 0;
			foreach (Vessel vess in vessels) {
				float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
				if (distance < 2000 && vess != this.vessel) {
					kerbal_hazard_count += vess.GetCrewCount ();
				}
			}

			if (kerbal_hazard_count > 0) {
				radhazard = true;
				if (kerbal_hazard_count > 1) {
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbals.";
				} else {
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbal.";
				}
				Fields["radhazardstr"].guiActive = true;
			} else {
				Fields["radhazardstr"].guiActive = false;
				radhazard = false;
				radhazardstr = "None.";
			}
		}

		public override void OnFixedUpdate() {
			ModuleEngines curEngineT = (ModuleEngines)this.part.Modules ["ModuleEngines"];

			float throttle = curEngineT.currentThrottle;

			if (radhazard && throttle > 0 && rad_safety_features) {
				curEngineT.Events ["Shutdown"].Invoke ();
				curEngineT.currentThrottle = 0;
				curEngineT.requestedThrottle = 0;
				ScreenMessages.PostScreenMessage("Engines throttled down as they presently pose a radiation hazard!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				foreach (FXGroup fx_group in part.fxGroups) {
					fx_group.setActive (false);
				}
			}

			System.Random rand = new System.Random (new System.DateTime().Millisecond);

			List<Vessel> vessels = FlightGlobals.Vessels;
			List<Vessel> vessels_to_remove = new List<Vessel> ();
			List<ProtoCrewMember> crew_to_remove = new List<ProtoCrewMember> ();
			double death_prob = 1.0 * TimeWarp.fixedDeltaTime;
			if (radhazard && throttle > 0 && !rad_safety_features) {
				foreach (Vessel vess in vessels) {
					float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
					if (distance < 2000 && vess != this.vessel && vess.GetCrewCount() > 0) {
						float inv_sq_dist = distance / 50.0f;
						float inv_sq_mult = 1.0f / inv_sq_dist / inv_sq_dist;
						List<ProtoCrewMember> vessel_crew = vess.GetVesselCrew ();
						foreach (ProtoCrewMember crew_member in vessel_crew) {
							if (UnityEngine.Random.value >= (1.0 - death_prob*inv_sq_mult)) {
								if(!vess.isEVA) {
									ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
									crew_to_remove.Add (crew_member);
								}else{
									ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
									vessels_to_remove.Add (vess);
								}
							}
						}
					}
				}

				foreach (Vessel vess in vessels_to_remove) {
					vess.rootPart.Die ();
				}

				foreach (ProtoCrewMember crew_member in crew_to_remove) {
					Vessel vess = FlightGlobals.Vessels.Find (p => p.GetVesselCrew ().Contains (crew_member));
					Part part = vess.Parts.Find(p => p.protoModuleCrew.Contains(crew_member));
					part.RemoveCrewmember (crew_member);
					crew_member.Die ();
				}
			}

			if (throttle > 0) {
                double power = consumeFNResource(2500.0 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio = (float)(standard_deut_rate / throttle / throttle);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio = (float)(standard_lith_rate / throttle / throttle);
                //curEngineT.propellants[1].ratio = (float)(standard_deut_rate / throttle / throttle);
                //curEngineT.propellants[2].ratio = (float)(standard_lith_rate / throttle / throttle);
                FloatCurve newISP = new FloatCurve();
                newISP.Add(0, (float)(minISP / throttle));
                curEngineT.atmosphereCurve = newISP;
                if (power >= 2500 * TimeWarp.fixedDeltaTime) {
                    curEngineT.maxThrust = 1100;
                } else {
                    curEngineT.maxThrust = 0.0001f;
                }
			}
		}

        public override string getResourceManagerDisplayName() {
            return "DT Vista Engine";
        }

        public override int getPowerPriority() {
            return 1;
        }

	}

}

