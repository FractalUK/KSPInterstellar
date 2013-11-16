using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNFusionReactor : FNReactor {
        [KSPField(isPersistant = false)]
        public float powerRequirements;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Laser Consumption")]
        public string laserPower;

        PartResource deuterium;
        PartResource tritium;
        protected double power_consumed;

        public override bool isNeutronRich() {
            return true;
        }

        public override bool shouldScaleDownJetISP() {
            return true;
        }

        public override void OnStart(PartModule.StartState state) {
            deuterium = part.Resources["Deuterium"];
            tritium = part.Resources["Tritium"];
            base.OnStart(state);
        }

        public override void OnUpdate() {
            Fields["laserPower"].guiActive = IsEnabled;
            laserPower = power_consumed.ToString("0.0") + "MW";
            base.OnUpdate();
        }

        public override string GetInfo() {
            float deut_rate_per_day = resourceRate * 86400;
            float up_deut_rate_per_day = upgradedResourceRate * 86400;
            return String.Format("Core Temperature: {0}K\n Thermal Power: {1}MW\n Laser Power Consumption: {6}MW\n D/T Max Consumption Rate: {2}Kg/day\n -Upgrade Information-\n Upgraded Core Temperate: {3}K\n Upgraded Power: {4}MW\n Upgraded D/T Consumption: {5}Kg/day", ReactorTemp, ThermalPower, deut_rate_per_day, upgradedReactorTemp, upgradedThermalPower, up_deut_rate_per_day,laserPower);
        }

        protected override double consumeReactorResource(double resource) {
            double min_fuel = Math.Min(deuterium.amount, tritium.amount);
            double consume_amount = Math.Min(min_fuel, resource/2.0);
            deuterium.amount -= consume_amount;
            tritium.amount -= consume_amount;
            power_consumed = consumeFNResource(powerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.fixedDeltaTime;
            if (power_consumed < powerRequirements * 0.9) {
                return 0;
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
            return "Deuterium/Tritium Deprived";
        }
        
    }
}
