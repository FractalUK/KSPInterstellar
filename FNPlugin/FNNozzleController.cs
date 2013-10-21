using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    class FNNozzleController : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool isHybrid = false;
        [KSPField(isPersistant = false)]
        public bool isJet = false;
        

        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
        public string fuelmode;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
		public string powerConsumptionStr = ":";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thrust Limiter")]
		public string thrustLimit;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
		[KSPField(isPersistant = false)]
		public float radius; 
		[KSPField(isPersistant = true)]
		public bool engineInit = false;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq = null;

        private float maxISP;
        private float minISP;
        private float assThermalPower;
        private float powerRatio = 0.358f;
        private float engineMaxThrust;
        private bool isLFO = false;
        private float ispMultiplier = 1;
        private ConfigNode[] propellants;
        private VInfoBox fuel_gauge;
		protected bool hasrequiredupgrade = false;
        
		protected bool hasstarted = false;
        protected float myScience = 0;
		
		private int thrustLimitRatio = 0;
		private float thrustLimiter = 0;
		private float intakeAtmThrustLimiter = 1;
		bool engineAutoShutdown = false;

		protected int shutdown_counter = 0;

        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
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

		[KSPEvent(guiActive = true, guiName = "TWR Limiter", active = true)]
		public void TWRLimiter() {
			if (thrustLimitRatio < 3) {
				thrustLimitRatio++;
			} else {
				thrustLimitRatio = 0;
			}

			float vesselMass = vessel.GetTotalMass ();

			if (thrustLimitRatio > 0) {
				thrustLimiter = vesselMass * thrustLimitRatio * 9.81f;
			} else {
				thrustLimiter = 0;
			}

			print ("Warp vesselMass: " + vesselMass.ToString() + " thrustLimiter " + thrustLimiter.ToString());

			setupPropellants();

		}

		[KSPAction("TWR Limiter")]
		public void TWRLimiterAction(KSPActionParam param) {
			TWRLimiter();
		}

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; } // || !hasScience || myScience < upgradeCost) { return; }
            isupgraded = true;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                engineType = upgradedName;
                propellants = FNNozzleController.getPropellantsHybrid();
                isHybrid = true;
            }

			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;

        }

        public void setupPropellants() {
			//skip calculations on other vessels
			if (vessel != FlightGlobals.ActiveVessel)
			{
				return;
			}
            ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
            ConfigNode chosenpropellant = propellants[fuel_mode];
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();
            
            //VStackIcon stackicon = new VStackIcon(part);
            bool currentpropellant_is_jet = false;
            //bool currentpropellant_is_electric = false;
            //part.stackIcon.RemoveInfo(fuel_gauge);
            //part.stackIcon.ClearInfoBoxes();
            //part.stackIcon.DisplayInfo().
                        
            for (int i = 0; i < assprops.Length; ++i) {
                fuelmode = chosenpropellant.GetValue("guiName");
                ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
                isLFO = bool.Parse(chosenpropellant.GetValue("isLFO"));
                if(chosenpropellant.HasValue("isJet")) {
                    currentpropellant_is_jet = bool.Parse(chosenpropellant.GetValue("isJet"));
                }
                
                ModuleEngines.Propellant curprop = new ModuleEngines.Propellant();
                curprop.Load(assprops[i]);
                if (curprop.drawStackGauge) {
                    curprop.drawStackGauge = false;
                    if (currentpropellant_is_jet) {
                        //print("Atmosphere");
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

			int activeEngines = 0;
			List<FNNozzleController> fnncs = vessel.FindPartModulesImplementing<FNNozzleController>();
			foreach (FNNozzleController fnnc in fnncs) {
				var me = fnnc.part.Modules["ModuleEngines"] as ModuleEngines;
				if (me.isOperational) {
					activeEngines++;
				}
			}

			float availableAtmosphere = 0;
			List<AtmosphericIntake> ais = vessel.FindPartModulesImplementing<AtmosphericIntake>();
			foreach (AtmosphericIntake ai in ais) {
				if (ai.getAtmosphericOutput () > 0) {
					if (activeEngines > 0) {
						availableAtmosphere += ai.getAtmosphericOutput () / activeEngines;
					} else {
						availableAtmosphere += ai.getAtmosphericOutput ();
					}
				}
			}

            Part[] childParts = this.part.FindChildParts<Part>(true);
            PartModuleList childModules;
            for (int i = 0; i < childParts.Length; ++i) {
                childModules = childParts.ElementAt(i).Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
					var thisModule3 = thisModule as FNMicrowaveThermalHeatExchanger;
					if (thisModule2 != null) {
						FNReactor fnr = (FNReactor)thisModule;
						FloatCurve newISP = new FloatCurve ();
						FloatCurve vCurve = new FloatCurve ();
						if (!currentpropellant_is_jet) {
							maxISP = (float)Math.Sqrt ((double)fnr.getReactorTemp ()) * 17 * ispMultiplier;
							minISP = maxISP * 0.4f;
							newISP.Add (0, maxISP, 0, 0);
							newISP.Add (1, minISP, 0, 0);
							curEngine.useVelocityCurve = false;
							curEngine.useEngineResponseTime = false;
						} else {
							if (thisModule2.getIsNuclear ()) {
								maxISP = 150;
								newISP.Add (0, 100);
								newISP.Add (0.3f, 150);
								newISP.Add (1, 75);
								vCurve.Add (0, 1);
								vCurve.Add (400, 0.8f);
								vCurve.Add (950, 0.9f);
								vCurve.Add (1471, 0);
							} else {
								maxISP = 2500;
								newISP.Add (0, 1200);
								newISP.Add (0.3f, 2500);
								newISP.Add (1, 800);
								vCurve.Add (0, 1);
								vCurve.Add (400, 0.8f);
								vCurve.Add (1000, 0.9f);
								vCurve.Add (2000, 0.5f);
							}

							curEngine.useVelocityCurve = true;
							curEngine.useEngineResponseTime = true;
						}
						//ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
						curEngine.atmosphereCurve = newISP;
						curEngine.velocityCurve = vCurve;
						assThermalPower = fnr.getReactorThermalPower ();
						engineMaxThrust = 2000 * assThermalPower / maxISP / 9.81f;

						float heat_exchanger_thrust_divisor = 1;
						if (radius > fnr.getRadius ()) {
							heat_exchanger_thrust_divisor = fnr.getRadius () * fnr.getRadius () / radius / radius;
						} else {
							heat_exchanger_thrust_divisor = radius * radius / fnr.getRadius () / fnr.getRadius ();
						}

						engineMaxThrust = engineMaxThrust * heat_exchanger_thrust_divisor;

						if (isLFO) {
							engineMaxThrust = engineMaxThrust * 1.5f;
						}
						curEngine.maxThrust = engineMaxThrust;

						print ("warp: Reactor MaxThrust: " + engineMaxThrust.ToString () + " ThermalPower: " + assThermalPower.ToString ());
					}
					else if (thisModule3 != null) {
						FNMicrowaveThermalHeatExchanger mthe = (FNMicrowaveThermalHeatExchanger)thisModule;
						FloatCurve newISP = new FloatCurve ();
						FloatCurve vCurve = new FloatCurve ();
						if (!currentpropellant_is_jet) {
							maxISP = (float)Math.Sqrt ((double)mthe.getThermalPower() * 4) * 17 * ispMultiplier;
							minISP = maxISP * 0.4f;
							newISP.Add (0, maxISP, 0, 0);
							newISP.Add (1, minISP, 0, 0);
							curEngine.useVelocityCurve = false;
							curEngine.useEngineResponseTime = false;
						} else {
							maxISP = 2500;
							newISP.Add (0, 1200);
							newISP.Add (0.3f, 2500);
							newISP.Add (1, 800);
							vCurve.Add (0, 1);
							vCurve.Add (400, 0.8f);
							vCurve.Add (1000, 0.9f);
							vCurve.Add (2000, 0.5f);
							curEngine.useVelocityCurve = true;
							curEngine.useEngineResponseTime = true;
						}
						//ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
						curEngine.atmosphereCurve = newISP;
						curEngine.velocityCurve = vCurve;
						assThermalPower = mthe.getThermalPower ();
						if (thrustLimitRatio == 0 || 54 * (assThermalPower / 275) <= thrustLimiter) {
							engineMaxThrust = 54 * (assThermalPower / 275);
							thrustLimit = "Max thrust";
						} else {
							thrustLimit = thrustLimitRatio.ToString () + ":1";
							engineMaxThrust = thrustLimiter;
						}

						float heat_exchanger_thrust_divisor = 1;
						if (radius > mthe.getRadius ()) {
							heat_exchanger_thrust_divisor = mthe.getRadius () * mthe.getRadius () / radius / radius;
						} else {
							heat_exchanger_thrust_divisor = radius * radius / mthe.getRadius () / mthe.getRadius ();
						}

						engineMaxThrust = engineMaxThrust * heat_exchanger_thrust_divisor;

						if (isLFO) {
							engineMaxThrust = engineMaxThrust * 1.5f;
						}
						curEngine.maxThrust = engineMaxThrust;

						//float atmosphereRequired = engineMaxThrust / (curEngine.realIsp * 9.81f);

						//print ("warp: Microwave MaxThrust: " + engineMaxThrust.ToString () + " ThermalPower: " + assThermalPower.ToString () + " Engine ISP " + curEngine.realIsp + " Atmosphere Required: " + atmosphereRequired.ToString());
					}

                }

            }

            Part parent = this.part.parent;
            if (parent != null) {
                childModules = parent.Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
					var thisModule3 = thisModule as FNMicrowaveThermalHeatExchanger;
                    if (thisModule2 != null) {
                        FNReactor fnr = (FNReactor)thisModule;
                        FloatCurve newISP = new FloatCurve();
						FloatCurve vCurve = new FloatCurve ();
                        if (!currentpropellant_is_jet) {
                            maxISP = (float)Math.Sqrt((double)fnr.getReactorTemp()) * 17 * ispMultiplier;
                            minISP = maxISP * 0.4f;
                            newISP.Add(0, maxISP, 0, 0);
                            newISP.Add(1, minISP, 0, 0);
                            curEngine.useVelocityCurve = false;
                            curEngine.useEngineResponseTime = false;
                        }
                        else {
							if (thisModule2.getIsNuclear()) {
								maxISP = 150;
								newISP.Add(0, 100);
								newISP.Add(0.3f, 150);
								newISP.Add(1, 75);
								vCurve.Add(0, 1);
								vCurve.Add(400, 0.8f);
								vCurve.Add(950, 0.9f);
								vCurve.Add (1471, 0);
							} else {
								maxISP = 2500;
								newISP.Add(0, 1200);
								newISP.Add(0.3f, 2500);
								newISP.Add(1, 800);
								vCurve.Add(0, 1);
								vCurve.Add(400, 0.8f);
								vCurve.Add(1000, 0.9f);
								vCurve.Add (2000, 0.5f);
							}
                            curEngine.useVelocityCurve = true;
                            curEngine.useEngineResponseTime = true;
                        }
                        //ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
                        curEngine.atmosphereCurve = newISP;
						curEngine.velocityCurve = vCurve;
                        assThermalPower = fnr.getReactorThermalPower();
                        engineMaxThrust = 2000 * assThermalPower / maxISP / 9.81f;

						float heat_exchanger_thrust_divisor = 1;
						if (radius > fnr.getRadius ()) {
							heat_exchanger_thrust_divisor = fnr.getRadius () * fnr.getRadius () / radius / radius;
						}else{
							heat_exchanger_thrust_divisor = radius * radius / fnr.getRadius () / fnr.getRadius ();
						}

						if (fnr.getRadius () == 0 || radius == 0) {
							heat_exchanger_thrust_divisor = 1;
						}

						engineMaxThrust = engineMaxThrust * heat_exchanger_thrust_divisor;

                        if (isLFO) {
                            engineMaxThrust = engineMaxThrust * 1.5f;
                        }
                        curEngine.maxThrust = engineMaxThrust;
                    }
					else if (thisModule3 != null) {
						FNMicrowaveThermalHeatExchanger mthe = (FNMicrowaveThermalHeatExchanger)thisModule;
						FloatCurve newISP = new FloatCurve ();
						FloatCurve vCurve = new FloatCurve ();
						if (!currentpropellant_is_jet) {
							maxISP = (float)Math.Sqrt ((double)mthe.getThermalPower() * 4) * 17 * ispMultiplier;
							minISP = maxISP * 0.4f;
							// if recieved thermal power decreases below a certain threshhold it will completely deplete fuel supply
							//since the new thrust/throttle limiting code will not engage fast enough to prevent that from happening, this code will prevent ISP reduction beyond that found at around 40MW of power
							if (maxISP < 215 || minISP < 86) {
								maxISP = 215;
								minISP = 86;
							}
							newISP.Add (0, maxISP, 0, 0);
							newISP.Add (1, minISP, 0, 0);
							curEngine.useVelocityCurve = false;
							curEngine.useEngineResponseTime = false;
						} else {
							maxISP = 2500;
							newISP.Add (0, 1200);
							newISP.Add (0.3f, 2500);
							newISP.Add (1, 800);
							vCurve.Add (0, 1);
							vCurve.Add (400, 0.8f);
							vCurve.Add (1000, 0.9f);
							vCurve.Add (2000, 0.5f);
							curEngine.useVelocityCurve = true;
							curEngine.useEngineResponseTime = true;
						}
						//ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
						curEngine.atmosphereCurve = newISP;
						curEngine.velocityCurve = vCurve;
						assThermalPower = mthe.getThermalPower ();

						if (thrustLimitRatio == 0 || 54 * (assThermalPower / 275) <= thrustLimiter) {
							engineMaxThrust = 54 * (assThermalPower / 275);
							thrustLimit = "Max Power";
						} else {
							thrustLimit = thrustLimitRatio.ToString () + ":1";
							engineMaxThrust = thrustLimiter;
						}

						float heat_exchanger_thrust_divisor = 1;
						if (radius > mthe.getRadius ()) {
							heat_exchanger_thrust_divisor = mthe.getRadius () * mthe.getRadius () / radius / radius;
						} else {
							heat_exchanger_thrust_divisor = radius * radius / mthe.getRadius () / mthe.getRadius ();
						}

						engineMaxThrust = engineMaxThrust * heat_exchanger_thrust_divisor;

						if (isLFO) {
							engineMaxThrust = engineMaxThrust * 1.5f;
						}

						//prevent jet flameouts
						if (curEngine.currentThrottle > 0 && engineMaxThrust > 0 && currentpropellant_is_jet) {
							//debug current atmosphere in use
							float currentAtmosphereConsumption = (engineMaxThrust * curEngine.currentThrottle) / (curEngine.realIsp * 9.81f);
							//print ("Warp FNNC: Throttle: " + vessel.ctrlState.mainThrottle + " Max Thrust: " + engineMaxThrust + " Current Atmopshere: " + currentAtmosphereConsumption);


							float maxAtmosphereRequired = engineMaxThrust / (curEngine.realIsp * 9.81f);
							intakeAtmThrustLimiter = availableAtmosphere / maxAtmosphereRequired;
							float intakeAtmP = intakeAtmThrustLimiter * 100;
							if (intakeAtmThrustLimiter > 1) {
								intakeAtmThrustLimiter = 1;
							} else if (intakeAtmThrustLimiter < 1) {
								//limit max thrust to available atmopshere (possibly smoother but commented out, my ultimate goal here is to get the engine throttle FX animated correctly)
								engineMaxThrust = engineMaxThrust * intakeAtmThrustLimiter;
								thrustLimit = "IntakeAtm " + intakeAtmP.ToString ("00.000") + "%";
								if(curEngine.currentThrottle > intakeAtmThrustLimiter){

									//animate throttle (doesn't work, but would be nice)
									/*
									foreach (FXGroup fx_group in part.fxGroups) {
										//fx_group.setActive (false);
										fx_group.Power = intakeAtmThrustLimiter; // didn't work
										fx_group.SetPower (intakeAtmThrustLimiter); //didn't work
										print ("Debug: " + intakeAtmThrustLimiter); // fires - from 1 down to 0 
									}*/


									/* //(optionally) we could limit mainThrottle of vessel instead of maxThrust to provide additional feedback to users, however, that would effect other non-thermal engines which may be throttled up (the following code will only reduce throttle, more would be needed to increase throttle)
									 * //I left this snippet in here more as a reference in case we want to use it for something else later...
									 * if (float.IsNaN(intakeAtmThrustLimiter)) intakeAtmThrustLimiter = 0; // setting FlightCtrlState to NaN will cause very bad things to happen
									 * FlightInputHandler.state.mainThrottle = intakeAtmThrustLimiter;
									 */

								}
							}

							//print ("warp FNNC: Microwave MaxThrust: " + engineMaxThrust.ToString () + " ThermalPower: " + assThermalPower.ToString () + " Engine ISP " + curEngine.realIsp + " Max Atmosphere Required: " + maxAtmosphereRequired.ToString() + " Current Atmosphere Required: " + atmosphereRequired.ToString());
							//print ("Warp FNNC: Atmosphere Max Thrust: " + engineMaxThrust.ToString () + " Max Atmosphere Required: " + maxAtmosphereRequired.ToString () + " Atm Limter " + intakeAtmThrustLimiter + " Atmosphere limit: " + maxAtmosphereRequired * intakeAtmThrustLimiter);
						}
						else if(!currentpropellant_is_jet && intakeAtmThrustLimiter < 1){
							intakeAtmThrustLimiter = 1;
						}

						//This will shutdown engine if less than 40MW of power is available or IntakeAtm reaches 0, this is equal to the ThermalPower of the unupgraded 1.25m reactor

						if (assThermalPower < 40 || intakeAtmThrustLimiter == 0) {
							if(curEngine.isOperational == true){
								curEngine.Events ["Shutdown"].Invoke ();
								engineAutoShutdown = true;
							}
							if(assThermalPower < 40){
								thrustLimit = "Insufficient Power";
							}
						} else {
							if(curEngine.isOperational == false && engineAutoShutdown == true){
								curEngine.Events ["Activate"].Invoke ();
								engineAutoShutdown = false;
							}
							curEngine.maxThrust = engineMaxThrust;
						}
					}

                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {
                curEngine.propellants.Clear();
                curEngine.propellants = list_of_propellants;
                curEngine.SetupPropellant();
            }

            //List<PartResource> partresources = new List<PartResource>();
            //part.GetConnectedResources(curEngine.propellants[0].id, partresources);

			bool next_propellant = false;

			List<ModuleEngines.Propellant> curEngine_propellants_list = new List<ModuleEngines.Propellant>();
			curEngine_propellants_list = curEngine.propellants;
			foreach(ModuleEngines.Propellant curEngine_propellant in curEngine_propellants_list) {
				List<PartResource> partresources = new List<PartResource>();
				part.GetConnectedResources(curEngine_propellant.id, partresources);
				if(partresources.Count == 0) {
					next_propellant = true;
				}
			}

			if (next_propellant && fuel_mode != 1) {
                TogglePropellant();
            }
            /*else {
                if ((!isJet && currentpropellant_is_jet) || (isJet && !currentpropellant_is_jet)) {
                    TogglePropellant();
                }
            }*/
        }
        
        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            Actions["TogglePropellantAction"].guiName = Events["TogglePropellant"].guiName = String.Format("Toggle Propellant");
            fuel_gauge = part.stackIcon.DisplayInfo();
            if (isHybrid) {
                propellants = getPropellantsHybrid();
            }else {
                propellants = getPropellants(isJet);
            }

			bool manual_upgrade = false;
			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(upgradeTechReq != null) {
					if(PluginHelper.hasTech(upgradeTechReq)) {
						hasrequiredupgrade = true;
					}else if(upgradeTechReq == "none") {
						manual_upgrade = true;
					}
				}else{
					manual_upgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}

			if (engineInit == false) {
				engineInit = true;
				if(hasrequiredupgrade) {
					isupgraded = true;
				}
			}

			if(manual_upgrade) {
				hasrequiredupgrade = true;
			}

            engineType = originalName;
            if (isupgraded) {
                engineType = upgradedName;
            }

            setupPropellants();
			hasstarted = true;
            
        }

        public override void OnUpdate() {
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitEngine"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded && isJet && hasrequiredupgrade;
            Fields["engineType"].guiActive = isJet;

			//print ("Nozzle Check-in 1 (" + vessel.GetName() + ")");

			//Modify any connected standard intakeAir parts to function as AtmosphericIntakes
 			List<ModuleResourceIntake> mris = vessel.FindPartModulesImplementing<ModuleResourceIntake> ();
			if (mris != null) {
				foreach (ModuleResourceIntake mri in mris) {
					if (mri.resourceName == "IntakeAir") {
						float intakeArea = mri.area;
						try {
							String path = "WarpPlugin/Overrides/" + mri.moduleName + "/" + mri.moduleName;

							ConfigNode config_override = null;

							if (GameDatabase.Instance.ExistsConfigNode (path)) {
								config_override = GameDatabase.Instance.GetConfigNode (path);
							}
							List<ConfigNode> config_nodes = new List<ConfigNode> ();
							//ConfigNode.ConfigNodeList config_nodes = new ConfigNode.ConfigNodeList();

							if (config_override != null) {
								foreach (ConfigNode conf_node in config_override.nodes) {
									config_nodes.Add (conf_node);
								}
							}

							if (config_nodes.Count > 0) {
								print ("[WarpPlugin] PartLoader making update to : " + mri.part.name + " part");
							}

							foreach (ConfigNode config_part_item in config_nodes) {
								if (config_part_item.name == "RESOURCEADD") {
									mri.part.AddResource (config_part_item);
								} else if (config_part_item.name == "MODULEADD") {
									mri.part.AddModule (config_part_item);
									AtmosphericIntake aipm = (AtmosphericIntake)mri.part.Modules ["AtmosphericIntake"];
									aipm.area = intakeArea;
									aipm.part.force_activate ();
								} else if (config_part_item.name == "MODULEREMOVE") {
									mri.part.RemoveModule (mri);
								}
							}

						} catch (Exception ex) {
							print ("[WarpPlugin] Exception caught adding to: " + mri.part.name + " part");
						}
					}
				}

				// Add resource manager for IntakeAtm (This should be revised but it was a quick copy paste job to save time)
				FNResourceManager manager;

				if (!FNResourceOvermanager.getResourceOvermanagerForResource ("IntakeAtm").hasManagerForVessel (vessel)) {
					manager = FNResourceOvermanager.getResourceOvermanagerForResource ("IntakeAtm").createManagerForVessel (this);

					print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + "IntakeAtm" + ")");


				} else {
					manager = FNResourceOvermanager.getResourceOvermanagerForResource ("IntakeAtm").getManagerForVessel (vessel);
					if (manager == null) {
						manager = FNResourceOvermanager.getResourceOvermanagerForResource ("IntakeAtm").createManagerForVessel (this);
						print ("[WarpPlugin] Creating Resource Manager for Vessel " + vessel.GetName() + " (" + "IntakeAtm" + ")");
					}
				}

				if (manager.getPartModule ().vessel != this.vessel) {
					manager.updatePartModule (this);
				}

				if (manager.getPartModule () == this) {
					manager.update ();
				}

			}
            
            ModuleEngines curEngineT = (ModuleEngines)this.part.Modules["ModuleEngines"];
            if (curEngineT.isOperational && !IsEnabled) {
                IsEnabled = true;
                part.force_activate();
                
            }

			//rint ("Nozzle Check-in 2 (" + vessel.GetName() + ")");

			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			}

            float currentpropellant = 0;
            float maxpropellant = 0;

			List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(curEngineT.propellants[0].id, partresources);

			//print ("Nozzle Check-in 3 (" + vessel.GetName() + ")");

            foreach (PartResource partresource in partresources) {
                currentpropellant += (float) partresource.amount;
                maxpropellant += (float)partresource.maxAmount;
            }

			if (fuel_gauge != null  && fuel_gauge.infoBoxRef != null) {
            
				if (curEngineT.isOperational) {
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

			//print ("Nozzle Check-in 4 (" + vessel.GetName() + ")");
        }

        public override void OnFixedUpdate() {
            ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];



			if (curEngine == null) {
				return;
			}


            //print(curEngine.currentThrottle.ToString() + "\n");

            if (curEngine.maxThrust <= 0 && curEngine.isEnabled && curEngine.currentThrottle > 0) {
                setupPropellants();
                if (curEngine.maxThrust <= 0) {
                    curEngine.maxThrust = 0.000001f;
                }
            }

			//curEngine.flameout = false;

			if (curEngine.currentThrottle > 0 && curEngine.isEnabled && assThermalPower > 0) {

                //float thermalReceived = part.RequestResource("ThermalPower", assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle);
                float thermalReceived = consumeFNResource(assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle, FNResourceManager.FNRESOURCE_THERMALPOWER);
				consumeFNResource(thermalReceived, FNResourceManager.FNRESOURCE_WASTEHEAT);
                if (thermalReceived >= assThermalPower * TimeWarp.fixedDeltaTime * curEngine.currentThrottle) {
					shutdown_counter = 0;
                    float thermalThrustPerSecond = thermalReceived / TimeWarp.fixedDeltaTime / curEngine.currentThrottle * engineMaxThrust / assThermalPower;
                    curEngine.maxThrust = thermalThrustPerSecond;
                }
                else {
                    if (thermalReceived/TimeWarp.fixedDeltaTime > 0.0001f) {
						shutdown_counter = 0;
                        float thermalThrustPerSecond = thermalReceived / TimeWarp.fixedDeltaTime / curEngine.currentThrottle * engineMaxThrust / assThermalPower;
                        curEngine.maxThrust = thermalThrustPerSecond;
                    }
                    else {
						//curEngine.maxThrust = 0.000001f;
						if (!curEngine.flameout) {
							shutdown_counter++;

							if (shutdown_counter > 2) {
								curEngine.Events ["Shutdown"].Invoke ();
								ScreenMessages.PostScreenMessage ("Engines shutdown due to lack of Thermal Power!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
								shutdown_counter = 0;
								foreach (FXGroup fx_group in part.fxGroups) {
									fx_group.setActive (false);
								}

							}
						}

                    }

                }
            }else {
				if (assThermalPower <= 0) {
					curEngine.maxThrust = 0.000001f;
				}

			}
            //curEngine.currentThrottle
			powerConsumptionStr = (assThermalPower*curEngine.currentThrottle).ToString() + "MW";

        }

        public override string GetInfo() {
            return String.Format("Engine parameters determined by attached reactor.");
        }

		public bool hasStarted() {
			return hasstarted;
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
