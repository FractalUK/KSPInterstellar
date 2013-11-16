using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin{
	class FNNozzleController : FNResourceSuppliableModule{
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
		[KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
		public string fuelmode;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thrust Limiter")]
		public string thrustLimit;
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
		protected FNThermalSource myAttachedReactor;
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
		protected const float g0 = 9.81f;

		//Static
		static Dictionary<string, double> intake_amounts = new Dictionary<string, double>();
		static Dictionary<string, double> fuel_flow_amounts = new Dictionary<string, double>();


		[KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
		public void TogglePropellant() {
			fuel_mode++;
			if (fuel_mode >= propellants.Length) {
				fuel_mode = 0;
			}
			setupPropellants();
		}

		[KSPEvent(guiActive = true, guiName = "TWR Limiter", active = true)]
		public void ToggleTWRLimiter() {
			if (thrustLimitRatio < 3) {
				thrustLimitRatio++;
			} else {
				thrustLimitRatio = 0;
			}

			float vesselMass = vessel.GetTotalMass ();
		}

		[KSPAction("Toggle Propellant")]
		public void TogglePropellantAction(KSPActionParam param) {
			TogglePropellant();
		}

		[KSPAction("TWR Limiter")]
		public void ToggleTWRLimiterAction(KSPActionParam param) {
			ToggleTWRLimiter();
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitEngine() {
			if (ResearchAndDevelopment.Instance == null) { return;}
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
			upgradePartModule ();
			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
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

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }
			fuel_gauge = part.stackIcon.DisplayInfo();
			myAttachedEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
			engineType = originalName;
			// check whether we have the technologies available to be able to perform an upgrade
			bool manual_upgrade = false;
			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(upgradeTechReq != null) {
					if(PluginHelper.hasTech(upgradeTechReq)) {
						hasrequiredupgrade = true;
					}else if(upgradeTechReq == "none") {
						manual_upgrade = true;
						hasrequiredupgrade = true;
					}
				}else{
					manual_upgrade = true;
					hasrequiredupgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}
			// if engine isn't already initialised, should we upgrade it for free?
			if (engineInit == false) {
				engineInit = true;
				if(hasrequiredupgrade && !manual_upgrade) {
					isupgraded = true;
				}
			}
			// if we can upgrade, let's do so
			if (isupgraded && isJet) {
				upgradePartModule ();
			} else {
				// if not, use basic propellants
				propellants = getPropellants (isJet);
			}
			// find attached thermal source
			foreach (AttachNode attach_node in part.attachNodes) {
				if (attach_node.attachedPart != null) {
					List<FNThermalSource> sources = attach_node.attachedPart.FindModulesImplementing<FNThermalSource> ();
					if (sources.Count > 0) {
						myAttachedReactor = sources.First ();
						if (myAttachedReactor != null) {
							break;
						}
					}
				}
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

				if (thrustLimitRatio == 0) {
					thrustLimit = "Prevent Flameout: " + (atmospheric_limit*100.0).ToString("0.0") + "%";
				} else {
					thrustLimit = "TWR = " + thrustLimitRatio.ToString ("0"); 
				}
                                
			}
		}

		public void updatePropellantBar() {
			//print ("Update Prop Bar");
			float currentpropellant = 0;
			float maxpropellant = 0;

			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(myAttachedEngine.propellants[0].id, partresources);

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
			List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();
			// loop though propellants until we get to the selected one, then set it up
			for (int i = 0; i < assprops.Length; ++i) {
				fuelmode = chosenpropellant.GetValue("guiName");
				ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
				isLFO = bool.Parse(chosenpropellant.GetValue("isLFO"));
				currentpropellant_is_jet = false;
				if(chosenpropellant.HasValue("isJet")) {
					currentpropellant_is_jet = bool.Parse(chosenpropellant.GetValue("isJet"));
				}
				//print (currentpropellant_is_jet);

				ModuleEngines.Propellant curprop = new ModuleEngines.Propellant();
				curprop.Load(assprops[i]);
				if (curprop.drawStackGauge) {
					curprop.drawStackGauge = false;
					if (currentpropellant_is_jet) {
						fuel_gauge.SetMessage("Atmosphere");
					}else {
						fuel_gauge.SetMessage(curprop.name);
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

			// should we switch to another propellant because we have none of this one?
			bool next_propellant = false;
			List<ModuleEngines.Propellant> curEngine_propellants_list = new List<ModuleEngines.Propellant>();
			curEngine_propellants_list = myAttachedEngine.propellants;
			foreach(ModuleEngines.Propellant curEngine_propellant in curEngine_propellants_list) {
				List<PartResource> partresources = new List<PartResource>();
				part.GetConnectedResources(curEngine_propellant.id, partresources);

				if (partresources.Count == 0 || !PartResourceLibrary.Instance.resourceDefinitions.Contains(list_of_propellants[0].name)) {
					next_propellant = true;
				} 
			}
			// do the switch if needed
			if (next_propellant && fuel_mode != 1) {
				TogglePropellant();
			}

		}

		public void updateIspEngineParams() {
			// recaculate ISP based on power and core temp available
			FloatCurve newISP = new FloatCurve();
			FloatCurve vCurve = new FloatCurve ();
			maxISP = (float)(Math.Sqrt ((double)myAttachedReactor.getThermalTemp ()) * 17.0 * ispMultiplier);
			if (!currentpropellant_is_jet) {
				minISP = maxISP * 0.4f;
				newISP.Add (0, maxISP, 0, 0);
				newISP.Add (1, minISP, 0, 0);
				myAttachedEngine.useVelocityCurve = false;
				myAttachedEngine.useEngineResponseTime = false;
			} else {
				if (myAttachedReactor.shouldScaleDownJetISP ()) {
					maxISP = maxISP*2.0f/3.0f;
					if (maxISP > 300) {
						maxISP = maxISP / 2.5f;
					}
				}
				newISP.Add(0, maxISP*2.0f/3.0f);
				newISP.Add(0.3f, maxISP);
				newISP.Add(1, maxISP/2.0f);
				vCurve.Add(0, 0.7f);
				vCurve.Add(maxISP*g0*1.0f/3.0f, 0.8f);
				vCurve.Add(maxISP*g0*2.0f/3.0f, 0.9f);
				vCurve.Add (maxISP*g0, 0);
				myAttachedEngine.useVelocityCurve = true;
				myAttachedEngine.useEngineResponseTime = true;
				myAttachedEngine.ignitionThreshold = 0.01f;
			}

			myAttachedEngine.atmosphereCurve = newISP;
			myAttachedEngine.velocityCurve = vCurve;
			assThermalPower = myAttachedReactor.getThermalPower();
		}

		public float getAtmosphericLimit() {
			float atmospheric_limit = 1.0f;
            if (currentpropellant_is_jet) {
                string resourcename = myAttachedEngine.propellants[0].name;
                double currentintakeatm = getIntakeAvailable(vessel, resourcename);
                if (getFuelRateThermalJetsForVessel(vessel, resourcename) > 0) {
                    // divide current available intake resource by fuel useage across all engines
                    atmospheric_limit = (float)Math.Min(currentintakeatm / (getFuelRateThermalJetsForVessel(vessel, resourcename) + currentintakeatm), 1.0);
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

		public override void OnFixedUpdate() {
			//tell static helper methods we are currently updating things
			if (!myAttachedReactor.isActive()) {
				myAttachedReactor.enableIfPossible();
			}
			if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0 && myAttachedReactor != null) {
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
				double thermal_power_received = consumeFNResource (assThermalPower * TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle*atmospheric_limit, FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
				consumeFNResource (thermal_power_received * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
				float power_ratio = 0.0f;
				double engineMaxThrust = 0.01;
				if (assThermalPower > 0) {
					power_ratio = (float)(thermal_power_received / assThermalPower);
					engineMaxThrust = Math.Max(2000.0 * thermal_power_received / maxISP / g0 * heat_exchanger_thrust_divisor*ispratio/myAttachedEngine.currentThrottle,0.01);
				} 
				//print ("B: " + engineMaxThrust);
				// set up TWR limiter if on
				double thrust_limit = vessel.GetTotalMass () * thrustLimitRatio * g0;
				double engine_thrust = engineMaxThrust;
				if (thrustLimitRatio > 0) {
					engine_thrust = Math.Min (engineMaxThrust, thrust_limit);
				}
				// engine thrust fixed
				//print ("A: " + engine_thrust*myAttachedEngine.velocityCurve.Evaluate((float)vessel.srf_velocity.magnitude));
                if (!double.IsInfinity(engine_thrust) && !double.IsNaN(engine_thrust)) {
                    if (isLFO) {
                        myAttachedEngine.maxThrust = (float)(2.0 * engine_thrust);
                    } else {
                        myAttachedEngine.maxThrust = (float)engine_thrust;
                    }
                } else {
                    myAttachedEngine.maxThrust = 0.000001f;
                }
				// control fx groups
				foreach (FXGroup fx_group in part.fxGroups) {
					fx_group.Power = powerRatio;
				} 
				// amount of fuel being used at max throttle with no atmospheric limits
				//fuel_flow_rate = 2000.0*assThermalPower/maxISP/g0/g0/currentIsp*heat_exchanger_thrust_divisor*ispratio*TimeWarp.fixedDeltaTime/0.005;
                if (current_isp > 0 && atmospheric_limit > 0) {
                    fuel_flow_rate = engine_thrust / current_isp / g0 / 0.005 * TimeWarp.fixedDeltaTime/atmospheric_limit;
                }

                foreach (FXGroup fx_group in myAttachedEngine.part.fxGroups) {
                    foreach (GameObject fx_emitter in fx_group.fxEmitters) {
                        if (fx_emitter.name.IndexOf("fx_exhaustFlame_blue") != -1) {
                            if (currentpropellant_is_jet) {
                                fx_emitter.SetActive(false);
                            } else {
                                fx_emitter.SetActive(true);
                            }
                        }
                    }
                }
			} else {
                if (myAttachedEngine.realIsp > 0) {
                    atmospheric_limit = getAtmosphericLimit();
                    fuel_flow_rate = myAttachedEngine.maxThrust / myAttachedEngine.realIsp / g0 / 0.005 * TimeWarp.fixedDeltaTime;
                    if (atmospheric_limit > 0 && atmospheric_limit <= 1) {
                        myAttachedEngine.maxThrust = myAttachedEngine.maxThrust * atmospheric_limit;
                    }
                }else {
                    fuel_flow_rate = 0;
                }
                
				if (myAttachedReactor == null && myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0) {
					myAttachedEngine.Events ["Shutdown"].Invoke ();
					ScreenMessages.PostScreenMessage ("Engine Shutdown: No thermal power source attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				}
			}

			static_updating = true;
			static_updating2 = true;
		}

		public override string GetInfo() {
			bool upgraded = false;
			if (HighLogic.CurrentGame != null) {
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
					if (PluginHelper.hasTech (upgradeTechReq)) {
						upgraded = true;
					}
				} else {
					upgraded = true;
				}
			} else {
				upgraded = false;
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

				List<PartResource> partresources = new List<PartResource> ();
				vess.rootPart.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition (resourcename).id, partresources);
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

		public static string getPropellantFilePath(bool isJet) {
			if (isJet) {
				return KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/IntakeEnginePropellants.cfg";
			}else {
				return KSPUtil.ApplicationRootPath + "GameData/WarpPlugin/EnginePropellants.cfg";
			}
		}

		public static ConfigNode[] getPropellants(bool isJet) {
			ConfigNode config = ConfigNode.Load(getPropellantFilePath(isJet));
			ConfigNode[] propellantlist = config.GetNodes("PROPELLANTS");

			if (config == null) {
				PluginHelper.showInstallationErrorMessage ();
			}

			return propellantlist;
		}

		public static ConfigNode[] getPropellantsHybrid() {
			ConfigNode config = ConfigNode.Load(getPropellantFilePath(true));
			ConfigNode config2 = ConfigNode.Load(getPropellantFilePath(false));
			ConfigNode[] propellantlist = config.GetNodes("PROPELLANTS");
			ConfigNode[] propellantlist2 = config2.GetNodes("PROPELLANTS");
			propellantlist = propellantlist.Concat(propellantlist2).ToArray();

			if (config == null || config2 == null) {
				PluginHelper.showInstallationErrorMessage ();
			}

			return propellantlist;
		}



	}
}