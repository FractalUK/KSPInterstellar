extern alias ORSv1_2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_2::OpenResourceSystem;

namespace FNPlugin {
    class ElectricEngineControllerFX : FNResourceSuppliableModule, FNUpgradeableModule {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public bool vacplasmaadded = false;
        

        //Persistent False
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public int type;
        [KSPField(isPersistant = false)]
        public int upgradedtype;
        [KSPField(isPersistant = false)]
        public float baseISP;
        [KSPField(isPersistant = false)]
        public float maxPower;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineTypeStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string efficiencyStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Propellant")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = "";

        // internal
        protected List<ElectricEnginePropellant> propellants;
        protected ElectricEnginePropellant current_propellant;
        protected VInfoBox fuel_gauge;
        protected ModuleEnginesFX attachedEngine;
        protected float electrical_consumption_f = 0;
        protected float heat_production_f = 0;
        protected int rep = 0;
        protected bool hasrequiredupgrade;

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
        public void TogglePropellant() {
            togglePropellants();
        }

        [KSPAction("Toggle Propellant")]
        public void TogglePropellantAction(KSPActionParam param) {
            TogglePropellant();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            upgradePartModule();
            ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
        }

        public override void OnLoad(ConfigNode node) {
            engineTypeStr = originalName;
            if (isupgraded) {
                upgradePartModule();
            }
        }

        public override void OnStart(PartModule.StartState state) {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            attachedEngine = this.part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;
            this.resources_to_supply = resources_to_supply;
            propellants = getPropellants();
            base.OnStart(state);

            if (state == StartState.Editor) {
                if (hasTechsRequiredToUpgrade()) {
                    upgradePartModule();
                }
                return;
            }

            if (hasTechsRequiredToUpgrade()) {
                hasrequiredupgrade = true;
            }

            if (attachedEngine != null) {
                attachedEngine.Fields["finalThrust"].guiFormat = "F5";
            }

            fuel_gauge = part.stackIcon.DisplayInfo();
            current_propellant = fuel_mode < propellants.Count ? propellants[fuel_mode] : propellants.FirstOrDefault();
            setupPropellants();
        }

        public void setupPropellants() {
            List<Propellant> list_of_propellants = new List<Propellant>();
            Propellant new_propellant = current_propellant.Propellant;
            if (new_propellant.drawStackGauge) {
                new_propellant.drawStackGauge = false;
                fuel_gauge.SetMessage(current_propellant.PropellantGUIName);
                fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                fuel_gauge.SetValue(0f);
            }
            list_of_propellants.Add(new_propellant);

            if (!list_of_propellants.Exists(prop => PartResourceLibrary.Instance.GetDefinition(prop.name) == null)) {
                attachedEngine.propellants.Clear();
                attachedEngine.propellants = list_of_propellants;
                attachedEngine.SetupPropellant();
            } else {
                if (rep < propellants.Count) {
                    rep++;
                    togglePropellants();
                    return;
                }
                
            }

            if (HighLogic.LoadedSceneIsFlight) { // you can have any fuel you want in the editor but not in flight
                List<PartResource> totalpartresources = list_of_propellants.SelectMany(prop => part.GetConnectedResources(prop.name)).ToList();
                if(!list_of_propellants.All(prop => totalpartresources.Select(pr => pr.resourceName).Contains(prop.name)) && rep < propellants.Count) {
                    rep++;
                    togglePropellants();
                    return;
                }
            }
            rep = 0;
        }

        public override void OnUpdate() {
            if (ResearchAndDevelopment.Instance != null) {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
            } else {
                Events["RetrofitEngine"].active = false;
                Fields["upgradeCostStr"].guiActive = false;
            }

            propNameStr = current_propellant != null ? current_propellant.PropellantGUIName : "";

            if (attachedEngine != null && attachedEngine.isOperational) {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                Fields["efficiencyStr"].guiActive = true;
                electricalPowerConsumptionStr = electrical_consumption_f.ToString("0.00") + " MW";
                heatProductionStr = heat_production_f.ToString("0.00") + " MW";
                efficiencyStr = current_propellant != null ? (current_propellant.Efficiency * 100.0).ToString("0.0") + "%" : "";
            } else {
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
                Fields["efficiencyStr"].guiActive = false;
            }

            updatePropellantBar();
        }

        public void FixedUpdate() {
            ElectricEngineControllerFX.getAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName,0)); // set all FX to zero

