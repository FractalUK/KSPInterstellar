using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    class InterstellarTokamakFusionReator : InterstellarFusionReactor
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heating maintance")]
        public string tokomakPower;

        protected bool fusion_alert = false;
        protected double power_consumed = 0.0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Plasma Ratio")]
        public float plasma_ratio = 1.0f;

        // properties

        public override float MaximumThermalPower { get { return base.MaximumThermalPower * (plasma_ratio > 0.0f ? Mathf.Pow(plasma_ratio, 4.0f) : 0.0f); } }

        public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio > 0.0f ? Mathf.Pow(plasma_ratio, 4.0f) : 0.0f); } }

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
                getCurrentHighPriorityResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) * 1.2 > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES)
                // getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES)
                // && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 
                && IsEnabled && !fusion_alert) 
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            } 
            else 
                fusion_alert = false;

            Events["SwapFuelMode"].active = isupgraded;
            tokomakPower = PluginHelper.getFormattedPowerString(power_consumed);
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();
            if (IsEnabled) 
            {
                power_consumed = consumeFNResource(HeatingPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                if (power_consumed < HeatingPowerRequirements) 
                    power_consumed += part.RequestResource("ElectricCharge", (HeatingPowerRequirements - power_consumed) * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime / 1000.0;
                plasma_ratio = (HeatingPowerRequirements != 0.0f) ? (float)(power_consumed / HeatingPowerRequirements) : 1.0f;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state != StartState.Editor) breedtritium = true;
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
