extern alias ORSvKSPIE;

using ORSvKSPIE::OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TweakScale;
using FNPlugin.Propulsion;
using FNPlugin.Extensions;

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
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1; // hidden setting used by ballance mods

        [KSPField(isPersistant = true, guiActive = true, guiName = "Soot Accumulation", guiUnits = " %")]
        public float sootAccumulationPercentage;


		//Persistent False
		[KSPField(isPersistant = false)]
		public bool isJet = false;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float IspTempMultOffset = 0;
        [KSPField(isPersistant = false)]
        public float sootHeatDivider = 50;
        //[KSPField(isPersistant = false)]
        //public float heatProduction = 1;
        //[KSPField(isPersistant = false)]
        //public float heatProductionMult = 5;
        [KSPField(isPersistant = false)]
        public float emisiveConstantMult = 3;
        [KSPField(isPersistant = false)]
        public float emisiveConstantExp = 0.6f;
        [KSPField(isPersistant = false)]
        public float maxTemp = 2750;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq = null;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radius", guiUnits = "m")]
        public float radius;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Exit Area", guiUnits = " m2")]
        public float exitArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 1;

		//External
		public bool static_updating = true;
		public bool static_updating2 = true;

		//GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string engineType = ":";
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Fuel Mode")]
		public string _fuelmode;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Fuel Isp Multiplier")]
        public float _ispPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Soot")]
        public float _propellantSootFactorFullThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Min Soot")]
        public float _propellantSootFactorMinThrotle;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Equilibrium Soot")]
        public float _propellantSootFactorEquilibrium;

        //[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra Heat Production ")]
        //public float heatProductionExtra;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
        public string temperatureStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Fuel Thrust Multiplier")]
        public float _thrustPropellantMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Upgrade Cost")]
		public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Base Heat Production")]
        public float baseHeatProduction = 80;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Heat Production")]
        public float engineHeatProduction;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Static Presure")]
        public string staticPresure;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Treshold", guiUnits = " kN")]
        public float pressureTreshold;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Atmospheric Limit")]
        public float atmospheric_limit;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Thermal")]
        public string requestedReactorThermalPower;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Requested Charge")]
        public string requestedReactorChargedPower;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Recieved")]
        public string recievedReactorPower;
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
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Final Thrust")]
        protected float final_max_engine_thrust_in_space;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MaxISP")]
        protected float _maxISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "MinISP")]
        protected float _minISP;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Fuel Flow", guiFormat="0.000000")]
        protected double max_fuel_flow_rate = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Isp")]
        protected float current_isp = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "MaxPressureThresshold")]
        protected float maxPressureThresholdAtKerbinSurface;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Ratio")]
        protected float thermalRatio;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Consumed")]
        protected float consumedWasteHeat;

		//Internal
        protected float _fuelToxicity;
        protected float _savedReputationCost;
        protected float _heatDecompositionFraction;
        protected float _currentMaximumPower;
		protected float _assThermalPower;
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
		protected int thrustLimitRatio = 0;
		protected double old_intake = 0;
        protected int partDistance = 0;
        protected float old_atmospheric_limit;
        protected double currentintakeatm;

        public bool Static_updating { get { return static_updating; } set { static_updating = value; } }
        public bool Static_updating2 { get { return static_updating2; } set { static_updating2 = value; } }
        public int Fuel_mode { get { return fuel_mode; } }

        private IThermalSource _myAttachedReactor;
        public IThermalSource MyAttachedReactor 
        {
            get { return _myAttachedReactor; }
            private set 
            {
                _myAttachedReactor = value;
                if (_myAttachedReactor == null) return;
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

            setupPropellants(true);
		}

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Propellant", active = true)]
        public void PreviousPropellant()
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = propellants.Length - 1;

            setupPropellants(false);
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
			propellants = getPropellantsHybrid();
			isHybrid = true;
			isupgraded = true;
		}

		public ConfigNode[] getPropellants() 
        {
			return propellants;
		}

        public void OnEditorAttach() 
        {
            FindAttachedThermalSource();

            if (MyAttachedReactor == null) return;

            estimateEditorPerformance();
        }

        public void OnEditorDetach()
        {
            if (MyAttachedReactor == null) return;

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

            if (state == StartState.Editor)
            {
                part.OnEditorAttach += OnEditorAttach;
                part.OnEditorDetach += OnEditorDetach;

                propellants = getPropellants(isJet);
                if (this.HasTechsRequiredToUpgrade() && isJet)
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                setupPropellants();
                estimateEditorPerformance();
                return;
            }
            else
                UpdateRadiusModifier();

            fuel_gauge = part.stackIcon.DisplayInfo();

            // if engine isn't already initialised, initialise it
            if (engineInit == false)
                engineInit = true;

            // if we can upgrade, let's do so
            if (isupgraded && isJet)
                upgradePartModule();
            else
            {
                if (this.HasTechsRequiredToUpgrade() && isJet)
                    hasrequiredupgrade = true;

                // if not, use basic propellants
                propellants = getPropellants(isJet);
            }

            maxPressureThresholdAtKerbinSurface = exitArea * (float)GameConstants.EarthAtmospherePressureAtSeaLevel;
            hasstarted = true;
        }

        private void FindAttachedThermalSource()
        {
            var source = ThermalSourceSearchResult.BreadthFirstSearchForThermalSource(part, 10, 1);
            if (source == null) return;

            MyAttachedReactor = source.Source;
            partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);
            UnityEngine.Debug.Log("[KSPI] - ThermalNozzleController - BreadthFirstSearchForThermalSource- Found thermal source with distance " + partDistance);
        }

        // Note: does not seem to be called while in vab mode
        public override void OnUpdate() 
        {
            // setup propellant after startup to allow InterstellarFuelSwitch to configure the propellant
            if (!hasSetupPropellant)
            {
                hasSetupPropellant = true;
                setupPropellants(true, true);
            }

            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";
            staticPresure = (FlightGlobals.getStaticPressure(vessel.transform.position)).ToString("0.0000") + " kPa";
            pressureTreshold = exitArea * (float)FlightGlobals.getStaticPressure(vessel.transform.position);

			Fields["engineType"].guiActive = isJet;
			if (ResearchAndDevelopment.Instance != null && isJet) 
            {
				Events ["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
				upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
			} 
            else
				Events ["RetrofitEngine"].active = false;
			
			Fields["upgradeCostStr"].guiActive = !isupgraded  && hasrequiredupgrade && isJet;

			if (myAttachedEngine != null) 
            {
				if (myAttachedEngine.isOperational && !IsEnabled) 
                {
					IsEnabled = true;
					part.force_activate ();
				}
				updatePropellantBar ();
			}
		}
        
		public void updatePropellantBar() 
        {
			float currentpropellant = 0;
			float maxpropellant = 0;

            List<PartResource> partresources = part.GetConnectedResources(myAttachedEngine.propellants.FirstOrDefault().name).ToList();
			
			foreach (PartResource partresource in partresources) 
            {
				currentpropellant += (float) partresource.amount;
				maxpropellant += (float)partresource.maxAmount;
			}

			if (fuel_gauge != null  && fuel_gauge.infoBoxRef != null) 
            {
				if (myAttachedEngine.isOperational) 
                {
					if (!fuel_gauge.infoBoxRef.expanded) 
						fuel_gauge.infoBoxRef.Expand ();
					
					fuel_gauge.length = 2;

					if (maxpropellant > 0) 
						fuel_gauge.SetValue (currentpropellant / maxpropellant);
					else
						fuel_gauge.SetValue (0);
				} 
                else if (!fuel_gauge.infoBoxRef.collapsed) 
				    fuel_gauge.infoBoxRef.Collapse ();
			}
		}

        public override void OnActive()
        {
            base.OnActive();
            setupPropellants(true, true);
        }

		public void setupPropellants(bool forward = true,  bool notifySwitching = false)
        {
            ConfigNode chosenpropellant = propellants[fuel_mode];
            UpdatePropellantModeBehavior(chosenpropellant);
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<Propellant> list_of_propellants = new List<Propellant>();

            // loop though propellants until we get to the selected one, then set it up
            foreach (ConfigNode prop_node in assprops)
            {
                Propellant curprop = new Propellant();
                curprop.Load(prop_node);
                if (curprop.drawStackGauge && HighLogic.LoadedSceneIsFlight)
                {
                    curprop.drawStackGauge = false;

                    if (_currentpropellant_is_jet)
                        fuel_gauge.SetMessage("Atmosphere");
                    else
                    {
                        fuel_gauge.SetMessage(curprop.name);
                        myAttachedEngine.thrustPercentage = 100;
                        //part.temperature = 1;
                    }

                    fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                    fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                    fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetValue(0f);
                }
                list_of_propellants.Add(curprop);
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
                    var partresources = part.GetConnectedResources(curEngine_propellant.name);

                    if (!partresources.Any() || !PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name))
                    {
                        if (notifySwitching)
                            missingResources += curEngine_propellant.name + " ";
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
                // Still ignore propellants that don't exist
                if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name) && (switches <= propellants.Length || fuel_mode != 0))
                {
                    ++switches;
                    if (forward)
                        NextPropellant();
                    else
                        PreviousPropellant();
                }

                estimateEditorPerformance(); // update editor estimates
            }
            switches = 0;
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
            _fuelToxicity = chosenpropellant.HasValue("Toxicity") ? float.Parse(chosenpropellant.GetValue("Toxicity")) : 0;

            _currentpropellant_is_jet = chosenpropellant.HasValue("isJet") ? bool.Parse(chosenpropellant.GetValue("isJet")) : false;

            if (!_currentpropellant_is_jet && _decompositionEnergy > 0 && _baseIspMultiplier > 0 && _minDecompositionTemp > 0 && _maxDecompositionTemp > 0)
                UpdateThrustPropellantMultiplier();
            else
            {
                _heatDecompositionFraction = 1;
                _ispPropellantMultiplier = chosenpropellant.HasValue("ispMultiplier") ? float.Parse(chosenpropellant.GetValue("ispMultiplier")) : 1;
                _thrustPropellantMultiplier = chosenpropellant.HasValue("thrustMultiplier") ? float.Parse(chosenpropellant.GetValue("thrustMultiplier")) : 1;
            }
        }

        private void UpdateThrustPropellantMultiplier()
        {
            var linearFraction = Math.Max(0, Math.Min(1, (MyAttachedReactor.CoreTemperature - _minDecompositionTemp) / (_maxDecompositionTemp - _minDecompositionTemp)));
            _heatDecompositionFraction = (float)Math.Pow(0.36, Math.Pow(3 - linearFraction * 3, 2) / 2);
            _thrustPropellantMultiplier = (float)Math.Sqrt(_heatDecompositionFraction * _decompositionEnergy / _hydroloxDecompositionEnergy) * 1.04f + 1;
            _ispPropellantMultiplier = _baseIspMultiplier * _thrustPropellantMultiplier;
        }

        public void updateIspEngineParams(double atmosphere_isp_efficiency = 1, double max_thrust_in_space = 0) 
        {
			// recaculate ISP based on power and core temp available
			FloatCurve newISP = new FloatCurve();
			FloatCurve vCurve = new FloatCurve ();

            _maxISP = (float)(Math.Sqrt((double)MyAttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());
            
			if (!_currentpropellant_is_jet) 
            {
                //if (maxPressureThresholdAtKerbinSurface <= max_thrust_in_space) //&& FlightGlobals.getStaticPressure(vessel.transform.position / 100) <= 1)
                //{
                //    var min_engine_thrust = Math.Max(max_thrust_in_space - maxPressureThresholdAtKerbinSurface, 0.00001);
                //    var minThrustAtmosphereRatio = min_engine_thrust / Math.Max(max_thrust_in_space, 0.000001);
                //    _minISP = _maxISP * (float)minThrustAtmosphereRatio * (float)GetHeatExchangerThrustDivisor();
                //    newISP.Add(0, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp), 0, 0);
                //    newISP.Add(1, Mathf.Min(_minISP, PluginHelper.MaxThermalNozzleIsp), 0, 0);
                //}
                //else
                    newISP.Add(0, Mathf.Min(_maxISP * (float)atmosphere_isp_efficiency, PluginHelper.MaxThermalNozzleIsp), 0, 0);

                myAttachedEngine.useVelCurve = false;
				myAttachedEngine.useEngineResponseTime = false;
			} 
            else 
            {
                newISP.Add(0, Mathf.Min(_maxISP * 4.0f / 5.0f, PluginHelper.MaxThermalNozzleIsp));
                newISP.Add(0.15f, Mathf.Min(_maxISP, PluginHelper.MaxThermalNozzleIsp));
                newISP.Add(0.3f, Mathf.Min(_maxISP * 4.0f / 5.0f, PluginHelper.MaxThermalNozzleIsp));
                newISP.Add(1, Mathf.Min(_maxISP * 4.0f / 5.0f, PluginHelper.MaxThermalNozzleIsp));
				vCurve.Add(0, 1.0f);
                vCurve.Add((float)(_maxISP * PluginHelper.GravityConstant * 1.0 / 3.0), 1.0f);
                vCurve.Add((float)(_maxISP * PluginHelper.GravityConstant), 1.0f);
                vCurve.Add((float)(_maxISP * PluginHelper.GravityConstant * 4.0 / 3.0), 0);
                myAttachedEngine.useVelCurve = true;
				myAttachedEngine.useEngineResponseTime = true;
				myAttachedEngine.ignitionThreshold = 0.01f;
			}

			myAttachedEngine.atmosphereCurve = newISP;
            myAttachedEngine.velCurve = vCurve;

            //thermalRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_THERMALPOWER);
            //_assThermalPower = MyAttachedReactor.MaximumPower * thermalRatio;

            //if (MyAttachedReactor is InterstellarFusionReactor) 
            //    _assThermalPower = _assThermalPower * 0.95f;

		}

		public float GetAtmosphericLimit() 
        {
			atmospheric_limit = 1.0f;
            if (_currentpropellant_is_jet) 
            {
                string resourcename = myAttachedEngine.propellants[0].name;
                currentintakeatm = getIntakeAvailable(vessel, resourcename);
                var fuelRateThermalJetsForVessel = getFuelRateThermalJetsForVessel(vessel, resourcename);

                if (fuelRateThermalJetsForVessel > 0) 
                {
                    // divide current available intake resource by fuel useage across all engines
                    var intakeFuelRate = (float)Math.Min(currentintakeatm / fuelRateThermalJetsForVessel, 1.0);

                    atmospheric_limit = intakeFuelRate; //getEnginesRunningOfTypeForVessel(vessel, resourcename);
                }
                old_intake = currentintakeatm;
            }
            atmospheric_limit = Mathf.MoveTowards(old_atmospheric_limit, atmospheric_limit, 0.1f);
            old_atmospheric_limit = atmospheric_limit;
			return atmospheric_limit;
		}

		public double getNozzleFlowRate() 
        {
			return max_fuel_flow_rate;
		}

		public bool getUpdating() 
        {
			return static_updating;
		}

		public bool hasStarted() 
        {
			return hasstarted;
		}

        public void estimateEditorPerformance() 
        {
            FloatCurve atmospherecurve = new FloatCurve();
            float thrust = 0;
            UpdateRadiusModifier();

            if (MyAttachedReactor != null) 
            {
                //if (myAttachedReactor is IUpgradeableModule) {
                //    IUpgradeableModule upmod = myAttachedReactor as IUpgradeableModule;
                //    if (upmod.HasTechsRequiredToUpgrade()) {
                //        attached_reactor_upgraded = true;
                //    }
                //}

                _maxISP = (float)(Math.Sqrt((double)MyAttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());
                _minISP = _maxISP * 0.4f;
                atmospherecurve.Add(0, _maxISP, 0, 0);
                atmospherecurve.Add(1, _minISP, 0, 0);

                thrust = (float)(MyAttachedReactor.MaximumPower * GetPowerThrustModifier() * GetHeatThrustModifier() / PluginHelper.GravityConstant / _maxISP);
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

		public override void OnFixedUpdate() 
        {
            // note: does not seem to be called in edit mode
            if (MyAttachedReactor == null)
            {
                myAttachedEngine.maxFuelFlow = 0;
                return;
            }

            delayedThrottle = _currentpropellant_is_jet || myAttachedEngine.currentThrottle < delayedThrottle
                ? myAttachedEngine.currentThrottle
                : Mathf.MoveTowards(delayedThrottle, myAttachedEngine.currentThrottle, 0.5f * TimeWarp.fixedDeltaTime);

            thermalRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_THERMALPOWER);
            _currentMaximumPower = MyAttachedReactor.MaximumPower * delayedThrottle;
            _assThermalPower = _currentMaximumPower * thermalRatio;

            staticPresure = (FlightGlobals.getStaticPressure(vessel.transform.position)).ToString("0.0000") + " kPa";

            // actively cool
            var wasteheatRatio = Math.Min(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 1);
            var tempRatio =  Math.Pow(part.temperature / part.maxTemp, 2);
            part.temperature = part.temperature - (0.05 * tempRatio * part.temperature * TimeWarp.fixedDeltaTime * (1 - Math.Pow(wasteheatRatio, 0.5)));

            if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0)
                GenerateThrustFromReactorHeat();
            else
            {
                requestedReactorThermalPower = String.Empty;
                requestedReactorChargedPower = String.Empty;
                recievedReactorPower = String.Empty;

                consumedWasteHeat = 0;

                atmospheric_limit = GetAtmosphericLimit();

                _maxISP = (float)(Math.Sqrt((double)MyAttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());
                
                var expectedMaxThrust = MyAttachedReactor.MaximumPower * GetPowerThrustModifier() * GetHeatThrustModifier() / PluginHelper.GravityConstant / _maxISP;

                expectedMaxThrust *= _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / 150f);

                max_fuel_flow_rate = expectedMaxThrust / _maxISP / PluginHelper.GravityConstant;

                pressureTreshold = _currentpropellant_is_jet ? 0 : exitArea * (float)FlightGlobals.getStaticPressure(vessel.transform.position);

                var thrustAtmosphereRatio = expectedMaxThrust <= 0 ? 0 : Math.Max(0, expectedMaxThrust - pressureTreshold) / expectedMaxThrust;

                current_isp = _maxISP * (float)thrustAtmosphereRatio;

                FloatCurve newISP = new FloatCurve();
                newISP.Add(0, Mathf.Min(current_isp, PluginHelper.MaxThermalNozzleIsp), 0, 0);
                myAttachedEngine.atmosphereCurve = newISP;

                if (myAttachedEngine.useVelCurve)
                {
                    double vcurve_at_current_velocity = myAttachedEngine.velCurve.Evaluate((float)vessel.srf_velocity.magnitude);

                    if (vcurve_at_current_velocity > 0 && !double.IsInfinity(vcurve_at_current_velocity) && !double.IsNaN(vcurve_at_current_velocity))
                        max_fuel_flow_rate = max_fuel_flow_rate / vcurve_at_current_velocity;
                }

                // set engines maximum fuel flow
                myAttachedEngine.maxFuelFlow = Math.Min(0.5f, (float)max_fuel_flow_rate);

                if (MyAttachedReactor == null && myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0)
                {
                    myAttachedEngine.Events["Shutdown"].Invoke();
                    ScreenMessages.PostScreenMessage("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
            }

            //tell static helper methods we are currently updating things
			static_updating = true;
			static_updating2 = true;
		}

        private void GenerateThrustFromReactorHeat()
        {
            if (!MyAttachedReactor.IsActive)
                MyAttachedReactor.enableIfPossible();

            GetMaximumIspAndThrustMultiplier();

            var thermal_modifiers = myAttachedEngine.currentThrottle * GetAtmosphericLimit() * _myAttachedReactor.GetFractionThermalReciever(id);
            var maximum_requested_thermal_power = _currentMaximumPower * thermal_modifiers;

            var requested_thermal_power = Math.Min(_assThermalPower * thermal_modifiers, MyAttachedReactor.MaximumThermalPower * delayedThrottle);
            requestedReactorThermalPower = requested_thermal_power.ToString("0.000000") + " MW " + (this._myAttachedReactor.GetFractionThermalReciever(id) * 100).ToString("0.0") + "%";

            var fixed_thermal_power_received = consumeFNResource(requested_thermal_power * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) * _myAttachedReactor.ThermalPropulsionEfficiency;
            var thermal_power_received = fixed_thermal_power_received / TimeWarp.fixedDeltaTime;
            

            if (thermal_power_received < maximum_requested_thermal_power)
            {
                var chargedParticleRatio = (float)Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_CHARGED_PARTICLES), 2);
                var requested_charge_particles = Math.Min((maximum_requested_thermal_power - thermal_power_received) * delayedThrottle, MyAttachedReactor.MaximumChargedPower * delayedThrottle) * chargedParticleRatio;

                requestedReactorChargedPower = requested_charge_particles.ToString("0.000") + " MW / " + MyAttachedReactor.MaximumChargedPower.ToString("0.000") + " MW";

                thermal_power_received += consumeFNResource(requested_charge_particles * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
            }



            recievedReactorPower = thermal_power_received.ToString("0.000000") + " MW ";

            UpdateSootAccumulation();

            //consumedWasteHeat = (1f - (sootAccumulationPercentage / 150f)) * thermal_power_received;
            consumedWasteHeat = (1f - (sootAccumulationPercentage / 150f)) * (float)MyAttachedReactor.ProducedWasteHeat;

            // consume wasteheat
            consumeFNResource(consumedWasteHeat * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
            
            // calculate max thrust
            heatExchangerThrustDivisor = (float)GetHeatExchangerThrustDivisor();
            engineMaxThrust = 0.01f;
            if (_assThermalPower > 0)
            {
                double ispRatio = _currentpropellant_is_jet ? current_isp / _maxISP : 1;
                double thrustLimit = myAttachedEngine.thrustPercentage / 100.0;
                engineMaxThrust = (float)Math.Max(thrustLimit * GetPowerThrustModifier() * GetHeatThrustModifier() * thermal_power_received / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor * ispRatio / myAttachedEngine.currentThrottle, 0.01);
            }

            max_thrust_in_space = engineMaxThrust / myAttachedEngine.thrustPercentage * 100;

            var vesselStaticPresure = FlightGlobals.getStaticPressure(vessel.transform.position);
            
            // update engine thrust/ISP for thermal noozle
            if (!_currentpropellant_is_jet)
            {
                pressureTreshold = exitArea * (float)vesselStaticPresure;
                max_thrust_in_space = (float)Math.Max(max_thrust_in_space - pressureTreshold, 0.00001);
                var thrustAtmosphereRatio = max_thrust_in_space / Math.Max(max_thrust_in_space, 0.000001);
                var ispReductionFraction = thrustAtmosphereRatio * heatExchangerThrustDivisor;
                updateIspEngineParams(ispReductionFraction, max_thrust_in_space);
                current_isp = (float)(_maxISP * ispReductionFraction);
            }

            final_max_engine_thrust_in_space = !double.IsInfinity(max_thrust_in_space) && !double.IsNaN(max_thrust_in_space)
                ? (float)max_thrust_in_space * _thrustPropellantMultiplier * (1f - sootAccumulationPercentage / 150f)
                : 0.000001f;

            // amount of fuel being used at max throttle with no atmospheric limits
            if (_maxISP <= 0) return;
            
			// calculate maximum fuel flow rate
            max_fuel_flow_rate = final_max_engine_thrust_in_space / _maxISP / PluginHelper.GravityConstant;

            if (myAttachedEngine.useVelCurve && myAttachedEngine.velCurve != null)
            {
                double vcurveAtCurrentVelocity = myAttachedEngine.velCurve.Evaluate((float)vessel.srf_velocity.magnitude);

                if (vcurveAtCurrentVelocity > 0 && !double.IsInfinity(vcurveAtCurrentVelocity) && !double.IsNaN(vcurveAtCurrentVelocity))
                    max_fuel_flow_rate = max_fuel_flow_rate / vcurveAtCurrentVelocity;
            }

            if (atmospheric_limit > 0 && atmospheric_limit != 1 && !double.IsInfinity(atmospheric_limit) && !double.IsNaN(atmospheric_limit))
                max_fuel_flow_rate = max_fuel_flow_rate / atmospheric_limit;

            //prevent divide by zero
            //max_fuel_flow_rate = Math.Max(0.00000001, max_fuel_flow_rate);



            engineHeatProduction = (max_fuel_flow_rate >= 0.000001) ? baseHeatProduction / (float)max_fuel_flow_rate / (float)Math.Pow(_maxISP / 1000, 0.1) / (float)Math.Sqrt(radius) : baseHeatProduction;
            myAttachedEngine.heatProduction = engineHeatProduction;

			// set engines maximum fuel flow
	        myAttachedEngine.maxFuelFlow = Math.Min(1, (float)max_fuel_flow_rate);

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
                updateIspEngineParams();
                this.current_isp = myAttachedEngine.atmosphereCurve.Evaluate((float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position), 1.0));
                int pre_coolers_active = vessel.FindPartModulesImplementing<FNModulePreecooler>().Sum(prc => prc.ValidAttachedIntakes);
                int intakes_open = vessel.FindPartModulesImplementing<ModuleResourceIntake>().Where(mre => mre.intakeEnabled).Count();

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

                _maxISP = (float)(Math.Sqrt((double)MyAttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());

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
                if (assprops[0].GetValue("name").Equals(resourcename) && nozzle.getNozzleFlowRate() > 0)
                    engines++;
			}
			return Math.Max(engines, 1);
		}

		// enumeration of the fuel useage rates of all jets on a vessel
		public static double getFuelRateThermalJetsForVessel (Vessel vess, string resourcename) 
        {
            List<INoozle> nozzles = vess.FindPartModulesImplementing<INoozle>();
			int engines = 0;
			bool updating = true;
            foreach (INoozle nozzle in nozzles) 
            {
				ConfigNode[] prop_node = nozzle.getPropellants ();

                if (prop_node == null) continue; 

				ConfigNode[] assprops = prop_node [nozzle.Fuel_mode].GetNodes ("PROPELLANT");

                if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

				if (!nozzle.Static_updating2) 
					updating = false;
							
				if (nozzle.getNozzleFlowRate () > 0) 
					engines++;
			}

			if (updating) 
            {
				double enum_rate = 0;
                foreach (INoozle nozzle in nozzles) 
                {
					ConfigNode[] prop_node = nozzle.getPropellants ();

                    if (prop_node == null) continue;

                    ConfigNode[] assprops = prop_node [nozzle.Fuel_mode].GetNodes ("PROPELLANT");

                    if (prop_node[nozzle.Fuel_mode] == null || !assprops[0].GetValue("name").Equals(resourcename)) continue;

					enum_rate += nozzle.getNozzleFlowRate ();
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
                : MyAttachedReactor.CoreTemperature < coretempthreshold
                    ? (MyAttachedReactor.CoreTemperature + lowcoretempbase) / (coretempthreshold + lowcoretempbase)
                    : 1.0 + PluginHelper.HighCoreTempThrustMult * Math.Max(Math.Log10(MyAttachedReactor.CoreTemperature / coretempthreshold), 0);
        }

        private double GetPowerThrustModifier()
        {
            return GameConstants.BaseThrustPowerMultiplier * PluginHelper.GlobalThermalNozzlePowerMaxThrustMult * powerTrustMultiplier;
        }

        private void UpdateRadiusModifier()
        {
            if (MyAttachedReactor != null)
            {
                // re-attach with updated radius
                _myAttachedReactor.DetachThermalReciever(id);
                _myAttachedReactor.AttachThermalReciever(id, radius);

                Fields["vacuumPerformance"].guiActiveEditor = true;
                Fields["radiusModifier"].guiActiveEditor = true;
                Fields["surfacePerformance"].guiActiveEditor = true;

                var heatExchangerThrustDivisor = (float)GetHeatExchangerThrustDivisor();

                radiusModifier = (heatExchangerThrustDivisor * 100.0).ToString("0.00") + "%";

                _maxISP = (float)(Math.Sqrt((double)MyAttachedReactor.CoreTemperature) * (PluginHelper.IspCoreTempMult + IspTempMultOffset) * GetIspPropellantModifier());

                var max_thrust_in_space = GetPowerThrustModifier() * GetHeatThrustModifier() * MyAttachedReactor.MaximumThermalPower / _maxISP / PluginHelper.GravityConstant * heatExchangerThrustDivisor;

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
            if (MyAttachedReactor == null || MyAttachedReactor.getRadius() == 0 || radius == 0 || _myAttachedReactor.GetFractionThermalReciever(id) == 0) return 0;

            var fractionalReactorRadius = Math.Sqrt(Math.Pow(MyAttachedReactor.getRadius(), 2) * _myAttachedReactor.GetFractionThermalReciever(id));

            // scale down thrust if it's attached to the wrong sized reactor
            double heat_exchanger_thrust_divisor = radius > fractionalReactorRadius
                ? fractionalReactorRadius * fractionalReactorRadius / radius / radius
                : normalizeFraction(radius / (float)fractionalReactorRadius, 1f);

            if (!_currentpropellant_is_jet)
            {
                for (int i = 0; i < partDistance; i++)
                    heat_exchanger_thrust_divisor *= MyAttachedReactor.ThermalTransportationEfficiency;
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
