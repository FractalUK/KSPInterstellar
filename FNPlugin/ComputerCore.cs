using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class ComputerCore : ModuleModableScienceGenerator, ITelescopeController, IUpgradeableModule
    {
        const float baseScienceRate = 0.3f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string computercoreType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Name")]
        public string nameStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Science Rate")]
        public string scienceRate;

        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public float megajouleRate;
        [KSPField(isPersistant = false)]
        public float upgradedMegajouleRate;

        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float science_to_add;
        [KSPField(isPersistant = true)]
        public bool coreInit = false;

        protected float science_rate_f;

        private ConfigNode _experiment_node;

        public String UpgradeTechnology { get { return "none"; } }

        public bool CanProvideTelescopeControl
        {
            get { return isupgraded; }
        }


        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitCore()
        {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;

            if (isupgraded || !PluginHelper.TechnologyIsInUse)
            {
                upgradePartModule();

                double now = Planetarium.GetUniversalTime();
                double time_diff = now - last_active_time;
                float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
                altitude_multiplier = Math.Max(altitude_multiplier, 1);

                double science_to_increment = baseScienceRate * time_diff / GameConstants.EARH_DAY_SECONDS * electrical_power_ratio * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) / ((float)Math.Sqrt(altitude_multiplier));
                science_to_increment = (double.IsNaN(science_to_increment) || double.IsInfinity(science_to_increment)) ? 0 : science_to_increment;
                science_to_add += (float)science_to_increment;

                var curReaction = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
                curReaction.PitchTorque = 5;
                curReaction.RollTorque = 5;
                curReaction.YawTorque = 5;
            } else
            {
                computercoreType = originalName;
            }


            this.part.force_activate();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitCore"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost;
            } else
            {
                Events["RetrofitCore"].active = false;
            }
            Fields["upgradeCostStr"].guiActive = !isupgraded;
            Fields["nameStr"].guiActive = isupgraded;
            Fields["scienceRate"].guiActive = isupgraded;

            float scienceratetmp = (float) (science_rate_f * GameConstants.KEBRIN_DAY_SECONDS);
            scienceRate = scienceratetmp.ToString("0.000") + "/Day";

            if (ResearchAndDevelopment.Instance != null)
            {
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
            }
        }

        public override void OnFixedUpdate()
        {

            if (!isupgraded)
            {
                float power_returned = consumeFNResource(megajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
            } else
            {
                float power_returned = consumeFNResource(upgradedMegajouleRate * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                electrical_power_ratio = power_returned / upgradedMegajouleRate;
                float altitude_multiplier = (float)(vessel.altitude / (vessel.mainBody.Radius));
                altitude_multiplier = Math.Max(altitude_multiplier, 1);
                science_rate_f = (float) (baseScienceRate * PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex, vessel.LandedOrSplashed) / GameConstants.KEBRIN_DAY_SECONDS * power_returned / upgradedMegajouleRate / Math.Sqrt(altitude_multiplier));
                if (ResearchAndDevelopment.Instance != null && !double.IsInfinity(science_rate_f) && !double.IsNaN(science_rate_f))
                {
                    science_to_add += science_rate_f * TimeWarp.fixedDeltaTime;
                }
            }
            last_active_time = (float)Planetarium.GetUniversalTime();
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(experimentID);
            if (experiment == null) return false;

            if (science_to_add > 0)
            {
                result_title = experiment.experimentTitle;
                //result_string = "Science experiments were conducted in the vicinity of " + vessel.mainBody.name + ".";

                result_string = this.nameStr + " " + getRandomExperimentResult();

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

        public void upgradePartModule()
        {
            computercoreType = upgradedName;
            if (nameStr == "")
            {
                ConfigNode[] namelist = ComputerCore.getNames();
                System.Random rands = new System.Random();
                ConfigNode myName = namelist[rands.Next(0, namelist.Length)];
                nameStr = myName.GetValue("name");
            }
            if (part.Modules.Contains("ModuleReactionWheel"))
            {
                ModuleReactionWheel reaction_wheel = this.part.Modules["ModuleReactionWheel"] as ModuleReactionWheel;
                reaction_wheel.PitchTorque = 5;
                reaction_wheel.RollTorque = 5;
                reaction_wheel.YawTorque = 5;
            }
            isupgraded = true;
            _experiment_node = GameDatabase.Instance.GetConfigNodes("EXPERIMENT_DEFINITION").FirstOrDefault(nd => nd.GetValue("id") == experimentID);
        }

        public static ConfigNode[] getNames()
        {
            ConfigNode[] namelist = GameDatabase.Instance.GetConfigNodes("AI_CORE_NAME");
            return namelist;
        }

        public override string getResourceManagerDisplayName()
        {
            return computercoreType;
        }

        public override string GetInfo()
        {
            string desc = "Power Requirements: " + megajouleRate.ToString("0.0") + " MW\n";
            desc = desc + "Upgraded Power Requirements: " + upgradedMegajouleRate.ToString("0.0") + " MW\n";
            return desc;
        }

        private string getRandomExperimentResult()
        {
            try
            {
                System.Random rnd = new System.Random();
                String[] result_strs = _experiment_node.GetNode("RESULTS").GetValuesStartsWith("default");
                int indx = rnd.Next(result_strs.Length);
                return result_strs[indx];
            } catch (Exception ex)
            {
                Debug.Log("[KSPI] Exception Generation Experiment Result: " + ex.Message + ": " + ex.StackTrace);
                return " has detected a glitch in the universe and recommends checking your installation of KSPInterstellar.";
            }
        }

    }
}

