using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ScienceModule : FNResourceSuppliableModule {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Science Rate")]
        public string scienceRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "A")]
        public string antimatterRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;
		[KSPField(isPersistant = false, guiActive = true, guiName = "C")]
		public string centrifugeRate;
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public int active_mode = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;
		[KSPField(isPersistant = false)]
		public string animName1;
		[KSPField(isPersistant = false)]
		public string animName2;

        protected float megajoules_supplied = 0;

        protected String[] modes = { "Researching" ,"Reprocessing","Producing Antimatter","Electrolysing","Centrifuging"};
        //protected int active_mode = 0;
        protected float science_rate_f;
        protected float reprocessing_rate_f = 0;
        protected float crew_capacity_ratio;
        
        protected float antimatter_rate_f = 0;
        protected float electrolysis_rate_f = 0;
		protected float deut_rate_f = 0;

		protected bool play_down = true;
		protected double science_awaiting_addition = 0;

		protected Animation anim;
		protected Animation anim2;

        [KSPEvent(guiActive = true, guiName = "Begin Research", active = true)]
        public void BeginResearch() {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 0;

			anim [animName1].speed = 1f;
			anim [animName1].normalizedTime = 0f;
			anim.Blend (animName1, 2f);
			anim2 [animName2].speed = 1f;
			anim2 [animName2].normalizedTime = 0f;
			anim2.Blend (animName2, 2f);
			play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 1;
            
			anim [animName1].speed = 1f;
			anim [animName1].normalizedTime = 0f;
			anim.Blend (animName1, 2f);
			anim2 [animName2].speed = 1f;
			anim2 [animName2].normalizedTime = 0f;
			anim2.Blend (animName2, 2f);
			play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Antimatter Factory", active = true)]
        public void ActivateFactory() {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 2;

			anim [animName1].speed = 1f;
			anim [animName1].normalizedTime = 0f;
			anim.Blend (animName1, 2f);
			anim2 [animName2].speed = 1f;
			anim2 [animName2].normalizedTime = 0f;
			anim2.Blend (animName2, 2f);
			play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Electrolysis", active = true)]
        public void ActivateElectrolysis() {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 3;

			anim [animName1].speed = 1f;
			anim [animName1].normalizedTime = 0f;
			anim.Blend (animName1, 2f);
			anim2 [animName2].speed = 1f;
			anim2 [animName2].normalizedTime = 0f;
			anim2.Blend (animName2, 2f);
			play_down = true;
        }

		[KSPEvent(guiActive = true, guiName = "Activate Centrifuge", active = true)]
		public void ActivateCentrifuge() {
			if (crew_capacity_ratio == 0) { return; }
			IsEnabled = true;
			active_mode = 4;

			anim [animName1].speed = 1f;
			anim [animName1].normalizedTime = 0f;
			anim.Blend (animName1, 2f);
			anim2 [animName2].speed = 1f;
			anim2 [animName2].normalizedTime = 0f;
			anim2.Blend (animName2, 2f);
			play_down = true;
		}

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() {
			IsEnabled = false;
            
        }
		              
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

			ConfigNode config = PluginHelper.getPluginSaveFile ();

            part.force_activate();

			anim = part.FindModelAnimators (animName1).FirstOrDefault ();
			anim2 = part.FindModelAnimators (animName2).FirstOrDefault ();
			if (anim != null && anim2 != null) {

				anim [animName1].layer = 1;
				anim2 [animName2].layer = 1;
				if (IsEnabled) {
					//anim [animName1].normalizedTime = 1f;
					//anim2 [animName2].normalizedTime = 1f;
					//anim [animName1].speed = -1f;
					//anim2 [animName2].speed = -1f;
					anim.Blend (animName1, 1, 0);
					anim2.Blend (animName2, 1, 0);
				} else {
					//anim [animName1].normalizedTime = 0f;
					//anim2 [animName2].normalizedTime = 0f;
					//anim [animName1].speed = 1f;
					//anim2 [animName2].speed = 1f;
					//anim.Blend (animName1, 0, 0);
					//anim2.Blend (animName2, 0, 0);
					play_down = false;
				}
				//anim.Play ();
				//anim2.Play ();
			}

            if (IsEnabled && last_active_time != 0) {
                float global_rate_multipliers = 1;
                crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
                global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

				if (active_mode == 0) { // Science persistence
					double now = Planetarium.GetUniversalTime ();
					double time_diff = now - last_active_time;
					float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
					altitude_multiplier = Math.Max (altitude_multiplier, 1);
					float stupidity = 0;
					foreach (ProtoCrewMember proto_crew_member in part.protoModuleCrew) {
						stupidity += proto_crew_member.stupidity;
					}
					stupidity = 1.5f - stupidity / 2.0f;
                    double science_to_add = GameConstants.baseScienceRate * time_diff / 86400 * electrical_power_ratio * stupidity * global_rate_multipliers * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) / ((float)Math.Sqrt(altitude_multiplier));
					//part.RequestResource ("Science", -science_to_add);
					science_awaiting_addition = science_to_add;

				} else if (active_mode == 2) { // Antimatter persistence
					double now = Planetarium.GetUniversalTime ();
					double time_diff = now - last_active_time;

					List<PartResource> partresources = new List<PartResource>();
					part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, partresources);
					float currentAntimatter_missing = 0;
					foreach (PartResource partresource in partresources) {
						currentAntimatter_missing += (float)(partresource.maxAmount-partresource.amount);
					}



                    float total_electrical_power_provided = (float) (electrical_power_ratio * (GameConstants.baseAMFPowerConsumption + GameConstants.basePowerConsumption) * 1E6);
                    double antimatter_mass = total_electrical_power_provided / GameConstants.warpspeed / GameConstants.warpspeed * 1E6 / 20000.0;
					float antimatter_peristence_to_add = (float) -Math.Min (currentAntimatter_missing, antimatter_mass * time_diff);
					part.RequestResource("Antimatter", antimatter_peristence_to_add);
				}
            }
        }

        public override void OnUpdate() {
            Events["BeginResearch"].active = !IsEnabled;
            Events["ReprocessFuel"].active = !IsEnabled;
            Events["ActivateFactory"].active = !IsEnabled;
            Events["ActivateElectrolysis"].active = false;
			Events["ActivateCentrifuge"].active = !IsEnabled && vessel.Splashed;
            Events["StopActivity"].active = IsEnabled;

            if (IsEnabled) {
				//anim [animName1].normalizedTime = 1f;
                statusTitle = modes[active_mode] + "...";
                Fields["scienceRate"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
				Fields["centrifugeRate"].guiActive = false;
                Fields["powerStr"].guiActive = true;

                double currentpowertmp = electrical_power_ratio * GameConstants.basePowerConsumption;
                powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.basePowerConsumption.ToString("0.00") + "MW";
				if (active_mode == 0) { // Research
					Fields ["scienceRate"].guiActive = true;
					float scienceratetmp = science_rate_f * 86400;
					scienceRate = scienceratetmp.ToString ("0.000") + "/Day";
				} else if (active_mode == 1) { // Fuel Reprocessing
					Fields ["reprocessingRate"].guiActive = true;
					float reprocessratetmp = reprocessing_rate_f;
					reprocessingRate = reprocessratetmp.ToString ("0.0") + " Hours Remaining";
				} else if (active_mode == 2) { // Antimatter
                    currentpowertmp = electrical_power_ratio * GameConstants.baseAMFPowerConsumption;
					Fields ["antimatterRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseAMFPowerConsumption.ToString("0.00") + "MW";
					antimatterRate = antimatter_rate_f.ToString ("E") + "mg/sec";
				} else if (active_mode == 3) { // Electrolysis
                    currentpowertmp = electrical_power_ratio * GameConstants.baseELCPowerConsumption;
					Fields ["electrolysisRate"].guiActive = true;
					float electrolysisratetmp = -electrolysis_rate_f * 86400;
					electrolysisRate = electrolysisratetmp.ToString ("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseELCPowerConsumption.ToString("0.00") + "MW";
				} else if (active_mode == 4) { // Centrifuge
                    currentpowertmp = electrical_power_ratio * GameConstants.baseCentriPowerConsumption;
					Fields["centrifugeRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseCentriPowerConsumption.ToString("0.00") + "MW";
					float deut_per_hour = deut_rate_f*3600;
					centrifugeRate = deut_per_hour.ToString ("0.00") + " Kg Deuterium/Hour";
				}else {

                }
            }else {
				if (play_down) {
					anim [animName1].speed = -1f;
					anim [animName1].normalizedTime = 1f;
					anim.Blend (animName1, 2f);
					anim2 [animName2].speed = -1f;
					anim2 [animName2].normalizedTime = 1f;
					anim2.Blend (animName2, 2f);
					play_down = false;
				}
				//anim [animName1].normalizedTime = 0f;
                Fields["scienceRate"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["powerStr"].guiActive = false;
				Fields["centrifugeRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                if (crew_capacity_ratio > 0) {
                    statusTitle = "Idle";
                }else {
                    statusTitle = "Not enough crew";
                }
            }
        }

        public override void OnFixedUpdate() {
            float global_rate_multipliers = 1;
            crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
            global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;
            
			if (ResearchAndDevelopment.Instance != null) {
                if (!double.IsNaN(science_awaiting_addition) && !double.IsInfinity(science_awaiting_addition) && science_awaiting_addition > 0) {
					ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)science_awaiting_addition;
                    ScreenMessages.PostScreenMessage(science_awaiting_addition.ToString("0") + " science has been added to the R&D centre.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                    science_awaiting_addition = 0;
				}
			}
            
			if (IsEnabled) {
				//float electrical_power_provided = part.RequestResource("Megajoules", basePowerConsumption * TimeWarp.fixedDeltaTime);
				//mega_manager.powerDraw(this, basePowerConsumption);
                float electrical_power_provided = consumeFNResource(GameConstants.basePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                electrical_power_ratio = (float) (electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.basePowerConsumption);
				global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

				if (electrical_power_ratio > 0) {
                
					if (active_mode == 0) { // Research
						float stupidity = 0;
						foreach (ProtoCrewMember proto_crew_member in part.protoModuleCrew) {
							stupidity += proto_crew_member.stupidity;
						}
						stupidity = 1.5f - stupidity / 2.0f;
						float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
						altitude_multiplier = Math.Max (altitude_multiplier, 1);

                        science_rate_f = (float) (GameConstants.baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) / 86400.0f * global_rate_multipliers * stupidity / (Mathf.Sqrt(altitude_multiplier)));
						//part.RequestResource ("Science", -science_rate_f * TimeWarp.fixedDeltaTime);
						//ScienceSubject subject = new ScienceSubject ();
						if (ResearchAndDevelopment.Instance != null) {
							if (!double.IsNaN (science_rate_f) && !double.IsInfinity (science_rate_f)) {
								ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + science_rate_f * TimeWarp.fixedDeltaTime;
							}
						}
					} else if (active_mode == 1) { // Fuel Reprocessing
						List<PartResource> partresources = new List<PartResource> ();
                        double currentActinides = 0;
                        double depletedfuelsparecapacity = 0;
                        double uf6sparecapacity = 0;
                        double thf4sparecapacity = 0;
                        double uf6tothf4_ratio = 0;
						part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition ("Actinides").id, partresources);
                        foreach (PartResource partresource in partresources) {
							currentActinides += partresource.amount;
						}
                        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("DepletedFuel").id, partresources);
                        foreach (PartResource partresource in partresources) {
                            depletedfuelsparecapacity += partresource.maxAmount - partresource.amount;
                        }
                        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF4").id, partresources);
                        foreach (PartResource partresource in partresources) {
                            uf6sparecapacity += partresource.maxAmount - partresource.amount;
                        }
                        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, partresources);
                        foreach (PartResource partresource in partresources) {
                            thf4sparecapacity += partresource.maxAmount - partresource.amount;
                        }
                        uf6tothf4_ratio = uf6sparecapacity / (thf4sparecapacity+uf6sparecapacity);
                        double amount_to_reprocess = Math.Min(currentActinides,depletedfuelsparecapacity*5.0);
						if (currentActinides > 0 && !double.IsNaN(uf6tothf4_ratio) && !double.IsInfinity(uf6tothf4_ratio)) {
                            double actinides_removed = part.RequestResource("Actinides", GameConstants.baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400.0 * global_rate_multipliers);
							double uf6added = part.RequestResource ("UF4", -actinides_removed*0.8*uf6tothf4_ratio);
                            double th4added = part.RequestResource("ThF4", -actinides_removed*0.8*(1-uf6tothf4_ratio));
                            double duf6added = part.RequestResource("DepletedFuel", -actinides_removed * 0.2);
                            double actinidesremovedperhour = actinides_removed / TimeWarp.fixedDeltaTime * 3600.0;
                            reprocessing_rate_f = (float)(amount_to_reprocess / actinidesremovedperhour);
						} else { // Finished, hurray!
							IsEnabled = false;
						}
					} else if (active_mode == 2) { //Antimatter
						//float more_electrical_power_provided = part.RequestResource("Megajoules", (baseAMFPowerConsumption-basePowerConsumption) * TimeWarp.fixedDeltaTime);
                        float more_electrical_power_provided = consumeFNResource((GameConstants.baseAMFPowerConsumption - GameConstants.basePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
						float total_electrical_power_provided = more_electrical_power_provided + electrical_power_provided;
                        electrical_power_ratio = (float) (total_electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseAMFPowerConsumption);
						global_rate_multipliers = crew_capacity_ratio * electrical_power_ratio;

                        total_electrical_power_provided = (float) (global_rate_multipliers * GameConstants.baseAMFPowerConsumption * 1E6f);
                        double antimatter_mass = total_electrical_power_provided / GameConstants.warpspeed / GameConstants.warpspeed * 1E6f / 20000.0f;
						antimatter_rate_f = (float) -part.RequestResource ("Antimatter", -antimatter_mass * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
					} else if (active_mode == 3) {
						IsEnabled = false;
					} else if (active_mode == 4) { // Centrifuge
						if (vessel.Splashed) {
                            float more_electrical_power_provided = consumeFNResource((GameConstants.baseCentriPowerConsumption - GameConstants.basePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
							global_rate_multipliers = global_rate_multipliers / electrical_power_ratio;
							float total_electrical_power_provided = more_electrical_power_provided + electrical_power_provided;
                            global_rate_multipliers = (float) (global_rate_multipliers * total_electrical_power_provided / GameConstants.baseCentriPowerConsumption / TimeWarp.fixedDeltaTime);
                            electrical_power_ratio = (float) (total_electrical_power_provided / GameConstants.baseCentriPowerConsumption / TimeWarp.fixedDeltaTime);
                            float deut_produced = (float) (global_rate_multipliers * GameConstants.deuterium_timescale * GameConstants.deuterium_abudance * 1000.0f);
							deut_rate_f = -part.RequestResource ("Deuterium", -deut_produced * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
						} else {
							ScreenMessages.PostScreenMessage ("You must be splashed down to perform this activity.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
							IsEnabled = false;
						}
					}

				} else {
					deut_rate_f = 0;
					electrolysis_rate_f = 0;
					science_rate_f = 0;
					antimatter_rate_f = 0;
					reprocessing_rate_f = 0;
				}

				last_active_time = (float)Planetarium.GetUniversalTime ();
			} else {

			}
        }

        public override string getResourceManagerDisplayName() {
            if (IsEnabled) {
                return "Science Lab (" + modes[active_mode] + ")";
            }
            return "Science Lab";
        }


    }
}
