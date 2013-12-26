using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNFusionReactor : FNReactor {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        [KSPField(isPersistant = false)]
        public float powerRequirements;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Laser Consumption")]
        public string laserPower;

        PartResource deuterium;
        PartResource tritium;
        PartResource he3;
        protected double power_consumed;
        protected float initial_laser_consumption;
        protected float initial_resource_rate;

        [KSPEvent(guiActive = true, guiName = "Swap Fuel Mode", active = false)]
        public void SwapFuelMode() {
            fuel_mode++;
            if (fuel_mode > 2) {
                fuel_mode = 0;
            }
            setupFuelMode();
        }
        
        public override bool isNeutronRich() {
            if (fuel_mode == 2) {
                return false;
            }
            return true;
        }

        public override bool shouldScaleDownJetISP() {
            return true;
        }

        public override void OnStart(PartModule.StartState state) {
            deuterium = part.Resources["Deuterium"];
            tritium = part.Resources["Tritium"];
            he3 = part.Resources["Helium-3"];
            Fields["fuelmodeStr"].guiActive = true;
            Fields["fuelmodeStr"].guiActiveEditor = true;
            initial_laser_consumption = powerRequirements;
            initial_resource_rate = resourceRate;
            setupFuelMode();
            base.OnStart(state);
            if (isupgraded) {
                Events["SwapFuelMode"].active = true;
                Events["SwapFuelMode"].guiActiveEditor = true;
            }
        }

        public override void OnUpdate() {
            Fields["laserPower"].guiActive = IsEnabled;
            Fields["fuelmodeStr"].guiActive = true;
            laserPower = power_consumed.ToString("0.0") + "MW";
            base.OnUpdate();
        }

        public override string GetInfo() {
            float deut_rate_per_day = resourceRate * 86400;
            float up_deut_rate_per_day = upgradedResourceRate * 86400;
            if (!hasTechsRequiredToUpgrade()) {
                return String.Format(originalName + "\nCore Temperature: {0}K\n Total Power: {1}MW\n Laser Power Consumption: {6}MW\n D/T Max Consumption Rate: {2}Kg/day\n -Upgrade Information-\n Upgraded Core Temperate: {3}K\n Upgraded Power: {4}MW\n Upgraded D/T Consumption: {5}Kg/day", ReactorTemp, ThermalPower, deut_rate_per_day, upgradedReactorTemp, upgradedThermalPower, up_deut_rate_per_day, powerRequirements);
            } else {
                return String.Format(upgradedName + "\nThis part is available automatically upgraded\nCore Temperature: {0}K\n Total Power: {1}MW\n Laser Power Consumption: {3}MW\n D/T Max Consumption Rate: {2}Kg/day\n", upgradedReactorTemp, upgradedThermalPower, up_deut_rate_per_day, powerRequirements);
            }
        }

        public override string getResourceManagerDisplayName() {
            return reactorType + " Reactor";
        }

        protected override double consumeReactorResource(double resource) {
            double min_fuel = 0;
            if (fuel_mode == 0) {
                min_fuel = Math.Min(deuterium.amount, tritium.amount);
            } else if (fuel_mode == 1) {
                min_fuel = Math.Min(deuterium.amount, he3.amount);
            } else {
                min_fuel = he3.amount;
            }
            double consume_amount = Math.Min(min_fuel, resource/2.0);
            double consume_amount2 = Math.Min(min_fuel, resource);
            if (fuel_mode == 0 || fuel_mode == 1) {
                deuterium.amount -= consume_amount;
                if (fuel_mode == 0) {
                    tritium.amount -= consume_amount;
                } else {
                    he3.amount -= consume_amount;
                }
            } else {
                he3.amount -= consume_amount2;
                consume_amount = consume_amount2 / 2.0;
            }
            power_consumed = consumeFNResource(powerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.fixedDeltaTime;
            if (power_consumed < powerRequirements) {
                power_consumed += part.RequestResource("ElectricCharge", (powerRequirements-power_consumed) * 1000 * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime/1000;
            }
            if (power_consumed < powerRequirements * 0.9) {
                return 0;
                IsEnabled = false;
            }
            return 2*consume_amount;
        }

        protected override double returnReactorResource(double resource) {
            double return_amount = resource / 2.0;
            deuterium.amount += return_amount;
            tritium.amount += return_amount;
            if (deuterium.amount > deuterium.maxAmount) {
                deuterium.amount = deuterium.maxAmount;
            }
            if (tritium.amount > tritium.maxAmount) {
                tritium.amount = tritium.maxAmount;
            }
            return resource;
        }

        protected override string getResourceDeprivedMessage() {
            return fuelmodeStr + " deprived";
        }

        protected void setupFuelMode() {
            if (fuel_mode == 0) {
                fuelmodeStr = GameConstants.deuterium_tritium_fuel_mode;
                powerRequirements = initial_laser_consumption;
                chargedParticleRatio = 0.21f;
                resourceRate = initial_resource_rate;
            } else if (fuel_mode == 1) {
                fuelmodeStr = GameConstants.deuterium_helium3_fuel_mode;
                powerRequirements = initial_laser_consumption*1.4f;
                chargedParticleRatio = 0.8f;
                resourceRate = initial_resource_rate / 1.03977f;
            } else {
                fuelmodeStr = GameConstants.helium3_fuel_mode;
                powerRequirements = initial_laser_consumption * 2.1f;
                chargedParticleRatio = 1.0f;
                resourceRate = initial_resource_rate / 0.7329545f;
            }
        }
        
    }
}
