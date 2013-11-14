using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNRefinery : FNResourceSuppliableModule {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public int active_mode = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;

        //Internal
        protected double electrolysis_rate_f = 0;
        protected double reprocessing_rate_f = 0;

        protected const float baseReprocessingRate = 0.4f;
        protected const float basePowerConsumption = 5f;
        protected const float electrolysisEnergyPerTon = 18159f;
        protected const float bakingEnergyPerTon = 4920f;
        protected const float aluminiumElectrolysisEnergyPerTon = 35485.714f;
        protected const float electrolysisMassRatio = 7.936429f;
        protected const float aluminiumElectrolysisMassRatio = 1.5f;
        protected const float baseELCPowerConsumption = 40f;
        protected String[] modes = { "Reprocessing...", "Electrolysing...", "Mining Uranium...", "Mining Thorium..." };

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() {
            IsEnabled = true;
            active_mode = 0;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Electrolysis", active = true)]
        public void ActivateElectrolysis() {
            IsEnabled = true;
            active_mode = 1;
        }

        [KSPEvent(guiActive = true, guiName = "Mine Uranium", active = true)]
        public void MineUranium() {
            IsEnabled = true;
            active_mode = 2;
        }

        [KSPEvent(guiActive = true, guiName = "Mine Thorium", active = true)]
        public void MineThorium() {
            IsEnabled = true;
            active_mode = 3;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() {
            IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            part.force_activate();
        }

        public override void OnUpdate() {
            Events["ReprocessFuel"].active = !IsEnabled;
            Events["ActivateElectrolysis"].active = !IsEnabled && (vessel.Splashed || vessel.Landed);
            Events["MineUranium"].active = !IsEnabled;
            Events["MineThorium"].active = !IsEnabled;
            Events["StopActivity"].active = IsEnabled;
            Fields["reprocessingRate"].guiActive = false;
            Fields["electrolysisRate"].guiActive = false;

            if (IsEnabled) {
                if (active_mode == 0) { // Fuel Reprocessing
					Fields ["reprocessingRate"].guiActive = true;
					reprocessingRate = reprocessing_rate_f.ToString("0.0") + " Hours Remaining";
                }else if (active_mode == 1) { // Electrolysis
                    Fields["electrolysisRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * baseELCPowerConsumption;
                    double electrolysisratetmp = -electrolysis_rate_f * 86400;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + "mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + baseELCPowerConsumption.ToString("0.00") + "MW";
                }
            }
        }

        public override void OnFixedUpdate() {
            if (IsEnabled) {
                if (active_mode == 1) { // Fuel Reprocessing
                    float electrical_power_provided = consumeFNResource(basePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = electrical_power_provided / TimeWarp.fixedDeltaTime / basePowerConsumption;

                    List<PartResource> partresources = new List<PartResource>();
                    double currentActinides = 0;
                    double depletedfuelsparecapacity = 0;
                    double uf6sparecapacity = 0;
                    double thf4sparecapacity = 0;
                    double uf6tothf4_ratio = 0;
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Actinides").id, partresources);
                    foreach (PartResource partresource in partresources) {
                        currentActinides += partresource.amount;
                    }
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("DepletedFuel").id, partresources);
                    foreach (PartResource partresource in partresources) {
                        depletedfuelsparecapacity += partresource.maxAmount - partresource.amount;
                    }
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF6").id, partresources);
                    foreach (PartResource partresource in partresources) {
                        uf6sparecapacity += partresource.maxAmount - partresource.amount;
                    }
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, partresources);
                    foreach (PartResource partresource in partresources) {
                        thf4sparecapacity += partresource.maxAmount - partresource.amount;
                    }
                    uf6tothf4_ratio = uf6sparecapacity / (thf4sparecapacity + uf6sparecapacity);
                    double amount_to_reprocess = Math.Min(currentActinides, depletedfuelsparecapacity * 5.0);
                    if (currentActinides > 0 && !double.IsNaN(uf6tothf4_ratio) && !double.IsInfinity(uf6tothf4_ratio)) {
                        double actinides_removed = part.RequestResource("Actinides", baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400.0 * electrical_power_ratio);
                        double uf6added = part.RequestResource("UF6", -actinides_removed * 0.8 * uf6tothf4_ratio);
                        double th4added = part.RequestResource("ThF4", -actinides_removed * 0.8 * (1 - uf6tothf4_ratio));
                        double duf6added = part.RequestResource("DepletedFuel", -actinides_removed * 0.2);
                        double actinidesremovedperhour = actinides_removed / TimeWarp.fixedDeltaTime * 3600.0;
                        reprocessing_rate_f = (float)(amount_to_reprocess / actinidesremovedperhour);
                    } else { // Finished, hurray!
                        IsEnabled = false;
                    }
                }
            }
        }


    }
}