            if (current_propellant != null && attachedEngine != null) {
                updateISP();
                List<ElectricEngineControllerFX> electric_engine_list = vessel.FindPartModulesImplementing<ElectricEngineControllerFX>();
                int engine_count = electric_engine_list.Count;
                double total_max_thrust = evaluateMaxThrust();
                double thrust_per_engine = total_max_thrust / (double)engine_count;
                double power_per_engine = Math.Min(0.5 * attachedEngine.currentThrottle * thrust_per_engine * current_propellant.IspMultiplier * baseISP / 1000.0 * 9.81, maxPower * current_propellant.Efficiency);
                double power_received = consumeFNResource(power_per_engine * TimeWarp.fixedDeltaTime / current_propellant.Efficiency, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                double heat_to_produce = power_received * (1.0 - current_propellant.Efficiency);
                double heat_production = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                // update GUI Values
                electrical_consumption_f = (float)power_received;
                heat_production_f = (float)heat_production;
                // thrust values
                double thrust_ratio = power_per_engine > 0 ? Math.Min(power_received / power_per_engine, 1.0) : 1;
                double actual_max_thrust = current_propellant.Efficiency * 2000.0f * power_received / (current_propellant.IspMultiplier * baseISP * 9.81f * attachedEngine.currentThrottle);

                if (attachedEngine.currentThrottle > 0) {
                    if (!double.IsNaN(actual_max_thrust) && !double.IsInfinity(actual_max_thrust)) {
                        attachedEngine.maxThrust = Mathf.Max((float)actual_max_thrust, 0.00001f);
                    } else {
                        attachedEngine.maxThrust = 0.00001f;
                    }
                    float fx_ratio = Mathf.Min(electrical_consumption_f / maxPower,attachedEngine.finalThrust/attachedEngine.maxThrust);
                    part.Effect(current_propellant.ParticleFXName, fx_ratio);
                } 

                if (isupgraded) {
                    List<PartResource> vacuum_resources = part.GetConnectedResources("VacuumPlasma").ToList();
                    double vacuum_plasma_needed = vacuum_resources.Sum(vc => vc.maxAmount - vc.amount);
                    double vacuum_plasma_current = vacuum_resources.Sum(vc => vc.amount);
                    if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                        part.RequestResource("VacuumPlasma", vacuum_plasma_current);
                    } else {
                        part.RequestResource("VacuumPlasma", -vacuum_plasma_needed);
                    }
                }
            } 
        }

        public void upgradePartModule() {
            isupgraded = true;
            type = upgradedtype;
            propellants = getPropellants();
            engineTypeStr = upgradedName;

            if (!vacplasmaadded && type == (int)ElectricEngineType.VACUUMTHRUSTER) {
                vacplasmaadded = true;
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", "VacuumPlasma");
                node.AddValue("maxAmount", 10);
                node.AddValue("amount", 10);
                part.AddResource(node);
            }
        }

        public bool hasTechsRequiredToUpgrade() {
            return PluginHelper.upgradeAvailable(upgradeTechReq);
        }

        public override string GetInfo() {
            List<ElectricEnginePropellant> props = getPropellants();
            string return_str = "Max Power Consumption: " + maxPower.ToString("") + " MW\n";
            double thrust_per_mw = 2e6/9.81/baseISP/1000.0;
            props.ForEach(prop => {
                double ispProp = baseISP * prop.IspMultiplier;
                double thrustProp = thrust_per_mw / prop.IspMultiplier*prop.Efficiency;
                return_str = return_str + "---" + prop.PropellantGUIName + "---\nThrust: " + thrustProp.ToString("0.0000") + " kN per MW\nEfficiency: " + (prop.Efficiency*100.0).ToString("0.00") + "%\nISP: " + ispProp.ToString("0.00") + "s\n";
            });
            return return_str;
        }

        public override string getResourceManagerDisplayName() {
            return engineTypeStr + " Thruster" + (current_propellant != null ? "(" + current_propellant.PropellantGUIName + ")" : "");
        }

        protected void togglePropellants() {
            fuel_mode++;
            if (fuel_mode >= propellants.Count) {
                fuel_mode = 0;
            }
            current_propellant = fuel_mode < propellants.Count ? propellants[fuel_mode] : propellants.FirstOrDefault();
            setupPropellants();
        }

        protected double evaluateMaxThrust() {
            if (current_propellant != null) {
                double total_power_output = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                double final_thrust_store = current_propellant.Efficiency * 2000.0 * total_power_output / (baseISP * current_propellant.IspMultiplier * 9.81f);
                return final_thrust_store;
            } else {
                return 0;
            }
        }

        protected void updateISP() {
            FloatCurve newISP = new FloatCurve();
            newISP.Add(0, (float)(baseISP * current_propellant.IspMultiplier));
            attachedEngine.atmosphereCurve = newISP;
        }

        protected void updatePropellantBar() {
            if (current_propellant != null) {
                List<PartResource> partresources = part.GetConnectedResources(current_propellant.Propellant.name).ToList();
                float currentpropellant = (float)partresources.Sum(pr => pr.amount);
                float maxpropellant = (float)partresources.Sum(pr => pr.maxAmount);
                if (fuel_gauge != null && fuel_gauge.infoBoxRef != null) {
                    if (attachedEngine.isOperational) {
                        if (!fuel_gauge.infoBoxRef.expanded) fuel_gauge.infoBoxRef.Expand();
                        fuel_gauge.length = 2;
                        fuel_gauge.SetMessage(current_propellant.PropellantGUIName);
                        fuel_gauge.SetValue(maxpropellant > 0 ? currentpropellant / maxpropellant : 0);
                    } else {
                        if (!fuel_gauge.infoBoxRef.collapsed) fuel_gauge.infoBoxRef.Collapse();
                    }
                }
            } else {
                if (fuel_gauge != null && fuel_gauge.infoBoxRef != null) {
                    if (!fuel_gauge.infoBoxRef.collapsed) fuel_gauge.infoBoxRef.Collapse();
                }
            }
        }

        protected List<ElectricEnginePropellant> getPropellants() { // propellants relevent to me
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0) {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            } else {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).Where(eep => (eep.SupportedEngines & type) == type).ToList();
            }
            return propellant_list;
        }

        protected static List<ElectricEnginePropellant> getAllPropellants() { // propellants available to any electric engine
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0) {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            } else {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).ToList();
            }
            return propellant_list;
        }
    }
}
