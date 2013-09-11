using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private float coldBathTemp = 500;
        public float hotBathTemp = 1;
        private float outputPower;
        private float totalEff;
        private float sectracker = 0;
        protected float maxThermalPowerDraw;

        protected bool hasScience = false;
        protected float myScience = 0;

        //protected bool responsible_for_megajoulemanager;
        //protected FNResourceManager megamanager;

		//protected String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES};

		public FNGenerator() : base() {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;
		}

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
            if (isupgraded || !hasScience || myScience < upgradeCost) { return; }
            isupgraded = true;
            pCarnotEff = upgradedpCarnotEff;
            generatorType = upgradedName;
            //recalculatePower();
            part.RequestResource("Science", upgradeCost);
            //IsEnabled = false;
        }

        
                
        public override void OnStart(PartModule.StartState state) {
			base.OnStart (state);

            Actions["ActivateGeneratorAction"].guiName = Events["ActivateGenerator"].guiName = String.Format("Activate Generator");
            Actions["DeactivateGeneratorAction"].guiName = Events["DeactivateGenerator"].guiName = String.Format("Deactivate Generator");
            Actions["ToggleGeneratorAction"].guiName = String.Format("Toggle Generator");
            if (state == StartState.Editor) { return; }
			/*
            if (FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel)) {
                megamanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).getManagerForVessel(vessel);
                responsible_for_megajoulemanager = false;

            }else {
                megamanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).createManagerForVessel(this);
                responsible_for_megajoulemanager = true;
                print("[WarpPlugin] Creating Megajoule Manager  for Vessel");
            }
            */
            this.part.force_activate();
            
            if (isupgraded) {
                pCarnotEff = upgradedpCarnotEff;
                generatorType = upgradedName;
            }else {
                generatorType = originalName;
            }
            print("[WarpPlugin] Configuring Generator");
            recalculatePower();

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            if (partresources.Count > 0) {
                hasScience = true;
            }
        }

        public void recalculatePower() {
            Part[] childParts = this.part.FindChildParts<Part>(true);
            PartModuleList childModules;
            for (int i = 0; i < childParts.Length; ++i) {
                childModules = childParts.ElementAt(i).Modules;
                for (int j = 0; j < childModules.Count; ++j) {
                    PartModule thisModule = childModules.GetModule(j);
                    var thisModule2 = thisModule as FNReactor;
                    if (thisModule2 != null) {
                        FNReactor fnr = (FNReactor)thisModule;
                        setHotBathTemp(fnr.getReactorTemp());
                        maxThermalPower = fnr.getReactorThermalPower();
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
                        setHotBathTemp(fnr.getReactorTemp());
                        maxThermalPower = fnr.getReactorThermalPower();
                    }

                }
            }

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThermalPower").id, partresources);
            maxThermalPowerDraw = 0;
            foreach (PartResource partresource in partresources) {
                maxThermalPowerDraw += (float)partresource.maxAmount;
            }
        }

        

        public override void OnUpdate() {
            Events["ActivateGenerator"].active = !IsEnabled;
            Events["DeactivateGenerator"].active = IsEnabled;
            Events["RetrofitReactor"].active = !isupgraded && hasScience && myScience >= upgradeCost;
            Fields["upgradeCostStr"].guiActive = !isupgraded;

            List<PartResource> partresources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
            float currentscience = 0;
            foreach (PartResource partresource in partresources) {
                currentscience += (float)partresource.amount;
            }
            myScience = currentscience;

            upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";

            if (IsEnabled) {
                //if (DateTime.Now.Second - sectracker >= 1) {
                    float percentOutputPower = totalEff * 100.0f;
                    float outputPowerReport = -outputPower;
                    OutputPower = outputPowerReport.ToString("0.000") + "MW";
                    OverallEfficiency = percentOutputPower.ToString("0.0") + "%";
					
                    //sectracker = DateTime.Now.Second;
                //}
            }else{
                OutputPower = "Generator Offline";
            }

			if (totalEff >= 0) {
				MaxPowerStr = (maxThermalPower*totalEff).ToString ("0.000") + "MW";
			} else {
				MaxPowerStr = (0).ToString() + "MW";
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

        public override string GetInfo() {
            return String.Format("Max Thermal Power: {0}MW\n Percent of Carnot Efficiency: {1}%", maxThermalPower,pCarnotEff*100);
        }

        public override void OnFixedUpdate() {
			/*
			 * if (megamanager.getVessel() != vessel) {
				FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).deleteManager(megamanager);
			}

            if (!FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).hasManagerForVessel(vessel)) {
                megamanager = FNResourceOvermanager.getResourceOvermanagerForResource(FNResourceManager.FNRESOURCE_MEGAJOULES).createManagerForVessel(this);
                responsible_for_megajoulemanager = true;
                print("[WarpPlugin] Creating Megajoule Manager  for Vessel");
            }



            if (responsible_for_megajoulemanager) {
                megamanager.update();
            }
			 */

			base.OnFixedUpdate ();

            if (!IsEnabled) { return; }
                 
            double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
            if (carnotEff < 0) {
                recalculatePower();
                carnotEff = 1.0f - coldBathTemp / hotBathTemp;
                if (carnotEff < 0) {
                    IsEnabled = false;
                    carnotEff = 0;
                    ScreenMessages.PostScreenMessage("Generator Shutdown: No thermal power connected!");
					return;
                }
            }

			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Megajoules").id, partresources);
			float currentmegajoules = 0;
			foreach (PartResource partresource in partresources) {
				currentmegajoules += (float)(partresource.maxAmount - partresource.amount);
			}

            totalEff = (float)carnotEff * pCarnotEff;
			float thermal_power_currently_needed = (getCurrentResourceDemand (FNResourceManager.FNRESOURCE_MEGAJOULES) + currentmegajoules) / totalEff;
            double thermaldt = Math.Min(maxThermalPower,thermal_power_currently_needed) * TimeWarp.fixedDeltaTime;
            //double inputThermalPower = 0;
            //if (thermaldt < maxThermalPowerDraw) {
                //inputThermalPower = part.RequestResource("ThermalPower", thermaldt);
            double inputThermalPower = consumeFNResource(thermaldt, FNResourceManager.FNRESOURCE_THERMALPOWER);


            //}else {
            //    if (part.RequestResource("ThermalPower", maxThermalPowerDraw) >= maxThermalPowerDraw) {
            //        inputThermalPower = thermaldt;
            //    }
            //}
            double electricdt = inputThermalPower * totalEff;
            double electricdtps = electricdt / TimeWarp.fixedDeltaTime;
            outputPower = 0;
            if (electricdtps > 1) {
                electricdtps = electricdtps - 1;
                //outputPower += (float)part.RequestResource("Megajoules", -electricdtps * TimeWarp.fixedDeltaTime);
                //outputPower -= (float)megamanager.powerSupply(electricdtps * TimeWarp.fixedDeltaTime);
				//outputPower -= (float)megamanager.powerSupply(electricdtps * TimeWarp.fixedDeltaTime);
				outputPower -= (float)supplyFNResource(electricdtps * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
                outputPower += (part.RequestResource("ElectricCharge", -1000.0f * TimeWarp.fixedDeltaTime)) / 1000.0f;
            }else {
                electricdtps = electricdtps * 1000;
                outputPower += (part.RequestResource("ElectricCharge", -(float)electricdtps * TimeWarp.fixedDeltaTime)) / 1000.0f;
            }
            outputPower = outputPower / TimeWarp.fixedDeltaTime;
            //outputPower = (float)part.RequestResource("Megajoules", -electricdt);


            
        }


    }
}
