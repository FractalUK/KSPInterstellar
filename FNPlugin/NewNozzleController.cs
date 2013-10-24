using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin{
	class NewNozzleController : FNResourceSuppliableModule{
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

		//GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string engineType = ":";
		[KSPField(isPersistant = false, guiActive = true, guiName = "Fuel Mode")]
		public string fuelmode;

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
		protected FNReactor myAttachedReactor;
		protected bool currentpropellant_is_jet = false;
		protected double fuel_flow_rate = 0;

		protected const float g0 = 9.81f;

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

		public void upgradePartModule() {
			engineType = upgradedName;
			propellants = FNNozzleController.getPropellantsHybrid();
			isHybrid = true;
		}

		public ConfigNode[] getPropellants() {
			return propellants;
		}

		public override void OnStart(PartModule.StartState state) {
			if (state == StartState.Editor) { return; }

			//print ("Starting");

			fuel_gauge = part.stackIcon.DisplayInfo();
			myAttachedEngine = this.part.Modules["ModuleEngines"] as ModuleEngines;
			engineType = originalName;

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

			if (engineInit == false) {
				engineInit = true;
				if(hasrequiredupgrade && !manual_upgrade) {
					isupgraded = true;
				}
			}

			if (isupgraded) {
				upgradePartModule ();
			}

			foreach (AttachNode attach_node in part.attachNodes) {
				myAttachedReactor = attach_node.attachedPart.Modules ["FNReactor"] as FNReactor;
				if (myAttachedReactor != null) {
					break;
				}
			}

			setupPropellants();
			hasstarted = true;

			//print ("Start Complete");
		}

		public override void OnUpdate() {
			//print ("Update");
			Fields["engineType"].guiActive = isJet;

			if (myAttachedEngine != null) {
				if (myAttachedEngine.isOperational && !IsEnabled) {
					IsEnabled = true;
					part.force_activate ();
				}

				updatePropellantBar ();
			}


			//print ("Update Complete");
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
			//print ("Setting up Propellants");

			ConfigNode chosenpropellant = propellants[fuel_mode];
			ConfigNode[] assprops = chosenpropellant.GetNodes("PROPELLANT");
			List<ModuleEngines.Propellant> list_of_propellants = new List<ModuleEngines.Propellant>();

			for (int i = 0; i < assprops.Length; ++i) {
				fuelmode = chosenpropellant.GetValue("guiName");
				ispMultiplier = float.Parse(chosenpropellant.GetValue("ispMultiplier"));
				isLFO = bool.Parse(chosenpropellant.GetValue("isLFO"));
				if(chosenpropellant.HasValue("isJet")) {
					currentpropellant_is_jet = bool.Parse(chosenpropellant.GetValue("isJet"));
				}
				print (currentpropellant_is_jet);

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

			if (PartResourceLibrary.Instance.GetDefinition(list_of_propellants[0].name) != null) {
				myAttachedEngine.propellants.Clear();
				myAttachedEngine.propellants = list_of_propellants;
				myAttachedEngine.SetupPropellant();
			}

			bool next_propellant = false;

			List<ModuleEngines.Propellant> curEngine_propellants_list = new List<ModuleEngines.Propellant>();
			curEngine_propellants_list = myAttachedEngine.propellants;
			foreach(ModuleEngines.Propellant curEngine_propellant in curEngine_propellants_list) {
				List<PartResource> partresources = new List<PartResource>();
				part.GetConnectedResources(curEngine_propellant.id, partresources);
				if(partresources.Count == 0 || PartResourceLibrary.Instance.GetDefinition(curEngine_propellant.name) == null) {
					next_propellant = true;
				}
			}

			if (next_propellant && fuel_mode != 1) {
				TogglePropellant();
			}

			//print ("Setting up Propellants Complete");
		}

		public void updateIspEngineParams() {
			FloatCurve newISP = new FloatCurve();
			FloatCurve vCurve = new FloatCurve ();
			maxISP = (float)(Math.Sqrt ((double)myAttachedReactor.getReactorTemp ()) * 17.0 * ispMultiplier);
			if (!currentpropellant_is_jet) {
				minISP = maxISP * 0.4f;
				newISP.Add (0, maxISP, 0, 0);
				newISP.Add (1, minISP, 0, 0);
				myAttachedEngine.useVelocityCurve = false;
				myAttachedEngine.useEngineResponseTime = false;
			} else {
				if (myAttachedReactor.getIsNuclear ()) {
					maxISP *= 2.0f/3.0f;
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
			}

			myAttachedEngine.atmosphereCurve = newISP;
			myAttachedEngine.velocityCurve = vCurve;
			assThermalPower = myAttachedReactor.getReactorThermalPower();

		}

		public float getAtmosphericLimit() {
			print (currentpropellant_is_jet);
			if (currentpropellant_is_jet) {
				string resourcename = myAttachedEngine.propellants [0].name;
				List<PartResource> partresources = new List<PartResource> ();
				part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition (resourcename).id, partresources);
				double currentintakeatm = 0;
				foreach (PartResource partresource in partresources) {
					currentintakeatm += partresource.amount;
				}

				print ("P " + getFuelRateThermalJetsForVessel (vessel, resourcename) + " / " + currentintakeatm);

				if (getFuelRateThermalJetsForVessel (vessel, resourcename) > 0) {

					return (float)Math.Min (currentintakeatm / getFuelRateThermalJetsForVessel (vessel, resourcename), 1.0);
				} else {
					return 1.0f;
				}

			} else {
				return 1.0f;
			}
		}

		public override void OnFixedUpdate() {
			//print ("Updating");

			if (myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0 && myAttachedReactor != null) {
				updateIspEngineParams ();
				float curve_eval_point = (float)Math.Min (FlightGlobals.getStaticPressure (vessel.transform.position), 1.0);
				float currentIsp = myAttachedEngine.atmosphereCurve.Evaluate (curve_eval_point);


				float heat_exchanger_thrust_divisor = 1;
				if (radius > myAttachedReactor.getRadius ()) {
					heat_exchanger_thrust_divisor = myAttachedReactor.getRadius () * myAttachedReactor.getRadius () / radius / radius;
				} else {
					heat_exchanger_thrust_divisor = radius * radius / myAttachedReactor.getRadius () / myAttachedReactor.getRadius ();
				}

				if (myAttachedReactor.getRadius () == 0 || radius == 0) {
					heat_exchanger_thrust_divisor = 1;
				}
				print (getAtmosphericLimit ());
				double thermal_power_received = consumeFNResource (assThermalPower * TimeWarp.fixedDeltaTime * myAttachedEngine.currentThrottle*getAtmosphericLimit(), FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
				float power_ratio = (float)(thermal_power_received / assThermalPower);
				consumeFNResource (thermal_power_received * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

				double engineMaxThrust = Math.Max (2000.0 * thermal_power_received / currentIsp / 9.81f * heat_exchanger_thrust_divisor, 0.01f);
				myAttachedEngine.maxThrust = (float)engineMaxThrust;

				foreach (FXGroup fx_group in part.fxGroups) {
					fx_group.Power = powerRatio;
				} 

				fuel_flow_rate = 5.0*engineMaxThrust / g0 / currentIsp/myAttachedEngine.currentThrottle/getAtmosphericLimit(); // fuel flow rate at max throttle
			} else {
				fuel_flow_rate = 0;
				if (myAttachedReactor == null && myAttachedEngine.isOperational && myAttachedEngine.currentThrottle > 0) {
					myAttachedEngine.Events ["Shutdown"].Invoke ();
					ScreenMessages.PostScreenMessage ("Engine Shutdown: No reactor attached!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				}
			}
		}


		// Static Methods

		public static double getFuelRateThermalJetsForVessel(Vessel vess, string resourname) {
			List<NewNozzleController> nozzles = vess.FindPartModulesImplementing<NewNozzleController> ();
			double enum_rate = 0;
			foreach (NewNozzleController nozzle in nozzles) {
				ConfigNode[] prop_node = nozzle.getPropellants ();
				if (prop_node != null) {
					ConfigNode[] assprops = prop_node[nozzle.fuel_mode].GetNodes("PROPELLANT");
					if (prop_node [nozzle.fuel_mode] != null) {
						if (assprops[0].GetValue ("name").Equals(resourname)) {
							enum_rate += nozzle.fuel_flow_rate;
						}
					}
				}
			}
			return enum_rate;
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

