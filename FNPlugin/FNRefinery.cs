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
        [KSPField(isPersistant = false, guiActive = true, guiName = "M")]
        public string miningRate;

        //Internal
        protected double electrolysis_rate_d = 0;
        protected double reprocessing_rate_d = 0;
        protected double mining_rate_d = 0;


        protected const double baseReprocessingRate = 0.4;
        protected const double basePowerConsumption = 5;
        protected const double electrolysisEnergyPerTon = 18159;
        protected const double bakingEnergyPerTon = 4920;
        protected const double aluminiumElectrolysisEnergyPerTon = 35485.714;
        protected const double electrolysisMassRatio = 7.936429;
        protected const double aluminiumElectrolysisMassRatio = 1.5;
        protected const double baseELCPowerConsumption = 40;
        protected const double baseMiningPowerConsumption = 10;
        protected const double baseMiningRatePerTon = 0.02777777777777777777;
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
            Fields["miningRate"].guiActive = false;

            if (IsEnabled) {
                statusTitle = modes[active_mode];
                if (active_mode == 0) { // Fuel Reprocessing
                    double currentpowertmp = electrical_power_ratio * baseELCPowerConsumption;
                    Fields["reprocessingRate"].guiActive = true;
                    reprocessingRate = reprocessing_rate_d.ToString("0.0") + " Hours Remaining";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + baseELCPowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 1) { // Electrolysis
                    Fields["electrolysisRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * baseELCPowerConsumption;
                    double electrolysisratetmp = -electrolysis_rate_d * 86400;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + " mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + baseELCPowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 2 || active_mode == 3) { // Mining
                    double currentpowertmp = electrical_power_ratio * baseELCPowerConsumption;
                    Fields["miningRate"].guiActive = true;
                    miningRate = (mining_rate_d * 3600).ToString("0.0") + " mT/hour";
                }
            } else {
                statusTitle = "Idle";
            }
        }

        public override void OnFixedUpdate() {
            if (IsEnabled) {
                if (active_mode == 0) { // Fuel Reprocessing
                    double electrical_power_provided = consumeFNResource(basePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float) (electrical_power_provided / TimeWarp.fixedDeltaTime / basePowerConsumption);

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
                        reprocessing_rate_d = (float)(amount_to_reprocess / actinidesremovedperhour);
                    } else { // Finished, hurray!
                        IsEnabled = false;
                    }
                } else if (active_mode == 1) { // Electrolysis
                    if (vessel.Splashed || (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_VALL) || (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_DUNA)) {
                        double electrical_power_provided = consumeFNResource((baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float) (electrical_power_provided / TimeWarp.fixedDeltaTime / baseELCPowerConsumption);
                        if (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_DUNA) { // Water on Duna must be baked out of the soil
                            electrolysis_rate_d = electrical_power_ratio / (electrolysisEnergyPerTon + bakingEnergyPerTon) / TimeWarp.fixedDeltaTime;
                        } else {
                            electrolysis_rate_d = electrical_power_ratio / electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                        }
                        double hydrogen_rate = electrolysis_rate_d / (1 + electrolysisMassRatio);
                        double oxygen_rate = hydrogen_rate * electrolysisMassRatio;
                        double density = PartResourceLibrary.Instance.GetDefinition("LiquidFuel").density;
                        electrolysis_rate_d = part.RequestResource("LiquidFuel", -hydrogen_rate * TimeWarp.fixedDeltaTime / density);
                        electrolysis_rate_d += part.RequestResource("Oxidizer", -oxygen_rate * TimeWarp.fixedDeltaTime / density);
                        electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime * density;
                    } else if (vessel.Landed) {
                        if (vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_MUN || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_IKE || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_TYLO) {
                            double electrical_power_provided = consumeFNResource((baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                            electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / baseELCPowerConsumption);
                            electrolysis_rate_d = electrical_power_provided / aluminiumElectrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                            double aluminium_density = PartResourceLibrary.Instance.GetDefinition("Aluminium").density;
                            double oxygen_density = PartResourceLibrary.Instance.GetDefinition("Oxidizer").density;
                            double mass_rate = electrolysis_rate_d;
                            electrolysis_rate_d = part.RequestResource("Aluminium", -mass_rate * TimeWarp.fixedDeltaTime / aluminium_density) * aluminium_density;
                            electrolysis_rate_d += part.RequestResource("Oxidizer", -aluminiumElectrolysisMassRatio * mass_rate * TimeWarp.fixedDeltaTime / oxygen_density) * oxygen_density;
                            electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime;
                        } else {
                            ScreenMessages.PostScreenMessage("No suitable resources found.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            IsEnabled = false;
                        }
                    } else {
                        ScreenMessages.PostScreenMessage("You must be landed or splashed down to perform this activity.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 2) { // Mine Uranium
                    double electrical_power_provided = consumeFNResource((baseMiningPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / baseELCPowerConsumption);
                    FNPlanetaryResourcePixel current_uranium_abundance_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Uranium", vessel.latitude, vessel.longitude);
                    double current_uranium_abundance = current_uranium_abundance_pixel.getAmount();
                    double mining_rate = baseMiningRatePerTon / current_uranium_abundance * electrical_power_ratio;
                    double uranium_density = PartResourceLibrary.Instance.GetDefinition(current_uranium_abundance_pixel.getResourceName()).density;
                    part.RequestResource(current_uranium_abundance_pixel.getResourceName(), -mining_rate / uranium_density*TimeWarp.fixedDeltaTime);
                } else if (active_mode == 3) { // " Thorium
                    double electrical_power_provided = consumeFNResource((baseMiningPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / baseELCPowerConsumption);
                    FNPlanetaryResourcePixel current_thorium_abundance_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", vessel.latitude, vessel.longitude);
                    double current_thorium_abundance = current_thorium_abundance_pixel.getAmount();
                    double mining_rate = baseMiningRatePerTon / current_thorium_abundance * electrical_power_ratio;
                    double thorium_density = PartResourceLibrary.Instance.GetDefinition(current_thorium_abundance_pixel.getResourceName()).density;
                    mining_rate_d = mining_rate;
                    part.RequestResource(current_thorium_abundance_pixel.getResourceName(), -mining_rate / thorium_density * TimeWarp.fixedDeltaTime);
                }
            }
        }


    }
}
