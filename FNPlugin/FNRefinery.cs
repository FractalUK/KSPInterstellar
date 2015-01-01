using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Refinery")]
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "U")]
        public string uraniumNitrideRate;
        [KSPField(isPersistant = false, guiActive = true, guiName = "H")]
        public string ammoniaRate;

        //Internal
        protected double electrolysis_rate_d = 0;
        protected double methane_rate_d = 0;
        protected double reprocessing_rate_d = 0;
        protected double mining_rate_d = 0;
        protected double anthra_rate_d = 0;
        protected double monoprop_rate_d = 0;
        protected double uranium_nitride_rate_d = 0;
        protected double ammonia_rate_d = 0;
        protected bool play_down = true;
        protected Animation anim;
        protected String[] modes = { "Nuclear Reprocessing", "Aluminium Electrolysis","Sabatier ISRU","Water Electrolysis","Anthraquinone Process","Monopropellant Production","UF4 Ammonolysis","Haber Process"};
        protected NuclearFuelReprocessor reprocessor;

        [KSPEvent(guiActive = true, guiName = "Reprocess Nuclear Fuel", active = true)]
        public void ReprocessFuel() {
            IsEnabled = true;
            play_down = true;
            active_mode = 0;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Electrolyse Aluminium", active = true)]
        public void ActivateElectrolysis() {
            IsEnabled = true;
            play_down = true;
            active_mode = 1;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Begin Sabatier ISRU", active = true)]
        public void ActivateSabatier() {
            IsEnabled = true;
            play_down = true;
            active_mode = 2;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Electrolyse Water", active = true)]
        public void ElectrolyseWater() {
            IsEnabled = true;
            play_down = true;
            active_mode = 3;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Anthraquinone Process", active = true)]
        public void AnthraquinoneProcess() {
            IsEnabled = true;
            play_down = true;
            active_mode = 4;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Produce Monopropellant", active = true)]
        public void ProduceMonoprop() {
            IsEnabled = true;
            play_down = true;
            active_mode = 5;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "UF4 Ammonolysis", active = true)]
        public void UraniumAmmonolysis() {
            IsEnabled = true;
            play_down = true;
            active_mode = 6;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Haber Process", active = true)]
        public void HaberProcess() {
            IsEnabled = true;
            play_down = true;
            active_mode = 7;
            activateAnimation();
        }

        [KSPEvent(guiActive = true, guiName = "Stop Current Activity", active = false)]
        public void StopActivity() {
            IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            part.force_activate();
            reprocessor = new NuclearFuelReprocessor(part);

            if (part.airlock != null && part.airlock.transform != null) {
                if (part.airlock.transform.gameObject != null) {
                    Destroy(part.airlock.transform.gameObject);
                }
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
            Events["ReprocessFuel"].active = !IsEnabled && reprocessor.HasActivityRequirements;
            Events["ActivateElectrolysis"].active = !IsEnabled;
            Events["ActivateSabatier"].active = !IsEnabled;
            Events["ElectrolyseWater"].active = !IsEnabled;
            Events["AnthraquinoneProcess"].active = !IsEnabled;
            Events["ProduceMonoprop"].active = !IsEnabled;
            Events["UraniumAmmonolysis"].active = !IsEnabled;
            Events["HaberProcess"].active = !IsEnabled;
            Events["StopActivity"].active = IsEnabled;
            Fields["reprocessingRate"].guiActive = false;
            Fields["electrolysisRate"].guiActive = false;
            Fields["sabatierRate"].guiActive = false;
            Fields["anthraquinoneRate"].guiActive = false;
            Fields["monopropellantRate"].guiActive = false;
            Fields["uraniumNitrideRate"].guiActive = false;
            Fields["ammoniaRate"].guiActive = false;
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
                } else if (active_mode == 6) { // Uranium Ammonolysis
                    Fields["uraniumNitrideRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseUraniumAmmonolysisConsumption;
                    double uraniumnitrideratetmp = uranium_nitride_rate_d * 3600;
                    uraniumNitrideRate = uraniumnitrideratetmp.ToString("0.0") + " mT/hour";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseUraniumAmmonolysisConsumption.ToString("0.00") + "MW";
                } else if (active_mode == 7) { // Haber Process
                    Fields["ammoniaRate"].guiActive = true;
                    double currentpowertmp = electrical_power_ratio * GameConstants.baseHaberProcessPowerConsumption;
                    double ammoniaratetmp = ammonia_rate_d * 3600;
                    ammoniaRate = ammoniaratetmp.ToString("0.00") + " mT/hour";
                    powerStr = currentpowertmp.ToString("0.00") + "MW / " + GameConstants.baseHaberProcessPowerConsumption.ToString("0.00") + "MW";
                }
            } else {
                if (play_down && anim != null) {
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
                    double electrical_power_provided = consumeFNResource(reprocessor.PowerRequirements, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / reprocessor.PowerRequirements);
                    reprocessor.UpdateFrame(electrical_power_ratio);
                    if (reprocessor.getActinidesRemovedPerHour() > 0) {
                        reprocessing_rate_d = reprocessor.getRemainingAmountToReprocess() / reprocessor.getActinidesRemovedPerHour();
                    } else {
                        ScreenMessages.PostScreenMessage("Unable to Reprocess Nuclear Fuel", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }                    
                } else if (active_mode == 1) { // Aluminium Electrolysis
                    double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                    double density_alumina = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Alumina).density;
                    double aluminium_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Aluminium).density;
                    double oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
                    electrolysis_rate_d = electrical_power_provided / GameConstants.aluminiumElectrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double alumina_consumption_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Alumina, electrolysis_rate_d * TimeWarp.fixedDeltaTime / density_alumina) / TimeWarp.fixedDeltaTime * density_alumina;
                    double mass_rate = alumina_consumption_rate;
                    electrolysis_rate_d = part.RequestResource(InterstellarResourcesConfiguration.Instance.Aluminium, -mass_rate * TimeWarp.fixedDeltaTime / aluminium_density) * aluminium_density;
                    electrolysis_rate_d += part.RequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -GameConstants.aluminiumElectrolysisMassRatio * mass_rate * TimeWarp.fixedDeltaTime / oxygen_density) * oxygen_density;
                    electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime;
                } else if (active_mode == 2) { // Sabatier ISRU
                    if (FlightGlobals.getStaticPressure(vessel.transform.position) * ORSAtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(vessel.mainBody.flightGlobalsIndex, "Carbon Dioxide") >= 0.01) {
                        double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                        electrolysis_rate_d = electrical_power_provided / GameConstants.electrolysisEnergyPerTon * vessel.atmDensity / TimeWarp.fixedDeltaTime;
                        double hydrogen_rate = electrolysis_rate_d / (1 + GameConstants.electrolysisMassRatio);
                        double oxygen_rate = hydrogen_rate * (GameConstants.electrolysisMassRatio-1);
                        double density_h = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
                        double density_o = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
                        double density_ch4 = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Methane).density;
                        double h2_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, hydrogen_rate * TimeWarp.fixedDeltaTime / density_h / 2);
                        if (h2_rate > 0) {
                            double o_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -oxygen_rate * TimeWarp.fixedDeltaTime / density_o);
                            double methane_rate = oxygen_rate * 2;
                            methane_rate_d = -part.RequestResource(InterstellarResourcesConfiguration.Instance.Methane, -methane_rate * TimeWarp.fixedDeltaTime / density_ch4) * density_ch4 / TimeWarp.fixedDeltaTime;
                        }
                    } else {
                        ScreenMessages.PostScreenMessage("Ambient C02 insufficient.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 3) { // Water Electrolysis
                    double density_h = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
                    double density_o = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
                    double density_h2o = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.baseELCPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseELCPowerConsumption);
                    electrolysis_rate_d = electrical_power_provided / GameConstants.electrolysisEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double water_consumption_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Water, electrolysis_rate_d * TimeWarp.fixedDeltaTime / density_h2o) / TimeWarp.fixedDeltaTime * density_h2o;
                    double hydrogen_rate = water_consumption_rate / (1 + GameConstants.electrolysisMassRatio);
                    double oxygen_rate = hydrogen_rate * GameConstants.electrolysisMassRatio;
                    electrolysis_rate_d = part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogen_rate * TimeWarp.fixedDeltaTime / density_h);
                    electrolysis_rate_d += part.RequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -oxygen_rate * TimeWarp.fixedDeltaTime / density_o);
                    electrolysis_rate_d = electrolysis_rate_d / TimeWarp.fixedDeltaTime * density_h;
                } else if (active_mode == 4) { // Anthraquinone Process
                    double density_h2o = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
                    double density_h2o2 = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.baseAnthraquiononePowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseAnthraquiononePowerConsumption);
                    anthra_rate_d = electrical_power_provided / GameConstants.anthraquinoneEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double water_consumption_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Water, anthra_rate_d * TimeWarp.fixedDeltaTime / density_h2o) / TimeWarp.fixedDeltaTime * density_h2o;
                    anthra_rate_d = -part.RequestResource(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide, -water_consumption_rate * TimeWarp.fixedDeltaTime / density_h2o2) * density_h2o2 / TimeWarp.fixedDeltaTime;
                    if (water_consumption_rate <= 0 && electrical_power_ratio > 0) {
                        ScreenMessages.PostScreenMessage("Water is required to perform the Anthraquinone Process.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
                    }
                } else if (active_mode == 5) { // Monoprop Production
                    double density_h2o2 = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
                    double density_h2o = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
                    double density_ammonia = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.basePechineyUgineKuhlmannPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.basePechineyUgineKuhlmannPowerConsumption);
                    monoprop_rate_d = electrical_power_provided / GameConstants.pechineyUgineKuhlmannEnergyPerTon / TimeWarp.fixedDeltaTime;
                    double ammonia_consumption_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, 0.5 * monoprop_rate_d * (1 - GameConstants.pechineyUgineKuhlmannMassRatio) * TimeWarp.fixedDeltaTime / density_ammonia) * density_ammonia / TimeWarp.fixedDeltaTime;
                    double h202_consumption_rate = part.RequestResource(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide, 0.5 * monoprop_rate_d * GameConstants.pechineyUgineKuhlmannMassRatio * TimeWarp.fixedDeltaTime / density_h2o2) * density_h2o2 / TimeWarp.fixedDeltaTime;
                    if (ammonia_consumption_rate > 0 && h202_consumption_rate > 0) {
                        double mono_prop_produciton_rate = ammonia_consumption_rate + h202_consumption_rate;
                        double density_monoprop = PartResourceLibrary.Instance.GetDefinition("MonoPropellant").density;
                        monoprop_rate_d = -ORSHelper.fixedRequestResource(part,"MonoPropellant", -mono_prop_produciton_rate * TimeWarp.fixedDeltaTime / density_monoprop)*density_monoprop/TimeWarp.fixedDeltaTime;
                        ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Water, -mono_prop_produciton_rate * TimeWarp.fixedDeltaTime * 1.12436683185 / density_h2o);
                    } else {
                        if (electrical_power_ratio > 0) {
                            monoprop_rate_d = 0;
                            ScreenMessages.PostScreenMessage("Ammonia and Hydrogen Peroxide are required to produce Monopropellant.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            IsEnabled = false;
                        }
                    }
                } else if (active_mode == 6) {
                    double density_ammonia = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
                    double density_uf4 = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).density;
                    double density_un = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumNitride).density;
                    double electrical_power_provided = consumeFNResource((GameConstants.baseUraniumAmmonolysisConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseUraniumAmmonolysisConsumption);
                    double lpersec = GameConstants.baseUraniumAmmonolysisRate * electrical_power_ratio;
                    double uf4persec = lpersec * 1.24597 / density_uf4;
                    double unpersec = lpersec / density_un;
                    double ammoniapersec = lpersec * 0.901 / density_ammonia;
                    double uf4_rate = ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.UraniumTetraflouride, uf4persec * TimeWarp.fixedDeltaTime);
                    double ammonia_rate = ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Ammonia, uf4persec * TimeWarp.fixedDeltaTime);
                    if (uf4_rate > 0 && ammonia_rate > 0) {
                        uranium_nitride_rate_d = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.UraniumNitride, -uf4_rate * density_uf4 / 1.24597 / density_un)/TimeWarp.fixedDeltaTime*density_un;
                    } else {
                        if (electrical_power_ratio > 0) {
                            uranium_nitride_rate_d = 0;
                            ScreenMessages.PostScreenMessage("Uranium Tetraflouride and Ammonia are required to produce Uranium Nitride.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                            IsEnabled = false;
                        }
                    }

                } else if (active_mode == 7) {
                    if (FlightGlobals.getStaticPressure(vessel.transform.position) * ORSAtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(vessel.mainBody.flightGlobalsIndex, "Nitrogen") >= 0.1) {
                        double density_ammonia = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
                        double density_h = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
                        double electrical_power_provided = consumeFNResource((GameConstants.baseHaberProcessPowerConsumption) * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                        electrical_power_ratio = (float)(electrical_power_provided / TimeWarp.fixedDeltaTime / GameConstants.baseHaberProcessPowerConsumption);
                        double hydrogen_rate_t = electrical_power_provided / GameConstants.baseHaberProcessEnergyPerTon * GameConstants.ammoniaHydrogenFractionByMass/TimeWarp.fixedDeltaTime;
                        double ammonia_rate_to_add_t = ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Hydrogen, hydrogen_rate_t * TimeWarp.fixedDeltaTime / density_h) * density_h / GameConstants.ammoniaHydrogenFractionByMass / TimeWarp.fixedDeltaTime;
                        if (ammonia_rate_to_add_t > 0) {
                            ammonia_rate_d = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Ammonia, -ammonia_rate_to_add_t * TimeWarp.fixedDeltaTime / density_ammonia) * density_ammonia / TimeWarp.fixedDeltaTime;
                        } else {
                            if (electrical_power_ratio > 0) {
                                ScreenMessages.PostScreenMessage("Hydrogen is required to perform the Haber Process.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                                IsEnabled = false;
                            }
                        }
                    } else {
                        ScreenMessages.PostScreenMessage("Ambient Nitrogen Insufficient.", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        IsEnabled = false;
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

        protected void activateAnimation() {
            if (anim != null) {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, 1, 1);
            }
        }

    }
}
