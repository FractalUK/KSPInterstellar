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

        public override float MaximumThermalPower { get { return (float)(base.MaximumThermalPower * Math.Pow((tempZeroPower - CoreTemperature) / (tempZeroPower - optimalPebbleTemp), 0.81)); } }

        public override float MinimumThermalPower { get { return MaximumThermalPower * minimumThrottle; } }

        public override bool IsNuclear { get { return true; } }

        public override float CoreTemperature
        {
            get
            {
                double temp_scale;
                if (vessel != null && FNRadiator.hasRadiatorsForVessel(vessel))
                {
                    temp_scale = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
                } else
                {
                    temp_scale = optimalPebbleTemp;
                }
                return (float)Math.Min(Math.Max(Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25) * temp_scale * 1.5, optimalPebbleTemp), tempZeroPower);
            }
        }

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

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        protected override double consumeReactorFuel(ReactorFuel fuel, double consume_amount)
        {
            if (!consumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName) && part.Resources.Contains("DepletedFuel"))
                {
                    double amount = Math.Min(consume_amount, part.Resources[fuel.FuelName].amount / FuelEfficiency);
                    part.Resources[fuel.FuelName].amount -= amount;
                    part.Resources["DepletedFuel"].amount += amount;
                    return amount;
                } else return 0;
            } else
            {
                return part.ImprovedRequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
            }
        }


    }
}
