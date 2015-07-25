using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    class InterstellarTokamakFusionReactor : InterstellarFusionReactor
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Maintance")]
        public string tokomakPower;

        public bool fusion_alert = false;
        public float power_consumed = 0.0f;
        public int jumpstartPowerTime = 0;
        public int fusionAlertFrames = 0;

        // properties
        public override float StableMaximumReactorPower { get { return IsEnabled && plasma_ratio >= 1 ? RawPowerOutput : 0; } }

        //public override float MaximumThermalPower { get { return base.MaximumThermalPower * (plasma_ratio > 0.0f ? Mathf.Pow(plasma_ratio, 4.0f) : 0.0f); } }
        public override float MaximumThermalPower { get { return base.MaximumThermalPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        //public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio > 0.0f ? Mathf.Pow(plasma_ratio, 4.0f) : 0.0f); } }
        public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Reactor"; } }

        public float HeatingPowerRequirements 
		{ 
			get { 
				return current_fuel_mode == null 
					? powerRequirements 
					: (float)(powerRequirements * current_fuel_mode.NormalisedPowerRequirements); 
			} 
		}

        public override void OnUpdate() 
        {
            base.OnUpdate();
            if (
                //getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) * 1.2 > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES)
                //getDemandSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1
                getDemandStableSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) > 1.01

                // getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES)
                // && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 
                && IsEnabled && !fusion_alert)
            {
                fusionAlertFrames++;
            }
            else
            {
                fusion_alert = false;
                fusionAlertFrames = 0;
            }

            if (fusionAlertFrames > 2)
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 0.1f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            }

            //Events["SwapFuelMode"].active = isupgraded;
            Events["SwapNextFuelMode"].active = true;
            Events["SwapPreviousFuelMode"].active = true;
            tokomakPower = PluginHelper.getFormattedPowerString(power_consumed) + "/" + PluginHelper.getFormattedPowerString(HeatingPowerRequirements);
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();
            if (IsEnabled) 
            {
                //var fixedHeatingPowerRequirements = HeatingPowerRequirements * TimeWarp.fixedDeltaTime;

                // don't try to start fusion is we don't have the power
                //var availablePower = (float)getResourceAvailability(FNResourceManager.FNRESOURCE_MEGAJOULES);
                //if (availablePower >= fixedHeatingPowerRequirements)
                //var stableSupply = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                //if (stableSupply > fixedHeatingPowerRequirements)
                power_consumed = consumeFNResource(HeatingPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                //else
                //    power_consumed = stableSupply;

                //if (power_consumed < HeatingPowerRequirements) 
                //    power_consumed += part.RequestResource("ElectricCharge", (HeatingPowerRequirements - power_consumed) * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime / 1000.0;
                //plasma_ratio= ((HeatingPowerRequirements != 0.0f) ? power_consumed / HeatingPowerRequirements : 1.0f);

                if(isSwappingFuelMode)
                {
                    plasma_ratio = 1;
                    isSwappingFuelMode = false;
                }
                else if (jumpstartPowerTime > 0)
                {
                    plasma_ratio = 1;
                    jumpstartPowerTime--;
                }
                else
                {
                    plasma_ratio = (float)Math.Round(HeatingPowerRequirements != 0.0f ? power_consumed / HeatingPowerRequirements : 1.0f, 4);
                    allowJumpStart = plasma_ratio == 1;
                }
            }
            else
            {
                plasma_ratio = 0;
                power_consumed = 0;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor)
            {
                //breedtritium = true;

                if (allowJumpStart)
                {
                    if (startDisabled)
                        allowJumpStart = false;
                    else
                        jumpstartPowerTime = 100;
                    
                    UnityEngine.Debug.LogWarning("[KSPI] - InterstellarTokamakFusionReactor.OnStart allowJumpStart");
                }
            }

            base.OnStart(state);
        }

        public override string getResourceManagerDisplayName() 
        {
            return TypeName;
        }

        public override int getPowerPriority() 
        {
            return 1;
        }

        protected override void setDefaultFuelMode()
        {
            current_fuel_mode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
        }

    }
}
