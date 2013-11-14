using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class FNReactor : FNResourceSuppliableModule, FNThermalSource    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;
        [KSPField(isPersistant = true)]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool breedtritium = false;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float ongoing_consumption_rate;
        [KSPField(isPersistant = true)]
        public bool reactorInit = false;

        // Persistent False
        [KSPField(isPersistant = false)]
        public float ReactorTemp;
        [KSPField(isPersistant = false)]
        public float ThermalPower;
        [KSPField(isPersistant = false)]
        public float upgradedReactorTemp;
        [KSPField(isPersistant = false)]
        public float upgradedThermalPower;
        [KSPField(isPersistant = false)]
		public string animName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
		public float upgradeCost;
		[KSPField(isPersistant = false)]
		public float radius; 
		[KSPField(isPersistant = false)]
		public string upgradeTechReq = null;
        [KSPField(isPersistant = false)]
        public float resourceRate;
        [KSPField(isPersistant = false)]
        public float upgradedResourceRate;
        [KSPField(isPersistant = false)]
        public float minimumThrottle = 0;
        
        // GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string reactorType;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temp")]
        public string coretempStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string statusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Tritium")]
		public string tritiumBreedRate;
                
        // Internal
        protected double resource_ratio;
        protected bool hasScience = false;
        //protected bool isNuclear = false;
		protected float powerPcnt = 0;
		protected Animation anim;
		protected bool play_down = true;
		protected bool play_up = true;
		protected float tritium_rate = 0;
		protected float tritium_produced_f = 0;
		protected bool hasrequiredupgrade = false;
		protected int deactivate_timer = 0;
        protected bool decay_products_ongoing = false;


        //protected bool responsible_for_thermalmanager = false;
        //protected FNResourceManager thermalmanager;
		       

        [KSPEvent(guiActive = true, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor() {
            if (getIsNuclear()) { return; }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor() {
            if (getIsNuclear()) { return; }
            IsEnabled = false;
        }

		[KSPEvent(guiActive = true, guiName = "Enable Tritium Breeding", active = false)]
		public void BreedTritium() {
            if (!getIsNuclear()) { return; }
			breedtritium = true;
		}

		[KSPEvent(guiActive = true, guiName = "Disable Tritium Breeding", active = true)]
		public void StopBreedTritium() {
            if (!getIsNuclear()) { return; }
			breedtritium = false;
		}

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; } 
			upgradePart ();
			ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
          
            //IsEnabled = false;
        }

        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            ActivateReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            IsEnabled = !IsEnabled;
        }

		public override void OnLoad(ConfigNode node) {
            if (isupgraded) {
				ThermalPower = upgradedThermalPower;
				ReactorTemp = upgradedReactorTemp;
				reactorType = upgradedName;
				resourceRate = upgradedResourceRate;
			}else {
				reactorType = originalName;
			}
            tritium_rate = ThermalPower/1000.0f/28800.0f;
		}

		public void upgradePart() {
			isupgraded = true;
			ThermalPower = upgradedThermalPower;
			ReactorTemp = upgradedReactorTemp;
			reactorType = upgradedName;
            resourceRate = upgradedResourceRate;
		}
		     
		public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_THERMALPOWER,FNResourceManager.FNRESOURCE_WASTEHEAT};
			this.resources_to_supply = resources_to_supply;

			base.OnStart(state);

            Actions["ActivateReactorAction"].guiName = Events["ActivateReactor"].guiName = String.Format("Activate Reactor");
            Actions["DeactivateReactorAction"].guiName = Events["DeactivateReactor"].guiName = String.Format("Deactivate Reactor");
            Actions["ToggleReactorAction"].guiName = String.Format("Toggle Reactor");
            
            if (state == StartState.Editor) { return; }

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

			if (reactorInit == false) {
				reactorInit = true;
				if(hasrequiredupgrade) {
					upgradePart();
				}
			}



			if(manual_upgrade) {
				hasrequiredupgrade = true;
			}

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
            
            this.part.force_activate();
            if (IsEnabled && last_active_time != 0) {
                double now = Planetarium.GetUniversalTime();
                double time_diff = now - last_active_time;
                double resource_to_take = consumeReactorResource(resourceRate*time_diff*ongoing_consumption_rate);
                if (breedtritium) {
                    List<PartResource> lithium_resources = new List<PartResource>();
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Lithium").id, lithium_resources);
                    float lithium_current_amount = 0;
                    foreach (PartResource lithium_resource in lithium_resources) {
                        lithium_current_amount += (float)lithium_resource.amount;
                    }

                    List<PartResource> tritium_resources = new List<PartResource>();
                    part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Tritium").id, tritium_resources);
                    float tritium_missing_amount = 0;
                    foreach (PartResource tritium_resource in tritium_resources) {
                        tritium_missing_amount += (float)(tritium_resource.maxAmount - tritium_resource.amount);
                    }

                    float lithium_to_take = (float)Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, lithium_current_amount);
                    float tritium_to_add = (float)-Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, tritium_missing_amount);
                    part.RequestResource("Lithium", lithium_to_take);
                    part.RequestResource("Tritium", tritium_to_add);
                }
            }
        }

        public override void OnUpdate() {
            Events["ActivateReactor"].active = !IsEnabled && !getIsNuclear();
            Events["DeactivateReactor"].active = IsEnabled && !getIsNuclear();
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitReactor"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitReactor"].active = false;
			}
            Events["BreedTritium"].active = !breedtritium && getIsNuclear();
            Events["StopBreedTritium"].active = breedtritium && getIsNuclear();
            Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;
            Fields["tritiumBreedRate"].guiActive = breedtritium && getIsNuclear();
            coretempStr = ReactorTemp.ToString("0") + "K";
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
            
			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			} 

			tritiumBreedRate = (tritium_produced_f * 86400).ToString ("0.00") + " Kg/day";
            
			if (IsEnabled) {
                if (resource_ratio > 0) {
					statusStr = "Active (" + powerPcnt.ToString ("0.00") + "%)";
				} else {
                    statusStr = getResourceDeprivedMessage();
				}
			} else {
                if (getIsNuclear()) {
                    if (decay_products_ongoing) {
                        statusStr = "Decay Heating (" + powerPcnt.ToString("0.00") + "%)";
                    } else {
                        statusStr = "EVA Maintenance Needed";
                    }
                }else {
                    statusStr = "Reactor Offline.";
                }
			}

            if (isupgraded) {
                reactorType = getThermalPowerFormatString() + " " + upgradedName;
            } else {
                reactorType = getThermalPowerFormatString() + " " + originalName;
            }
        }

        public float getCoreTemp() {
            return ReactorTemp;
        }

        public float getThermalPower() {
            return ThermalPower;
        }

		public virtual bool getIsNuclear() {
			return false;
		}

		public float getRadius() {
			return radius;
		}

		public bool isActive() {
			return IsEnabled;
		}

		public void enableIfPossible() {
			if (!getIsNuclear() && !IsEnabled) {
				IsEnabled = true;
			}
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
            if (IsEnabled && ThermalPower > 0) {
                if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95) {
                    deactivate_timer++;
                    if (deactivate_timer > 3) {
                        IsEnabled = false;
                        if (FlightGlobals.ActiveVessel == vessel) {
                            ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency reactor shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                    }
                    return;
                }
                deactivate_timer = 0;
                double resource_provided = consumeReactorResource(resourceRate * TimeWarp.fixedDeltaTime);
                resource_ratio = resource_provided / resourceRate / TimeWarp.fixedDeltaTime;
                double power_to_supply = Math.Max(ThermalPower * TimeWarp.fixedDeltaTime * resource_ratio, 0);
                double thermal_power_received = supplyManagedFNResourceWithMinimum(power_to_supply,minimumThrottle, FNResourceManager.FNRESOURCE_THERMALPOWER);
                if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) < 0.95) {
                    supplyFNResource(thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                }
                double thermal_power_ratio = thermal_power_received / ThermalPower / TimeWarp.fixedDeltaTime;
                ongoing_consumption_rate = (float)thermal_power_ratio;
                double return_ratio = 1 - thermal_power_ratio;
                returnReactorResource(resource_provided * return_ratio);
                powerPcnt = (float)(resource_ratio * 100.0 * thermal_power_ratio);
                if (getIsNuclear() && breedtritium) {
                    float lith_used = part.RequestResource("Lithium", tritium_rate * TimeWarp.fixedDeltaTime);
                    tritium_produced_f = -part.RequestResource("Tritium", -lith_used) / TimeWarp.fixedDeltaTime;
                    if (tritium_produced_f <= 0) {
                        breedtritium = false;
                    }
                }
                if (Planetarium.GetUniversalTime() != 0) {
                    last_active_time = (float)Planetarium.GetUniversalTime();
                }
                if (resource_ratio < minimumThrottle*0.99 && getIsNuclear()) {
                    IsEnabled = false;
                }
                decay_products_ongoing = false;
            } else {
                if (ThermalPower > 0 && Planetarium.GetUniversalTime() - last_active_time <= 3 * 86400 && getIsNuclear()) {
                    double daughter_half_life = 86400.0 / 24.0 * 9.0;
                    double time_t = Planetarium.GetUniversalTime() - last_active_time;
                    double power_fraction = 0.1 * Math.Exp(-time_t / daughter_half_life);
                    double power_to_supply = Math.Max(ThermalPower * TimeWarp.fixedDeltaTime * power_fraction, 0);
                    double thermal_power_received = supplyManagedFNResourceWithMinimum(power_to_supply,1.0, FNResourceManager.FNRESOURCE_THERMALPOWER);
                    supplyFNResource(thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                    double thermal_power_ratio = thermal_power_received / ThermalPower / TimeWarp.fixedDeltaTime;
                    powerPcnt = (float)(100.0 * thermal_power_ratio);
                    ongoing_consumption_rate = (float)thermal_power_ratio;
                    decay_products_ongoing = true;
                } else {
                    decay_products_ongoing = false;
                }
            }
        }

        protected virtual double consumeReactorResource(double resource) {
            return 0;
        }

        protected virtual double returnReactorResource(double resource) {
            return 0;
        }

        protected virtual string getResourceDeprivedMessage() {
            return "Resource Deprived";
        }

        protected string getThermalPowerFormatString() {
            if (ThermalPower > 1000) {
                if (ThermalPower > 20000) {
                    return (ThermalPower / 1000).ToString("0") + "GW";
                } else {
                    return (ThermalPower / 1000).ToString("0.0") + "GW";
                }
            } else {
                if (ThermalPower > 20) {
                    return ThermalPower.ToString("0") + "MW";
                } else {
                    return ThermalPower.ToString("0.0") + "MW";
                }
            }
        }

		public static double getTemperatureofHottestReactor(Vessel vess) {
			List<FNReactor> reactors = vess.FindPartModulesImplementing<FNReactor> ();
			double temp = 0;
			foreach (FNReactor reactor in reactors) {
				if (reactor != null) {
					if (reactor.getCoreTemp () > temp) {
						temp = reactor.getCoreTemp ();
					}
				}
			}
			return temp;
		}

		public static bool hasActiveReactors(Vessel vess) {
			List<FNReactor> reactors = vess.FindPartModulesImplementing<FNReactor> ();
			foreach (FNReactor reactor in reactors) {
				if (reactor != null) {
					if (reactor.IsEnabled) {
						return true;
					}
				}
			}
			return false;
		}


    }
}
