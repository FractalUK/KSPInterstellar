using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Fission Reactor")]
    class InterstellarFissionPBDP : InterstellarReactor, IChargedParticleSource
    {
        // Persistant False
        [KSPField(isPersistant = false)]
        public float optimalPebbleTemp;
        [KSPField(isPersistant = false)]
        public float tempZeroPower;

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

        // Properties
        public double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return current_fuel_mode != null ? !current_fuel_mode.Aneutronic : false; } }

        public override float MaximumThermalPower { get { return isupgraded ? base.MaximumThermalPower : (float)(base.MaximumThermalPower * Math.Pow((tempZeroPower - CoreTemperature) / (tempZeroPower - optimalPebbleTemp), 0.81)); } }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override bool IsNuclear { get { return true; } }

        public override float CoreTemperature
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight && !isupgraded)
                {
                    double temp_scale = (vessel != null && FNRadiator.hasRadiatorsForVessel(vessel)) ? FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel) : optimalPebbleTemp;
                    return (float)Math.Min(Math.Max(Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25) * temp_scale * 1.5, optimalPebbleTemp), tempZeroPower);
                } return base.CoreTemperature;
            }
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
                if (part.Resources.Contains(fuel.FuelName) && part.Resources.Contains(InterstellarResourcesConfiguration.Instance.DepletedFuel))
                {
                    double amount = Math.Min(consume_amount, part.Resources[fuel.FuelName].amount / FuelEfficiency);
                    part.Resources[fuel.FuelName].amount -= amount;
                    part.Resources[InterstellarResourcesConfiguration.Instance.DepletedFuel].amount += amount;
                    return amount;
                } else return 0;
            } else
            {
                return part.ImprovedRequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
            }
        }

        public override float GetCoreTempAtRadiatorTemp(float rad_temp)
        {
            float pfr_temp = 0;
            if (!isupgraded)
            {
                if (!double.IsNaN(rad_temp) && !double.IsInfinity(rad_temp))
                {
                    pfr_temp = (float)Math.Min(Math.Max(rad_temp * 1.5, optimalPebbleTemp), tempZeroPower);
                } else
                {
                    pfr_temp = optimalPebbleTemp;
                }
            } else
            {
                return ReactorTemp;
            }
            return pfr_temp;
        }

        public override float GetThermalPowerAtTemp(float temp)
        {
            float rel_temp_diff = 0;
            if (temp > optimalPebbleTemp && temp < tempZeroPower && !isupgraded)
            {
                rel_temp_diff = (float)Math.Pow((tempZeroPower - temp) / (tempZeroPower - optimalPebbleTemp), 0.81);
            } else
            {
                rel_temp_diff = 1;
            }
            return MaximumPower * rel_temp_diff;
        }


    }
}
