extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin
{
    class ElectricEngineControllerFX : FNResourceSuppliableModule, IUpgradeableModule
    {
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

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        // internal
        protected List<ElectricEnginePropellant> _propellants;
        protected ElectricEnginePropellant _current_propellant;
        protected VInfoBox fuel_gauge;
        protected ModuleEnginesFX _attached_engine;
        protected float _electrical_consumption_f = 0;
        protected float _heat_production_f = 0;
        protected int _rep = 0;
        protected bool _hasrequiredupgrade;

        public bool IsOperational { get { return _attached_engine != null ? _attached_engine.isOperational : false; } }

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
        public void TogglePropellant()
        {
            togglePropellants();
        }

        [KSPAction("Toggle Propellant")]
        public void TogglePropellantAction(KSPActionParam param)
        {
            TogglePropellant();
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public override void OnLoad(ConfigNode node)
        {
            engineTypeStr = originalName;
            if (isupgraded)
            {
                upgradePartModule();
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            _attached_engine = this.part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;
            this.resources_to_supply = resources_to_supply;
            _propellants = getPropellants();
            base.OnStart(state);

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                {
                    upgradePartModule();
                }
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
            {
                _hasrequiredupgrade = true;
            }

            if (_attached_engine != null)
            {
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";
            }

            fuel_gauge = part.stackIcon.DisplayInfo();
            _current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            setupPropellants();
        }

        private void setupPropellants()
        {
            List<Propellant> list_of_propellants = new List<Propellant>();
            Propellant new_propellant = _current_propellant.Propellant;
            if (new_propellant.drawStackGauge)
            {
                new_propellant.drawStackGauge = false;
                fuel_gauge.SetMessage(_current_propellant.PropellantGUIName);
                fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                fuel_gauge.SetValue(0f);
            }
            list_of_propellants.Add(new_propellant);

            if (!list_of_propellants.Exists(prop => PartResourceLibrary.Instance.GetDefinition(prop.name) == null))
            {
                _attached_engine.propellants.Clear();
                _attached_engine.propellants = list_of_propellants;
                _attached_engine.SetupPropellant();
            } else
            {
                if (_rep < _propellants.Count)
                {
                    _rep++;
                    togglePropellants();
                    return;
                }

            }

            if (HighLogic.LoadedSceneIsFlight)
            { // you can have any fuel you want in the editor but not in flight
                List<PartResource> totalpartresources = list_of_propellants.SelectMany(prop => part.GetConnectedResources(prop.name)).ToList();
                if (!list_of_propellants.All(prop => totalpartresources.Select(pr => pr.resourceName).Contains(prop.name)) && _rep < _propellants.Count)
                {
                    _rep++;
                    togglePropellants();
                    return;
                }
            }
            _rep = 0;
        }

        public override void OnUpdate()
        {
            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                Fields["upgradeCostStr"].guiActive = !isupgraded && _hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
            } else
            {
                Events["RetrofitEngine"].active = false;
                Fields["upgradeCostStr"].guiActive = false;
            }

            propNameStr = _current_propellant != null ? _current_propellant.PropellantGUIName : "";

            if (this.IsOperational)
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                Fields["efficiencyStr"].guiActive = true;
                electricalPowerConsumptionStr = _electrical_consumption_f.ToString("0.00") + " MW";
                heatProductionStr = _heat_production_f.ToString("0.00") + " MW";
                efficiencyStr = _current_propellant != null ? (_current_propellant.Efficiency * 100.0).ToString("0.0") + "%" : "";
            } else
            {
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
                Fields["efficiencyStr"].guiActive = false;
            }

            updatePropellantBar();
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                ElectricEngineControllerFX.getAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName, 0)); // set all FX to zero

                if (_current_propellant != null && _attached_engine != null)
                {
                    updateISP();
                    int engine_count = Math.Max(vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Count(ee => ee.IsOperational),1); // max of operational electric engines and 1
                    double total_max_thrust = evaluateMaxThrust();
                    double thrust_per_engine = total_max_thrust / (double)engine_count;
                    double power_per_engine = Math.Min(0.5 * _attached_engine.currentThrottle * thrust_per_engine * _current_propellant.IspMultiplier * baseISP / 1000.0 * 9.81, maxPower * _current_propellant.Efficiency);
                    double power_received = consumeFNResource(power_per_engine * TimeWarp.fixedDeltaTime / _current_propellant.Efficiency, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                    double heat_to_produce = power_received * (1.0 - _current_propellant.Efficiency);
                    double heat_production = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                    // update GUI Values
                    _electrical_consumption_f = (float)power_received;
                    _heat_production_f = (float)heat_production;
                    // thrust values
                    double thrust_ratio = power_per_engine > 0 ? Math.Min(power_received / power_per_engine, 1.0) : 1;
                    double actual_max_thrust = _current_propellant.Efficiency * 2000.0f * power_received / (_current_propellant.IspMultiplier * baseISP * 9.81f * _attached_engine.currentThrottle);

                    if (_attached_engine.currentThrottle > 0)
                    {
                        if (!double.IsNaN(actual_max_thrust) && !double.IsInfinity(actual_max_thrust))
                        {
                            _attached_engine.maxThrust = Mathf.Max((float)actual_max_thrust, 0.00001f);
                        } else
                        {
                            _attached_engine.maxThrust = 0.00001f;
                        }
                        float fx_ratio = Mathf.Min(_electrical_consumption_f / maxPower, _attached_engine.finalThrust / _attached_engine.maxThrust);
                        part.Effect(_current_propellant.ParticleFXName, fx_ratio);
                    }

                    if (isupgraded)
                    {
                        List<PartResource> vacuum_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.VacuumPlasma).ToList();
                        double vacuum_plasma_needed = vacuum_resources.Sum(vc => vc.maxAmount - vc.amount);
                        double vacuum_plasma_current = vacuum_resources.Sum(vc => vc.amount);

                        if (vessel.IsInAtmosphere())
                            part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, vacuum_plasma_current);
                        else
                            part.RequestResource(InterstellarResourcesConfiguration.Instance.VacuumPlasma, -vacuum_plasma_needed);
                    }
                }
            }
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            type = upgradedtype;
            _propellants = getPropellants();
            engineTypeStr = upgradedName;

            if (!vacplasmaadded && type == (int)ElectricEngineType.VACUUMTHRUSTER)
            {
                vacplasmaadded = true;
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.VacuumPlasma);
                node.AddValue("maxAmount", 10);
                node.AddValue("amount", 10);
                part.AddResource(node);
            }
        }

        public override string GetInfo()
        {
            List<ElectricEnginePropellant> props = getPropellants();
            string return_str = "Max Power Consumption: " + maxPower.ToString("") + " MW\n";
            double thrust_per_mw = 2e6 / 9.81 / baseISP / 1000.0;
            props.ForEach(prop =>
            {
                double ispProp = baseISP * prop.IspMultiplier;
                double thrustProp = thrust_per_mw / prop.IspMultiplier * prop.Efficiency;
                return_str = return_str + "---" + prop.PropellantGUIName + "---\nThrust: " + thrustProp.ToString("0.0000") + " kN per MW\nEfficiency: " + (prop.Efficiency * 100.0).ToString("0.00") + "%\nISP: " + ispProp.ToString("0.00") + "s\n";
            });
            return return_str;
        }

        public override string getResourceManagerDisplayName()
        {
            return engineTypeStr + " Thruster" + (_current_propellant != null ? "(" + _current_propellant.PropellantGUIName + ")" : "");
        }

        protected void togglePropellants()
        {
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
            {
                fuel_mode = 0;
            }
            _current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            setupPropellants();
        }

        protected double evaluateMaxThrust()
        {
            if (_current_propellant != null)
            {
                double total_power_output = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                double final_thrust_store = _current_propellant.Efficiency * 2000.0 * total_power_output / (baseISP * _current_propellant.IspMultiplier * 9.81f);
                return final_thrust_store;
            } 
            return 0;
        }

        protected void updateISP()
        {
            FloatCurve newISP = new FloatCurve();
            newISP.Add(0, (float)(baseISP * _current_propellant.IspMultiplier));
            _attached_engine.atmosphereCurve = newISP;
        }

        protected void updatePropellantBar()
        {
            if (_current_propellant != null)
            {
                List<PartResource> partresources = part.GetConnectedResources(_current_propellant.Propellant.name).ToList();
                float currentpropellant = (float)partresources.Sum(pr => pr.amount);
                float maxpropellant = (float)partresources.Sum(pr => pr.maxAmount);
                if (fuel_gauge != null && fuel_gauge.infoBoxRef != null)
                {
                    if (_attached_engine.isOperational)
                    {
                        if (!fuel_gauge.infoBoxRef.expanded) fuel_gauge.infoBoxRef.Expand();
                        fuel_gauge.length = 2;
                        fuel_gauge.SetMessage(_current_propellant.PropellantGUIName);
                        fuel_gauge.SetValue(maxpropellant > 0 ? currentpropellant / maxpropellant : 0);
                    } else
                    {
                        if (!fuel_gauge.infoBoxRef.collapsed) fuel_gauge.infoBoxRef.Collapse();
                    }
                }
            } else
            {
                if (fuel_gauge != null && fuel_gauge.infoBoxRef != null)
                {
                    if (!fuel_gauge.infoBoxRef.collapsed) fuel_gauge.infoBoxRef.Collapse();
                }
            }
        }

        protected List<ElectricEnginePropellant> getPropellants()
        { // propellants relevent to me
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            } else
            {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).Where(eep => (eep.SupportedEngines & type) == type).ToList();
            }
            return propellant_list;
        }

        protected static List<ElectricEnginePropellant> getAllPropellants()
        { // propellants available to any electric engine
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            } else
            {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).ToList();
            }
            return propellant_list;
        }
    }
}
