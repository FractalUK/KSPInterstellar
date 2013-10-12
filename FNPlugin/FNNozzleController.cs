using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private float maxISP;
        private float minISP;
        private float assThermalPower;
        private float powerRatio = 0.358f;
        private float engineMaxThrust;
        private bool isLFO = false;
        private float ispMultiplier = 1;
        private ConfigNode[] propellants;
        private VInfoBox fuel_gauge;
        protected float myScience = 0;

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

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
            if (isupgraded || myScience < upgradeCost) { return; } // || !hasScience || myScience < upgradeCost) { return; }
            isupgraded = true;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                engineType = upgradedName;
                propellants = FNNozzleController.getPropellantsHybrid();
                isHybrid = true;
            }

        }

        public void setupPropellants() {
            ModuleEngines curEngine = (ModuleEngines)this.part.Modules["ModuleEngines"];
            ConfigNode chosenpropellant = propellants[fuel_mode];
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();
            
            //VStackIcon stackicon = new VStackIcon(part);
            bool currentpropellant_is_jet = false;
            bool currentpropellant_is_electric = false;
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
                        print("Atmosphere");
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
            
            
            Part[] childParts = this.part.FindChildParts<Part>(true);
            PartModuleList childModules;
            for (int i = 0; i < childParts.Length; ++i) {
                childModules = childParts.ElementAt(i).Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
                    if (thisModule2 != null) {
                        FNReactor fnr = (FNReactor)thisModule;
                        FloatCurve newISP = new FloatCurve();
						FloatCurve vCurve = new FloatCurve ();
                        if (!currentpropellant_is_jet) {
                            maxISP = (float)Math.Sqrt((double)fnr.getReactorTemp()) * 17*ispMultiplier;
                            minISP = maxISP * 0.4f;
                            newISP.Add(0, maxISP, 0, 0);
                            newISP.Add(1, minISP, 0, 0);
                            curEngine.useVelocityCurve = false;
                            curEngine.useEngineResponseTime = false;
                        }else {
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

						engineMaxThrust = engineMaxThrust * heat_exchanger_thrust_divisor;

                        if (isLFO) {
                            engineMaxThrust = engineMaxThrust*1.5f;
                        }
                        curEngine.maxThrust = engineMaxThrust;
                    }

                }

            }

            Part parent = this.part.parent;
            if (parent != null) {
                childModules = parent.Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
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
            engineType = originalName;
            if (isupgraded) {
                engineType = upgradedName;
            }

            setupPropellants();
            
            
        }

        public override void OnUpdate() {
            Events["RetrofitEngine"].active = !isupgraded && isJet && myScience >= upgradeCost;
            Fields["upgradeCostStr"].guiActive = !isupgraded && isJet;
            Fields["engineType"].guiActive = isJet;

			//print ("Nozzle Check-in 1 (" + vessel.GetName() + ")");
            
            ModuleEngines curEngineT = (ModuleEngines)this.part.Modules["ModuleEngines"];
            if (curEngineT.isOperational && !IsEnabled) {
                IsEnabled = true;
                part.force_activate();
                
            }

			//rint ("Nozzle Check-in 2 (" + vessel.GetName() + ")");

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            float currentscience = 0;
            foreach (PartResource partresource in partresources) {
                currentscience += (float)partresource.amount;
            }
            myScience = currentscience;

            upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";

            float currentpropellant = 0;
            float maxpropellant = 0;

            partresources = new List<PartResource>();
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

        }

        public override string GetInfo() {
            return String.Format("Engine parameters determined by attached reactor.");
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
