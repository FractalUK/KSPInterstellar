using FNPlugin.Propulsion;
using FNPlugin.Extensions;
using OpenResourceSystem;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using TweakScale;

namespace FNPlugin
{
    class ThermalNozzleController : FNResourceSuppliableModule, INoozle, IUpgradeableModule, IRescalable<ThermalNozzleController>
    {
		// Persistent True
		[KSPField(isPersistant = true)]
		public bool IsEnabled;
		[KSPField(isPersistant = true)]
		public bool isHybrid = false;
        [KSPField(isPersistant = true)]
		public bool isupgraded = false;
		[KSPField(isPersistant = true)]
		public bool engineInit = false;
		[KSPField(isPersistant = true)]
		public int fuel_mode = 0;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Soot Accumulation", guiUnits = " %")]
        public float sootAccumulationPercentage;

        /// <summary>
        /// hidden setting used by ballance mods
        /// </summary>
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        /// <summary>
        /// Determing Jet Engine Performance
        /// </summary>
        [KSPField(isPersistant = false)]
        public int jetPerformanceProfile = 0;
        [KSPField(isPersistant = false)]
        public int buildInPrecoolers = 0;
        [KSPField(isPersistant = false)]
        public bool canUseLFO = false;
		[KSPField(isPersistant = false)]
		public bool isJet = false;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplierJet = 1;
        [KSPField(isPersistant = false)]
        public float IspTempMultOffset = -1.371670613f;
        [KSPField(isPersistant = false)]
        public float sootHeatDivider = 150;
        [KSPField(isPersistant = false)]
        public float sootThrustDivider = 150;

        [KSPField(isPersistant = false)]
        public float delayedThrottleFactor = 0.5f;
        [KSPField(isPersistant = false)]
        public float maxTemp = 2750;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public string EffectNameJet = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameLFO = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameNonLFO = String.Empty;
        [KSPField(isPersistant = false)]
        public string EffectNameLithium = String.Empty;
        [KSPField(isPersistant = false)]
        public bool showPartTemperature = true;


        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius", guiUnits = "m")]
        public float radius;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Exit Area", guiUnits = " m2")]
        public float exitArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Empty Mass", guiUnits = " t")]
        public float partMass = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Afterburner upgrade tech")]
        public string afterburnerTechReq = String.Empty;

		//External
		public bool static_updating = true;
		public bool static_updating2 = true;

		//GUI
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
		public string engineType = ":";
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Propellant")]
		public string _fuelmode;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Propellant Isp Multiplier")]
        public float _ispPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Soot")]
        public float _propellantSootFactorFullThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Min Soot")]
        public float _propellantSootFactorMinThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Equilibrium Soot")]
        public float _propellantSootFactorEquilibrium;

