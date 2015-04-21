using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin 
{
    enum PowerStates { powerChange, powerOnline, powerDown, powerOffline };

    [KSPModule("Electrical Generator")]
    class FNGenerator : FNResourceSuppliableModule, IUpgradeableModule, IElectricPowerSource
    {
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Max Thermal Power")]
		public float _maxThermalPower;
        [KSPField(isPersistant = false)]
        public float _maxChargedPower;
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Radius")]
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Combined Power", guiUnits = " MW_e")]
        public float _totalMaximumPowerAllReactors;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Exchange Divisor")]
        public float heat_exchanger_thrust_divisor;

		// Internal
		protected float coldBathTemp = 500;
		protected float hotBathTemp = 1;
		protected float outputPower;
		protected double _totalEff;
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
        protected int partDistance;

        private PowerStates _powerState;

        public String UpgradeTechnology { get { return upgradeTechReq; } }

		[KSPEvent(guiActive = true, guiName = "Activate Generator", active = true)]
		public void ActivateGenerator() 
        {
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
        public void EditorSwapType() 
        {
            if (!chargedParticleMode) 
            {
                generatorType = altUpgradedName;
                chargedParticleMode = true;
            } 
            else 
            {
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

		public void upgradePartModule () 
        {
			isupgraded = true;
			pCarnotEff = upgradedpCarnotEff;
            generatorType = chargedParticleMode ? altUpgradedName : upgradedName;
            Events["EditorSwapType"].guiActiveEditor = true;
		}

        public void OnEditorAttach() 
        {
            FindAttachedThermalSource();
        }

		public override void OnStart(PartModule.StartState state) 
        {
            previousTimeWarp = TimeWarp.fixedDeltaTime;
            String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES,FNResourceManager.FNRESOURCE_WASTEHEAT};
			this.resources_to_supply = resources_to_supply;
			base.OnStart (state);
            generatorType = originalName;

            if (state == StartState.Editor) 
            {
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
                hasrequiredupgrade = true;

			this.part.force_activate();
			
			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) 
            {
				anim [animName].layer = 1;
				if (!IsEnabled) 
                {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;
				} 
                else 
                {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;
				}
				anim.Play ();
			}

			if (generatorInit == false) 
            {
				generatorInit = true;
				IsEnabled = true;
			}

			if (isupgraded) 
				upgradePartModule ();

            FindAttachedThermalSource();

            UpdateHeatExchangedThrustDivisor();

            UpdateMaximumPowerAllReactors();

            print("[KSP Interstellar] Configuring Generator");
		}

        private void FindAttachedThermalSource()
        {
            partDistance = 0;

            // first look if part contains an thermal source
            myAttachedReactor = part.FindModulesImplementing<IThermalSource>().FirstOrDefault();
            if (myAttachedReactor != null) return;

            // otherwise look for other non selfcontained thermal sources
            var source = ThermalSourceSearchResult.BreadthFirstSearchForThermalSource(part, 3, 0, true);
            if (source == null) return;
            
            // verify cost is not higher than 1
            partDistance = (int)Math.Max(Math.Ceiling(source.Cost) - 1, 0);
            if (partDistance > 0) return;

            myAttachedReactor = source.Source;
        }

        private void UpdateMaximumPowerAllReactors()
        {
            //_totalMaximumPowerAllReactors = part.vessel.FindPartModulesImplementing<IThermalSource>().Where(t => t.IsActive).Sum(t => t.MaximumPower);
            _totalMaximumPowerAllReactors = (float)part.vessel.FindPartModulesImplementing<IElectricPowerSource>().Sum(t => t.MaxStableMegaWattPower);
        }

		public override void OnUpdate() 
        {
            UpdateMaximumPowerAllReactors();

			Events["ActivateGenerator"].active = !IsEnabled;
			Events["DeactivateGenerator"].active = IsEnabled;
			Fields["OverallEfficiency"].guiActive = IsEnabled;
			Fields["MaxPowerStr"].guiActive = IsEnabled;

			if (ResearchAndDevelopment.Instance != null) 
            {
				Events ["RetrofitGenerator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
				upgradeCostStr = ResearchAndDevelopment.Instance.Science.ToString("0") + " / " + upgradeCost;
			} 
            else 
				Events ["RetrofitGenerator"].active = false;
			
			Fields["upgradeCostStr"].guiActive = !isupgraded  && hasrequiredupgrade;

			if (IsEnabled) 
            {
				if (play_up && anim != null) 
                {
					play_down = true;
					play_up = false;
					anim [animName].speed = 1f;
					anim [animName].normalizedTime = 0f;
					anim.Blend (animName, 2f);
				}
			} 
            else 
            {
				if (play_down && anim != null) 
                {
					play_down = false;
					play_up = true;
					anim [animName].speed = -1f;
					anim [animName].normalizedTime = 1f;
					anim.Blend (animName, 2f);
				}
			}
            
			if (IsEnabled) 
            {
				float percentOutputPower = (float) (_totalEff * 100.0);
				float outputPowerReport = -outputPower;
				if (update_count - last_draw_update > 10) 
                {
                    OutputPower = getPowerFormatString(outputPowerReport) + "_e";
					OverallEfficiency = percentOutputPower.ToString ("0.00") + "%";

                    MaxPowerStr = (_totalEff >= 0) 
                        ? !chargedParticleMode
                            ? getPowerFormatString(_maxThermalPower * _totalEff) + "_e"
                            : getPowerFormatString(_maxChargedPower * _totalEff) + "_e"
                        : (0).ToString() + "MW";

                    last_draw_update = update_count;
				}
			} 
            else 
				OutputPower = "Generator Offline";

            update_count++;
		}

		public float getMaxPowerOutput() 
        {
            if (!chargedParticleMode) 
            {
                double carnotEff = 1.0f - coldBathTemp / hotBathTemp;
                return _maxThermalPower * (float)carnotEff * pCarnotEff;
            } 
            else 
                return _maxChargedPower * 0.85f;
		}


        public bool isActive() { return IsEnabled; }

        public IThermalSource getThermalSource() {  return myAttachedReactor;  }

        public double MaxStableMegaWattPower
        {
            get
            {
                return myAttachedReactor != null && IsEnabled 
                    ? chargedParticleMode 
                        ? myAttachedReactor.StableMaximumThermalPower * 0.85 
                        : myAttachedReactor.StableMaximumThermalPower * pCarnotEff 
                    : 0;
            }
        }

        private void UpdateHeatExchangedThrustDivisor()
        {
            if (myAttachedReactor == null) return;

            if (myAttachedReactor.getRadius() <= 0 || radius <= 0)
            {
                heat_exchanger_thrust_divisor = 1;
                return;
            }

            heat_exchanger_thrust_divisor = radius > myAttachedReactor.getRadius()
                ? myAttachedReactor.getRadius() * myAttachedReactor.getRadius() / radius / radius
                : radius * radius / myAttachedReactor.getRadius() / myAttachedReactor.getRadius();
        }

		public void updateGeneratorPower() 
        {
            if (myAttachedReactor == null) return;

			hotBathTemp = myAttachedReactor.CoreTemperature;

            if (HighLogic.LoadedSceneIsEditor)
                UpdateHeatExchangedThrustDivisor();

            _maxThermalPower = myAttachedReactor.MaximumPower * heat_exchanger_thrust_divisor;

            _maxChargedPower = (myAttachedReactor is IChargedParticleSource)
                ? (myAttachedReactor as IChargedParticleSource).MaximumChargedPower * heat_exchanger_thrust_divisor : 0;
            
			coldBathTemp = (float) FNRadiator.getAverageRadiatorTemperatureForVessel (vessel);


		}

        private double _previousMaxStableMegaWattPower;

        private double previousTimeWarp;

		public override void OnFixedUpdate() 
        {
			base.OnFixedUpdate ();
			if (IsEnabled && myAttachedReactor != null && FNRadiator.hasRadiatorsForVessel (vessel)) 
            {
				updateGeneratorPower();

                // check if MaxStableMegaWattPower is changed
                var maxStableMegaWattPower = MaxStableMegaWattPower;
                if (maxStableMegaWattPower != _previousMaxStableMegaWattPower)
                    _powerState = PowerStates.powerChange;
                _previousMaxStableMegaWattPower = maxStableMegaWattPower;

                if (maxStableMegaWattPower > 0 && (TimeWarp.fixedDeltaTime != previousTimeWarp || _powerState != PowerStates.powerOnline))
                {
                    _powerState = PowerStates.powerOnline;

                    var requiredMegawattCapacity = Math.Max(1, TimeWarp.fixedDeltaTime * maxStableMegaWattPower);
                    var previousMegawattCapacity = Math.Max(1, previousTimeWarp * maxStableMegaWattPower);

                    PartResource megajouleResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES);

                    if (megajouleResource != null)
                    {
                        var oldRatio = megajouleResource.amount / megajouleResource.maxAmount;

                        megajouleResource.maxAmount = requiredMegawattCapacity;
                        
                        if (maxStableMegaWattPower > 0)
                        megajouleResource.amount = requiredMegawattCapacity > previousMegawattCapacity
                                ? Math.Max(0, Math.Min(requiredMegawattCapacity, megajouleResource.amount + requiredMegawattCapacity - previousMegawattCapacity))
                                : Math.Max(0, Math.Min(requiredMegawattCapacity, oldRatio * requiredMegawattCapacity));
                    }

                    PartResource wasteheatResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
                    if (wasteheatResource != null)
                    {
                        var previousMaxAmount = wasteheatResource.maxAmount;
                        wasteheatResource.maxAmount = TimeWarp.fixedDeltaTime * part.mass * 1000;
                        this.part.RequestResource(FNResourceManager.FNRESOURCE_WASTEHEAT, previousTimeWarp > TimeWarp.fixedDeltaTime ? previousMaxAmount - wasteheatResource.maxAmount : 0);
                    }

                    PartResource electricChargeResource = part.Resources.list.FirstOrDefault(r => r.resourceName == "ElectricCharge");
                    if (electricChargeResource != null)
                    {
                        if (maxStableMegaWattPower <= 0)
                            
                        electricChargeResource.maxAmount = requiredMegawattCapacity;
                        electricChargeResource.amount = maxStableMegaWattPower <= 0 ? 0 : Math.Min(electricChargeResource.maxAmount, electricChargeResource.amount);
                    }
                }
                previousTimeWarp = TimeWarp.fixedDeltaTime;

                // don't produce any power when our reactor has stopped
                if (maxStableMegaWattPower <= 0)
                {
                    PowerDown();
                    return;
                }
                else
                {
                    powerDownFraction = 1;
                }

                var electrical_power_currently_needed = getCurrentUnfilledResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) 
                    + getSpareResourceCapacity(FNResourceManager.FNRESOURCE_MEGAJOULES);

                double electricdt = 0;
                double electricdtps = 0;
                double max_electricdtps = 0;

                if (!chargedParticleMode) 
                {
                    var thermalTransportationEfficency = (2f + myAttachedReactor.ThermalTransportationEfficiency) / 3f;
                    double carnotEff = 1.0 - coldBathTemp / hotBathTemp;
                    _totalEff = carnotEff * pCarnotEff * thermalTransportationEfficency;
                    if (_totalEff <= 0 || coldBathTemp <= 0 || hotBathTemp <= 0 || _maxThermalPower <= 0) return;
                    
                    double thermal_power_currently_needed = electrical_power_currently_needed / _totalEff;
                    double thermaldt = Math.Max(Math.Min(_maxThermalPower, thermal_power_currently_needed) * TimeWarp.fixedDeltaTime, 0.0);
                    double input_power = consumeFNResource(thermaldt, FNResourceManager.FNRESOURCE_THERMALPOWER);

                    if (input_power < thermaldt) 
                        input_power += consumeFNResource(thermaldt-input_power, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);

                    double wastedt = input_power * _totalEff;

                    consumeFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);
                    electricdt = input_power * _totalEff;
                    electricdtps = Math.Max(electricdt / TimeWarp.fixedDeltaTime, 0.0);
                    max_electricdtps = _maxThermalPower * _totalEff;
                } 
                else 
                {
                    _totalEff = 0.85;
                    double charged_power_currently_needed = electrical_power_currently_needed / _totalEff;
                    double input_power = consumeFNResource(Math.Max(charged_power_currently_needed * TimeWarp.fixedDeltaTime, 0), FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                    electricdt = input_power * _totalEff;
                    electricdtps = Math.Max(electricdt / TimeWarp.fixedDeltaTime, 0.0);
                    double wastedt = input_power * _totalEff;
                    max_electricdtps = _maxChargedPower * _totalEff;
                    consumeFNResource(wastedt, FNResourceManager.FNRESOURCE_WASTEHEAT);
                }
				outputPower = -(float)supplyFNResourceFixedMax (electricdtps * TimeWarp.fixedDeltaTime, max_electricdtps * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
			} 
            else 
            {
                previousTimeWarp = TimeWarp.fixedDeltaTime;
                if (IsEnabled && !vessel.packed)
                {
                    if (!FNRadiator.hasRadiatorsForVessel(vessel))
                    {
                        IsEnabled = false;
                        Debug.Log("[WarpPlugin] Generator Shutdown: No radiators available!");
                        ScreenMessages.PostScreenMessage("Generator Shutdown: No radiators available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }

                    if (myAttachedReactor == null)
                    {
                        IsEnabled = false;
                        Debug.Log("[WarpPlugin] Generator Shutdown: No reactor available!");
                        ScreenMessages.PostScreenMessage("Generator Shutdown: No reactor available!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        PowerDown();
                    }
                }
                else
                {
                    PowerDown();
                }
			}
            
		}

        private double powerDownFraction;

        private void PowerDown()
        {
            if (_powerState != PowerStates.powerOffline)
            {
                if (powerDownFraction <= 0)
                    _powerState = PowerStates.powerOffline;
                else
                    powerDownFraction -= 0.01;

                PartResource megajouleResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_MEGAJOULES);
                if (megajouleResource != null)
                {
                    megajouleResource.maxAmount = Math.Max(1, megajouleResource.maxAmount * powerDownFraction);
                    megajouleResource.amount = Math.Min(megajouleResource.maxAmount, megajouleResource.amount);
                }

                PartResource electricChargeResource = part.Resources.list.FirstOrDefault(r => r.resourceName == "ElectricCharge");
                if (electricChargeResource != null)
                {
                    electricChargeResource.maxAmount = Math.Max(1, electricChargeResource.maxAmount * powerDownFraction);
                    electricChargeResource.amount = Math.Min(electricChargeResource.maxAmount, electricChargeResource.amount);
                }
            }
        }

		public override string GetInfo() 
        {
			return String.Format("Percent of Carnot Efficiency: {0}%\n-Upgrade Information-\n Upgraded Percent of Carnot Efficiency: {1}%", pCarnotEff*100, upgradedpCarnotEff*100);
		}
                
        protected string getPowerFormatString(double power) 
        {
            if (power > 1000) 
            {
                if (power > 20000) 
                    return (power / 1000).ToString("0.0") + " GW";
                else 
                    return (power / 1000).ToString("0.00") + " GW";
            } 
            else 
            {
                if (power > 20) 
                    return power.ToString("0.0") + " MW";
                else 
                {
                    if (power > 1) 
                        return power.ToString("0.00") + " MW";
                    else 
                        return (power * 1000).ToString("0.0") + " KW";
                }
            }
        }

	}
}

