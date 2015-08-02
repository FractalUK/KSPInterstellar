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
        public float powerRequirements = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Plasma Ratio")]
        public float plasma_ratio = 1.0f;

        public override float StableMaximumReactorPower { get { return IsEnabled && plasma_ratio >= 1 ? RawPowerOutput : 0; } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override float MaximumThermalPower { get { return base.MaximumThermalPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        public override float MaximumChargedPower { get { return base.MaximumChargedPower * (plasma_ratio >= 1.0 ? 1 : 0.000000001f); } }

        public override float CoreTemperature {  get { return base.CoreTemperature * (current_fuel_mode != null ? (float)Math.Sqrt(current_fuel_mode.NormalisedPowerRequirements) : 1); } }

        protected bool isSwappingFuelMode = false;

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

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

            if (!HasAllFuels() && fuel_mode != initial_fuel_mode)
                SwitchToNextFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
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

            if (!HasAllFuels() && fuel_mode != initial_fuel_mode)
                SwitchToPreviousFuelMode(initial_fuel_mode);

            isSwappingFuelMode = true;
        }
    }
}
