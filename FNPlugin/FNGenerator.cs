using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class FNGenerator : FNResourceSuppliableModule {
        [KSPField(isPersistant = false)]
        public float pCarnotEff;
        [KSPField(isPersistant = false)]
        public float maxThermalPower;
        [KSPField(isPersistant = true)]
        public bool IsEnabled= true;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string generatorType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Power")]
        public string OutputPower;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Max Power")]
		public string MaxPowerStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
        public string OverallEfficiency;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public float upgradedpCarnotEff;
		[KSPField(isPersistant = false)]
		public string animName;
		[KSPField(isPersistant = true)]
		public bool generatorInit = false;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;

        private float coldBathTemp = 500;
        public float hotBathTemp = 1;
        private float outputPower;
        private float totalEff;
        private float sectracker = 0;
        
        protected bool hasScience = false;
        
		protected bool play_down = true;
		protected bool play_up = true;
		protected FNReactor myReactor;

		protected bool hasrequiredupgrade = false;

		protected double last_draw_update = 0.0;

		protected int shutdown_counter = 0;

		protected Animation anim;

		protected bool hasstarted = false;

        //protected bool responsible_for_megajoulemanager;
        //protected FNResourceManager megamanager;

		//protected String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES};

        [KSPEvent(guiActive = true, guiName = "Activate Generator", active = true)]
        public void ActivateGenerator() {
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Generator", active = false)]
        public void DeactivateGenerator() {
            IsEnabled = false;
        }

        [KSPAction("Activate Generator")]
        public void ActivateGeneratorAction(KSPActionParam param) {
            ActivateGenerator();
        }

        [KSPAction("Deactivate Generator")]
        public void DeactivateGeneratorAction(KSPActionParam param) {
            DeactivateGenerator();
        }

        [KSPAction("Toggle Generator")]
        public void ToggleGeneratorAction(KSPActionParam param) {
            IsEnabled = !IsEnabled;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            isupgraded = true;
            pCarnotEff = upgradedpCarnotEff;
            generatorType = upgradedName;
            //recalculatePower();
			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
            //IsEnabled = false;
        }

        
                
        public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);

            Actions["ActivateGeneratorAction"].guiName = Events["ActivateGenerator"].guiName = String.Format("Activate Generator");
            Actions["DeactivateGeneratorAction"].guiName = Events["DeactivateGenerator"].guiName = String.Format("Deactivate Generator");
            Actions["ToggleGeneratorAction"].guiName = String.Format("Toggle Generator");
            if (state == StartState.Editor) { return; }

            this.part.force_activate();

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) {
				anim [animName].layer = 1;
				if (!IsEnabled) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}

			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(PluginHelper.hasTech(upgradeTechReq)) {
					hasrequiredupgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}

			if (generatorInit == false) {
				generatorInit = true;
				IsEnabled = true;
				if(hasrequiredupgrade) {
					isupgraded = true;
				}
			}
            
            if (isupgraded) {
                pCarnotEff = upgradedpCarnotEff;
                generatorType = upgradedName;
            }else {
                generatorType = originalName;
            }
            print("[WarpPlugin] Configuring Generator");
            recalculatePower();


			hasstarted = true;
            
        }

        public void recalculatePower() {
            Part[] childParts = this.part.FindChildParts<Part>(false);
            PartModuleList childModules;

			foreach (Part childPart in childParts) {
				childModules = childPart.Modules;
				foreach (PartModule thisModule in childModules) {
					var thisModule2 = thisModule as FNReactor;
					if (thisModule2 != null) {
						FNReactor fnr = (FNReactor)thisModule;
						setHotBathTemp(fnr.getReactorTemp());
						maxThermalPower = fnr.getReactorThermalPower();
						myReactor = fnr;
					}
				}
			}

            Part parent = this.part.parent;
            if (parent != null) {
                childModules = parent.Modules;
				foreach (PartModule thisModule in childModules) {
					var thisModule2 = thisModule as FNReactor;
					if (thisModule2 != null) {
						FNReactor fnr = (FNReactor)thisModule;
						setHotBathTemp(fnr.getReactorTemp());
						maxThermalPower = fnr.getReactorThermalPower();
						myReactor = fnr;
					}
				}
            }
			            
        }

        

        public override void OnUpdate() {
            Events["ActivateGenerator"].active = !IsEnabled;
            Events["DeactivateGenerator"].active = IsEnabled;
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitReactor"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitReactor"].active = false;
			}
            Fields["upgradeCostStr"].guiActive = !isupgraded;
			Fields["OverallEfficiency"].guiActive = IsEnabled;
			Fields["MaxPowerStr"].guiActive = IsEnabled;

			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			}

			if (IsEnabled) {
				if (play_up && anim != null) {
					play_down = true;
					play_up = false;
					anim [animName].speed = 1f;
					anim [animName].normalizedTime = 0f;
					anim.Blend (animName, 2f);
				}
			} else {
				if (play_down && anim != null) {
					play_down = false;
					play_up = true;
					anim [animName].speed = -1f;
					anim [animName].normalizedTime = 1f;
					anim.Blend (animName, 2f);
				}
			}

			if (IsEnabled) {
				float percentOutputPower = totalEff * 100.0f;
				float outputPowerReport = -outputPower;
                    

				if (Environment.TickCount - last_draw_update > 40) {
					OutputPower = outputPowerReport.ToString ("0.000") + "MW";
					OverallEfficiency = percentOutputPower.ToString ("0.0") + "%";

					if (totalEff >= 0) {
						MaxPowerStr = (maxThermalPower*totalEff).ToString ("0.000") + "MW";
					} else {
						MaxPowerStr = (0).ToString() + "MW";
					}

					last_draw_update = Environment.TickCount;
				}

			} else {
                OutputPower = "Generator Offline";
            }


        }

        public float getMaxPowerOutput() {
            double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
            float maxTotalEff = (float)carnotEff * pCarnotEff;
            return maxThermalPower * maxTotalEff;
        }

        public void setHotBathTemp(float temp) {
            hotBathTemp = temp;
        }

		public bool hasStarted() {
			return hasstarted;
		}

        public override string GetInfo() {
			return String.Format("Percent of Carnot Efficiency: {0}%\n-Upgrade Information-\n Upgraded Percent of Carnot Efficiency: {1}%", pCarnotEff*100, upgradedpCarnotEff*100);
        }

        public override void OnFixedUpdate() {
			base.OnFixedUpdate ();

			//print ("Generator Check-in 1 (" + vessel.GetName() + ")");

            if (!IsEnabled) { return; }

			if (FNRadiator.hasRadiatorsForVessel (vessel)) {
				coldBathTemp = FNRadiator.getAverageRadiatorTemperatureForVessel (vessel);
			} else {
				coldBathTemp = hotBathTemp;
			}
            double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
			if (carnotEff <= 0) {
				recalculatePower ();
				if (!FNRadiator.hasRadiatorsForVessel (vessel)) {
					coldBathTemp = hotBathTemp;
				}
				carnotEff = 1.0f - coldBathTemp / hotBathTemp;
				if (carnotEff <= 0) {
					shutdown_counter++;
					carnotEff = 0;
					if (shutdown_counter > 20) {
						IsEnabled = false;
						if (FlightGlobals.ActiveVessel == vessel) {
							if (FNRadiator.hasRadiatorsForVessel (vessel)) {
								ScreenMessages.PostScreenMessage ("Generator Shutdown: No thermal power available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
							} else {
								ScreenMessages.PostScreenMessage ("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
							}
						}
						print ("[WarpPlugin] Generator Shutdown - no thermal power");
					}
					return;
				}
			} else {
				shutdown_counter = 0;
			}

			//print ("Generator Check-in 2 (" + vessel.GetName() + ")");

			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Megajoules").id, partresources);
			float currentmegajoules = 0;
			foreach (PartResource partresource in partresources) {
				currentmegajoules += (float)(partresource.maxAmount - partresource.amount);
			}
			currentmegajoules = currentmegajoules / TimeWarp.fixedDeltaTime;
            totalEff = (float)carnotEff * pCarnotEff;

			//print ("Generator Check-in 3 (" + vessel.GetName() + ")");

			float waste_heat_produced = (getCurrentUnfilledResourceDemand (FNResourceManager.FNRESOURCE_MEGAJOULES) + currentmegajoules);
			float thermal_power_currently_needed = waste_heat_produced / totalEff;
            double thermaldt = Math.Min(maxThermalPower,thermal_power_currently_needed) * TimeWarp.fixedDeltaTime;
			double wastedt = thermaldt * totalEff;
            
            double inputThermalPower = consumeFNResource(thermaldt, FNResourceManager.FNRESOURCE_THERMALPOWER);
			consumeFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);

			//print ("Generator Check-in 4 (" + vessel.GetName() + ")");

			            
            double electricdt = inputThermalPower * totalEff;
            double electricdtps = electricdt / TimeWarp.fixedDeltaTime;
			double max_electricdtps = maxThermalPower * totalEff;
			outputPower = 0;
			if (electricdtps > 0.001) {
				outputPower = -(float)supplyFNResourceFixedMax (electricdtps * TimeWarp.fixedDeltaTime, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
			} else {
				outputPower = -(float)supplyFNResourceFixedMax (0, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
			}

            outputPower = outputPower / TimeWarp.fixedDeltaTime;
            
			//print ("Generator Check-in 5 (" + vessel.GetName() + ")");
            
        }




    }
}
