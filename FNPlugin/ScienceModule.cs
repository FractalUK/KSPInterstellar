using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
    class ScienceModule : FNResourceSuppliableModule {
        const float baseScienceRate = 0.1f;
        const float baseReprocessingRate = 0.4f;
        const float basePowerConsumption = 5f;
        const float baseAMFPowerConsumption = 5000f;
        const float baseELCPowerConsumption = 25f;
        const float electrolysisEnergyPerTon = 18159f;
        const float aluminiumElectrolysisEnergyPerTon = 35485.714f;
        const float electrolysisMassRatio = 7.936429f;
        const float aluminiumElectrolysisMassRatio = 1.5f;

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
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public float minDUF6;
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

        protected String[] modes = { "Researching..." ,"Reprocessing...","Producing Antimatter...","Electrolysing..."};
        //protected int active_mode = 0;
        protected float science_rate_f;
        protected float reprocessing_rate_f = 0;
        protected float crew_capacity_ratio;
        
        protected float antimatter_rate_f = 0;
        protected float electrolysis_rate_f = 0;

		protected bool play_down = true;

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
            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("DUF6").id, partresources);
            float currentDUF6 = 0;
            foreach (PartResource partresource in partresources) {
                currentDUF6 += (float)partresource.amount;
            }
            minDUF6 = currentDUF6 * 0.1f;

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

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() {
			IsEnabled = false;
            
        }

		[KSPEvent(guiActive = true, guiName = "Transmit Scientific Data", active = true)]
		public void TransmitPacket() {
			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
			float currentscience = 0;
			foreach (PartResource partresource in partresources) {
				currentscience += (float)partresource.amount;
			}

			if (currentscience > 0) {
				ConfigNode config = ConfigNode.Load (PluginHelper.getPluginSaveFilePath ());
				float science_to_transmit = Math.Min (currentscience, 100f);
				part.RequestResource ("Science", science_to_transmit);
				ConfigNode data_packet = config.AddNode ("DATA_PACKET");
				data_packet.AddValue("science",science_to_transmit.ToString("E"));
				config.Save (PluginHelper.getPluginSaveFilePath ());
			}
					

		}

		[KSPEvent(guiActive = true, guiName = "Receive Scientific Data", active = false)]
		public void ReceivePacket() {
			ConfigNode config = ConfigNode.Load (PluginHelper.getPluginSaveFilePath ());
			if (config.HasNode ("DATA_PACKET")) {
				ConfigNode data_packet = config.GetNode ("DATA_PACKET");
				part.RequestResource ("Science", -float.Parse(data_packet.GetValue("science")));
				config.RemoveNode ("DATA_PACKET");
			}

			if (config.HasNode ("DATA_PACKET")) {

			} else {
				Events ["ReceivePacket"].active = false;
			}

			config.Save (PluginHelper.getPluginSaveFilePath ());

		}
                
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

			ConfigNode config = ConfigNode.Load (PluginHelper.getPluginSaveFilePath ());
			if (config.HasNode ("DATA_PACKET")) {
				Events ["ReceivePacket"].active = true;
			}

            part.force_activate();

			anim = part.FindModelAnimators (animName1).FirstOrDefault ();
			anim2 = part.FindModelAnimators (animName2).FirstOrDefault ();
			if (anim != null && anim2 != null) {

				anim [animName1].layer = 1;
				anim2 [animName2].layer = 1;
				if (IsEnabled) {
					anim [animName1].normalizedTime = 0f;
					anim [animName1].speed = -1f;
					anim2 [animName2].normalizedTime = 0f;
					anim2 [animName2].speed = -1f;
				} else {
					anim [animName1].normalizedTime = 1f;
					anim [animName1].speed = 1f;
					anim2 [animName2].normalizedTime = 1f;
					anim2 [animName2].speed = 1f;
				}
				anim.Play ();
				anim2.Play ();
			}

            if (IsEnabled && last_active_time != 0) {
                float global_rate_multipliers = 1;
                crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
                global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

                if (active_mode == 0) {
                    double now = Planetarium.GetUniversalTime();
                    double time_diff = now - last_active_time;
                    float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
                    altitude_multiplier = Math.Max(altitude_multiplier, 1);
                    float stupidity = 0;
                    foreach (ProtoCrewMember proto_crew_member in part.protoModuleCrew) {
                        stupidity += proto_crew_member.stupidity;
                    }
                    stupidity = 1.5f - stupidity / 2.0f;
                    double science_to_add = baseScienceRate*time_diff / 86400 * electrical_power_ratio * stupidity * global_rate_multipliers * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex) / ((float)Math.Sqrt(altitude_multiplier));
                    part.RequestResource("Science", -science_to_add);
                }
            }
        }

        public override void OnUpdate() {
            Events["BeginResearch"].active = !IsEnabled;
            Events["ReprocessFuel"].active = !IsEnabled;
            Events["ActivateFactory"].active = !IsEnabled;
            Events["ActivateElectrolysis"].active = !IsEnabled && (vessel.Splashed || vessel.Landed);
            Events["StopActivity"].active = IsEnabled;

            if (IsEnabled) {
				//anim [animName1].normalizedTime = 1f;
                statusTitle = modes[active_mode];
                Fields["scienceRate"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                Fields["powerStr"].guiActive = true;
                float currentpowertmp = electrical_power_ratio * basePowerConsumption;
                powerStr = currentpowertmp.ToString("0.00") + "MW / " + basePowerConsumption.ToString("0.00") + "MW";
                if (active_mode == 0) { // Research
                    Fields["scienceRate"].guiActive = true;
                    float scienceratetmp = science_rate_f * 86400 ;
                    scienceRate = scienceratetmp.ToString("0.000") + "/Day";
                }else if (active_mode == 1) { // Fuel Reprocessing
                    Fields["reprocessingRate"].guiActive = true;
                    float reprocessratetmp = reprocessing_rate_f;
                    reprocessingRate = reprocessratetmp.ToString("0.0") + " Hours Remaining";
                }else if (active_mode == 2) { // Antimatter
                    currentpowertmp = electrical_power_ratio * baseAMFPowerConsumption;
                    Fields["antimatterRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + baseAMFPowerConsumption.ToString("0.00") + "MW";
                    antimatterRate = antimatter_rate_f.ToString("E") + "mg/sec";
                }else if (active_mode == 3) { // Electrolysis
                    currentpowertmp = electrical_power_ratio * baseELCPowerConsumption;
                    Fields["electrolysisRate"].guiActive = true;
                    float electrolysisratetmp = -electrolysis_rate_f * 86400;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + baseELCPowerConsumption.ToString("0.00") + "MW";
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

            

            if (IsEnabled) {
                //float electrical_power_provided = part.RequestResource("Megajoules", basePowerConsumption * TimeWarp.fixedDeltaTime);
                //mega_manager.powerDraw(this, basePowerConsumption);
                float electrical_power_provided = consumePower(basePowerConsumption * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
                electrical_power_ratio = electrical_power_provided / TimeWarp.fixedDeltaTime / basePowerConsumption;
                global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                
                if (active_mode == 0) { // Research
                    float stupidity = 0;
                    foreach (ProtoCrewMember proto_crew_member in part.protoModuleCrew) {
                        stupidity += proto_crew_member.stupidity;
                    }
                    stupidity = 1.5f - stupidity / 2.0f;
                    float altitude_multiplier = (float) (vessel.altitude / (vessel.mainBody.Radius));
                    altitude_multiplier = Math.Max(altitude_multiplier, 1);
                    science_rate_f = baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex) / 86400 * global_rate_multipliers*stupidity / ((float)Math.Sqrt(altitude_multiplier));
                    part.RequestResource("Science", -science_rate_f * TimeWarp.fixedDeltaTime);
                }else if(active_mode == 1) { // Fuel Reprocessing
                    List<PartResource> partresources = new List<PartResource>();
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("DUF6").id, partresources);
                    float currentDUF6 = 0;
                    foreach (PartResource partresource in partresources) {
                        currentDUF6 += (float)partresource.amount;
                    }
                    if (currentDUF6 > minDUF6) {
                        float amount_to_reprocess = currentDUF6 - minDUF6;
                        float duf6removed = part.RequestResource("DUF6", baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400 * global_rate_multipliers);
                        float uf6added = part.RequestResource("UF6", -duf6removed);
                        float duf6removedperhour = duf6removed / TimeWarp.fixedDeltaTime * 3600;
                        reprocessing_rate_f = amount_to_reprocess / duf6removedperhour;
                    }else { // Finished, hurray!
                        IsEnabled = false;
                    }
                }else if (active_mode == 2) { //Antimatter
                    //float more_electrical_power_provided = part.RequestResource("Megajoules", (baseAMFPowerConsumption-basePowerConsumption) * TimeWarp.fixedDeltaTime);
                    float more_electrical_power_provided = consumePower((baseAMFPowerConsumption - basePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    global_rate_multipliers = global_rate_multipliers / electrical_power_ratio;
                    float total_electrical_power_provided = more_electrical_power_provided + electrical_power_provided;
                    electrical_power_ratio = total_electrical_power_provided / TimeWarp.fixedDeltaTime / baseAMFPowerConsumption;
                    global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

                    total_electrical_power_provided = total_electrical_power_provided * 1E6f;
                    float antimatter_mass = total_electrical_power_provided/AlcubierreDrive.warpspeed/AlcubierreDrive.warpspeed*1E6f/2.0f;
                    antimatter_rate_f = -part.RequestResource("Antimatter", -antimatter_mass * TimeWarp.fixedDeltaTime);
                }else if (active_mode == 3) { // Electrolysis
                    if (vessel.Splashed || (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_VALL)) {
                        //float more_electrical_power_provided = part.RequestResource("Megajoules", (baseELCPowerConsumption - basePowerConsumption) * TimeWarp.fixedDeltaTime);
                        float more_electrical_power_provided = consumePower((baseELCPowerConsumption - basePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        global_rate_multipliers = global_rate_multipliers / electrical_power_ratio;
                        float total_electrical_power_provided = more_electrical_power_provided + electrical_power_provided;
                        electrolysis_rate_f = total_electrical_power_provided / electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                        global_rate_multipliers = global_rate_multipliers * electrolysis_rate_f;
                        float hydrogen_rate = electrolysis_rate_f / (1 + electrolysisMassRatio);
                        float oxygen_rate = hydrogen_rate * electrolysisMassRatio;
                        float density = PartResourceLibrary.Instance.GetDefinition("LiquidFuel").density;
                        electrolysis_rate_f = part.RequestResource("LiquidFuel", -hydrogen_rate *TimeWarp.fixedDeltaTime/density);
                        electrolysis_rate_f += part.RequestResource("Oxidizer", -oxygen_rate * TimeWarp.fixedDeltaTime/density);
                        electrolysis_rate_f = electrolysis_rate_f / TimeWarp.fixedDeltaTime*density;
                    }else if (vessel.Landed){
                        if (vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_MUN || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_IKE) {
                            //float more_electrical_power_provided = part.RequestResource("Megajoules", (baseELCPowerConsumption - basePowerConsumption) * TimeWarp.fixedDeltaTime);
                            float more_electrical_power_provided = consumePower((baseELCPowerConsumption - basePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                            global_rate_multipliers = global_rate_multipliers / electrical_power_ratio;
                            float total_electrical_power_provided = more_electrical_power_provided + electrical_power_provided;
                            electrolysis_rate_f = total_electrical_power_provided / aluminiumElectrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                            global_rate_multipliers = global_rate_multipliers * electrolysis_rate_f;
                            float aluminium_density = PartResourceLibrary.Instance.GetDefinition("Aluminium").density;
                            float oxygen_density = PartResourceLibrary.Instance.GetDefinition("Oxidizer").density;
                            float mass_rate = electrolysis_rate_f;
                            electrolysis_rate_f = part.RequestResource("Aluminium", -mass_rate * TimeWarp.fixedDeltaTime / aluminium_density) * aluminium_density;
                            electrolysis_rate_f += part.RequestResource("Oxidizer", -aluminiumElectrolysisMassRatio * mass_rate * TimeWarp.fixedDeltaTime / oxygen_density) * oxygen_density;
                            electrolysis_rate_f = electrolysis_rate_f / TimeWarp.fixedDeltaTime;
                        }else {
                            IsEnabled = false;
                        }
                    }else {
                        IsEnabled = false;
                    }
                }

                last_active_time = (float) Planetarium.GetUniversalTime();
            }
        }


    }
}
