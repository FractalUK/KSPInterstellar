extern alias ORSv1_3;
using ORSv1_3::OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Fission Reactor")]
    class InterstellarFissionPBDP : InterstellarReactor
    {
        // Persistant False
        [KSPField(isPersistant = false)]
        public float optimalPebbleTemp;
        [KSPField(isPersistant = false)]
        public float tempZeroPower;

        // Properties
        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public override float MinimumThermalPower { get { return MaximumThermalPower * minimumThrottle; } }

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void ManualRestart()
        {
            IsEnabled = true;
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void ManualShutdown()
        {
            IsEnabled = false;
        }

        public override void OnUpdate()
        {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            base.OnUpdate();
        }

        public override void OnFixedUpdate()
        {
            // if reactor is overloaded with actinides, stop functioning
            if (IsEnabled && part.Resources.Contains("Actinides"))
            {
                if (part.Resources["Actinides"].amount >= part.Resources["Actinides"].maxAmount)
                {
                    part.Resources["Actinides"].amount = part.Resources["Actinides"].maxAmount;
                    IsEnabled = false;
                }
            }
            base.OnFixedUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }
    }
}
