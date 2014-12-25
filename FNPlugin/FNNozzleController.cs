extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin{
    class FNNozzleController : FNResourceSuppliableModule, IUpgradeableModule {
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

		//Persistent False
		[KSPField(isPersistant = false)]
		public bool isJet = false;
		[KSPField(isPersistant = false)]
		public float upgradeCost;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public float radius; 
		[KSPField(isPersistant = false)]
		public string upgradeTechReq = null;

		//External
		public bool static_updating = true;
		public bool static_updating2 = true;

		//GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string engineType = ":";
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Fuel Mode")]
		public string fuelmode;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade Cost")]
		public string upgradeCostStr;

		//Internal
		protected float maxISP;
		protected float minISP;
		protected float assThermalPower;
		protected float powerRatio = 0.358f;
		protected float engineMaxThrust;
		protected bool isLFO = false;
		protected float ispMultiplier = 1;
		protected ConfigNode[] propellants;
		protected VInfoBox fuel_gauge;
		protected bool hasrequiredupgrade = false;
		protected bool hasstarted = false;
		protected ModuleEngines myAttachedEngine;
		protected IThermalSource myAttachedReactor;
		protected bool currentpropellant_is_jet = false;
		protected double fuel_flow_rate = 0;
		protected int thrustLimitRatio = 0;
		protected double old_thermal_power = 0;
		protected double old_isp = 0;
		protected double current_isp = 0;
		protected double old_intake = 0;
        protected bool flameFxOn = true;
        protected float atmospheric_limit;
        protected float old_atmospheric_limit;

		//Constants
		protected const double g0 = 9.82;
        protected const double isp_temp_rat = 22.371670613;

		//Static
		static Dictionary<string, double> intake_amounts = new Dictionary<string, double>();
		static Dictionary<string, double> fuel_flow_amounts = new Dictionary<string, double>();

        public String UpgradeTechnology { get { return upgradeTechReq; } }

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Propellant", active = true)]
		public void TogglePropellant() {
			fuel_mode++;
			if (fuel_mode >= propellants.Length) {
				fuel_mode = 0;
			}
			setupPropellants();
		}
        
		[KSPAction("Toggle Propellant")]
		public void TogglePropellantAction(KSPActionParam param) {
			TogglePropellant();
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitEngine() {
			if (ResearchAndDevelopment.Instance == null) { return;}
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
			upgradePartModule ();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
		}

		public void upgradePartModule() {
			engineType = upgradedName;
			propellants = FNNozzleController.getPropellantsHybrid();
			isHybrid = true;
			isupgraded = true;
		}

		public ConfigNode[] getPropellants() {
			return propellants;
		}

        public void OnEditorAttach() {
            foreach (AttachNode attach_node in part.attachNodes) {
                if (attach_node.attachedPart != null) {
                    List<IThermalSource> sources = attach_node.attachedPart.FindModulesImplementing<IThermalSource>();
                    if (sources.Count > 0) {
                        myAttachedReactor = sources.First();
                        if (myAttachedReactor != null) {
                            break;
                        }
                    }
                }
            }
            estimateEditorPerformance();
        }

		public override void OnStart(PartModule.StartState state) {
            engineType = originalName;
            myAttachedEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            // find attached thermal source
            foreach (AttachNode attach_node in part.attachNodes) {
                if (attach_node.attachedPart != null) {
                    List<IThermalSource> sources = attach_node.attachedPart.FindModulesImplementing<IThermalSource>();
                    if (sources.Count > 0) {
                        myAttachedReactor = sources.First();
                        if (myAttachedReactor != null) {
                            break;
                        }
                    }
                }
            }

            if (state == StartState.Editor) {
                part.OnEditorAttach += OnEditorAttach;
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
			fuel_gauge = part.stackIcon.DisplayInfo();
			
            // if engine isn't already initialised, initialise it
			if (engineInit == false) {
				engineInit = true;
			}
			// if we can upgrade, let's do so
			if (isupgraded && isJet) {
				upgradePartModule ();
			} else {
                if (this.HasTechsRequiredToUpgrade() && isJet)
                {
                    hasrequiredupgrade = true;
                }
				// if not, use basic propellants
				propellants = getPropellants (isJet);
			}
			
			setupPropellants();
			hasstarted = true;

			//print ("Start Complete");
		}

		public override void OnUpdate() {
			//update GUI stuff
			Fields["engineType"].guiActive = isJet;
			if (ResearchAndDevelopment.Instance != null && isJet) {
				Events ["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
				upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
			} else {
				Events ["RetrofitEngine"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded  && hasrequiredupgrade && isJet;

			if (myAttachedEngine != null) {
				if (myAttachedEngine.isOperational && !IsEnabled) {
					IsEnabled = true;
					part.force_activate ();
				}
				updatePropellantBar ();
			}
		}
        
		public void updatePropellantBar() {
			//print ("Update Prop Bar");
			float currentpropellant = 0;
			float maxpropellant = 0;

            List<PartResource> partresources = part.GetConnectedResources(myAttachedEngine.propellants.FirstOrDefault().name).ToList();
			
			foreach (PartResource partresource in partresources) {
				currentpropellant += (float) partresource.amount;
				maxpropellant += (float)partresource.maxAmount;
			}

			if (fuel_gauge != null  && fuel_gauge.infoBoxRef != null) {

				if (myAttachedEngine.isOperational) {
					if (!fuel_gauge.infoBoxRef.expanded) {
						fuel_gauge.infoBoxRef.Expand ();
					}
					fuel_gauge.length = 2;
					if (maxpropellant > 0) {
						fuel_gauge.SetValue (currentpropellant / maxpropellant);
					} else {
						fuel_gauge.SetValue (0);
					}
				} else {
					if (!fuel_gauge.infoBoxRef.collapsed) {
						fuel_gauge.infoBoxRef.Collapse ();
					}
				}
			}

			//print ("Update Prop Bar Complete");
		}

		public void setupPropellants() {
			ConfigNode chosenpropellant = propellants[fuel_mode];
			ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
			List<Propellant> list_of_propellants = new List<Propellant>();
			// loop though propellants until we get to the selected one, then set it up
			foreach(ConfigNode prop_node in assprops) 
            {
				fuelmode = chosenpropellant.GetValue("guiName");
				ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
				isLFO = bool.Parse(chosenpropellant.GetValue("isLFO"));
				currentpropellant_is_jet = false;
				if(chosenpropellant.HasValue("isJet")) {
					currentpropellant_is_jet = bool.Parse(chosenpropellant.GetValue("isJet"));
				}
				//print (currentpropellant_is_jet);

				Propellant curprop = new Propellant();
				curprop.Load(prop_node);
				if (curprop.drawStackGauge && HighLogic.LoadedSceneIsFlight) {
					curprop.drawStackGauge = false;
					if (currentpropellant_is_jet) {
						fuel_gauge.SetMessage("Atmosphere");
					}else {
						fuel_gauge.SetMessage(curprop.name);
                        myAttachedEngine.thrustPercentage = 100;
                        part.temperature = 1;
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
			if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {
				myAttachedEngine.propellants.Clear();
				myAttachedEngine.propellants = list_of_propellants;
				myAttachedEngine.SetupPropellant();
			}

            if (HighLogic.LoadedSceneIsFlight) { // you can have any fuel you want in the editor but not in flight
                // should we switch to another propellant because we have none of this one?
                bool next_propellant = false;
                List<Propellant> curEngine_propellants_list = new List<Propellant>();
                curEngine_propellants_list = myAttachedEngine.propellants;
                foreach (Propellant curEngine_propellant in curEngine_propellants_list) {
                    List<PartResource> partresources = part.GetConnectedResources(curEngine_propellant.name).ToList();

                    if (partresources.Count == 0 || !PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name)) {
                        next_propellant = true;
                    }
                }

                // do the switch if needed
                if (next_propellant && fuel_mode != 1) {
                    TogglePropellant();
                }
            } else {
                if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name) && fuel_mode != 1) { // Still ignore propellants that don't exist
                    TogglePropellant();
                }
                estimateEditorPerformance(); // update editor estimates
            }

		}

		public void updateIspEngineParams() {
			// recaculate ISP based on power and core temp available
			FloatCurve newISP = new FloatCurve();
			FloatCurve vCurve = new FloatCurve ();
			maxISP = (float)(Math.Sqrt ((double)myAttachedReactor.CoreTemperature) * isp_temp_rat * ispMultiplier);
            
			if (!currentpropellant_is_jet) {
				minISP = maxISP * 0.4f;
                newISP.Add(0, Mathf.Min(maxISP, 2997.13f), 0, 0);
                newISP.Add(1, Mathf.Min(minISP, 2997.13f), 0, 0);
				myAttachedEngine.useVelocityCurve = false;
				myAttachedEngine.useEngineResponseTime = false;
			} else {
				if (myAttachedReactor.shouldScaleDownJetISP ()) {
					maxISP = maxISP*2.0f/3.0f;
					if (maxISP > 300) {
						maxISP = maxISP / 2.5f;
					}
				}
                newISP.Add(0, Mathf.Min(maxISP * 4.0f / 5.0f, 2997.13f));
                newISP.Add(0.15f, Mathf.Min(maxISP, 2997.13f));
                newISP.Add(0.3f, Mathf.Min(maxISP * 4.0f / 5.0f, 2997.13f));
                newISP.Add(1, Mathf.Min(maxISP * 2.0f / 3.0f, 2997.13f));
				vCurve.Add(0, 1.0f);
				vCurve.Add((float)(maxISP*g0*1.0/3.0), 1.0f);
				vCurve.Add((float)(maxISP*g0), 1.0f);
				vCurve.Add ((float)(maxISP*g0*4.0/3.0), 0);
				myAttachedEngine.useVelocityCurve = true;
				myAttachedEngine.useEngineResponseTime = true;
				myAttachedEngine.ignitionThreshold = 0.01f;
			}

			myAttachedEngine.atmosphereCurve = newISP;
			myAttachedEngine.velocityCurve = vCurve;
			assThermalPower = myAttachedReactor.MaximumPower;
            if (myAttachedReactor is InterstellarFusionReactor) {
                assThermalPower = assThermalPower * 0.95f;
            }
		}

		public float getAtmosphericLimit() {
			float atmospheric_limit = 1.0f;
            if (currentpropellant_is_jet) {
                string resourcename = myAttachedEngine.propellants[0].name;
                double currentintakeatm = getIntakeAvailable(vessel, resourcename);
                if (getFuelRateThermalJetsForVessel(vessel, resourcename) > 0) {
                    // divide current available intake resource by fuel useage across all engines
                    atmospheric_limit = (float)Math.Min(currentintakeatm / (getFuelRateThermalJetsForVessel(vessel, resourcename)), 1.0);
                }
                old_intake = currentintakeatm;
            }
            atmospheric_limit = Mathf.MoveTowards(old_atmospheric_limit, atmospheric_limit, 0.1f);
            old_atmospheric_limit = atmospheric_limit;
			return atmospheric_limit;
		}

		public double getNozzleFlowRate() {
			return fuel_flow_rate;
		}

		public bool getUpdating() {
			return static_updating;
		}

		public bool hasStarted() {
			return hasstarted;
		}

        public void estimateEditorPerformance() {
            bool attached_reactor_upgraded = false;
            FloatCurve atmospherecurve = new FloatCurve();
            float thrust = 0;
            if (myAttachedReactor != null) {
                if (myAttachedReactor is IUpgradeableModule) {
                    IUpgradeableModule upmod = myAttachedReactor as IUpgradeableModule;
                    if (upmod.HasTechsRequiredToUpgrade()) {
                        attached_reactor_upgraded = true;
                    }
                }
                maxISP = (float)(Math.Sqrt((double)myAttachedReactor.CoreTemperature) * isp_temp_rat * ispMultiplier);
                minISP = maxISP * 0.4f;
                atmospherecurve.Add(0, maxISP, 0, 0);
                atmospherecurve.Add(1, minISP, 0, 0);
                thrust = (float)(2 * myAttachedReactor.MaximumPower * 1000 / g0 / maxISP);
                myAttachedEngine.maxThrust = thrust;
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            } else {
                atmospherecurve.Add(0, 0.00001f, 0, 0);
                myAttachedEngine.maxThrust = thrust;
                myAttachedEngine.atmosphereCurve = atmospherecurve;
            }
        }

		public override void OnFixedUpdate() {
            if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0 && myAttachedReactor != null) {
				if (!myAttachedReactor.IsActive) {
					myAttachedReactor.enableIfPossible();
				}
				updateIspEngineParams ();
				float curve_eval_point = (float)Math.Min (FlightGlobals.getStaticPressure (vessel.transform.position), 1.0);
				float currentIsp = myAttachedEngine.atmosphereCurve.Evaluate (curve_eval_point);
				double ispratio = currentIsp / maxISP;
				this.current_isp = currentIsp;
				// scale down thrust if it's attached to the wrong sized reactor
				float heat_exchanger_thrust_divisor = 1;
				if (radius > myAttachedReactor.getRadius ()) {
					heat_exchanger_thrust_divisor = myAttachedReactor.getRadius () * myAttachedReactor.getRadius () / radius / radius;
				} else {
					heat_exchanger_thrust_divisor = radius * radius / myAttachedReactor.getRadius () / myAttachedReactor.getRadius ();
				}

				if (myAttachedReactor.getRadius () == 0 || radius == 0) {
					heat_exchanger_thrust_divisor = 1;
				}
				// get the flameout safety limit
				atmospheric_limit = getAtmosphericLimit ();
                double thrust_limit = myAttachedEngine.thrustPercentage / 100;
                if (currentpropellant_is_jet) {
                    int pre_coolers_active = vessel.FindPartModulesImplementing<FNModulePreecooler>().Where(prc => prc.isFunctional()).Count();
                    int intakes_open = vessel.FindPartModulesImplementing<ModuleResourceIntake>().Where(mre => mre.intakeEnabled).Count();
                    double proportion = Math.Pow((double)(intakes_open - pre_coolers_active) / (double)intakes_open, 0.1);
                    if (double.IsNaN(proportion) || double.IsInfinity(proportion)) {
                        proportion = 1;
                    }
                    float temp = (float)Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * proportion, 1);
                    if (temp > part.maxTemp - 10.0f)
                    {
                        ScreenMessages.PostScreenMessage("Engine Shutdown: Catastrophic overheating was imminent!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        myAttachedEngine.Shutdown();
                        part.temperature = 1;
                    } else
                    {
                        part.temperature = temp;
                    }
                    //myAttachedEngine.DeactivateRunningFX();
                } else {
                    //myAttachedEngine.ActivateRunningFX();
                }
                double thermal_consume_total = assThermalPower * TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle * atmospheric_limit;
                double thermal_power_received = consumeFNResource(thermal_consume_total, FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
                if (thermal_power_received * TimeWarp.fixedDeltaTime < thermal_consume_total) {
                    thermal_power_received += consumeFNResource(thermal_consume_total-thermal_power_received*TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
                }
				consumeFNResource (thermal_power_received * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
				float power_ratio = 0.0f;
				double engineMaxThrust = 0.01;
				if (assThermalPower > 0) {
					power_ratio = (float)(thermal_power_received / assThermalPower);
					engineMaxThrust = Math.Max(thrust_limit*2000.0 * thermal_power_received / maxISP / g0 * heat_exchanger_thrust_divisor*ispratio/myAttachedEngine.currentThrottle,0.01);
				} 
				//print ("B: " + engineMaxThrust);
				// set up TWR limiter if on
                //double additional_thrust_compensator = myAttachedEngine.finalThrust / (myAttachedEngine.maxThrust * myAttachedEngine.currentThrottle)/ispratio;
				double engine_thrust = engineMaxThrust/myAttachedEngine.thrustPercentage*100;
				// engine thrust fixed
				//print ("A: " + engine_thrust*myAttachedEngine.velocityCurve.Evaluate((float)vessel.srf_velocity.magnitude));
                if (!double.IsInfinity(engine_thrust) && !double.IsNaN(engine_thrust)) {
                    if (isLFO) {
                        myAttachedEngine.maxThrust = (float)(2.2222 * engine_thrust);
                    } else {
                        myAttachedEngine.maxThrust = (float)engine_thrust;
                    }
                } else {
                    myAttachedEngine.maxThrust = 0.000001f;
                }
								
				// amount of fuel being used at max throttle with no atmospheric limits
                if (current_isp > 0) {
                    double vcurve_at_current_velocity = 1;
                    
                    if (myAttachedEngine.useVelocityCurve && myAttachedEngine.velocityCurve != null) {
                        vcurve_at_current_velocity = myAttachedEngine.velocityCurve.Evaluate((float)vessel.srf_velocity.magnitude);
                    }
                    //if (!double.IsNaN(additional_thrust_compensator) && !double.IsInfinity(additional_thrust_compensator)) {
                        //vcurve_at_current_velocity = additional_thrust_compensator;
                    //}
                    fuel_flow_rate = engine_thrust / current_isp / g0 / 0.005 * TimeWarp.fixedDeltaTime;
                    if (vcurve_at_current_velocity > 0 && !double.IsInfinity(vcurve_at_current_velocity) && !double.IsNaN(vcurve_at_current_velocity)) {
                        fuel_flow_rate = fuel_flow_rate / vcurve_at_current_velocity;
                    }

                    if (atmospheric_limit > 0 && !double.IsInfinity(atmospheric_limit) && !double.IsNaN(atmospheric_limit)) {
                        fuel_flow_rate = fuel_flow_rate / atmospheric_limit;
                    }
                }

			} else {
                if (myAttachedEngine.realIsp > 0) {
                    atmospheric_limit = getAtmosphericLimit();
                    double vcurve_at_current_velocity = 1;
                    if (myAttachedEngine.useVelocityCurve) {
                        vcurve_at_current_velocity = myAttachedEngine.velocityCurve.Evaluate((float)vessel.srf_velocity.magnitude);
                    }
                    fuel_flow_rate = myAttachedEngine.maxThrust / myAttachedEngine.realIsp / g0 / 0.005 * TimeWarp.fixedDeltaTime;
                    if (vcurve_at_current_velocity > 0 && !double.IsInfinity(vcurve_at_current_velocity) && !double.IsNaN(vcurve_at_current_velocity)) {
                        fuel_flow_rate = fuel_flow_rate / vcurve_at_current_velocity;
                    }
                }else {
                    fuel_flow_rate = 0;
                }

                if (currentpropellant_is_jet) {
                    part.temperature = 1;
                }
                
				if (myAttachedReactor == null && myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0) {
					myAttachedEngine.Events ["Shutdown"].Invoke ();
					ScreenMessages.PostScreenMessage ("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				}
			}
            //tell static helper methods we are currently updating things
			static_updating = true;
			static_updating2 = true;
		}

		public override string GetInfo() {
			bool upgraded = false;
            if (this.HasTechsRequiredToUpgrade())
            {
                upgraded = true;
            }
			ConfigNode[] prop_nodes;
			if(upgraded && isJet) {
				prop_nodes = getPropellantsHybrid();
			}else{
				prop_nodes = getPropellants(isJet);
			}
			string return_str = "Thrust: Variable\n";
			foreach (ConfigNode propellant_node in prop_nodes) {
				float ispMultiplier = float.Parse(propellant_node.GetValue("ispMultiplier"));
				string guiname = propellant_node.GetValue("guiName");
				return_str = return_str + "--" + guiname + "--\n" + "ISP: " + ispMultiplier.ToString ("0.00") + " x 17 x Sqrt(Core Temperature)" + "\n";
			}
			return return_str;
		}

        public override int getPowerPriority() {
            return 1;
        }

		// Static Methods
		// Amount of intake air available to use of a particular resource type
		public static double getIntakeAvailable(Vessel vess, string resourcename) {
			List<FNNozzleController> nozzles = vess.FindPartModulesImplementing<FNNozzleController> ();
			bool updating = true;
			foreach (FNNozzleController nozzle in nozzles) {
				if (!nozzle.static_updating) {
					updating = false;
					break;
				}
			}

			if (updating) {
				foreach (FNNozzleController nozzle in nozzles) {
					nozzle.static_updating = false;
				}

				List<PartResource> partresources = vess.rootPart.GetConnectedResources (resourcename).ToList();
				double currentintakeatm = 0;
				foreach (PartResource partresource in partresources) {
					currentintakeatm += partresource.amount;
				}

				if (intake_amounts.ContainsKey (resourcename)) {
					intake_amounts [resourcename] = currentintakeatm;
				} else {
					intake_amounts.Add (resourcename, currentintakeatm);
				}

			}

			if (intake_amounts.ContainsKey (resourcename)) {
				double intake_to_return = Math.Max (intake_amounts [resourcename], 0);
				return intake_to_return;
			}

			return 0.00001;
		}

		// enumeration of the fuel useage rates of all jets on a vessel
		public static int getEnginesRunningOfTypeForVessel (Vessel vess, string resourcename) {
			List<FNNozzleController> nozzles = vess.FindPartModulesImplementing<FNNozzleController> ();
			int engines = 0;
			foreach (FNNozzleController nozzle in nozzles) {
				ConfigNode[] prop_node = nozzle.getPropellants ();
				if (prop_node != null) {
					ConfigNode[] assprops = prop_node [nozzle.fuel_mode].GetNodes ("PROPELLANT");
					if (prop_node [nozzle.fuel_mode] != null) {
						if (assprops [0].GetValue ("name").Equals (resourcename)) {
							if (nozzle.getNozzleFlowRate () > 0) {
								engines++;
							}
						}
					}
				}
			}
			return engines;
		}

		// enumeration of the fuel useage rates of all jets on a vessel
		public static double getFuelRateThermalJetsForVessel (Vessel vess, string resourcename) {
			List<FNNozzleController> nozzles = vess.FindPartModulesImplementing<FNNozzleController> ();
			int engines = 0;
			bool updating = true;
			foreach (FNNozzleController nozzle in nozzles) {
				ConfigNode[] prop_node = nozzle.getPropellants ();
				if (prop_node != null) {
					ConfigNode[] assprops = prop_node [nozzle.fuel_mode].GetNodes ("PROPELLANT");
					if (prop_node [nozzle.fuel_mode] != null) {
						if (assprops [0].GetValue ("name").Equals (resourcename)) {
							if (!nozzle.static_updating2) {
								updating = false;
							}
							if (nozzle.getNozzleFlowRate () > 0) {
								engines++;
							}
						}
					}
				}
			}

			if (updating) {
				double enum_rate = 0;
				foreach (FNNozzleController nozzle in nozzles) {
					ConfigNode[] prop_node = nozzle.getPropellants ();
					if (prop_node != null) {
						ConfigNode[] assprops = prop_node [nozzle.fuel_mode].GetNodes ("PROPELLANT");
						if (prop_node [nozzle.fuel_mode] != null) {
							if (assprops [0].GetValue ("name").Equals (resourcename)) {
								enum_rate += nozzle.getNozzleFlowRate ();
								nozzle.static_updating2 = false;
							}
						}
					}
				}
				if (fuel_flow_amounts.ContainsKey (resourcename)) {
					fuel_flow_amounts [resourcename] = enum_rate;
				} else {
					fuel_flow_amounts.Add (resourcename, enum_rate);
				}
				//print (enum_rate);
			}

			if (fuel_flow_amounts.ContainsKey (resourcename)) {
				return fuel_flow_amounts [resourcename];
			}

			return 0.1;
		}


        public static ConfigNode[] getPropellants(bool isJet) {
            ConfigNode[] propellantlist;
            if (isJet) {
                propellantlist = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT");
            } else {
                propellantlist = GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");
            }

            if (propellantlist == null) {
                PluginHelper.showInstallationErrorMessage();
            }

            return propellantlist;
        }

        public static ConfigNode[] getPropellantsHybrid() {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_NTR_PROPELLANT");
            ConfigNode[] propellantlist2 = GameDatabase.Instance.GetConfigNodes("BASIC_NTR_PROPELLANT");
            propellantlist = propellantlist.Concat(propellantlist2).ToArray();
            if (propellantlist == null || propellantlist2 == null) {
                PluginHelper.showInstallationErrorMessage();
            }
            return propellantlist;
        }



	}
}