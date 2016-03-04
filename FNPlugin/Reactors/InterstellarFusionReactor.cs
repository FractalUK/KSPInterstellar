using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IChargedParticleSource
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;
        [KSPField(isPersistant = true)]
        public bool allowJumpStart = true;

        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk1 = 10;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk2 = 20;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk3 = 40;
        [KSPField(isPersistant = false)]
        public float fusionEnergyGainFactorMk4 = 80;

        [KSPField(isPersistant = false)]
        public string upgradeTechReqMk2 = null;
        [KSPField(isPersistant = false)]
        public string upgradeTechReqMk3 = null;
        [KSPField(isPersistant = false)]
        public string upgradeTechReqMk4 = null;
        [KSPField(isPersistant = false)]
        public string upgradeTechReqMk5 = null;

        [KSPField(isPersistant = false)]
        public float powerOutputMk1 = 0;
        [KSPField(isPersistant = false)]
        public float powerOutputMk2 = 0;
        [KSPField(isPersistant = false)]
        public float powerOutputMk3 = 0;
        [KSPField(isPersistant = false)]
        public float powerOutputMk4 = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Maintance")]
        public string electricPowerMaintenance;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Plasma Ratio")]
        public float plasma_ratio = 1.0f;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Is Swapping Fuel Mode")]
        public bool isSwappingFuelMode = false;

        public GenerationType FusionGenerationType { get; private set; }

        public float MaximumChargedIspMult { get { return 100 ; } }

        public float MinimumChargdIspMult { get { return 1; } }

        public override float StableMaximumReactorPower { get { return IsEnabled && plasma_ratio >= 1 ? RawPowerOutput : 0; } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override float MaximumThermalPower { get { return base.MaximumThermalPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        public override float CoreTemperature {  get { return base.CoreTemperature * (current_fuel_mode != null ? (float)Math.Sqrt(current_fuel_mode.NormalisedPowerRequirements) : 1); } }

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public float PowerRequirement { get { return RawPowerOutput / FusionEnergyGainFactor; } }

        public float FusionEnergyGainFactor
        {
            get
            {
                if (FusionGenerationType == GenerationType.Mk4)
                    return fusionEnergyGainFactorMk4;
                else if (FusionGenerationType == GenerationType.Mk3)
                    return fusionEnergyGainFactorMk3;
                else if (FusionGenerationType == GenerationType.Mk2)
                    return fusionEnergyGainFactorMk2;
                else
                    return fusionEnergyGainFactorMk1;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            // initialise power output when missing
            if (powerOutputMk1 == 0)
                powerOutputMk1 = PowerOutput;
            if (powerOutputMk2 == 0)
                powerOutputMk2 = PowerOutput * 1.5f;
            if (powerOutputMk3 == 0)
                powerOutputMk3 = PowerOutput * 1.5f * 1.5f;
            if (powerOutputMk4 == 0)
                powerOutputMk4 = PowerOutput * 1.5f * 1.5f * 1.5f;

            // initialse tech requirment if missing 
            if (upgradeTechReqMk2 == null)
                upgradeTechReqMk2 = upgradeTechReq;
            if (upgradeTechReqMk3 == null)
                upgradeTechReqMk3 = powerUpgradeTechReq;

            DetermineGenerationType();

            // call Interstellar Reactor Onstart
            base.OnStart(state);
        }

        private void DetermineGenerationType()
        {
            // determine number of upgrade techs
            int nrAvailableUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk5))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk4))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk3))
                nrAvailableUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReqMk2))
                nrAvailableUpgradeTechs++;

            // determine fusion tech levels
            if (nrAvailableUpgradeTechs == 5)
                FusionGenerationType = GenerationType.Mk5;
            else if (nrAvailableUpgradeTechs == 4)
                FusionGenerationType = GenerationType.Mk4;
            else if (nrAvailableUpgradeTechs == 3)
                FusionGenerationType = GenerationType.Mk3;
            else if (nrAvailableUpgradeTechs == 2)
                FusionGenerationType = GenerationType.Mk2;
            else
                FusionGenerationType = GenerationType.Mk1;
        }

        public override float RawPowerOutput
        {
            get
            {
                float rawPowerOutput;
                if (FusionGenerationType == GenerationType.Mk4)
                    rawPowerOutput = powerOutputMk4;
                else if (FusionGenerationType == GenerationType.Mk3)
                    rawPowerOutput = powerOutputMk3;
                else if (FusionGenerationType == GenerationType.Mk2)
                    rawPowerOutput = powerOutputMk2;
                else 
                    rawPowerOutput = powerOutputMk1;

                return rawPowerOutput * powerOutputMultiplier;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next Fuel Mode", active = true)]
        public void SwapNextFuelMode()
        {
            SwitchToNextFuelMode(fuel_mode);
        }

        private void SwitchToNextFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count)
                fuel_mode = 0;

            current_fuel_mode = fuel_modes[fuel_mode];

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToNextFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }

        private bool FullFuelRequirments()
        {
            return HasAllFuels() && FuelRequiresLab(current_fuel_mode.RequiresLab);
        }

        private bool HasAllFuels()
        {
            bool hasAllFuels = true;
            foreach (var fuel in current_fuel_mode.ReactorFuels)
            {
                if (GetFuelRatio(fuel, FuelEfficiency, NormalisedMaximumPower) < 1)
                {
                    hasAllFuels = false;
                    break;
                }
            }
            return hasAllFuels;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous Fuel Mode", active = true)]
        public void SwapPreviousFuelMode()
        {
            SwitchToPreviousFuelMode(fuel_mode);
        }

        private void SwitchToPreviousFuelMode(int initial_fuel_mode)
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuel_modes.Count - 1;

            current_fuel_mode = fuel_modes[fuel_mode];

            UpdateFuelMode();

            if (!FullFuelRequirments() && fuel_mode != initial_fuel_mode)
                SwitchToPreviousFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }
    }
}
