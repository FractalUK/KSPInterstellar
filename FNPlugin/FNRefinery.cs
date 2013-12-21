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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Refinery")]
        public string statusTitle;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "R")]
        public string reprocessingRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "E")]
        public string electrolysisRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "S")]
        public string sabatierRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "A")]
        public string anthraquinoneRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "M")]
        public string monopropellantRate;

        //Internal
        protected double electrolysis_rate_d = 0;
        protected double methane_rate_d = 0;
        protected double reprocessing_rate_d = 0;
        protected double mining_rate_d = 0;
        protected double anthra_rate_d = 0;
        protected double monoprop_rate_d = 0;
        protected bool play_down = true;
        protected Animation anim;
        protected String[] modes = { "Nuclear Reprocessing", "Aluminium Electrolysis","Sabatier ISRU","Water Electrolysis","Anthraquinone Process","Monopropellant Production"};

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() {
            IsEnabled = true;
            play_down = true;
            active_mode = 0;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Electrolyse Aluminium", active = true)]
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

        [KSPEvent(guiActive = true, guiName = "Electrolyse Water", active = true)]
        public void ElectrolyseWater() {
            IsEnabled = true;
            play_down = true;
            active_mode = 3;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Anthraquinone Process", active = true)]
        public void AnthraquinoneProcess() {
            IsEnabled = true;
            play_down = true;
            active_mode = 4;
            anim[animName].speed = 1f;
            anim[animName].normalizedTime = 0f;
            anim.Blend(animName, 1, 1);
        }

        [KSPEvent(guiActive = true, guiName = "Produce Monopropellant", active = true)]
        public void ProduceMonoprop() {
            IsEnabled = true;
            play_down = true;
            active_mode = 5;
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
            Events["ActivateElectrolysis"].active = !IsEnabled;
            Events["ActivateSabatier"].active = !IsEnabled;
            Events["ElectrolyseWater"].active = !IsEnabled;
            Events["AnthraquinoneProcess"].active = !IsEnabled;
            Events["ProduceMonoprop"].active = !IsEnabled;
            Events["StopActivity"].active = IsEnabled;
            Fields["reprocessingRate"].guiActive = false;
            Fields["electrolysisRate"].guiActive = false;
            Fields["sabatierRate"].guiActive = false;
            Fields["anthraquinoneRate"].guiActive = false;
            Fields["monopropellantRate"].guiActive = false;
            Fields["powerStr"].guiActive = false;

            if (IsEnabled) {
                Events["StopActivity"].guiName = "Stop " + modes[active_mode];
                Fields["powerStr"].guiActive = true;
                statusTitle = modes[active_mode] + "...";
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
                } else if (active_mode == 3) { // Water Electrolysis
                    Fields["electrolysisRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseELCPowerConsumption;
                    double electrolysisratetmp = -electrolysis_rate_d * 86400;
                    electrolysisRate = electrolysisratetmp.ToString("0.0") + " mT/day";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseELCPowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 4) { // Anthraquinone Process
                    Fields["anthraquinoneRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseAnthraquiononePowerConsumption;
                    double anthraratetmp = anthra_rate_d * 3600;
                    anthraquinoneRate = anthraratetmp.ToString("0.0") + " mT/hour";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseAnthraquiononePowerConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 5) { // Produce MonoProp
                    Fields["monopropellantRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.basePechineyUgineKuhlmannPowerConsumption;
                    double monoratetmp = monoprop_rate_d * 3600;
                    monopropellantRate = monoratetmp.ToString("0.0") + " mT/hour";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.basePechineyUgineKuhlmannPowerConsumption.ToString("0.00") + "MW";
                }
            } else {
                if (play_down) {
                    anim[animName].speed = -1f;
                    anim[animName].normalizedTime = 0f;
                    anim.Blend(animName,0,1);
                    play_down = false;
                }
                statusTitle = "Offline";
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
                } else if (active_mode == 1) { // Aluminium Electrolysis
                    double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                    double density_alumina = PartResourceLibrary.Instance.GetDefinition("Alumina").density;
                    double aluminium_density = PartResourceLibrary.Instance.GetDefinition(PluginHelper.aluminium_resource_name).density;
                    double oxygen_density = PartResourceLibrary.Instance.GetDefinition(PluginHelper.oxygen_resource_name).density;
                    electrolysis_rate_d = electrical_power_provided / GameConstants.aluminiumElectrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double alumina_consumption_rate = part.RequestResource("Alumina", electrolysis_rate_d * TimeWarp.fixedDeltaTime / density_alumina) / TimeWarp.fixedDeltaTime * density_alumina;
                    double mass_rate = alumina_consumption_rate;
                    electrolysis_rate_d = part.RequestResource(PluginHelper.aluminium_resource_name, -mass_rate * TimeWarp.fixedDeltaTime / aluminium_density) * aluminium_density;
                    electrolysis_rate_d += part.RequestResource(PluginHelper.oxygen_resource_name, -GameConstants.aluminiumElectrolysisMassRatio * mass_rate * TimeWarp.fixedDeltaTime / oxygen_density) * oxygen_density;
                    electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime;
                } else if (active_mode == 2) { // Sabatier ISRU
                    if (FlightGlobals.getStaticPressure(vessel.transform.position) * AtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(vessel.mainBody.flightGlobalsIndex, "Carbon Dioxide") >= 0.01) {
                        double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                        electrolysis_rate_d = electrical_power_provided / GameConstants.electrolysisEnergyPerTon * vessel.atmDensity / TimeWarp.fixedDeltaTime;
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
                } else if (active_mode == 3) { // Water Electrolysis
                    double density_h = PartResourceLibrary.Instance.GetDefinition(PluginHelper.hydrogen_resource_name).density;
                    double density_o = PartResourceLibrary.Instance.GetDefinition(PluginHelper.oxygen_resource_name).density;
                    double density_h2o = PartResourceLibrary.Instance.GetDefinition(PluginHelper.water_resource_name).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                    electrolysis_rate_d = electrical_power_provided / GameConstants.electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double water_consumption_rate = part.RequestResource(PluginHelper.water_resource_name, electrolysis_rate_d * TimeWarp.fixedDeltaTime / density_h2o) / TimeWarp.fixedDeltaTime*density_h2o;
                    double hydrogen_rate = water_consumption_rate / (1 + GameConstants.electrolysisMassRatio);
                    double oxygen_rate = hydrogen_rate * GameConstants.electrolysisMassRatio;
                    electrolysis_rate_d = part.RequestResource(PluginHelper.hydrogen_resource_name, -hydrogen_rate * TimeWarp.fixedDeltaTime / density_h);
                    electrolysis_rate_d += part.RequestResource(PluginHelper.oxygen_resource_name, -oxygen_rate * TimeWarp.fixedDeltaTime / density_o);
                    electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime * density_h;
                } else if (active_mode == 4) { // Anthraquinone Process
                    double density_h2o = PartResourceLibrary.Instance.GetDefinition(PluginHelper.water_resource_name).density;
                    double density_h2o2 = PartResourceLibrary.Instance.GetDefinition(PluginHelper.hydrogen_peroxide_resource_name).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.baseAnthraquiononePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseAnthraquiononePowerConsumption);
                    anthra_rate_d = electrical_power_provided / GameConstants.anthraquinoneEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double water_consumption_rate = part.RequestResource(PluginHelper.water_resource_name, anthra_rate_d * TimeWarp.fixedDeltaTime / density_h2o) / TimeWarp.fixedDeltaTime * density_h2o;
                    anthra_rate_d = -part.RequestResource(PluginHelper.hydrogen_peroxide_resource_name, -water_consumption_rate * TimeWarp.fixedDeltaTime / density_h2o2) * density_h2o2/TimeWarp.fixedDeltaTime;
                    if (water_consumption_rate <= 0 && electrical_power_ratio > 0) {
                        ScreenMessages.PostScreenMessage("Water is required to perform the Anthraquinone Process.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 5) { // Monoprop Production
                    double density_h2o2 = PartResourceLibrary.Instance.GetDefinition(PluginHelper.hydrogen_peroxide_resource_name).density;
                    double density_ammonia = PartResourceLibrary.Instance.GetDefinition(PluginHelper.ammonia_resource_name).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.basePechineyUgineKuhlmannPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.basePechineyUgineKuhlmannPowerConsumption);
                    monoprop_rate_d = electrical_power_provided / GameConstants.pechineyUgineKuhlmannEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double ammonia_consumption_rate = part.RequestResource(PluginHelper.ammonia_resource_name, 0.5 * monoprop_rate_d * (1 - GameConstants.pechineyUgineKuhlmannMassRatio) * TimeWarp.fixedDeltaTime / density_ammonia) * density_ammonia * TimeWarp.fixedDeltaTime;
                    double h202_consumption_rate = part.RequestResource(PluginHelper.hydrogen_peroxide_resource_name, 0.5 * monoprop_rate_d * GameConstants.pechineyUgineKuhlmannMassRatio * TimeWarp.fixedDeltaTime / density_h2o2) * density_h2o2 * TimeWarp.fixedDeltaTime;
                    if (ammonia_consumption_rate > 0 && h202_consumption_rate > 0) {
                        double mono_prop_produciton_rate = ammonia_consumption_rate + h202_consumption_rate;
                        double density_monoprop = PartResourceLibrary.Instance.GetDefinition("MonoPropellant").density;
                        monoprop_rate_d = -part.RequestResource("MonoPropellant", -mono_prop_produciton_rate * TimeWarp.fixedDeltaTime / density_monoprop)*density_monoprop/TimeWarp.fixedDeltaTime;
                    } else {
                        if (electrical_power_ratio > 0) {
                            monoprop_rate_d = 0;
                            ScreenMessages.PostScreenMessage("Ammonia and Hydrogen Peroxide are required to produce Monopropellant.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            IsEnabled = false;
                        }
                    }
                }
            } else {
                
            }
        }

        public override string getResourceManagerDisplayName() {
            if (IsEnabled) {
                return "ISRU Refinery (" + modes[active_mode] + ")";
            }
            return "ISRU Refinery";
        }

        public override string GetInfo() {
            string infostr = "ISRU Refinery\nFunctions:\n";
            foreach (string mode in modes) {
                infostr += mode + "\n";
            }
            return infostr;
        }

    }
}
