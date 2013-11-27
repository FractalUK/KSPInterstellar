using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class FNRefinery : FNResourceSuppliableModule {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
        [KSPField(isPersistant = true)]
        public int active_mode = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float electrical_power_ratio;

        // Persistent False
        [KSPField(isPersistant = false)]
        public string animName;

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
        [KSPField(isPersistant = false, guiActive = true, guiName = "S")]
        public string sabatierRate;

        //Internal
        protected double electrolysis_rate_d = 0;
        protected double methane_rate_d = 0;
        protected double reprocessing_rate_d = 0;
        protected double mining_rate_d = 0;
        protected bool play_down = true;
        protected Animation anim;
        protected String[] modes = { "Reprocessing...", "Electrolysing...","Sabatier ISRU...", "Mining Uranium...", "Mining Thorium..." };

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() {
            IsEnabled = true;
            play_down = true;
            active_mode = 0;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Activate Electrolysis", active = true)]
        public void ActivateElectrolysis() {
            IsEnabled = true;
            play_down = true;
            active_mode = 1;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Begin Sabatier ISRU", active = true)]
        public void ActivateSabatier() {
            IsEnabled = true;
            play_down = true;
            active_mode = 2;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Mine Uranium", active = true)]
        public void MineUranium() {
            IsEnabled = true;
            play_down = true;
            active_mode = 3;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Mine Thorium", active = true)]
        public void MineThorium() {
            IsEnabled = true;
            play_down = true;
            active_mode = 4;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() {
            IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            part.force_activate();

            if (part.airlock.transform.gameObject != null) {
                Destroy(part.airlock.transform.gameObject);
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null) {
                anim[animName].layer = 1;
                if (IsEnabled) {
                    anim.Blend(animName, 2, 0);
                } else {
                    play_down = false;
                    anim[animName].speed = -1f;
                    anim[animName].normalizedTime = 0f;
                    anim.Blend(animName, 0, 1);
                }
            }
        }

        public override void OnUpdate() {
            Events["ReprocessFuel"].active = !IsEnabled;
            Events["ActivateElectrolysis"].active = !IsEnabled && (vessel.Splashed || vessel.Landed);
            Events["ActivateSabatier"].active = !IsEnabled;
            Events["MineUranium"].active = !IsEnabled && vessel.Landed;
            Events["MineThorium"].active = !IsEnabled && vessel.Landed;
            Events["StopActivity"].active = IsEnabled;
            Fields["reprocessingRate"].guiActive = false;
            Fields["electrolysisRate"].guiActive = false;
            Fields["miningRate"].guiActive = false;
            Fields["sabatierRate"].guiActive = false;
            Fields["powerStr"].guiActive = false;

            if (IsEnabled) {
                Fields["powerStr"].guiActive = true;
                statusTitle = modes[active_mode];
                if (active_mode == 0) { // Fuel Reprocessing
                    double currentpowertmp = electrical_power_ratio * GameConstants.basePowerConsumption;
                    Fields["reprocessingRate"].guiActive = true;
                    reprocessingRate = reprocessing_rate_d.ToString("0.0") + " Hours Remaining";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.basePowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 1) { // Electrolysis
                    Fields["electrolysisRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseELCPowerConsumption;
                    double electrolysisratetmp = -electrolysis_rate_d * 86400;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + " mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseELCPowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 2) { // Sabatier ISRU
                    Fields["sabatierRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseELCPowerConsumption;
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseELCPowerConsumption.ToString("0.00") + "MW";
                    sabatierRate = "CH4 " + (methane_rate_d * 86400).ToString("0.00") + " mT/day";
                } else if (active_mode == 3 || active_mode == 4) { // Mining
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseMiningPowerConsumption;
                    Fields["miningRate"].guiActive = true;
                    print(mining_rate_d);
                    if (mining_rate_d * 3600 < 0.01) {
                        if (mining_rate_d * 3600 < 0.00001) {
                            miningRate = (mining_rate_d * 3600000000).ToString("0.000") + " mL/hour";
                        } else {
                            miningRate = (mining_rate_d * 3600000).ToString("0.000") + " L/hour";
                        }
                    } else {
                        miningRate = (mining_rate_d * 3600).ToString("0.000") + " m^3/hour";
                    }
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseMiningPowerConsumption.ToString("0.00") + "MW";
                }
            } else {
                if (play_down) {
                    anim[animName].speed = -1f;
                    anim[animName].normalizedTime = 0f;
                    anim.Blend(animName,0,1);
                    play_down = false;
                }
                statusTitle = "Idle";
            }
        }

        public override void OnFixedUpdate() {
            if (IsEnabled) {
                if (active_mode == 0) { // Fuel Reprocessing
                    double electrical_power_provided = consumeFNResource(GameConstants.basePowerConsumption * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.basePowerConsumption);

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
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF4").id, partresources);
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
                        double actinides_removed = part.RequestResource("Actinides", GameConstants.baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400.0 * electrical_power_ratio);
                        double uf6added = part.RequestResource("UF4", -actinides_removed * 0.8 * uf6tothf4_ratio);
                        double th4added = part.RequestResource("ThF4", -actinides_removed * 0.8 * (1 - uf6tothf4_ratio));
                        double duf6added = part.RequestResource("DepletedFuel", -actinides_removed * 0.2);
                        double actinidesremovedperhour = actinides_removed / TimeWarp.fixedDeltaTime * 3600.0;
                        reprocessing_rate_d = (float)(amount_to_reprocess / actinidesremovedperhour);
                    } else { // Finished, hurray!
                        IsEnabled = false;
                    }
                } else if (active_mode == 1) { // Electrolysis
                    if (vessel.Splashed || (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_VALL) || (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_DUNA)) {
                        double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                        if (vessel.Landed && vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_DUNA) { // Water on Duna must be baked out of the soil
                            electrolysis_rate_d = electrical_power_ratio / (GameConstants.electrolysisEnergyPerTon + GameConstants.bakingEnergyPerTon) / TimeWarp.fixedDeltaTime;
                        } else {
                            electrolysis_rate_d = electrical_power_ratio / GameConstants.electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                        }
                        double hydrogen_rate = electrolysis_rate_d / (1 + GameConstants.electrolysisMassRatio);
                        double oxygen_rate = hydrogen_rate * GameConstants.electrolysisMassRatio;
                        double density_h = PartResourceLibrary.Instance.GetDefinition(PluginHelper.hydrogen_resource_name).density;
                        double density_o = PartResourceLibrary.Instance.GetDefinition(PluginHelper.oxygen_resource_name).density;
                        electrolysis_rate_d = part.RequestResource(PluginHelper.hydrogen_resource_name, -hydrogen_rate * TimeWarp.fixedDeltaTime / density_h);
                        electrolysis_rate_d += part.RequestResource(PluginHelper.oxygen_resource_name, -oxygen_rate * TimeWarp.fixedDeltaTime / density_o);
                        electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime * density_h;
                    } else if (vessel.Landed) {
                        if (vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_MUN || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_IKE || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_TYLO) {
                            double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                            electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                            electrolysis_rate_d = electrical_power_provided / GameConstants.aluminiumElectrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                            double aluminium_density = PartResourceLibrary.Instance.GetDefinition(PluginHelper.aluminium_resource_name).density;
                            double oxygen_density = PartResourceLibrary.Instance.GetDefinition(PluginHelper.oxygen_resource_name).density;
                            double mass_rate = electrolysis_rate_d;
                            electrolysis_rate_d = part.RequestResource(PluginHelper.aluminium_resource_name, -mass_rate * TimeWarp.fixedDeltaTime / aluminium_density) * aluminium_density;
                            electrolysis_rate_d += part.RequestResource(PluginHelper.oxygen_resource_name, -GameConstants.aluminiumElectrolysisMassRatio * mass_rate * TimeWarp.fixedDeltaTime / oxygen_density) * oxygen_density;
                            electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime;
                        } else {
                            ScreenMessages.PostScreenMessage("No suitable resources found.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            IsEnabled = false;
                        }
                    } else {
                        ScreenMessages.PostScreenMessage("You must be landed or splashed down to perform this activity.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 2) { // Sabatier ISRU
                    if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) && (vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_DUNA || vessel.mainBody.flightGlobalsIndex == PluginHelper.REF_BODY_EVE)) {
                        double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                        electrolysis_rate_d = electrical_power_ratio / GameConstants.electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime * vessel.atmDensity;
                        double hydrogen_rate = electrolysis_rate_d / (1 + GameConstants.electrolysisMassRatio);
                        double oxygen_rate = hydrogen_rate * GameConstants.electrolysisMassRatio;
                        double density_h = PartResourceLibrary.Instance.GetDefinition(PluginHelper.hydrogen_resource_name).density;
                        double density_o = PartResourceLibrary.Instance.GetDefinition(PluginHelper.oxygen_resource_name).density;
                        double density_ch4 = PartResourceLibrary.Instance.GetDefinition(PluginHelper.methane_resource_name).density;
                        double h2_rate = part.RequestResource(PluginHelper.hydrogen_resource_name, hydrogen_rate * TimeWarp.fixedDeltaTime / density_h / 2);
                        if (h2_rate > 0) {
                            double o_rate = part.RequestResource(PluginHelper.oxygen_resource_name, -oxygen_rate * TimeWarp.fixedDeltaTime / density_o);
                            double methane_rate = electrolysis_rate_d / 4.5;
                            methane_rate_d = -part.RequestResource(PluginHelper.methane_resource_name, -methane_rate * TimeWarp.fixedDeltaTime / density_ch4) * density_ch4 / TimeWarp.fixedDeltaTime;
                        }
                    } else {
                        ScreenMessages.PostScreenMessage("Ambient C02 insufficient.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 3) { // Mine Uranium
                    double electrical_power_provided = consumeFNResource((GameConstants.baseMiningPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseMiningPowerConsumption);
                    FNPlanetaryResourcePixel current_uranium_abundance_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Uranium", vessel.latitude, vessel.longitude);
                    double current_uranium_abundance = current_uranium_abundance_pixel.getAmount();
                    double mining_rate = GameConstants.baseMiningRatePerTon * current_uranium_abundance * electrical_power_ratio;
                    double uranium_density = PartResourceLibrary.Instance.GetDefinition(current_uranium_abundance_pixel.getResourceName()).density;
                    mining_rate_d = -part.RequestResource(current_uranium_abundance_pixel.getResourceName(), -mining_rate / uranium_density * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                } else if (active_mode == 4) { // " Thorium
                    double electrical_power_provided = consumeFNResource((GameConstants.baseMiningPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseMiningPowerConsumption);
                    FNPlanetaryResourcePixel current_thorium_abundance_pixel = FNPlanetaryResourceMapData.getResourceAvailability(vessel.mainBody.flightGlobalsIndex, "Thorium", vessel.latitude, vessel.longitude);
                    double current_thorium_abundance = current_thorium_abundance_pixel.getAmount();
                    double mining_rate = GameConstants.baseMiningRatePerTon * current_thorium_abundance * electrical_power_ratio;
                    double thorium_density = PartResourceLibrary.Instance.GetDefinition(current_thorium_abundance_pixel.getResourceName()).density;
                    mining_rate_d = -part.RequestResource(current_thorium_abundance_pixel.getResourceName(), -mining_rate / thorium_density * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                }
            } else {
                
            }
        }


    }
}
