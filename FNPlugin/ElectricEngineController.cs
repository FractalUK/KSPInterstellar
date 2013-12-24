using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    
	class ElectricEngineController : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = ":";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
		public string electricalPowerConsumptionStr = ":";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
		public string efficiencyStr = ":";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
		public string heatProductionStr = ":";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr = ":";
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
		[KSPField(isPersistant = false)]
		public float maxPower;
        [KSPField(isPersistant = true)]
		public int fuel_mode = 1;
		[KSPField(isPersistant = true)]
		public bool engineInit = false;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;
        protected float total_power_output = 0;
        protected float reference_power = 8000;
        protected float initial_thrust = 0;
        protected float initial_isp = 11213.0f;
        protected int eval_counter = 0;
        protected ConfigNode upgrade_resource;
        protected float ispMultiplier = 1;
        protected ConfigNode[] propellants;
        protected VInfoBox fuel_gauge;
		protected float final_thrust_store = 0;
		protected float heat_production_f = 0;
		protected float electrical_consumption_f = 0;
		protected bool hasrequiredupgrade = false;

		protected int shutdown_counter = 0;

		protected float thrust_efficiency = 0.72f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
        public string fuelmode;

        [KSPEvent(guiActive = true, guiName = "Toggle Propellant", active = true)]
        public void TogglePropellant() {

            fuel_mode++;
            if (fuel_mode >= propellants.Length) {
                fuel_mode = 0;
            }


            evaluateMaxThrust();

        }

		[KSPAction("Toggle Propellant")]
		public void TogglePropellantAction(KSPActionParam param) {
			TogglePropellant();
		}
        
        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            isupgraded = true;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                Propellant prop = new Propellant();
                //prop.id = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id;
                //ConfigNode prop_node = new ConfigNode();
                //PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").Save(prop_node);

                //PartResource part_resource = part.Resources.list[0];
                //part_resource.info = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma");
                //part_resource.maxAmount = 10;
                //part_resource.amount = 10;

				ConfigNode node = new ConfigNode("RESOURCE");
				node.AddValue("name", "VacuumPlasma");
				node.AddValue("maxAmount", 10);
				node.AddValue("amount", 10);
				part.AddResource(node);

                propellants = ElectricEngineController.getPropellants(isupgraded);
                fuel_mode = 0;

                //curEngine.propellants[1].id = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id;
                //curEngine.propellants[1].name = PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").name;
                engineType = upgradedName;
				ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
                evaluateMaxThrust();
            }
            
        }

        public override void OnLoad(ConfigNode node) {
                        
        }
        
        public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_WASTEHEAT};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);

			Actions["TogglePropellantAction"].guiName = Events["TogglePropellant"].guiName = String.Format("Toggle Propellant");

            if (state == StartState.Editor) { return; }
            //this.part.force_activate();

            fuel_gauge = part.stackIcon.DisplayInfo();
            

            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            if (curEngine != null) {
                initial_thrust = curEngine.maxThrust;
                initial_isp = curEngine.atmosphereCurve.Evaluate(0);
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
					ConfigNode node = new ConfigNode("RESOURCE");
					node.AddValue("name", "VacuumPlasma");
					node.AddValue("maxAmount", 10);
					node.AddValue("amount", 10);
					part.AddResource(node);
					fuel_mode = 0;
				}
			}

			propellants = getPropellants(isupgraded);

			if(manual_upgrade) {
				hasrequiredupgrade = true;
			}

			if(isupgraded) {
				engineType = upgradedName;
			}else{
				engineType = originalName;
			}


			evaluateMaxThrust();
            
        }

        public override void OnUpdate() {
            if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitEngine"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;
			           
			electricalPowerConsumptionStr = electrical_consumption_f.ToString ("0.00") + " MW";
			efficiencyStr = (thrust_efficiency * 100.0f).ToString ("0") + "%";
			heatProductionStr = heat_production_f.ToString ("0.00") + " MW";
			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			}

            ModuleEngines curEngineT = (ModuleEngines)this.part.Modules["ModuleEngines"];
            if (curEngineT.isOperational && !IsEnabled) {
                IsEnabled = true;
                part.force_activate();
            }

            float currentpropellant = 0;
            float maxpropellant = 0;

			List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(curEngineT.propellants[0].id, partresources);

            foreach (PartResource partresource in partresources) {
                currentpropellant += (float)partresource.amount;
                maxpropellant += (float)partresource.maxAmount;
            }

            if (curEngineT.isOperational) {
                if (!fuel_gauge.infoBoxRef.expanded) {
                    fuel_gauge.infoBoxRef.Expand();
                }
                fuel_gauge.length = 2;
                if (maxpropellant > 0) {
                    fuel_gauge.SetValue(currentpropellant / maxpropellant);
                }
                else {
                    fuel_gauge.SetValue(0);
                }
            }
            else {
                if (!fuel_gauge.infoBoxRef.collapsed) {
                    fuel_gauge.infoBoxRef.Collapse();
                }
            }
        }

        public override void OnFixedUpdate() {
			base.OnFixedUpdate ();

			List<Part> vessel_parts = vessel.parts;
			int engines = 0;
			foreach (Part vessel_part in vessel_parts) {
				foreach (PartModule vessel_part_module in vessel_part.Modules) {
					var curEngine2 = vessel_part_module as ElectricEngineController;
					if (curEngine2 != null) {
						var curEngine3 = curEngine2.part.Modules["ModuleEngines"] as ModuleEngines;
						if (curEngine3.isOperational) {
							engines++;
						}
					}
				}

			}

			if (engines <= 0) {
				engines = 1;
			}

            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            evaluateMaxThrust();
			if (final_thrust_store <= 0) {
				final_thrust_store = initial_thrust;
            }
            
			float thrust_per_engine = final_thrust_store / engines;
			float power_per_engine = Math.Min(0.5f*curEngine.currentThrottle*thrust_per_engine*curEngine.atmosphereCurve.Evaluate (0)/1000.0f*9.81f,maxPower);
			//print (power_per_engine);
			float power_received = consumeFNResource (power_per_engine * TimeWarp.fixedDeltaTime/thrust_efficiency, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.fixedDeltaTime;
			electrical_consumption_f = power_received;
			float heat_to_produce = power_received * (1.0f - thrust_efficiency);
			heat_production_f = supplyFNResource (heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

			float thrust_ratio;
			if (power_per_engine > 0) {
				thrust_ratio = power_received / power_per_engine;
				thrust_ratio = Mathf.Min (thrust_ratio, 1);
			} else {
				thrust_ratio = 1;

				if (curEngine.currentThrottle * thrust_per_engine > 0.0001f  && !curEngine.flameout) {
					shutdown_counter++;
					if (shutdown_counter > 2) {
						curEngine.Events ["Shutdown"].Invoke ();
						ScreenMessages.PostScreenMessage ("Engines shutdown due to lack of Electrical Power (Megajoules)!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
						shutdown_counter = 0;
						foreach (FXGroup fx_group in part.fxGroups) {
							fx_group.setActive (false);
						}

					}
				} else {
					shutdown_counter = 0;
				}
			}

			//float thrust_to_use = thrust_per_engine;
			float thrust_to_use = thrust_efficiency*2000.0f*power_received / (curEngine.atmosphereCurve.Evaluate (0) * 9.81f*curEngine.currentThrottle);

			float temp_to_part_set = Mathf.Min(curEngine.currentThrottle * part.maxTemp * 0.8f,1);

			//curEngine.maxThrust = Mathf.Max(thrust_to_use*thrust_ratio,0.00001f);
            if (curEngine.currentThrottle > 0) {
                if (!float.IsNaN(thrust_to_use) && !float.IsInfinity(thrust_to_use)) {
                    curEngine.maxThrust = Mathf.Max(thrust_to_use, 0.00001f);
                } else {
                    curEngine.maxThrust = 0.00001f;
                }
            }

			if (thrust_to_use * thrust_ratio <= 0.0001f && curEngine.currentThrottle * thrust_per_engine > 0.0001f  && !curEngine.flameout) {
				shutdown_counter++;
				if (shutdown_counter > 2) {
					curEngine.Events ["Shutdown"].Invoke ();
					ScreenMessages.PostScreenMessage ("Engines shutdown due to lack of Electrical Power (Megajoules)!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					shutdown_counter = 0;
					foreach (FXGroup fx_group in part.fxGroups) {
						fx_group.setActive (false);
					}

				}
			} else {
				shutdown_counter = 0;
			}

            if (isupgraded) {
				float vacuum_plasma_needed = 0;
				float vacuum_plasma_current = 0;
				List<PartResource> vacuum_resources = new List<PartResource>();
				part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("VacuumPlasma").id, vacuum_resources);

				foreach (PartResource partresource in vacuum_resources) {
					vacuum_plasma_needed += (float)(partresource.maxAmount-partresource.amount);
					vacuum_plasma_current += (float)partresource.amount;
				}

				if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude (vessel.mainBody)) {
					part.RequestResource ("VacuumPlasma", vacuum_plasma_current);
				} else {
					part.RequestResource ("VacuumPlasma", -vacuum_plasma_needed);
				}
            }
        }

        public void evaluateMaxThrust() {
            List<Part> vessel_parts = vessel.parts;
            total_power_output = 0;
            var curEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
            ConfigNode chosenpropellant = propellants[fuel_mode];
            ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
            List<Propellant> list_of_propellants = new List<Propellant>();
            //bool propellant_is_upgrade = false;

            for (int i = 0; i < assprops.Length; ++i) {
                fuelmode = chosenpropellant.GetValue("guiName");
                ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
                thrust_efficiency = float.Parse(chosenpropellant.GetValue("efficiency"));
                //propellant_is_upgrade = bool.Parse(chosenpropellant.GetValue("isUpgraded"));
                
                Propellant curprop = new Propellant();
                curprop.Load(assprops[i]);
                if (curprop.drawStackGauge) {
                    curprop.drawStackGauge = false;
                    fuel_gauge.SetMessage(curprop.name);
                    fuel_gauge.SetMsgBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetMsgTextColor(XKCDColors.ElectricLime);
                    fuel_gauge.SetProgressBarColor(XKCDColors.Yellow);
                    fuel_gauge.SetProgressBarBgColor(XKCDColors.DarkLime);
                    fuel_gauge.SetValue(0f);
                }
                list_of_propellants.Add(curprop);
            }

            total_power_output = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);  

			final_thrust_store = thrust_efficiency*2000.0f*total_power_output / (initial_isp * ispMultiplier * 9.81f);

			//float thrust_ratio = total_power_output / reference_power;
			//final_thrust_store = initial_thrust * thrust_ratio / ispMultiplier;

			FloatCurve newISP = new FloatCurve ();
			newISP.Add (0, initial_isp * ispMultiplier);
			curEngine.atmosphereCurve = newISP;
            
            if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {

                curEngine.propellants.Clear();
                curEngine.propellants = list_of_propellants;
                curEngine.SetupPropellant();
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(curEngine.propellants[0].id, partresources);

            //if(!isupgraded) {
            if (partresources.Count == 0 && fuel_mode != 0) {
                TogglePropellant();
            }
            //}else{
            //    if(!propellant_is_upgrade) {
                    //TogglePropellant();
           //     }
            //}
            
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
			ConfigNode[] prop_nodes = getPropellants(upgraded);
            string return_str = "Max Power Consumption: " + maxPower.ToString("") + " MW\n";
			float thrust_per_mw = 0.013089f;
			foreach (ConfigNode propellant_node in prop_nodes) {
				float ispMultiplier = float.Parse(propellant_node.GetValue("ispMultiplier"));
				float ispProp = initial_isp * ispMultiplier;
				float thrustProp = thrust_per_mw / ispMultiplier;
				string guiname = propellant_node.GetValue("guiName");
				return_str = return_str + "--" + guiname + "--\n" + "Thrust: " + thrustProp.ToString ("0.0000") + " kN per MW\n" + "ISP: " + ispProp.ToString ("0.0") + "s\n";
			}
			return return_str;
		}

        public static ConfigNode[] getPropellants(bool isupgraded) {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("BASIC_ELECTRIC_PROPELLANT");
            ConfigNode[] propellantlist2 = GameDatabase.Instance.GetConfigNodes("ADVANCED_ELECTRIC_PROPELLANT");

			if (isupgraded && propellantlist2 != null) {
				propellantlist = propellantlist2.Concat(propellantlist).ToArray();
			}

            if (propellantlist == null) {
				PluginHelper.showInstallationErrorMessage ();
			}

            return propellantlist;
        }

        public override string getResourceManagerDisplayName() {
            if (isupgraded) {
                return upgradedName + " Plasma Thruster";
            } else {
                return originalName + " Thruster";
            }
        }
    }
}
