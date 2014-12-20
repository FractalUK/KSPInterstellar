using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPModule("Electrical Generator")]
	class FNGenerator : FNResourceSuppliableModule, IUpgradeableModule{
		// Persistent True
		[KSPField(isPersistant = true)]
		public bool IsEnabled = true;
		[KSPField(isPersistant = true)]
		public bool generatorInit = false;
		[KSPField(isPersistant = true)]
		public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        public bool chargedParticleMode;

		// Persistent False
		[KSPField(isPersistant = false)]
		public float pCarnotEff;
		[KSPField(isPersistant = false)]
		public float maxThermalPower;
        [KSPField(isPersistant = false)]
        public float maxChargedPower;
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradedpCarnotEff;
		[KSPField(isPersistant = false)]
		public string animName;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;
		[KSPField(isPersistant = false)]
		public float upgradeCost;
        [KSPField(isPersistant = false)]
        public float radius;
        [KSPField(isPersistant = false)]
        public string altUpgradedName;

		// GUI
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Type")]
		public string generatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Current Power")]
		public string OutputPower;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Max Power")]
		public string MaxPowerStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency")]
		public string OverallEfficiency;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade Cost")]
		public string upgradeCostStr;

		// Internal
		protected float coldBathTemp = 500;
		protected float hotBathTemp = 1;
		protected float outputPower;
		protected double totalEff;
		protected float sectracker = 0;
		protected bool play_down = true;
		protected bool play_up = true;
		protected IThermalSource myAttachedReactor;
		protected bool hasrequiredupgrade = false;
		protected long last_draw_update = 0;
		protected int shutdown_counter = 0;
		protected Animation anim;
		protected bool hasstarted = false;
        protected long update_count = 0;

        public String UpgradeTechnology { get { return upgradeTechReq; } }

		[KSPEvent(guiActive = true, guiName = "Activate Generator", active = true)]
		public void ActivateGenerator() {
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Deactivate Generator", active = false)]
		public void DeactivateGenerator() {
			IsEnabled = false;
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitGenerator() {
			if (ResearchAndDevelopment.Instance == null) { return;}
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
			upgradePartModule ();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
		}

        [KSPEvent(guiName = "Swap Type", guiActiveEditor = false, guiActiveUnfocused = false, guiActive = false)]
        public void EditorSwapType() {
            if (!chargedParticleMode) {
                generatorType = altUpgradedName;
                chargedParticleMode = true;
            } else {
                generatorType = upgradedName;
                chargedParticleMode = false;
            }
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

		public void upgradePartModule () {
			isupgraded = true;
			pCarnotEff = upgradedpCarnotEff;
            if (chargedParticleMode) {
                generatorType = altUpgradedName;
            } else {
                generatorType = upgradedName;
            }
            Events["EditorSwapType"].guiActiveEditor = true;
		}

        public void OnEditorAttach() {
            List<IThermalSource> source_list = part.attachNodes.Where(atn => atn.attachedPart != null).SelectMany(atn => atn.attachedPart.FindModulesImplementing<IThermalSource>()).ToList();
            myAttachedReactor = source_list.FirstOrDefault();
            if (myAttachedReactor != null && myAttachedReactor is IChargedParticleSource && (myAttachedReactor as IChargedParticleSource).ChargedParticleRatio > 0)
            {
                generatorType = altUpgradedName;
                chargedParticleMode = true;
            }
        }

		public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES,FNResourceManager.FNRESOURCE_WASTEHEAT};
			this.resources_to_supply = resources_to_supply;
			base.OnStart (state);
            generatorType = originalName;
            if (state == StartState.Editor) {
                if (this.HasTechsRequiredToUpgrade())
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                    upgradePartModule();
                }
                part.OnEditorAttach += OnEditorAttach;
                return;
            }

            if (this.HasTechsRequiredToUpgrade())
            {
                hasrequiredupgrade = true;
            }

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

			if (generatorInit == false) {
				generatorInit = true;
				IsEnabled = true;
			}

			if (isupgraded) {
				upgradePartModule ();
			}

            List<IThermalSource> source_list = part.attachNodes.Where(atn => atn.attachedPart != null).SelectMany(atn => atn.attachedPart.FindModulesImplementing<IThermalSource>()).ToList();
            myAttachedReactor = source_list.FirstOrDefault();
			print("[KSP Interstellar] Configuring Generator");
		}

		public override void OnUpdate() {
			Events["ActivateGenerator"].active = !IsEnabled;
			Events["DeactivateGenerator"].active = IsEnabled;
			Fields["OverallEfficiency"].guiActive = IsEnabled;
			Fields["MaxPowerStr"].guiActive = IsEnabled;
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitGenerator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
				upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
			} else {
				Events ["RetrofitGenerator"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded  && hasrequiredupgrade;

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
				float percentOutputPower = (float) (totalEff * 100.0);
				float outputPowerReport = -outputPower;
				if (update_count - last_draw_update > 10) {
                    OutputPower = getPowerFormatString(outputPowerReport) + "_e";
					OverallEfficiency = percentOutputPower.ToString ("0.0") + "%";
					if (totalEff >= 0) {
                        if (!chargedParticleMode) {
                            MaxPowerStr = getPowerFormatString(maxThermalPower * totalEff) + "_e";
                        } else {
                            MaxPowerStr = getPowerFormatString(maxChargedPower * totalEff) + "_e";
                        }
					} else {
						MaxPowerStr = (0).ToString() + "MW";
					}
                    last_draw_update = update_count;
				}
			} else {
				OutputPower = "Generator Offline";
			}

            update_count++;
		}

		public float getMaxPowerOutput() {
            float maxTotalEff = 0;
            if (!chargedParticleMode) {
                double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
                maxTotalEff = (float)carnotEff * pCarnotEff;
                return maxThermalPower * maxTotalEff;
            } else {
                maxTotalEff = 0.85f;
                return maxChargedPower * maxTotalEff;
            }
		}

        public float getCurrentPower() {
            return outputPower;
        }

        public bool isActive() {
            return IsEnabled;
        }

        public IThermalSource getThermalSource() {
            return myAttachedReactor;
        }
               

		public void updateGeneratorPower() {
			hotBathTemp = myAttachedReactor.CoreTemperature;
            float heat_exchanger_thrust_divisor = 1;
            if (radius > myAttachedReactor.getRadius()) {
                heat_exchanger_thrust_divisor = myAttachedReactor.getRadius() * myAttachedReactor.getRadius() / radius / radius;
            } else {
                heat_exchanger_thrust_divisor = radius * radius / myAttachedReactor.getRadius() / myAttachedReactor.getRadius();
            }
            if (myAttachedReactor.getRadius() <= 0 || radius <= 0) {
                heat_exchanger_thrust_divisor = 1;
            }
			maxThermalPower = myAttachedReactor.MaximumPower*heat_exchanger_thrust_divisor;

            if (myAttachedReactor is IChargedParticleSource) 
                maxChargedPower = (myAttachedReactor as IChargedParticleSource).MaximumChargedPower * heat_exchanger_thrust_divisor;
            else 
                maxChargedPower = 0;
            
			coldBathTemp = (float) FNRadiator.getAverageRadiatorTemperatureForVessel (vessel);
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			if (IsEnabled && myAttachedReactor != null && FNRadiator.hasRadiatorsForVessel (vessel)) {
				updateGeneratorPower ();
                double electricdt = 0;
                double electricdtps = 0;
                double max_electricdtps = 0;
                double input_power = 0;
                double currentmegajoules = getSpareResourceCapacity(FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                double electrical_power_currently_needed = (getCurrentUnfilledResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) + currentmegajoules);
                if (!chargedParticleMode) {
                    double carnotEff = 1.0 - coldBathTemp / hotBathTemp;
                    totalEff = carnotEff * pCarnotEff;
                    if (totalEff <= 0 || coldBathTemp <= 0 || hotBathTemp <= 0 || maxThermalPower <= 0) {
                        return;
                    }
                    double thermal_power_currently_needed = electrical_power_currently_needed / totalEff;
                    double thermaldt = Math.Max(Math.Min(maxThermalPower, thermal_power_currently_needed) * TimeWarp.fixedDeltaTime, 0.0);
                    input_power = consumeFNResource(thermaldt, FNResourceManager.FNRESOURCE_THERMALPOWER);
                    if (input_power < thermaldt) {
                        input_power += consumeFNResource(thermaldt-input_power, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                    }
                    double wastedt = input_power * totalEff;
                    consumeFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);
                    electricdt = input_power * totalEff;
                    electricdtps = Math.Max(electricdt / TimeWarp.fixedDeltaTime, 0.0);
                    max_electricdtps = maxThermalPower * totalEff;
                } else {
                    totalEff = 0.85;
                    double charged_power_currently_needed = electrical_power_currently_needed / totalEff;
                    input_power = consumeFNResource(Math.Max(charged_power_currently_needed*TimeWarp.fixedDeltaTime,0), FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                    electricdt = input_power * totalEff;
                    electricdtps = Math.Max(electricdt / TimeWarp.fixedDeltaTime, 0.0);
                    double wastedt = input_power * totalEff;
                    max_electricdtps = maxChargedPower * totalEff;
                    consumeFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);
                    //supplyFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);
                }
				outputPower = -(float)supplyFNResourceFixedMax (electricdtps * TimeWarp.fixedDeltaTime, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
			} else {
				if (IsEnabled && !vessel.packed) {
					if (!FNRadiator.hasRadiatorsForVessel (vessel)) {
						IsEnabled = false;
						Debug.Log ("[WarpPlugin] Generator Shutdown: No radiators available!");
						ScreenMessages.PostScreenMessage ("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}

					if (myAttachedReactor == null) {
						IsEnabled = false;
						Debug.Log ("[WarpPlugin] Generator Shutdown: No reactor available!");
						ScreenMessages.PostScreenMessage ("Generator Shutdown: No reactor available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}
				}
			}
		}

		public override string GetInfo() {
			return String.Format("Percent of Carnot Efficiency: {0}%\n-Upgrade Information-\n Upgraded Percent of Carnot Efficiency: {1}%", pCarnotEff*100, upgradedpCarnotEff*100);
		}
                
        protected string getPowerFormatString(double power) {
            if (power > 1000) {
                if (power > 20000) {
                    return (power / 1000).ToString("0.0") + " GW";
                } else {
                    return (power / 1000).ToString("0.00") + " GW";
                }
            } else {
                if (power > 20) {
                    return power.ToString("0.0") + " MW";
                } else {
                    if (power > 1) {
                        return power.ToString("0.00") + " MW";
                    } else {
                        return (power * 1000).ToString("0.0") + " KW";
                    }
                }
            }
        }

	}
}