        [KSPField(isPersistant = false, guiActive = false, guiName = "Temperature")]
        public string temperatureStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "ISP / Thrust Mult")]
        public string thrustIspMultiplier = "";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Fuel Thrust Multiplier")]
        public float _thrustPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Upgrade Cost")]
		public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Base Heat Production")]
        public float baseHeatProduction = 80;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Heat Production")]
        public float engineHeatProduction;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Treshold", guiUnits = " kN")]
        public float pressureTreshold;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmospheric Limit")]
        public float atmospheric_limit;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Heat", guiUnits = " MJ")]
        public float requested_thermal_power;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Charge", guiUnits = " MJ")]
        public float requested_charge_particles;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Recieved Power", guiUnits = " MJ")]
        public float thermal_power_received;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius Modifier")]
        public string radiusModifier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Vacuum")]
        public string vacuumPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Sea")]
        public string surfacePerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Base Isp")]
        protected float _baseIspMultiplier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Decomposition Energy")]
        protected float _decompositionEnergy;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Exchange Divider")]
        protected float heatExchangerThrustDivisor;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Engine Max Thrust")]
        protected float engineMaxThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust In Space")]
        protected float max_thrust_in_space;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thrust In Current")]
        protected float max_thrust_in_current_atmosphere;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Final Engine Thrust")]
        protected float final_max_engine_thrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MaxISP")]
        protected float _maxISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MinISP")]
        protected float _minISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Fuel Flow", guiFormat = "0.000000")]
        protected float max_fuel_flow_rate = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Isp")]
        protected float current_isp = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "MaxPressureThresshold")]
        protected float maxPressureThresholdAtKerbinSurface;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Ratio")]
        protected float thermalRatio;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Consumed")]
        protected float consumedWasteHeat;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Expected Max Thrust")]
        protected float expectedMaxThrust;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Is LFO")]
        protected bool _propellantIsLFO = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Velocity Modifier")]
        protected float vcurveAtCurrentVelocity;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Propellant Type")]
        protected int _propellantType = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Is Neutron Absorber")]
        protected bool _isNeutronAbsorber = false;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Maximum Power", guiUnits = " MJ")]
        protected float _currentMaximumPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Modifier")]
        protected float thermal_modifiers;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Available T Power ", guiUnits = " MJ")]
        protected float _availableThermalPower;

		//Internal
        protected string _particleFXName;
        //protected string _currentAudioFX;
        protected bool _fuelRequiresUpgrade;
        protected string _fuelTechRequirement;
        protected float _fuelToxicity;
        protected float _savedReputationCost;
        protected float _heatDecompositionFraction;

        protected float _minDecompositionTemp;
        protected float _maxDecompositionTemp;
        protected const float _hydroloxDecompositionEnergy = 16.2137f;
        protected Guid id = Guid.NewGuid();
		protected ConfigNode[] propellants;
		protected VInfoBox fuel_gauge;
		protected bool hasrequiredupgrade = false;
		protected bool hasstarted = false;
        protected bool hasSetupPropellant = false;
		protected ModuleEngines myAttachedEngine;
		protected bool _currentpropellant_is_jet = false;

        protected ModuleResourceIntake cooledIntake;
		protected int thrustLimitRatio = 0;
		protected double old_intake = 0;
        protected int partDistance = 0;
        protected float old_atmospheric_limit;
        protected double currentintakeatm;

        protected List<FNModulePreecooler> _vesselPrecoolers;
        protected List<ModuleResourceIntake> _vesselResourceIntake;
        protected List<INoozle> _vesselThermalNozzles;

        protected float jetTechBonus;
        protected float jetTechBonusPercentage;

        public bool Static_updating { get { return static_updating; } set { static_updating = value; } }
        public bool Static_updating2 { get { return static_updating2; } set { static_updating2 = value; } }
        public int Fuel_mode { get { return fuel_mode; } }

        private IThermalSource _myAttachedReactor;
        public IThermalSource AttachedReactor 
        {
            get { return _myAttachedReactor; }
            private set 
            {
                _myAttachedReactor = value;
                if (_myAttachedReactor == null) 
                    return;
                _myAttachedReactor.AttachThermalReciever(id, radius);
            }
        }

		//Static
		static Dictionary<string, double> intake_amounts = new Dictionary<string, double>();
        static Dictionary<string, double> intake_maxamounts = new Dictionary<string, double>();
		static Dictionary<string, double> fuel_flow_amounts = new Dictionary<string, double>();

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        private int switches = 0;

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next Propellant", active = true)]
		public void NextPropellant() 
        {
			fuel_mode++;
			if (fuel_mode >= propellants.Length) 
				fuel_mode = 0;

            SetupPropellants(true);
		}

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Propellant", active = true)]
        public void PreviousPropellant()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = propellants.Length - 1;

            SetupPropellants(false);
        }

        public void OnRescale(TweakScale.ScalingFactor factor)
        {
            // update variables
            radius *= factor.relative.linear;
            exitArea *= factor.relative.quadratic;

            // update simulation
            UpdateRadiusModifier();
        }
        
		[KSPAction("Next Propellant")]
		public void TogglePropellantAction(KSPActionParam param) 
        {
			NextPropellant();
		}

        [KSPAction("Previous Propellant")]
        public void PreviousPropellant(KSPActionParam param)
        {
            PreviousPropellant();
        }

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitEngine() 
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

			upgradePartModule ();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
		}

        public void upgradePartModule()
        {
            engineType = upgradedName;
            isupgraded = true;

            if (isJet)
            {
                propellants = getPropellantsHybrid();
                isHybrid = true;
            }
            else
                propellants = getPropellants(isJet);
        }

		public ConfigNode[] getPropellants() 
        {
			return propellants;
		}

        public void OnEditorAttach() 
        {
            FindAttachedThermalSource();

            if (AttachedReactor == null) return;

            EstimateEditorPerformance();
        }

        public void OnEditorDetach()
        {
            if (AttachedReactor == null) return;

            _myAttachedReactor.DetachThermalReciever(id);
        }

        public override void OnStart(PartModule.StartState state)
        {
            // make sure max temp is correct
            part.maxTemp = maxTemp;

            PartResource wasteheatPowerResource = part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT];

            // calculate WasteHeat Capacity
            if (wasteheatPowerResource != null)
            {
                var ratio = wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount;
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            engineType = originalName;

            myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();

            // find attached thermal source
            FindAttachedThermalSource();

            // find intake we need to cool
            foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null))
            {
                var attachedPart = attach_node.attachedPart;

                cooledIntake = attachedPart.FindModuleImplementing<ModuleResourceIntake>();

                if (cooledIntake != null)
                    break;
            }

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                propellants = getPropellants(isJet);
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                SetupPropellants();
                EstimateEditorPerformance();
                return;
            }
            else
                UpdateRadiusModifier();

            // presearch all avaialble precoolers, intakes and nozzles on the vessel
            _vesselPrecoolers = vessel.FindPartModulesImplementing<FNModulePreecooler>();
            _vesselResourceIntake = vessel.FindPartModulesImplementing<ModuleResourceIntake>();
            _vesselThermalNozzles = vessel.FindPartModulesImplementing<INoozle>();

            fuel_gauge = part.stackIcon.DisplayInfo();

            // if engine isn't already initialised, initialise it
            if (engineInit == false)
                engineInit = true;

            // if we can upgrade, let's do so
            if (isupgraded)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade())
                    hasrequiredupgrade = true;

                // if not, use basic propellants
                propellants = getPropellants(isJet);
            }

            bool hasJetUpgradeTech0 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech0);
            bool hasJetUpgradeTech1 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech1);
            bool hasJetUpgradeTech2 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech2);
            bool hasJetUpgradeTech3 = PluginHelper.HasTechRequirementOrEmpty(PluginHelper.JetUpgradeTech3);

            jetTechBonus = Convert.ToInt32(hasJetUpgradeTech0) + 1.2f * Convert.ToInt32(hasJetUpgradeTech1) + 1.44f * Convert.ToInt32(hasJetUpgradeTech2) + 1.728f *Convert.ToInt32(hasJetUpgradeTech3);
            jetTechBonusPercentage = jetTechBonus / 26.84f;

            SetupPropellants();

            maxPressureThresholdAtKerbinSurface = exitArea * (float)GameConstants.EarthAtmospherePressureAtSeaLevel;

            hasstarted = true;

            try
            {

                Fields["temperatureStr"].guiActive = showPartTemperature;
                //Fields["chargedParticlePropulsionIsp"].guiActive = showChargedParticlePropulsionIsp;
            }
            catch
            {
                Debug.LogError("OnStart Exception in Field Visibility Configuration") ;
            }
        }

        private void ConfigEffects()
        {
            if (myAttachedEngine is ModuleEnginesFX)
            {
                if (!String.IsNullOrEmpty(EffectNameJet))
                    part.Effect(EffectNameJet, 0);
                if (!String.IsNullOrEmpty(EffectNameLFO))
                    part.Effect(EffectNameLFO, 0);
                if (!String.IsNullOrEmpty(EffectNameNonLFO))
                    part.Effect(EffectNameNonLFO, 0);
                if (!String.IsNullOrEmpty(EffectNameLithium))
                    part.Effect(EffectNameLithium, 0);

                if (_currentpropellant_is_jet && !String.IsNullOrEmpty(EffectNameJet))
                    _particleFXName = EffectNameJet;
                else if (_propellantIsLFO && !String.IsNullOrEmpty(EffectNameLFO))
                    _particleFXName = EffectNameLFO;
                else if (_isNeutronAbsorber && !String.IsNullOrEmpty(EffectNameLithium))
                    _particleFXName = EffectNameLithium;
                else if (!String.IsNullOrEmpty(EffectNameNonLFO))
                    _particleFXName = EffectNameNonLFO;
            }
        }

        private void FindAttachedThermalSource()
        {
            var source = ThermalSourceSearchResult.BreadthFirstSearchForThermalSource(part, (p) => p.IsThermalSource, 10, 1);
            if (source == null) return;

            AttachedReactor = source.Source;
            partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);
            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource- Found thermal searchResult with distance " + partDistance);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate()
        {
            // setup propellant after startup to allow InterstellarFuelSwitch to configure the propellant
            if (!hasSetupPropellant)
            {
                hasSetupPropellant = true;
                SetupPropellants(true, true);
            }

            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
            //staticPresure = (FlightGlobals.getStaticPressure(vessel.transform.position)).ToString("0.0000") + " kPa";
            pressureTreshold = exitArea * (float)FlightGlobals.getStaticPressure(vessel.transform.position);

            Fields["sootAccumulationPercentage"].guiActive = sootAccumulationPercentage > 0;

            thrustIspMultiplier = _ispPropellantMultiplier + " / " + _thrustPropellantMultiplier;

            Fields["engineType"].guiActive = isJet;
            if (ResearchAndDevelopment.Instance != null && isJet)
            {
                Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
            }
            else
                Events["RetrofitEngine"].active = false;

            Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade && isJet;

            if (myAttachedEngine != null)
            {
                if (myAttachedEngine.isOperational && !IsEnabled)
                {
                    IsEnabled = true;
                    part.force_activate();
                }
                updatePropellantBar();
            }
        }

        public void updatePropellantBar()
        {
            float currentpropellant = 0;
            float maxpropellant = 0;

            List<PartResource> partresources = part.GetConnectedResources(myAttachedEngine.propellants.FirstOrDefault().name).ToList();

            foreach (PartResource partresource in partresources)
            {
                currentpropellant += (float)partresource.amount;
                maxpropellant += (float)partresource.maxAmount;
            }

            if (fuel_gauge != null && fuel_gauge.infoBoxRef != null)
            {
                if (myAttachedEngine.isOperational)
                {
                    if (!fuel_gauge.infoBoxRef.expanded)
                        fuel_gauge.infoBoxRef.Expand();

                    fuel_gauge.length = 2;

                    if (maxpropellant > 0)
                        fuel_gauge.SetValue(currentpropellant / maxpropellant);
                    else
                        fuel_gauge.SetValue(0);
                }
                else if (!fuel_gauge.infoBoxRef.collapsed)
                    fuel_gauge.infoBoxRef.Collapse();
            }
        }

        public override void OnActive()
        {
            base.OnActive();
            SetupPropellants(true, true);
        }


        public void SetupPropellants(bool forward = true, bool notifySwitching = false)
        {
            try
            {

                ConfigNode chosenpropellant = propellants[fuel_mode];
                UpdatePropellantModeBehavior(chosenpropellant);
                ConfigNode[] propellantNodes = chosenpropellant.GetNodes("PROPELLANT");
                List<Propellant> list_of_propellants = new List<Propellant>();
                // loop though propellants until we get to the selected one, then set it up
                foreach (ConfigNode prop_node in propellantNodes)
                {
                    ExtendedPropellant curprop = new ExtendedPropellant();

                    curprop.Load(prop_node);

                    if (curprop.drawStackGauge && HighLogic.LoadedSceneIsFlight)
                    {
                        curprop.drawStackGauge = false;

                        if (_currentpropellant_is_jet)
                            fuel_gauge.SetMessage("Atmosphere");
                        else
                        {
                            fuel_gauge.SetMessage(curprop.StoragePropellantName);
                            myAttachedEngine.thrustPercentage = 100;
                        }

                        //fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                        fuel_gauge.SetMsgBgColor(XKCDColors.White);
                        //fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                        fuel_gauge.SetMsgTextColor(XKCDColors.Black);
                        fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                        fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                        fuel_gauge.SetValue(0f);
                    }

                    if (list_of_propellants == null)
                        UnityEngine.Debug.LogWarning("[KSPI] - ThermalNozzleController - SetupPropellants list_of_propellants ia null");

                    list_of_propellants.Add(curprop);

                    if (curprop.name == "LqdWater")
                    {
                        if (!part.Resources.Contains("LqdWater"))
                        {
                            ConfigNode node = new ConfigNode("RESOURCE");
                            node.AddValue("name", curprop.name);
                            node.AddValue("maxAmount", AttachedReactor.MaximumPower * CurrentPowerThrustMultiplier / Math.Sqrt(AttachedReactor.CoreTemperature));
                            node.AddValue("possibleAmount", 0);
                            this.part.AddResource(node);
                            this.part.Resources.UpdateList();
                        }
                    }
                    else
                    {
                        if (part.Resources.Contains("LqdWater"))
                        {
                            var partresource = part.Resources["LqdWater"];
                            if (partresource.amount > 0 && HighLogic.LoadedSceneIsFlight)
                                ORSHelper.fixedRequestResource(this.part, "Water", -partresource.amount);
                            this.part.Resources.list.Remove(partresource);
                            DestroyImmediate(partresource);
                        }
                    }

                }

                // update the engine with the new propellants
                if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null)
                {
                    myAttachedEngine.propellants.Clear();
                    myAttachedEngine.propellants = list_of_propellants;
                    myAttachedEngine.SetupPropellant();
                }

                if (HighLogic.LoadedSceneIsFlight)
                { // you can have any fuel you want in the editor but not in flight
                    // should we switch to another propellant because we have none of this one?
                    bool next_propellant = false;

                    string missingResources = String.Empty;

                    foreach (Propellant curEngine_propellant in myAttachedEngine.propellants)
                    {
                        var extendedPropellant = curEngine_propellant as ExtendedPropellant;
                        IEnumerable<PartResource> partresources = part.GetConnectedResources(extendedPropellant.StoragePropellantName);

                        if (!partresources.Any() || !PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name))
                        {
                            if (notifySwitching)
                                missingResources += curEngine_propellant.name + " ";
                            next_propellant = true;
                        }
                        else if (
                               (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                            || (_fuelRequiresUpgrade && !isupgraded)
                            || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                            || ((_propellantType & _myAttachedReactor.SupportedPropellantsTypes) != _propellantType)
                            )
                        {
                            next_propellant = true;
                        }
                    }

                    // do the switch if needed
                    if (next_propellant && (switches <= propellants.Length || fuel_mode != 0))
                    {// always shows the first fuel mode when all fuel mods are tested at least once
                        ++switches;
                        if (notifySwitching)
                            ScreenMessages.PostScreenMessage("Switching Propellant, missing resource " + missingResources, 5.0f, ScreenMessageStyle.LOWER_CENTER);

                        if (forward)
                            NextPropellant();
                        else
                            PreviousPropellant();
                    }
                }
                else
                {
                    bool next_propellant = false;

                    // Still ignore propellants that don't exist or we cannot use due to the limmitations of the engine
                    if (
                           (!PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name) && (switches <= propellants.Length || fuel_mode != 0))
                        || (!PluginHelper.HasTechRequirementOrEmpty(_fuelTechRequirement))
                        || (_fuelRequiresUpgrade && !isupgraded)
                        || (_propellantIsLFO && !PluginHelper.HasTechRequirementAndNotEmpty(afterburnerTechReq))
                        || ((_propellantType & _myAttachedReactor.SupportedPropellantsTypes) != _propellantType)
                        )
                    {
                        next_propellant = true;
                    }

                    if (next_propellant)
                    {
                        ++switches;
                        if (forward)
                            NextPropellant();
                        else
                            PreviousPropellant();
                    }

                    EstimateEditorPerformance(); // update editor estimates
                }

                switches = 0;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - Error SetupPropellants " + e.Message);
            }
        }



        private void UpdatePropellantModeBehavior(ConfigNode chosenpropellant)
        {
            _fuelmode = chosenpropellant.GetValue("guiName");
            _propellantSootFactorFullThrotle = chosenpropellant.HasValue("maxSootFactor") ? float.Parse(chosenpropellant.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = chosenpropellant.HasValue("minSootFactor") ? float.Parse(chosenpropellant.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = chosenpropellant.HasValue("levelSootFraction") ? float.Parse(chosenpropellant.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = chosenpropellant.HasValue("MinDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = chosenpropellant.HasValue("MaxDecompositionTemp") ? float.Parse(chosenpropellant.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = chosenpropellant.HasValue("DecompositionEnergy") ? float.Parse(chosenpropellant.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = chosenpropellant.HasValue("BaseIspMultiplier") ? float.Parse(chosenpropellant.GetValue("BaseIspMultiplier")) : 0;
            _fuelTechRequirement = chosenpropellant.HasValue("TechRequirement") ? chosenpropellant.GetValue("TechRequirement") : String.Empty;
            _fuelToxicity = chosenpropellant.HasValue("Toxicity") ? float.Parse(chosenpropellant.GetValue("Toxicity")) : 0;
            _fuelRequiresUpgrade = chosenpropellant.HasValue("RequiresUpgrade") ? Boolean.Parse(chosenpropellant.GetValue("RequiresUpgrade")) : false;

            _currentpropellant_is_jet = chosenpropellant.HasValue("isJet") ? bool.Parse(chosenpropellant.GetValue("isJet")) : false;
            _propellantIsLFO = chosenpropellant.HasValue("isLFO") ? bool.Parse(chosenpropellant.GetValue("isLFO")) : false;
            _propellantType = chosenpropellant.HasValue("type") ? int.Parse(chosenpropellant.GetValue("type")) : 1;
            _isNeutronAbsorber = chosenpropellant.HasValue("isNeutronAbsorber") ? bool.Parse(chosenpropellant.GetValue("isNeutronAbsorber")) : false;

            if (!_currentpropellant_is_jet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;
                _ispPropellantMultiplier = chosenpropellant.HasValue("ispMultiplier") ? float.Parse(chosenpropellant.GetValue("ispMultiplier")) : 1;
                var rawthrustPropellantMultiplier = chosenpropellant.HasValue("thrustMultiplier") ? float.Parse(chosenpropellant.GetValue("thrustMultiplier")) : 1;
                _thrustPropellantMultiplier = _propellantIsLFO ? rawthrustPropellantMultiplier : ((rawthrustPropellantMultiplier + 1) / 2.0f);
            }
        }

        private void UpdateThrustPropellantMultiplier()
        {
            var linearFraction = Math.Max(0, Math.Min(1, (AttachedReactor.CoreTemperature - _minDecompositionTemp) / (_maxDecompositionTemp - _minDecompositionTemp)));
            _heatDecompositionFraction = (float)Math.Pow(0.36, Math.Pow(3 - linearFraction * 3, 2) / 2);
            var thrustPropellantMultiplier = (float)Math.Sqrt(_heatDecompositionFraction * _decompositionEnergy / _hydroloxDecompositionEnergy) * 1.04f + 1;
            _ispPropellantMultiplier = _baseIspMultiplier * thrustPropellantMultiplier;
            _thrustPropellantMultiplier = _propellantIsLFO ? thrustPropellantMultiplier : thrustPropellantMultiplier + 1 / 2;
        }

        public void UpdateIspEngineParams(double atmosphere_isp_efficiency = 1) // , double max_thrust_in_space = 0) 
        {
            // recaculate ISP based on power and core temp available
            FloatCurve atmCurve = new FloatCurve();
            FloatCurve atmosphereCurve = new FloatCurve();
            FloatCurve velCurve = new FloatCurve();

            _maxISP = (float)(Math.Sqrt((double)AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());

            if (!_currentpropellant_is_jet)
            {
                //var effectiveIsp = Mathf.Min(_maxISP * (float)atmosphere_isp_efficiency, PluginHelper.MaxThermalNozzleIsp);

                atmosphereCurve.Add(0, _maxISP * (float)atmosphere_isp_efficiency, 0, 0);

                myAttachedEngine.useAtmCurve = false;
                myAttachedEngine.useVelCurve = false;
                myAttachedEngine.useEngineResponseTime = false;
            }
            else
            {
                if (jetPerformanceProfile == 0)
                {
                    atmosphereCurve.Add(0, Mathf.Min(_maxISP * 5.0f / 4.0f, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(0.15f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(0.3f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(1, Mathf.Min(_maxISP * 4.0f / 5.0f, PluginHelper.MaxThermalNozzleIsp));

                    var curveChange = jetTechBonus / 5.368f;

                    velCurve.Add(0, 0.1f);
                    velCurve.Add(3f - curveChange, 1f);
                    velCurve.Add(4f + curveChange, 1f);
                    velCurve.Add(12f, 0 + jetTechBonusPercentage);

                    // configure atmCurve
                    atmCurve.Add(0, 0, 0, 0);
                    atmCurve.Add(0.045f, 0.166f, 4.304647f, 4.304647f);
                    atmCurve.Add(0.16f, 0.5f, 0.5779132f, 5779132f);
                    atmCurve.Add(0.5f, 0.6f, 0.4809403f, 4809403f);
                    atmCurve.Add(1f, 1f, 1.013946f, 0f);

                    myAttachedEngine.atmCurve = atmCurve;
                    myAttachedEngine.useAtmCurve = true;
                }
                else if (jetPerformanceProfile == 1)
                {
                    atmosphereCurve.Add(0, Mathf.Min(_maxISP * 5.0f / 4.0f, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(0.15f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(0.3f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                    atmosphereCurve.Add(1, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));

                    velCurve.Add(0.00f, 0.50f + jetTechBonusPercentage);
                    velCurve.Add(1.50f, 1.00f);
                    velCurve.Add(2.50f, 0.80f + jetTechBonusPercentage);
                    velCurve.Add(3.50f, 0.60f + jetTechBonusPercentage);
                    velCurve.Add(4.50f, 0.40f + jetTechBonusPercentage);
                    velCurve.Add(5.50f, 0.20f + jetTechBonusPercentage);
                    velCurve.Add(6.50f, 0.00f + jetTechBonusPercentage);
                    velCurve.Add(7.50f, 0.00f);
                }

                myAttachedEngine.ignitionThreshold = 0.01f;
                myAttachedEngine.useVelCurve = true;
                myAttachedEngine.velCurve = velCurve;
                myAttachedEngine.useEngineResponseTime = true;
            }

            
            myAttachedEngine.atmosphereCurve = atmosphereCurve;
        }

    

        public float GetAtmosphericLimit()
        {
            atmospheric_limit = 1.0f;
            if (_currentpropellant_is_jet)
            {
                string resourcename = myAttachedEngine.propellants[0].name;
                currentintakeatm = getIntakeAvailable(vessel, resourcename);
                var fuelRateThermalJets = GetFuelRateThermalJets(resourcename);

                if (fuelRateThermalJets > 0)
                {
                    // divide current available intake resource by fuel useage across all engines
                    var intakeFuelRate = (float)Math.Min(currentintakeatm / fuelRateThermalJets, 1.0);

                    atmospheric_limit = intakeFuelRate; //getEnginesRunningOfTypeForVessel(vessel, resourcename);
                }
                old_intake = currentintakeatm;
            }
            atmospheric_limit = Mathf.MoveTowards(old_atmospheric_limit, atmospheric_limit, 0.2f);
            old_atmospheric_limit = atmospheric_limit;
            return atmospheric_limit;
        }

		public double GetNozzleFlowRate() 
        {
            return myAttachedEngine.isOperational ? max_fuel_flow_rate : 0;
		}

        public void EstimateEditorPerformance()
        {
            FloatCurve atmospherecurve = new FloatCurve();
            float thrust = 0;
            UpdateRadiusModifier();

            if (AttachedReactor != null)
            {
                //if (myAttachedReactor is IUpgradeableModule) {
                //    IUpgradeableModule upmod = myAttachedReactor as IUpgradeableModule;
                //    if (upmod.HasTechsRequiredToUpgrade()) {
                //        attached_reactor_upgraded = true;
                //    }
                //}

                _maxISP = (float)(Math.Sqrt((double)AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());
                _minISP = _maxISP * 0.4f;
                atmospherecurve.Add(0, _maxISP, 0, 0);
                atmospherecurve.Add(1, _minISP, 0, 0);

                thrust = (float)(AttachedReactor.MaximumPower * GetPowerThrustModifier() * GetHeatThrustModifier() / PluginHelper.GravityConstant / _maxISP);
                myAttachedEngine.maxThrust = thrust;
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
            else
            {
                atmospherecurve.Add(0, 0.00001f, 0, 0);
                myAttachedEngine.maxThrust = thrust;
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
        }

        private double GetIspPropellantModifier()
        {
            double ispModifier = (PluginHelper.IspNtrPropellantModifierBase == 0
                ? _ispPropellantMultiplier
                : (PluginHelper.IspNtrPropellantModifierBase + _ispPropellantMultiplier) / (1.0f + PluginHelper.IspNtrPropellantModifierBase));
            return ispModifier;
        }

        private float delayedThrottle = 0;

        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (myAttachedEngine == null) return;

            // attach/detach with radius
            if (myAttachedEngine.isOperational)
                _myAttachedReactor.AttachThermalReciever(id, radius);
            else
            {
                _myAttachedReactor.DetachThermalReciever(id);
                ConfigEffects();
            }
        }

        public override void OnFixedUpdate() // OnFixedUpdate does not seem to be called in edit mode
        {
            ConfigEffects();

            if (cooledIntake != null)
            {
                if ((cooledIntake.part.temperature / cooledIntake.part.maxTemp) > (part.temperature / part.maxTemp))
                    cooledIntake.part.temperature = (part.temperature / part.maxTemp) * cooledIntake.part.maxTemp;
            }

            if (AttachedReactor == null)
            {
                if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0)
                {
                    myAttachedEngine.Events["Shutdown"].Invoke();
                    ScreenMessages.PostScreenMessage("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
                myAttachedEngine.maxFuelFlow = 0;
                return;
            }

            delayedThrottle = _currentpropellant_is_jet || myAttachedEngine.currentThrottle < delayedThrottle
                ? myAttachedEngine.currentThrottle
                : Mathf.MoveTowards(delayedThrottle, myAttachedEngine.currentThrottle, delayedThrottleFactor * TimeWarp.fixedDeltaTime);

            thermalRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_THERMALPOWER);
            _currentMaximumPower = AttachedReactor.MaximumPower * delayedThrottle;
            _availableThermalPower = _currentMaximumPower * thermalRatio;

            //staticPresure = (FlightGlobals.getStaticPressure(vessel.transform.position)).ToString("0.0000") + " kPa";

            // actively cool
            var wasteheatRatio = Math.Min(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 1);
            var tempRatio =  Math.Pow(part.temperature / part.maxTemp, 2);
            part.temperature = part.temperature - (0.05 * tempRatio * part.temperature * TimeWarp.fixedDeltaTime * (1 - Math.Pow(wasteheatRatio, 0.5)));

            var extendedPropellant = myAttachedEngine.propellants[0] as ExtendedPropellant;
            if (extendedPropellant.name != extendedPropellant.StoragePropellantName)
            {
                var propellantResourse = part.Resources[extendedPropellant.name];
                var storageResourse = part.GetConnectedResources(extendedPropellant.StoragePropellantName);
                var propellantShortage = propellantResourse.maxAmount - propellantResourse.amount;
                var totalAmount = storageResourse.Sum(r => r.amount) + propellantResourse.amount;
                var totalMaxAmount = storageResourse.Sum(r => r.maxAmount);
                var waterStorageRatio = totalMaxAmount > 0 ? totalAmount / totalMaxAmount : 0;
                var message = (waterStorageRatio * 100).ToString("0") + "% " + extendedPropellant.StoragePropellantName + " " + totalAmount.ToString("0") + "/" + totalMaxAmount.ToString("0");
                fuel_gauge.SetLength(5);
                fuel_gauge.SetMessage(message);

                var collectFlowGlobal = ORSHelper.fixedRequestResource(this.part, extendedPropellant.StoragePropellantName, propellantShortage);
                propellantResourse.amount += collectFlowGlobal;
            }
            else
                fuel_gauge.SetLength(2.5f);

            if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0)
                GenerateThrustFromReactorHeat();
            else
            {
                //requestedReactorThermalPower = String.Empty;
                //requestedReactorChargedPower = String.Empty;
                //recievedReactorPower = String.Empty;

                consumedWasteHeat = 0;

                atmospheric_limit = GetAtmosphericLimit();

                _maxISP = (float)(Math.Sqrt((double)AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());
                
                expectedMaxThrust = (float)(AttachedReactor.MaximumPower * GetPowerThrustModifier() * GetHeatThrustModifier() / PluginHelper.GravityConstant / _maxISP);

                expectedMaxThrust *= _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / 200f);

                max_fuel_flow_rate = (float)(expectedMaxThrust / _maxISP / PluginHelper.GravityConstant);

                pressureTreshold = _currentpropellant_is_jet ? 0 : exitArea * (float)FlightGlobals.getStaticPressure(vessel.transform.position);

                var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureTreshold) / expectedMaxThrust;

                current_isp = _maxISP * (float)thrustAtmosphereRatio;

                FloatCurve newISP = new FloatCurve();
                var effectiveIsp = isJet ? Mathf.Min(current_isp, PluginHelper.MaxThermalNozzleIsp) : current_isp;
                newISP.Add(0, effectiveIsp, 0, 0);
                myAttachedEngine.atmosphereCurve = newISP;

                if (myAttachedEngine.useVelCurve)
                {
                    double vcurve_at_current_velocity = myAttachedEngine.velCurve.Evaluate((float)vessel.srf_velocity.magnitude);

                    if (vcurve_at_current_velocity > 0 && !double.IsInfinity(vcurve_at_current_velocity) && !double.IsNaN(vcurve_at_current_velocity))
                        max_fuel_flow_rate = (float)(max_fuel_flow_rate / vcurve_at_current_velocity);
                }

                // set engines maximum fuel flow
                myAttachedEngine.maxFuelFlow = Math.Min(1000f, (float)max_fuel_flow_rate);

                if (myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
                {
                    part.Effect(_particleFXName, 0);
                }
            }

            //tell static helper methods we are currently updating things
			static_updating = true;
			static_updating2 = true;
		}

        private void GenerateThrustFromReactorHeat()
        {
            if (!AttachedReactor.IsActive)
                AttachedReactor.EnableIfPossible();

            GetMaximumIspAndThrustMultiplier();

            float chargedPowerModifier = _isNeutronAbsorber ? 1 : (AttachedReactor.FullPowerForNonNeutronAbsorbants ? 1 : (float)_myAttachedReactor.ChargedPowerRatio);

            thermal_modifiers = myAttachedEngine.currentThrottle * GetAtmosphericLimit() * _myAttachedReactor.GetFractionThermalReciever(id) * chargedPowerModifier;

            var maximum_requested_thermal_power = _currentMaximumPower * thermal_modifiers;

            var neutronAbsorbingModifier = _isNeutronAbsorber ? 1 : (AttachedReactor.FullPowerForNonNeutronAbsorbants ? 1 : 0);
            requested_thermal_power = Math.Min(_availableThermalPower * thermal_modifiers, AttachedReactor.MaximumThermalPower * delayedThrottle * neutronAbsorbingModifier);

            thermal_power_received = consumeFNResource(requested_thermal_power * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) * _myAttachedReactor.ThermalPropulsionEfficiency / TimeWarp.fixedDeltaTime;

            if (thermal_power_received < maximum_requested_thermal_power)
            {
                var chargedParticleRatio = (float)Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES), 2);
                requested_charge_particles = Math.Min((maximum_requested_thermal_power - thermal_power_received), AttachedReactor.MaximumChargedPower) * chargedParticleRatio;

                thermal_power_received += consumeFNResource(requested_charge_particles * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
            }

            UpdateSootAccumulation();

            var extraWasteheatRedution = AttachedReactor.FullPowerForNonNeutronAbsorbants 
                ? TimeWarp.fixedDeltaTime * getResourceAvailability(FNResourceManager.FNRESOURCE_WASTEHEAT) * myAttachedEngine.currentThrottle 
                : 0;

            var sootModifier = 1f - (sootAccumulationPercentage / sootHeatDivider);

            consumedWasteHeat = sootModifier * (float)Math.Max(AttachedReactor.ProducedWasteHeat + extraWasteheatRedution, thermal_power_received);

            // consume wasteheat
            consumeFNResource(consumedWasteHeat * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
            
            // calculate max thrust
            heatExchangerThrustDivisor = (float)GetHeatExchangerThrustDivisor();
            radiusModifier = (heatExchangerThrustDivisor * 100).ToString("0.00") + "%";
            engineMaxThrust = 0.01f;
            if (_availableThermalPower > 0)
            {
                var ispRatio = _currentpropellant_is_jet ? current_isp / _maxISP : 1;
                var thrustLimit = myAttachedEngine.thrustPercentage / 100.0f;
                engineMaxThrust = (float)Math.Max(thrustLimit * GetPowerThrustModifier() * GetHeatThrustModifier() * thermal_power_received / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor * ispRatio / myAttachedEngine.currentThrottle, 0.01);
            }

            max_thrust_in_space = engineMaxThrust / myAttachedEngine.thrustPercentage * 100;

            var vesselStaticPresure = (float)FlightGlobals.getStaticPressure(vessel.transform.position);

            max_thrust_in_current_atmosphere = max_thrust_in_space;
            
            // update engine thrust/ISP for thermal noozle
            if (!_currentpropellant_is_jet)
            {
                pressureTreshold = exitArea * vesselStaticPresure;
                max_thrust_in_current_atmosphere = Mathf.Max(max_thrust_in_space - pressureTreshold, Mathf.Max(myAttachedEngine.currentThrottle * 0.01f, 0.0001f));

                var thrustAtmosphereRatio = max_thrust_in_space > 0 ? Math.Max(max_thrust_in_current_atmosphere / max_thrust_in_space, 0.01 ) : 0.01;
                UpdateIspEngineParams(thrustAtmosphereRatio);
                current_isp = _maxISP * (float)thrustAtmosphereRatio;
            }
            else
                current_isp = _maxISP;

            final_max_engine_thrust = !Single.IsInfinity(max_thrust_in_current_atmosphere) && !Single.IsNaN(max_thrust_in_current_atmosphere)
                ? max_thrust_in_current_atmosphere * _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / sootThrustDivider)
                : 0.000001f;

            // amount of fuel being used at max throttle with no atmospheric limits
            if (_maxISP <= 0) return;
            
			// calculate maximum fuel flow rate
            max_fuel_flow_rate = final_max_engine_thrust / current_isp / PluginHelper.GravityConstant / myAttachedEngine.currentThrottle;

            if (myAttachedEngine.useVelCurve && myAttachedEngine.velCurve != null)
            {
                vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)(vessel.speed / vessel.speedOfSound));

                if (vcurveAtCurrentVelocity > 0 && !double.IsInfinity(vcurveAtCurrentVelocity) && !double.IsNaN(vcurveAtCurrentVelocity))
                    max_fuel_flow_rate = (float)(max_fuel_flow_rate * vcurveAtCurrentVelocity);
                else
                    max_fuel_flow_rate = 0.000001f;
            }

            if (atmospheric_limit > 0 && atmospheric_limit != 1 && !double.IsInfinity(atmospheric_limit) && !double.IsNaN(atmospheric_limit))
                max_fuel_flow_rate = max_fuel_flow_rate * atmospheric_limit;

            engineHeatProduction = (max_fuel_flow_rate >= 0.0001) ? baseHeatProduction * 350 / max_fuel_flow_rate /(float)Math.Pow(_maxISP, 0.8)  : baseHeatProduction;
            myAttachedEngine.heatProduction = engineHeatProduction;

			// set engines maximum fuel flow
	        myAttachedEngine.maxFuelFlow = Math.Min(1000, max_fuel_flow_rate);

            if (myAttachedEngine is ModuleEnginesFX && !String.IsNullOrEmpty(_particleFXName))
            {
                part.Effect(_particleFXName, Mathf.Max(0.1f * myAttachedEngine.currentThrottle,  Mathf.Min((float)Math.Pow(thermal_power_received / requested_thermal_power, 0.5), delayedThrottle)));
            }

            if (_fuelToxicity > 0 && max_fuel_flow_rate > 0 && vesselStaticPresure > 1)
            {
                _savedReputationCost += (float)(max_fuel_flow_rate * _fuelToxicity * TimeWarp.fixedDeltaTime * Math.Pow(vesselStaticPresure / 100, 3));
                if (_savedReputationCost > 1)
                {
                    float flooredReputationCost = (int)Math.Floor(_savedReputationCost);

                    if (Reputation.Instance != null)
                        Reputation.Instance.addReputation_discrete(-flooredReputationCost, TransactionReasons.None);
                    else
                        UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - No Reputation found, was not able to reduce reputation by " + flooredReputationCost);

                    ScreenMessages.PostScreenMessage("You are poisoning the environment with " + _fuelmode + " from your exhaust!", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    _savedReputationCost -= flooredReputationCost;
                }
            }
        }

        private void UpdateSootAccumulation()
        {
            if (myAttachedEngine.currentThrottle > 0 && _propellantSootFactorFullThrotle != 0 || _propellantSootFactorMinThrotle != 0)
            {
                float sootEffect;

                if (_propellantSootFactorEquilibrium != 0)
                {
                    var ratio = myAttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
                        ? (myAttachedEngine.currentThrottle - _propellantSootFactorEquilibrium) / (1 - _propellantSootFactorEquilibrium)
                        : 1 - (myAttachedEngine.currentThrottle / _propellantSootFactorEquilibrium);

                    var sootMultiplier = myAttachedEngine.currentThrottle < _propellantSootFactorEquilibrium ? 1 
                        : _propellantSootFactorFullThrotle > 0 ? _heatDecompositionFraction : 1;

                    sootEffect = myAttachedEngine.currentThrottle > _propellantSootFactorEquilibrium
                        ? _propellantSootFactorFullThrotle * ratio * sootMultiplier
                        : _propellantSootFactorMinThrotle * ratio * sootMultiplier;
                 }
                else
                {
                    var sootMultiplier = _heatDecompositionFraction > 0 ? _heatDecompositionFraction : 1;
                    sootEffect = _propellantSootFactorFullThrotle * sootMultiplier;
                }

                sootAccumulationPercentage = Math.Min(100, Math.Max(0, sootAccumulationPercentage + (TimeWarp.fixedDeltaTime * sootEffect)));
            }
            else
            {
                sootAccumulationPercentage -= TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle * 0.1f;
                sootAccumulationPercentage = Math.Max(0, sootAccumulationPercentage);
            }
        }

        private void GetMaximumIspAndThrustMultiplier()
        {
            // get the flameout safety limit
            if (_currentpropellant_is_jet)
            {
                UpdateIspEngineParams();
                this.current_isp = myAttachedEngine.atmosphereCurve.Evaluate((float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position), 1.0));

                int pre_coolers_active = _vesselPrecoolers.Sum(prc => prc.ValidAttachedIntakes) + buildInPrecoolers;
                int intakes_open = _vesselResourceIntake.Where(mre => mre.intakeEnabled).Count();

                double proportion = Math.Pow((double)(intakes_open - pre_coolers_active) / (double)intakes_open, 0.1);
                if (double.IsNaN(proportion) || double.IsInfinity(proportion))
                    proportion = 1;

                float temp = (float)Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * proportion, 1);
                if (temp > part.maxTemp - 10.0f)
                {
                    ScreenMessages.PostScreenMessage("Engine Shutdown: Catastrophic overheating was imminent!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    myAttachedEngine.Shutdown();
                    //part.temperature = 1;
                }
                else
                    part.temperature = temp;
            }
            else
            {
                if (_decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                    UpdateThrustPropellantMultiplier();
                else
                    _heatDecompositionFraction = 1;

                _maxISP = (float)(Math.Sqrt((double)AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());

                //thermalRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_THERMALPOWER);
                //_assThermalPower = MyAttachedReactor.MaximumPower * thermalRatio * delayedThrottle;
            }
        }

		public override string GetInfo() 
        {
			bool upgraded = false;
            if (this.HasTechsRequiredToUpgrade())
                upgraded = true;

            ConfigNode[] prop_nodes = upgraded && isJet ? getPropellantsHybrid() : getPropellants(isJet);
			
			string return_str = "Thrust: Variable\n";
			foreach (ConfigNode propellant_node in prop_nodes) 
            {
				float ispMultiplier = float.Parse(propellant_node.GetValue("ispMultiplier"));
				string guiname = propellant_node.GetValue("guiName");
                return_str = return_str + "--" + guiname + "--\n" + "ISP: " + ispMultiplier.ToString("0.000") + " x " + (PluginHelper.IspCoreTempMult + IspTempMultOffset).ToString("0.000") + " x Sqrt(Core Temperature)" + "\n";
			}
			return return_str;
		}

        public override int getPowerPriority() 
        {
            return 1;
        }

		// Static Methods
		// Amount of intake air available to use of a particular resource type
		public static double getIntakeAvailable(Vessel vess, string resourcename) 
        {
            List<INoozle> nozzles = vess.FindPartModulesImplementing<INoozle>();
            bool updating = true;
            foreach (INoozle nozzle in nozzles)
            {
                if (!nozzle.Static_updating)
                {
                    updating = false;
                    break;
                }
            }

            if (updating)
            {
                nozzles.ForEach(nozzle => nozzle.Static_updating = false);
                List<PartResource> partresources = vess.rootPart.GetConnectedResources(resourcename).ToList();
                
                double currentintakeatm = 0;
                double maxintakeatm = 0;

                partresources.ForEach(partresource => currentintakeatm += partresource.amount);
                partresources.ForEach(partresource => maxintakeatm += partresource.maxAmount);

                intake_amounts[resourcename] = currentintakeatm;
                intake_maxamounts[resourcename] = maxintakeatm;
            }

            if (intake_amounts.ContainsKey(resourcename))
                return Math.Max(intake_amounts[resourcename], 0);
            
			return 0.00001;
		}

		// enumeration of the fuel useage rates of all jets on a vessel
		public static int getEnginesRunningOfTypeForVessel (Vessel vess, string resourcename) 
        {
            List<INoozle> nozzles = vess.FindPartModulesImplementing<INoozle>();
			int engines = 0;
            foreach (INoozle nozzle in nozzles) 
            {
				ConfigNode[] prop_node = nozzle.getPropellants ();

                if (prop_node == null || prop_node[nozzle.Fuel_mode] == null) continue;

                ConfigNode[] assprops = prop_node[nozzle.Fuel_mode].GetNodes("PROPELLANT");
                if (assprops[0].GetValue("name").Equals(resourcename) && nozzle.GetNozzleFlowRate() > 0)
                    engines++;
			}
			return Math.Max(engines, 1);
		}

		// enumeration of the fuel useage rates of all jets on a vessel
		public double GetFuelRateThermalJets (string resourcename) 
        {
			int engines = 0;
			bool updating = true;
            foreach (INoozle nozzle in _vesselThermalNozzles) 
            {
				ConfigNode[] prop_node = nozzle.getPropellants ();

                if (prop_node == null) continue; 

				ConfigNode[] assprops = prop_node [nozzle.Fuel_mode].GetNodes ("PROPELLANT");

                if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

				if (!nozzle.Static_updating2) 
					updating = false;
							
				if (nozzle.GetNozzleFlowRate () > 0) 
					engines++;
			}

			if (updating) 
            {
				double enum_rate = 0;
                foreach (INoozle nozzle in _vesselThermalNozzles) 
                {
					ConfigNode[] prop_node = nozzle.getPropellants ();

                    if (prop_node == null) continue;

                    ConfigNode[] assprops = prop_node [nozzle.Fuel_mode].GetNodes ("PROPELLANT");

                    if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

					enum_rate += nozzle.GetNozzleFlowRate ();
					nozzle.Static_updating2 = false;
				}

				if (fuel_flow_amounts.ContainsKey (resourcename)) 
					fuel_flow_amounts [resourcename] = enum_rate;
				else
				    fuel_flow_amounts.Add (resourcename, enum_rate);
			}

			if (fuel_flow_amounts.ContainsKey (resourcename)) 
				return fuel_flow_amounts [resourcename];

			return 0.1;
		}


        public static ConfigNode[] getPropellants(bool isJet) 
        {
            ConfigNode[] propellantlist = isJet
                ? GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT")
                : GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");

            if (propellantlist == null) 
                PluginHelper.showInstallationErrorMessage();

            return propellantlist;
        }

        private double GetHeatThrustModifier()
        {
            double coretempthreshold = PluginHelper.ThrustCoreTempThreshold;
            double lowcoretempbase = PluginHelper.LowCoreTempBaseThrust;

            return coretempthreshold <= 0 
                ? 1.0 
                : AttachedReactor.CoreTemperature < coretempthreshold
                    ? (AttachedReactor.CoreTemperature + lowcoretempbase) / (coretempthreshold + lowcoretempbase)
                    : 1.0 + PluginHelper.HighCoreTempThrustMult * Math.Max(Math.Log10(AttachedReactor.CoreTemperature / coretempthreshold), 0);
        }

        private double CurrentPowerThrustMultiplier
        {
            get
            {
                return _propellantIsLFO
                    ? powerTrustMultiplierJet  //* (jetTechBonus / 21.472)
                    : powerTrustMultiplier;
            }
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalThermalNozzlePowerMaxThrustMult * CurrentPowerThrustMultiplier;
        }

        private void UpdateRadiusModifier()
        {
            if (_myAttachedReactor != null)
            {
                // re-attach with updated radius
                _myAttachedReactor.DetachThermalReciever(id);
                _myAttachedReactor.AttachThermalReciever(id, radius);

                Fields["vacuumPerformance"].guiActiveEditor = true;
                Fields["radiusModifier"].guiActiveEditor = true;
                Fields["surfacePerformance"].guiActiveEditor = true;

                var heatExchangerThrustDivisor = (float)GetHeatExchangerThrustDivisor();

                radiusModifier = (heatExchangerThrustDivisor * 100.0).ToString("0.00") + "%";

                _maxISP = (float)(Math.Sqrt(AttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());

                var max_thrust_in_space = GetPowerThrustModifier() * GetHeatThrustModifier() * AttachedReactor.MaximumThermalPower / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor;

                var final_max_thrust_in_space = max_thrust_in_space * _thrustPropellantMultiplier;

                var isp_in_space = heatExchangerThrustDivisor * _maxISP;

                vacuumPerformance = final_max_thrust_in_space.ToString("0.0") + "kN @ " + isp_in_space.ToString("0.0") + "s";

                maxPressureThresholdAtKerbinSurface = exitArea * (float)GameConstants.EarthAtmospherePressureAtSeaLevel;

                var maxSurfaceThrust = Math.Max(max_thrust_in_space - (maxPressureThresholdAtKerbinSurface), 0.00001);

                var maxSurfaceISP = _maxISP * (maxSurfaceThrust / max_thrust_in_space) * heatExchangerThrustDivisor;

                var final_max_surface_thrust = maxSurfaceThrust * _thrustPropellantMultiplier;

                surfacePerformance = final_max_surface_thrust.ToString("0.0") + "kN @ " + maxSurfaceISP.ToString("0.0") + "s";
            }
            else
            {
                Fields["vacuumPerformance"].guiActiveEditor = false;
                Fields["radiusModifier"].guiActiveEditor = false;
                Fields["surfacePerformance"].guiActiveEditor = false;
            }
        }


        private double GetHeatExchangerThrustDivisor()
        {
            if (AttachedReactor == null || AttachedReactor.GetRadius() == 0 || radius == 0 || _myAttachedReactor.GetFractionThermalReciever(id) == 0) return 0;

            var fractionalReactorRadius = Math.Sqrt(Math.Pow(AttachedReactor.GetRadius(), 2) * _myAttachedReactor.GetFractionThermalReciever(id));

            // scale down thrust if it's attached to the wrong sized reactor
            double heat_exchanger_thrust_divisor = radius > fractionalReactorRadius
                ? fractionalReactorRadius * fractionalReactorRadius / radius / radius
                : normalizeFraction(radius / (float)fractionalReactorRadius, 1f);

            if (!_currentpropellant_is_jet)
            {
                for (int i = 0; i < partDistance; i++)
                    heat_exchanger_thrust_divisor *= AttachedReactor.ThermalTransportationEfficiency;
            }

            return heat_exchanger_thrust_divisor;
        }

        private float normalizeFraction(float variable, float normalizer)
        {
            return (normalizer + variable) / (1f + normalizer);
        }

        public static ConfigNode[] getPropellantsHybrid() 
        {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT");
            ConfigNode[] propellantlist2 = GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");
            propellantlist = propellantlist.Concat(propellantlist2).ToArray();
            if (propellantlist == null || propellantlist2 == null) 
                PluginHelper.showInstallationErrorMessage();
            
            return propellantlist;
        }
	}
}
