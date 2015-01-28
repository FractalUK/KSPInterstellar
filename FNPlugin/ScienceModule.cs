extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    class ScienceModule : ModuleModableScienceGenerator, ITelescopeController
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Science Rate")]
        public string scienceRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Collected Science")]
        public string collectedScience;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "A")]
        public string antimatterRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "C")]
        public string centrifugeRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string antimatterProductionEfficiency;
        
        // persistant false
        [KSPField(isPersistant = false)]
        public string animName1;
        [KSPField(isPersistant = false)]
        public string animName2;

        // persistant true
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public int active_mode = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;
        [KSPField(isPersistant = true)]
        public float science_to_add;

        protected float megajoules_supplied = 0;
        protected String[] modes = { "Researching", "Reprocessing", "Producing Antimatter", "Electrolysing", "Centrifuging" };
        protected float science_rate_f;
        protected float reprocessing_rate_f = 0;
        protected float crew_capacity_ratio;
        protected float antimatter_rate_f = 0;
        protected float electrolysis_rate_f = 0;
        protected float deut_rate_f = 0;
        protected bool play_down = true;
        protected Animation anim;
        protected Animation anim2;
        protected NuclearFuelReprocessor reprocessor;
        protected AntimatterFactory anti_factory;

        public bool CanProvideTelescopeControl
        {
            get { return part.protoModuleCrew.Count > 0; }
        }

        [KSPEvent(guiActive = true, guiName = "Begin Research", active = true)]
        public void BeginResearch() 
        {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 0;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() 
        {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 1;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Antimatter Factory", active = true)]
        public void ActivateFactory() 
        {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 2;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Electrolysis", active = true)]
        public void ActivateElectrolysis() 
        {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 3;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Centrifuge", active = true)]
        public void ActivateCentrifuge() 
        {
            if (crew_capacity_ratio == 0) { return; }
            IsEnabled = true;
            active_mode = 4;

            anim[animName1].speed = 1f;
            anim[animName1].normalizedTime = 0f;
            anim.Blend(animName1, 2f);
            anim2[animName2].speed = 1f;
            anim2[animName2].normalizedTime = 0f;
            anim2.Blend(animName2, 2f);
            play_down = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() 
        {
            IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) { return; }
            reprocessor = new NuclearFuelReprocessor(part);
            anti_factory = new AntimatterFactory(part);
            ConfigNode config = PluginHelper.getPluginSaveFile();

            part.force_activate();

            anim = part.FindModelAnimators(animName1).FirstOrDefault();
            anim2 = part.FindModelAnimators(animName2).FirstOrDefault();
            if (anim != null && anim2 != null) {

                anim[animName1].layer = 1;
                anim2[animName2].layer = 1;
                if (IsEnabled) {
                    //anim [animName1].normalizedTime = 1f;
                    //anim2 [animName2].normalizedTime = 1f;
                    //anim [animName1].speed = -1f;
                    //anim2 [animName2].speed = -1f;
                    anim.Blend(animName1, 1, 0);
                    anim2.Blend(animName2, 1, 0);
                } else {
                    //anim [animName1].normalizedTime = 0f;
                    //anim2 [animName2].normalizedTime = 0f;
                    //anim [animName1].speed = 1f;
                    //anim2 [animName2].speed = 1f;
                    //anim.Blend (animName1, 0, 0);global_rate_multipliers
                    //anim2.Blend (animName2, 0, 0);
                    play_down = false;
                }
                //anim.Play ();
                //anim2.Play ();
            }

            if (IsEnabled && last_active_time != 0) 
            {
                float global_rate_multipliers = 1;
                crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
                global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

                if (active_mode == 0) // Science persistence
                { 
                    double time_diff = Planetarium.GetUniversalTime() - last_active_time;
                    float altitude_multiplier = Math.Max((float)(vessel.altitude / (vessel.mainBody.Radius)), 1);
                    float kerbalResearchSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);

                    double science_to_increment = kerbalResearchSkillFactor * GameConstants.baseScienceRate * time_diff 
                        / GameConstants.EARH_DAY_SECONDS * electrical_power_ratio * global_rate_multipliers * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) 
                        / ((float)Math.Sqrt(altitude_multiplier));

                    science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                    science_to_add += (float)science_to_increment;

                }
                else if (active_mode == 2) // Antimatter persistence
                { 
                    double time_diff = Planetarium.GetUniversalTime() - last_active_time;

                    List<PartResource> antimatter_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Antimatter).ToList();
                    float currentAntimatter_missing = (float) antimatter_resources.Sum(ar => ar.maxAmount-ar.amount);

                    float total_electrical_power_provided = (float)(electrical_power_ratio * (PluginHelper.BaseAMFPowerConsumption + PluginHelper.BasePowerConsumption) * 1E6);
                    double antimatter_mass = total_electrical_power_provided / GameConstants.warpspeed / GameConstants.warpspeed * 1E6 / 20000.0;
                    float antimatter_peristence_to_add = (float)-Math.Min(currentAntimatter_missing, antimatter_mass * time_diff);
                    part.RequestResource("Antimatter", antimatter_peristence_to_add);
                }
            }
        }

        public override void OnUpdate() 
        {
            base.OnUpdate();
            Events["BeginResearch"].active = !IsEnabled;
            Events["ReprocessFuel"].active = !IsEnabled;
            Events["ActivateFactory"].active = !IsEnabled;
            Events["ActivateElectrolysis"].active = false;
            Events["ActivateCentrifuge"].active = !IsEnabled && vessel.Splashed;
            Events["StopActivity"].active = IsEnabled;

            if (IsEnabled) 
            {
                //anim [animName1].normalizedTime = 1f;
                statusTitle = modes[active_mode] + "...";
                Fields["scienceRate"].guiActive = false;
                Fields["collectedScience"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                Fields["centrifugeRate"].guiActive = false;
                Fields["antimatterProductionEfficiency"].guiActive = false;
                Fields["powerStr"].guiActive = true;

                double currentpowertmp = electrical_power_ratio * PluginHelper.BasePowerConsumption;
                powerStr = currentpowertmp.ToString("0.0000") + "MW / " + PluginHelper.BasePowerConsumption.ToString("0.0000") + "MW";
                if (active_mode == 0) // Research
                { 
                    Fields["scienceRate"].guiActive = true;
                    Fields["collectedScience"].guiActive = true;
                    double scienceratetmp = science_rate_f * GameConstants.EARH_DAY_SECONDS;
                    scienceRate = scienceratetmp.ToString("0.0000") + "/Day";
                    collectedScience = science_to_add.ToString("0.000000");
                }
                else if (active_mode == 1) // Fuel Reprocessing
                { 
                    Fields["reprocessingRate"].guiActive = true;
                    reprocessingRate = reprocessing_rate_f.ToString("0.0") + " Hours Remaining";
                }
                else if (active_mode == 2) // Antimatter
                { 
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseAMFPowerConsumption;
                    Fields["antimatterRate"].guiActive = true;
                    Fields["antimatterProductionEfficiency"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + PluginHelper.BaseAMFPowerConsumption.ToString("0.00") + "MW";
                    antimatterProductionEfficiency = (anti_factory.getAntimatterProductionEfficiency() * 100).ToString("0.0000") + "%";
                    double antimatter_rate_per_day = antimatter_rate_f * GameConstants.EARH_DAY_SECONDS;
                    
                    if (antimatter_rate_per_day > 0.1) 
                        antimatterRate = (antimatter_rate_per_day).ToString("0.0000") + " mg/day";
                    else 
                    {
                        if (antimatter_rate_per_day > 0.1e-3) 
                            antimatterRate = (antimatter_rate_per_day*1e3).ToString("0.0000") + " ug/day";
                        else 
                            antimatterRate = (antimatter_rate_per_day*1e6).ToString("0.0000") + " ng/day";
                    }
                }
                else if (active_mode == 3) // Electrolysis
                { 
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseELCPowerConsumption;
                    Fields["electrolysisRate"].guiActive = true;
                    double electrolysisratetmp = -electrolysis_rate_f * GameConstants.EARH_DAY_SECONDS;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + PluginHelper.BaseELCPowerConsumption.ToString("0.00") + "MW";
                }
                else if (active_mode == 4) // Centrifuge
                {
                    currentpowertmp = electrical_power_ratio * PluginHelper.BaseCentriPowerConsumption;
                    Fields["centrifugeRate"].guiActive = true;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + PluginHelper.BaseCentriPowerConsumption.ToString("0.00") + "MW";
                    float deut_per_hour = deut_rate_f * 3600;
                    centrifugeRate = deut_per_hour.ToString("0.00") + " Kg Deuterium/Hour";
                }
                else
                { 
                    // nothing 
                }
            } 
            else 
            {
                if (play_down) 
                {
                    anim[animName1].speed = -1f;
                    anim[animName1].normalizedTime = 1f;
                    anim.Blend(animName1, 2f);
                    anim2[animName2].speed = -1f;
                    anim2[animName2].normalizedTime = 1f;
                    anim2.Blend(animName2, 2f);
                    play_down = false;
                }

                //anim [animName1].normalizedTime = 0f;
                Fields["scienceRate"].guiActive = false;
                Fields["collectedScience"].guiActive = false;
                Fields["reprocessingRate"].guiActive = false;
                Fields["antimatterRate"].guiActive = false;
                Fields["powerStr"].guiActive = false;
                Fields["centrifugeRate"].guiActive = false;
                Fields["electrolysisRate"].guiActive = false;
                Fields["antimatterProductionEfficiency"].guiActive = false;

                if (crew_capacity_ratio > 0) 
                    statusTitle = "Idle";
                else 
                    statusTitle = "Not enough crew";
            }
        }

        private float GetKerbalScienceFactor(ProtoCrewMember kerbal)
        {
            float kerbalFactor;
            
            // initialise with profession
            if (kerbal.experienceTrait.Title == "Scientist")
                kerbalFactor = 1.5f;
            else if (kerbal.experienceTrait.Title == "Engineer")
                kerbalFactor = 1f;
            else
                kerbalFactor = 0.5f;

            // moidy by experience level
            kerbalFactor *= (kerbal.experienceLevel + 10f) / 10f;

            // final modify result by kerbal stupidity (+/- 10%)
            return kerbalFactor * (1.1f - (kerbal.stupidity / 5f));
        }

        public override void OnFixedUpdate() 
        {
            float global_rate_multipliers = 1;
            crew_capacity_ratio = ((float)part.protoModuleCrew.Count) / ((float)part.CrewCapacity);
            global_rate_multipliers = global_rate_multipliers * crew_capacity_ratio;

            if (IsEnabled) 
            {
                if (active_mode == 0)  // Research
                { 
                    double electrical_power_provided = consumeFNResource(PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption);
                    global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;

                    float kerbalScienceSkillFactor = part.protoModuleCrew.Sum(proto_crew_member => GetKerbalScienceFactor(proto_crew_member) / 2f);
                    float altitude_multiplier = Math.Max((float)(vessel.altitude / (vessel.mainBody.Radius)), 1);

                    science_rate_f = (float)(kerbalScienceSkillFactor * GameConstants.baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) 
                        / GameConstants.EARH_DAY_SECONDS * global_rate_multipliers 
                        / (Mathf.Sqrt(altitude_multiplier)));

                    if (ResearchAndDevelopment.Instance != null && !double.IsNaN(science_rate_f) && !double.IsInfinity(science_rate_f))
                    {
                        //ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + science_rate_f * TimeWarp.fixedDeltaTime;
                        science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                    }
                }
                else if (active_mode == 1) // Fuel Reprocessing
                { 
                    double electrical_power_provided = consumeFNResource(PluginHelper.BasePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BasePowerConsumption);
                    global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                    reprocessor.UpdateFrame(global_rate_multipliers);
                    if (reprocessor.getActinidesRemovedPerHour() > 0) {
                        reprocessing_rate_f = (float)(reprocessor.getRemainingAmountToReprocess() / reprocessor.getActinidesRemovedPerHour());
                    } else {
                        IsEnabled = false;
                    }
                } else if (active_mode == 2) 
                { //Antimatter
                    double electrical_power_provided = consumeFNResource(PluginHelper.BaseAMFPowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BaseAMFPowerConsumption);
                    global_rate_multipliers = crew_capacity_ratio * electrical_power_ratio;
                    anti_factory.produceAntimatterFrame(global_rate_multipliers);
                    antimatter_rate_f = (float)anti_factory.getAntimatterProductionRate();
                } else if (active_mode == 3) 
                {
                    IsEnabled = false;
                } else if (active_mode == 4) 
                { // Centrifuge
                    if (vessel.Splashed) {
                        float electrical_power_provided = consumeFNResource(PluginHelper.BaseCentriPowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / PluginHelper.BaseCentriPowerConsumption);
                        global_rate_multipliers = global_rate_multipliers * electrical_power_ratio;
                        float deut_produced = (float)(global_rate_multipliers * GameConstants.deuterium_timescale * GameConstants.deuterium_abudance * 1000.0f);
                        deut_rate_f = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Deuterium, -deut_produced * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                    } else {
                        ScreenMessages.PostScreenMessage("You must be splashed down to perform this activity.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                }

                if (electrical_power_ratio <= 0) {
                    deut_rate_f = 0;
                    electrolysis_rate_f = 0;
                    science_rate_f = 0;
                    antimatter_rate_f = 0;
                    reprocessing_rate_f = 0;
                }

                last_active_time = (float)Planetarium.GetUniversalTime();
            } 
            else 
            {
                // nothing
            }
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null) return false;

            if (science_to_add > 0)
            {
                result_title = experiment.experimentTitle;
                result_string = "Science experiments were conducted in the vicinity of " + vessel.mainBody.name + ".";

                transmit_value = science_to_add;
                recovery_value = science_to_add;
                data_size = science_to_add * 1.25f;
                xmit_scalar = 1;
                
                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(vessel), vessel.mainBody, "");
                subject.scienceCap = 167 * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, false);
                ref_value = subject.scienceCap;

                science_data = new ScienceData(science_to_add, 1, 0, subject.id, "Science Lab Data");

                return true;
            }
            return false;
        }

        protected override void cleanUpScienceData()
        {
            science_to_add = 0;
        }

        public override string getResourceManagerDisplayName() {
            if (IsEnabled) {
                return "Science Lab (" + modes[active_mode] + ")";
            }
            return "Science Lab";
        }


    }
}
