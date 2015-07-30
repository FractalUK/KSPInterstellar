extern alias ORSvKSPIE;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Extensions;

using UnityEngine;
using ORSvKSPIE::OpenResourceSystem;

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
        public string gearsTechReq;

        [KSPField(isPersistant = false)]
        public int type;
        [KSPField(isPersistant = false)]
        public int upgradedtype;
        [KSPField(isPersistant = false)]
        public float baseISP;
        [KSPField(isPersistant = false)]
        public float ispGears = 3;
        [KSPField(isPersistant = false)]
        public float exitArea = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor=true)]
        public float maxPower;
        [KSPField(isPersistant = false, guiName = "Power Thrust Multiplier")]
        public float powerThrustMultiplier = 1.0f;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float baseEfficency = 0.3f;
        [KSPField(isPersistant = false)]
        public float variableEfficency = 0.3f;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineTypeStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Propellant")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Share")]
        public string electricalPowerShareStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Recieved")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string efficiencyStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = "";

        // GUI to turn on persistent acceleration
        //[KSPField(isPersistant = true)]
        //public bool IsEnabled = false;

        //// new GUI display values
        //[KSPField(guiActive = true, guiName = "Warp Thrust")]
        //protected string Thrust = "";
        //[KSPField(guiActive = true, guiName = "Warp Isp")]
        //protected string Isp = "";
        //[KSPField(guiActive = true, guiName = "Warp Throttle")]
        //protected string Throttle = "";

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        // internal
        protected double _g0 = PluginHelper.GravityConstant;
        protected double _modifiedEngineBaseISP;
        protected List<ElectricEnginePropellant> _propellants;
        protected VInfoBox fuel_gauge;
        protected ModuleEnginesFX _attached_engine;
        protected float _electrical_share_f = 0;
        protected float _electrical_consumption_f = 0;
        protected double _previousAvailablePower = 0;
        protected float _heat_production_f = 0;
        protected int _rep = 0;
        protected bool _hasrequiredupgrade;
        protected bool _hasGearTechnology;
        protected double _modifiedCurrentPropellantIspMultiplier;
        protected double _propellantIspMultiplierPowerLimitModifier;
        protected double _maxISP;
        protected double _max_fuel_flow_rate;

        // Persistent values to use during timewarp
        float IspPersistent = 0;
        float ThrustPersistent = 0;
        float ThrottlePersistent = 0;

        // Numeric display values
        protected double thrust_d = 0;
        protected double isp_d = 0;
        protected double throttle_d = 0;

        private ElectricEnginePropellant _current_propellant;
        public ElectricEnginePropellant Current_propellant
        {
            get { return _current_propellant; }
            set 
            { 
                _current_propellant = value;
                _modifiedCurrentPropellantIspMultiplier = (PluginHelper.IspElectroPropellantModifierBase + (float)Current_propellant.IspMultiplier) / (1 + PluginHelper.IspNtrPropellantModifierBase);
                _propellantIspMultiplierPowerLimitModifier = _modifiedCurrentPropellantIspMultiplier + ((1 - _modifiedCurrentPropellantIspMultiplier) * PluginHelper.ElectricEnginePowerPropellantIspMultLimiter);
            }
        }

        /*

        // GUI to activate persistent thrust
        [KSPEvent(guiActive = true, guiName = "Activate Persistent Thrust", active = true)]
        public void ActivatePersistentThrust()
        {
            IsEnabled = true;
        }

        // GUI to activate persistent thrust
        [KSPEvent(guiActive = true, guiName = "Deactivate Persistent Thrust", active = false)]
        public void DeactivatePersistentThrust()
        {
            IsEnabled = false;
        }
         */ 

        public bool IsOperational 
        { 
            get { return _attached_engine != null ? _attached_engine.isOperational : false; } 
        }

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
            if (ResearchAndDevelopment.Instance == null) return; 
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        public override void OnLoad(ConfigNode node)
        {
            engineTypeStr = originalName;
            if (isupgraded)
                upgradePartModule();
        }

        public override void OnStart(PartModule.StartState state)
        {
            _g0 = PluginHelper.GravityConstant;
            _hasGearTechnology = String.IsNullOrEmpty(gearsTechReq) || PluginHelper.upgradeAvailable(gearsTechReq);
            _modifiedEngineBaseISP = baseISP * PluginHelper.ElectricEngineIspMult;

            //_attached_engine = this.part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;
            _attached_engine = this.part.FindModuleImplementing<ModuleEnginesFX>();

            var wasteheatPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
            // calculate WasteHeat Capacity
            if (wasteheatPowerResource != null)
            {
                var ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resources_to_supply;
            _propellants = getPropellantsEngineType();
            base.OnStart(state);

            if (state == StartState.Editor)
            {
                if (this.HasTechsRequiredToUpgrade())
                    upgradePartModule();

                return;
            }

            if (this.HasTechsRequiredToUpgrade())
                _hasrequiredupgrade = true;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";

            fuel_gauge = part.stackIcon.DisplayInfo();
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            setupPropellants();
        }

        private void setupPropellants()
        {
            List<Propellant> list_of_propellants = new List<Propellant>();
            Propellant new_propellant = Current_propellant.Propellant;
            if (new_propellant.drawStackGauge)
            {
                new_propellant.drawStackGauge = false;
                fuel_gauge.SetMessage(Current_propellant.PropellantGUIName);
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
            } 
            else if (_rep < _propellants.Count)
            {
                _rep++;
                togglePropellants();
                return;
            }

            if (Current_propellant.SupportedEngines == 8 && vessel.IsInAtmosphere())
            {
                _rep++;
                togglePropellants();
                return;
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


        // Format thrust into mN, N, kN
        public static string FormatThrust(double thrust)
        {
            if (thrust < 0.001)
                return Math.Round(thrust * 1000000.0, 3).ToString() + " mN";
            else if (thrust < 1.0)
                return Math.Round(thrust * 1000.0, 3).ToString() + " N";
            else
                return Math.Round(thrust, 3).ToString() + " kN";
        }

        public override void OnUpdate()
        {
            // Base class update
            base.OnUpdate();

            // Persistent thrust GUI
            //Events["ActivatePersistentThrust"].active = !IsEnabled;
            //Events["DeactivatePersistentThrust"].active = IsEnabled;
            //Fields["Thrust"].guiActive = IsEnabled;
            //Fields["Isp"].guiActive = IsEnabled;
            //Fields["Throttle"].guiActive = IsEnabled;

            // Update display values
            //Thrust = FormatThrust(thrust_d);
            //Isp = Math.Round(isp_d, 2).ToString() + " s";
            //Throttle = Math.Round(throttle_d * 100).ToString() + "%";

            if (ResearchAndDevelopment.Instance != null)
            {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && _hasrequiredupgrade;
                Fields["upgradeCostStr"].guiActive = !isupgraded && _hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";
            } 
            else
            {
                Events["RetrofitEngine"].active = false;
                Fields["upgradeCostStr"].guiActive = false;
            }

            propNameStr = Current_propellant != null ? Current_propellant.PropellantGUIName : "";

            if (this.IsOperational)
            {
                Fields["electricalPowerShareStr"].guiActive = true;
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                Fields["efficiencyStr"].guiActive = true;
                electricalPowerShareStr = (100.0 * _electrical_share_f).ToString("0.00") + "%";
                electricalPowerConsumptionStr = _electrical_consumption_f.ToString("0.000") + " MW";
                heatProductionStr = _heat_production_f.ToString("0.000") + " MW";
                efficiencyStr = Current_propellant != null ? (CurrentPropellantEfficiency * 100.0).ToString("0.00") + "%" : "";
            } 
            else
            {
                Fields["electricalPowerShareStr"].guiActive = false;
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
                Fields["efficiencyStr"].guiActive = false;
            }

            updatePropellantBar();
        }

        private float IspGears
        {
            get { return _hasGearTechnology ? ispGears : 1; }
        }

        private float GetModifiedThrotte()
        {
            return Math.Min(_attached_engine.currentThrottle * IspGears, 1);
        }

        private float ThrottleModifiedIsp()
        {
            return _attached_engine.currentThrottle < (1f / IspGears) ? IspGears : IspGears - ((_attached_engine.currentThrottle - (1f / IspGears)) * IspGears);
        }

        // Low thrust acceleration
        public static Vector3d CalculateLowThrustForce(Part part, float thrust, Vector3d up)
        {
            if (part != null)
            {
                return up * thrust;
            }
            else
            {
                return Vector3d.zero;
            }
        }

        public static double CalculateDeltaV(float Isp, float m0, float thrust, double dT)
        {
            // Mass flow rate
            double mdot = thrust / (Isp * 9.81);
            // Final mass
            double m1 = m0 - mdot * dT;
            // DeltaV
            return Isp * 9.81 * Math.Log(m0 / m1);
        }

        public override void OnFixedUpdate()
        {

            base.OnFixedUpdate();

            /*

            if (FlightGlobals.fetch != null && IsEnabled)
            {

                if (!this.vessel.packed)
                {
                    // During realtime mode, update values to use during timewarp
                    IspPersistent = 15000; //realIsp;
                    ThrottlePersistent = 1; //requestedThrottle;
                    ThrustPersistent = 2; //this.CalculateThrust();
                }
                else
                {
                    // If in timewarp, perturb orbit using thrust
                    double UT = Planetarium.GetUniversalTime(); // Universal time
                    double dT = TimeWarp.fixedDeltaTime; // Time step size
                    double m0 = this.vessel.GetTotalMass(); // Current mass
                    double mdot = ThrustPersistent / (IspPersistent * 9.81); // Mass burn rate of engine
                    double dm = mdot * dT; // Change in mass over dT
                    // TODO test if dm exceeds remaining propellant mass
                    // TODO reduce propellant mass by dm
                    double m1 = m0 - dm; // Mass at end of burn
                    double deltaV = IspPersistent * 9.81 * Math.Log(m0 / m1); // Delta V from burn
                    Vector3d down = -this.part.transform.up; // Thrust direction
                    Vector3d deltaVV = deltaV * down; // DeltaV vector
                    vessel.orbit.Perturb(deltaVV, UT, dT); // Update vessel orbit
                }

                // Update display numbers
                thrust_d = ThrustPersistent;
                isp_d = IspPersistent;
                throttle_d = ThrottlePersistent;
            }
              
             */
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;
            
            ElectricEngineControllerFX.getAllPropellants().ForEach(prop => part.Effect(prop.ParticleFXName, 0)); // set all FX to zero

            if (Current_propellant == null || _attached_engine == null) return;

            // retrieve power
            _electrical_share_f = maxPower / Math.Max(vessel.FindPartModulesImplementing<ElectricEngineControllerFX>().Where(ee => ee.IsOperational).Sum(ee => ee.maxPower), maxPower);
            double powerAvailableForEngine = Math.Max(getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) - getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES), 0) * _electrical_share_f;
            double power_per_engine = Math.Min(GetModifiedThrotte() * EvaluateMaxThrust(powerAvailableForEngine) * _current_propellant.IspMultiplier * _modifiedEngineBaseISP / GetPowerThrustModifier() * _g0, maxPower * CurrentPropellantEfficiency);

            double power_received = consumeFNResource(power_per_engine * TimeWarp.fixedDeltaTime / CurrentPropellantEfficiency, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                    
            // produce waste heat
            double heat_to_produce = power_received * (1.0 - CurrentPropellantEfficiency) * Current_propellant.WasteHeatMultiplier;
            double heat_production = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                    
            // update GUI Values
            _electrical_consumption_f = (float)power_received;
            _heat_production_f = (float)heat_production;
                     
            // produce thrust
            double thrust_ratio = power_per_engine > 0 ? Math.Min(power_received / power_per_engine, 1.0) : 1;

            double max_thrust_in_space = CurrentPropellantEfficiency * CurrentPropellantThrustMultiplier * GetPowerThrustModifier() * power_received / (_modifiedCurrentPropellantIspMultiplier * _modifiedEngineBaseISP * ThrottleModifiedIsp() * _g0 * GetModifiedThrotte());

            _maxISP = (float)(_modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier) * ThrottleModifiedIsp();
            _max_fuel_flow_rate = _maxISP <= 0 ? 0 :  max_thrust_in_space / _maxISP / PluginHelper.GravityConstant;

            if (GetModifiedThrotte() > 0)
            {
                double max_thrust_with_current_throttle = max_thrust_in_space * GetModifiedThrotte();
                double actual_max_thrust = Math.Max(max_thrust_with_current_throttle - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0.0);

                if (actual_max_thrust > 0 && !double.IsNaN(actual_max_thrust) && max_thrust_with_current_throttle > 0 && !double.IsNaN(max_thrust_with_current_throttle))
                {
                    updateISP(actual_max_thrust / max_thrust_with_current_throttle);
                    _attached_engine.maxFuelFlow = (float)_max_fuel_flow_rate * (GetModifiedThrotte() / _attached_engine.currentThrottle);
                }
                else
                {
                    updateISP(0.000001);
                    _attached_engine.maxFuelFlow = 0;
                }

                part.Effect(Current_propellant.ParticleFXName, Mathf.Min( (float)Math.Pow( _electrical_consumption_f / maxPower, 0.5), _attached_engine.finalThrust / _attached_engine.maxThrust));
            }
            else
            {
                
                double actual_max_thrust = Math.Max(max_thrust_in_space - (exitArea * FlightGlobals.getStaticPressure(vessel.transform.position)), 0.0);

                if (!double.IsNaN(actual_max_thrust) && !double.IsInfinity(actual_max_thrust) && actual_max_thrust > 0 && max_thrust_in_space > 0)
                {
                    updateISP(actual_max_thrust / max_thrust_in_space);
                    _attached_engine.maxFuelFlow = (float)_max_fuel_flow_rate;
                }
                else
                {
                    updateISP(1);
                    _attached_engine.maxFuelFlow = 0;
                }
                
                //_attached_engine.maxThrust = _avrageragedLastActualMaxThrustWithTrottle > 1 ? _avrageragedLastActualMaxThrustWithTrottle : 1;
                part.Effect(Current_propellant.ParticleFXName, 0);
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

        public void upgradePartModule()
        {
            isupgraded = true;
            type = upgradedtype;
            _propellants = getPropellantsEngineType();
            engineTypeStr = upgradedName;

            if (!vacplasmaadded && type == (int)ElectricEngineType.VACUUMTHRUSTER)
            {
                vacplasmaadded = true;
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", InterstellarResourcesConfiguration.Instance.VacuumPlasma);
                node.AddValue("maxAmount", maxPower / 100);
                node.AddValue("amount", maxPower / 100);
                part.AddResource(node);
            }
        }

        protected double CurrentPropellantThrustMultiplier 
        {
            get { return type == (int)ElectricEngineType.ARCJET || type == (int)ElectricEngineType.VASIMR  ? Current_propellant.ThrustMultiplier : 1; } 
        }
        protected double CurrentPropellantEfficiency 
        { 
            get 
            {
                if (type == (int)ElectricEngineType.ARCJET)
                    return 0.87;
                else if (type == (int)ElectricEngineType.VASIMR)
                    return baseEfficency + ((1 - _attached_engine.currentThrottle) * variableEfficency);
                else 
                    return Current_propellant.Efficiency; 
            } 
        }
       

        public override string GetInfo()
        {
            double powerThrustModifier = GetPowerThrustModifier();
            List<ElectricEnginePropellant> props = getPropellantsEngineType();
            string return_str = "Max Power Consumption: " + maxPower.ToString("") + " MW\n";
            double thrust_per_mw = (2e6 * powerThrustMultiplier) / _g0 / (baseISP * PluginHelper.ElectricEngineIspMult) / 1000.0;
            props.ForEach(prop =>
            {
                double ispPropellantModifier = (PluginHelper.IspElectroPropellantModifierBase + (float)prop.IspMultiplier) / (1 + PluginHelper.IspNtrPropellantModifierBase);
                double ispProp = _modifiedEngineBaseISP * ispPropellantModifier;

                double efficiency;
                
                if (type == (int)ElectricEngineType.ARCJET)
                    efficiency = 0.87;
                else if (type == (int)ElectricEngineType.VASIMR)
                    efficiency = 0.55;
                else 
                    efficiency = prop.Efficiency;

                double thrustProp = thrust_per_mw / ispPropellantModifier * efficiency * (type == (int)ElectricEngineType.ARCJET || type == (int)ElectricEngineType.VASIMR ? prop.ThrustMultiplier : 1);
                return_str = return_str + "---" + prop.PropellantGUIName + "---\nThrust: " + thrustProp.ToString("0.000") + " kN per MW\nEfficiency: " + (efficiency * 100.0).ToString("0.00") + "%\nISP: " + ispProp.ToString("0.00") + "s\n";
            });
            return return_str;
        }

        public override string getResourceManagerDisplayName()
        {
            return engineTypeStr + " Thruster" + (Current_propellant != null ? "(" + Current_propellant.PropellantGUIName + ")" : "");
        }

        protected void togglePropellants()
        {
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            setupPropellants();
        }

        protected double EvaluateMaxThrust(double power_supply)
        {
            if (Current_propellant == null) return 0;

            return CurrentPropellantEfficiency * GetPowerThrustModifier() * power_supply / (_modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * _g0);
        }

        protected void updateISP(double isp_efficiency)
        {
            FloatCurve newISP = new FloatCurve();
            newISP.Add(0, (float)(isp_efficiency * _modifiedEngineBaseISP * _modifiedCurrentPropellantIspMultiplier * CurrentPropellantThrustMultiplier * ThrottleModifiedIsp()));
            _attached_engine.atmosphereCurve = newISP;
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalElectricEnginePowerMaxThrustMult * powerThrustMultiplier;
        }

        private double GetAtmosphericDensityModifier()
        {
            return Math.Max(1.0 - (part.vessel.atmDensity * PluginHelper.ElectricEngineAtmosphericDensityThrustLimiter), 0.0);
        }

        protected void updatePropellantBar()
        {
            if (Current_propellant != null)
            {
                List<PartResource> partresources = part.GetConnectedResources(Current_propellant.Propellant.name).ToList();
                float currentpropellant = (float)partresources.Sum(pr => pr.amount);
                float maxpropellant = (float)partresources.Sum(pr => pr.maxAmount);
                if (fuel_gauge != null && fuel_gauge.infoBoxRef != null)
                {
                    if (_attached_engine.isOperational)
                    {
                        if (!fuel_gauge.infoBoxRef.expanded) 
                            fuel_gauge.infoBoxRef.Expand();

                        fuel_gauge.length = 2;
                        fuel_gauge.SetMessage(Current_propellant.PropellantGUIName);
                        fuel_gauge.SetValue(maxpropellant > 0 ? currentpropellant / maxpropellant : 0);
                    }
                    else if (!fuel_gauge.infoBoxRef.collapsed)
                    {
                        fuel_gauge.infoBoxRef.Collapse();
                    }
                }
            } 
            else if (fuel_gauge != null && fuel_gauge.infoBoxRef != null && (!fuel_gauge.infoBoxRef.collapsed))
            {
                fuel_gauge.infoBoxRef.Collapse();
            }
        }

        protected List<ElectricEnginePropellant> getPropellantsEngineType()
        { // propellants relevent to me
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            } 
            else
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).Where(eep => (eep.SupportedEngines & type) == type).ToList();

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
            } 
            else
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop)).ToList();

            return propellant_list;
        }
    }
}
