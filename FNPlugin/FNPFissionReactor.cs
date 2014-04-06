﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    [KSPModule("Fission Reactor")]
    class FNPFissionReactor : FNReactor {
        [KSPField(isPersistant = false)]
        public float optimalPebbleTemp;
        [KSPField(isPersistant = false)]
        public float tempZeroPower;

        protected PartResource uranium_mononitride;
        protected PartResource depleted_fuel;
        protected float initial_thermal_power;
        protected float initial_resource_rate;

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualRestart() {
            //if (fuel_resource.amount > 0.001) {
                IsEnabled = true;
            //}
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualShutdown() {
            IsEnabled = false;
        }

        public override bool getIsNuclear() {
            return true;
        }

        public override bool isNeutronRich() {
            return true;
        }

        public override bool shouldScaleDownJetISP() {
            return true;
        }

        public override float getCoreTemp() {
            return ReactorTemp;
        }

        public override float getMinimumThermalPower() {
            return getThermalPower() * minimumThrottle;
        }

        public override float getCoreTempAtRadiatorTemp(float rad_temp) {
            float pfr_temp = 0;
            if (!isupgraded) {
                if (!double.IsNaN(rad_temp) && !double.IsInfinity(rad_temp)) {
                    pfr_temp = (float)Math.Min(Math.Max(rad_temp * 1.5, optimalPebbleTemp), tempZeroPower);
                } else {
                    pfr_temp = optimalPebbleTemp;
                }
            } else {
                return ReactorTemp;
            }
            return pfr_temp;
        }

        public override float getThermalPowerAtTemp(float temp) {
            float rel_temp_diff = 0;
            if (temp > optimalPebbleTemp && temp < tempZeroPower && !isupgraded) {
                rel_temp_diff = (float)Math.Pow((tempZeroPower - temp) / (tempZeroPower - optimalPebbleTemp), 0.81);
            } else {
                rel_temp_diff = 1;
            }
            return ThermalPower * rel_temp_diff;
        }

        public override void OnStart(PartModule.StartState state) {
            uranium_mononitride = part.Resources["UraniumNitride"];
            depleted_fuel = part.Resources["DepletedFuel"];
            base.OnStart(state);
            initial_thermal_power = ThermalPower;
            initial_resource_rate = resourceRate;
        }

        public override void OnUpdate() {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            base.OnUpdate();
        }

        public override string GetInfo() {
            float un_rate_per_day = resourceRate * 86400;
            float up_un_rate_per_day = upgradedResourceRate * 86400;
            return String.Format("[Base Part Information]\nPart Name: {0}\nCore Temperature: {1:n0}K\nTotal Power Output: {2:n0}MW\nUranium Nitride Consumption Rate (Max):{3}L/day\n\n[Upgrade Information]\nScience Tech Required:\n- Fusion Power\nPart Name: {4}\nCore Temperature: {5:n0}K\nTotal Power Output: {6:n0}MW\nUranium Nitride Consumption Rate (Max):{7}L/day", originalName, ReactorTemp, ThermalPower, un_rate_per_day, upgradedName, upgradedReactorTemp, upgradedThermalPower, up_un_rate_per_day);
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            if (IsEnabled && !isupgraded) {
                double temp_scale;
                if(FNRadiator.hasRadiatorsForVessel(vessel)) {
                    temp_scale = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
                }else{
                    temp_scale = optimalPebbleTemp;
                }
                ReactorTemp = (float) Math.Min(Math.Max(Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25)*temp_scale*1.5,optimalPebbleTemp),tempZeroPower);
                //ReactorTemp = (float) (Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25) * temp_scale * 1.5);
                float rel_temp_diff = (float) Math.Pow((tempZeroPower - ReactorTemp)/(tempZeroPower - optimalPebbleTemp),0.81);
                ThermalPower = initial_thermal_power * rel_temp_diff;
                resourceRate = initial_resource_rate * rel_temp_diff;
            } else if (IsEnabled && isupgraded) {
                ThermalPower = upgradedThermalPower;
                resourceRate = upgradedResourceRate;
            }
        }

        protected override double consumeReactorResource(double resource) {
            resource = Math.Min(uranium_mononitride.amount, resource);
            uranium_mononitride.amount -= resource;
            depleted_fuel.amount += resource*2.86;
            if (depleted_fuel.amount > depleted_fuel.maxAmount) {
                depleted_fuel.amount = depleted_fuel.maxAmount;
            }
            return resource;
        }

        protected override double returnReactorResource(double resource) {
            uranium_mononitride.amount += resource;
            if (uranium_mononitride.amount > uranium_mononitride.maxAmount) {
                uranium_mononitride.amount = uranium_mononitride.maxAmount;
            }
            depleted_fuel.amount -= Math.Min(resource * 2.86, depleted_fuel.amount);
            return resource;
        }

    }
}
