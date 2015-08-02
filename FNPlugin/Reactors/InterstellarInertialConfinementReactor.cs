using System;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("IC Fusion Reactor")]
    class InterstellarInertialConfinementReactor : InterstellarFusionReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Maintenance")]
        public string laserPower;
        [KSPField(isPersistant = true)]
        protected double accumulatedElectricChargeInMW;


        [KSPField(isPersistant = false, guiActive = true, guiName = "Charge")]
        public string accumulatedChargeStr = String.Empty;

        protected float power_consumed;
        protected bool fusion_alert = false;
        protected int shutdown_c = 0;
        public int jumpstartPowerTime = 0;
        protected bool isChargingForJumpstart;

        [KSPEvent(guiActive = true, guiName = "Charge Jumpstart", active = true)]
        public void ChargeStartup()
        {
            isChargingForJumpstart = true;
        }

        public override void OnStart(PartModule.StartState state)
        {
            Events["SwapNextFuelMode"].active = true;
            Events["SwapPreviousFuelMode"].active = true;

            if (state != StartState.Editor && allowJumpStart)
            {
                if (startDisabled)
                    allowJumpStart = false;
                else
                    jumpstartPowerTime = 100;

                UnityEngine.Debug.LogWarning("[KSPI] - InterstellarInertialConfinementReactor.OnStart allowJumpStart");
            }
            base.OnStart(state);
        }

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Reactor"; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

	    [KSPField(isPersistant = false, guiActive = true, guiName = "HeatingPowerRequirements")]
	    public float LaserPowerRequirements
	    {
		    get { return current_fuel_mode == null 
				? powerRequirements 
				: (float)(powerRequirements * current_fuel_mode.NormalisedPowerRequirements); }
	    }
        
        public override bool shouldScaleDownJetISP() 
        {
            return isupgraded ? false : true;
        }

        public override void OnUpdate() 
        {
            if (!isSwappingFuelMode && getCurrentResourceDemand(FNResourceManager.FNRESOURCE_MEGAJOULES) > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) && getResourceBarRatio(FNResourceManager.FNRESOURCE_MEGAJOULES) < 0.1 && IsEnabled && !fusion_alert) 
            {
                ScreenMessages.PostScreenMessage("Warning: Fusion Reactor plasma heating cannot be guaranteed, reducing power requirements is recommended.", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                fusion_alert = true;
            } 
            else 
                fusion_alert = false;

            Events["SwapNextFuelMode"].active = true;
            Events["SwapPreviousFuelMode"].active = true;

            Fields["accumulatedChargeStr"].guiActive = plasma_ratio < 1;


            laserPower = PluginHelper.getFormattedPowerString(power_consumed) + "/" + PluginHelper.getFormattedPowerString(LaserPowerRequirements);
            base.OnUpdate();
        }

        public override void OnFixedUpdate() 
        {
            base.OnFixedUpdate();

	        if (!IsEnabled) return;

            if (isChargingForJumpstart)
            {
                var neededPower = LaserPowerRequirements - accumulatedElectricChargeInMW;
                if (neededPower > 0)
                    accumulatedElectricChargeInMW += part.RequestResource("ElectricCharge", neededPower * 1000) / 1000;

                if (accumulatedElectricChargeInMW >= LaserPowerRequirements)
                    isChargingForJumpstart = false;
            }

            accumulatedChargeStr = FNGenerator.getPowerFormatString(accumulatedElectricChargeInMW) + " / " + FNGenerator.getPowerFormatString(LaserPowerRequirements);

            if (!IsEnabled)
            {
                plasma_ratio = 0;
                power_consumed = 0;
                return;
            }

			//power_consumed = part.RequestResource(FNResourceManager.FNRESOURCE_MEGAJOULES, LaserPowerRequirements * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
            power_consumed = consumeFNResource(LaserPowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

            if (TimeWarp.fixedDeltaTime <= 0.1 && accumulatedElectricChargeInMW > 0 && power_consumed < LaserPowerRequirements && (accumulatedElectricChargeInMW + power_consumed) >= LaserPowerRequirements)
            {
                var shortage = LaserPowerRequirements - power_consumed;
                if (shortage <= accumulatedElectricChargeInMW)
                {
                    ScreenMessages.PostScreenMessage("Attempting to Jump start", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    power_consumed += (float)accumulatedElectricChargeInMW;
                }
            }

	        //plasma_ratio = power_consumed / LaserPowerRequirements;
            if (isSwappingFuelMode)
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
                plasma_ratio = (float)Math.Round(LaserPowerRequirements != 0.0f ? power_consumed / LaserPowerRequirements : 1.0f, 4);
                allowJumpStart = plasma_ratio == 1;
            }


            if (plasma_ratio >= 0.99)
            {
                plasma_ratio = 1;
                isChargingForJumpstart = false;
                framesPlasmaRatioIsGood++;
                if (framesPlasmaRatioIsGood > 10)
                    accumulatedElectricChargeInMW = 0;
            }
            else
            {
                framesPlasmaRatioIsGood = 0;

                if (plasma_ratio < 0.001)
                    plasma_ratio = 0;
            }
        }

        private int framesPlasmaRatioIsGood;

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
