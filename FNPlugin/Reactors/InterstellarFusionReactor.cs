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

        protected bool isSwappingFuelMode = false;

        public virtual double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        [KSPEvent(guiActive = true, guiName = "Next Fuel Mode", active = true)]
        public void SwapNextFuelMode()
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count)
                fuel_mode = 0;

            current_fuel_mode = fuel_modes[fuel_mode];

            isSwappingFuelMode = true;
        }

        [KSPEvent(guiActive = true, guiName = "Previous Fuel Mode", active = true)]
        public void SwapPreviousFuelMode()
        {
            if (fuel_modes == null || fuel_modes.Count == 0)
                return;

            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = fuel_modes.Count - 1;

            current_fuel_mode = fuel_modes[fuel_mode];

            isSwappingFuelMode = true;
        }
    }
}
