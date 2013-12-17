using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNNuclearReactor : FNReactor {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool upgradedToV08 = false;
        [KSPField(isPersistant = true)]
        public bool uranium_fuel = true;
        
        //Internal
        protected PartResource thf4;
        protected PartResource uf4;
        protected PartResource fuel_resource;
        protected PartResource actinides;
        protected double initial_thermal_power = 0;
        protected double initial_resource_rate = 0;
        
        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void ManualRestart() {
            if (fuel_resource.amount > 0.001) {
                IsEnabled = true;
            }
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void ManualShutdown() {
            IsEnabled = false;
        }

        [KSPEvent(guiName = "Refuel UF4", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void RefuelUranium() {
            List<PartResource> uf6_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF4").id, uf6_resources);
            double spare_capacity_for_uf6 = uf4.maxAmount - actinides.amount;
            foreach (PartResource uf6_resource in uf6_resources) {
                if (uf6_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double uf6_available = uf6_resource.amount;
                    double uf6_added = Math.Min(uf6_available, spare_capacity_for_uf6);
                    uf4.amount += uf6_added;
                    uf6_resource.amount -= uf6_added;
                    spare_capacity_for_uf6 -= uf6_added;
                }
            }
        }

        [KSPEvent(guiName = "Refuel ThF4", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void RefuelThorium() {
            List<PartResource> th4_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, th4_resources);
            double spare_capacity_for_thf4 = thf4.maxAmount - actinides.amount;
            foreach (PartResource thf4_resource in th4_resources) {
                if (thf4_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double thf4_available = thf4_resource.amount;
                    double thf4_added = Math.Min(thf4_available, spare_capacity_for_thf4);
                    thf4.amount += thf4_added;
                    thf4_resource.amount -= thf4_added;
                    spare_capacity_for_thf4 -= thf4_added;
                }
            }
        }

        [KSPEvent(guiName = "Swap Fuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.0f)]
        public void SwapFuel() {
            if (actinides.amount <= 0.0001) {
                if (uranium_fuel) {
                    defuelUranium();
                    if (uf4.amount > 0) { return; }
                    setThoriumFuel();
                    RefuelThorium();
                } else {
                    defuelThorium();
                    if (thf4.amount > 0) { return; }
                    setUraniumFuel();
                    RefuelUranium();
                }
            }
        }

        [KSPEvent(guiName = "Swap Fuel", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
        public void EditorSwapFuel() {
            if (uranium_fuel) {
                uranium_fuel = !uranium_fuel;
                uf4.amount = 0;
                thf4.amount = thf4.maxAmount;
                fuelmodeStr = "Thorium";
            } else {
                uranium_fuel = !uranium_fuel;
                thf4.amount = 0;
                uf4.amount = uf4.maxAmount;
                fuelmodeStr = "Uranium";
            }
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
            if (uranium_fuel) {
                return ReactorTemp;
            } else {
                return (float) (ReactorTemp * GameConstants.thorium_temperature_ratio_factor);
            }
        }

        public override string GetInfo() {
            float uf6_rate_per_day = resourceRate * 86400;
            float up_uf6_rate_per_day = upgradedResourceRate * 86400;
            if (!hasTechsRequiredToUpgrade()) {
                return String.Format(originalName + "\nCore Temperature: {0}K\n Total Power: {1}MW\n UF4 Max Consumption Rate: {2}m³/day\n -Upgrade Information-\n Upgraded Core Temperate: {3}K\n Upgraded Power: {4}MW\n Upgraded UF4 Consumption: {5}m³/day", ReactorTemp, ThermalPower, uf6_rate_per_day, upgradedReactorTemp, upgradedThermalPower, up_uf6_rate_per_day);
            } else {
                return String.Format(upgradedName + "\nThis part is available automatically upgraded\nCore Temperature: {0}K\n Total Power: {1}MW\n UF4 Max Consumption Rate: {2}m³/day\n", ReactorTemp, ThermalPower, uf6_rate_per_day);
            }
        }

        public override void OnStart(PartModule.StartState state) {
            uf4 = part.Resources["UF4"];
            thf4 = part.Resources["ThF4"];
            actinides = part.Resources["Actinides"];
            Fields["fuelmodeStr"].guiActiveEditor = true;
            if (double.IsNaN(uf4.amount)) {
                uf4.amount = 0;
            }
            if (double.IsNaN(thf4.amount)) {
                thf4.amount = 0;
            }
            if (double.IsNaN(actinides.amount)) {
                actinides.amount = actinides.maxAmount;
            }
            if (!upgradedToV08) {
                upgradedToV08 = true;
                actinides.amount = actinides.maxAmount - uf4.amount;
            }
            if (uranium_fuel) {
                fuel_resource = uf4;
            } else {
                fuel_resource = thf4;
            }
            base.OnStart(state);
            initial_thermal_power = ThermalPower;
            initial_resource_rate = resourceRate;
            if (uranium_fuel) {
                setUraniumFuel();
            } else {
                setThoriumFuel();
            }
        }

        public override void OnUpdate() {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            Events["RefuelUranium"].active = Events["RefuelUranium"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing && uranium_fuel;
            Events["RefuelThorium"].active = Events["RefuelThorium"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing && !uranium_fuel;
            Events["SwapFuel"].active = Events["SwapFuel"].guiActiveUnfocused = !IsEnabled && !decay_products_ongoing;
            base.OnUpdate();
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
        }

        protected override double consumeReactorResource(double resource) {
            double fuel_to_actinides_ratio = fuel_resource.amount / (actinides.amount + fuel_resource.amount) * fuel_resource.amount / (actinides.amount + fuel_resource.amount);
            if (!uranium_fuel) {
                if (!double.IsInfinity(fuel_to_actinides_ratio) && !double.IsNaN(fuel_to_actinides_ratio)) {
                    resource = resource * Math.Min(Math.Exp(-GameConstants.thorium_actinides_ratio_factor / fuel_to_actinides_ratio + 1), 1);
                }
            }
            double actinides_max_amount = actinides.maxAmount;
            resource = Math.Min(fuel_resource.amount, resource);
            fuel_resource.amount -= resource;
            actinides.amount += resource;
            if (actinides.amount > actinides_max_amount) {
                actinides.amount = actinides_max_amount;
            }
            return resource;
        }

        protected override double returnReactorResource(double resource) {
            fuel_resource.amount += resource;
            double actinides_current_amount = actinides.amount;
            if (fuel_resource.amount > fuel_resource.maxAmount) {
                fuel_resource.amount = fuel_resource.maxAmount;
            }
            actinides.amount -= Math.Min(resource, actinides_current_amount);
            return resource;
        }
        
        protected override string getResourceDeprivedMessage() {
            if (uranium_fuel) {
                return "UF4 Deprived";
            } else {
                return "ThF4 Deprived";
            }
        }

        protected void setThoriumFuel() {
            fuel_resource = thf4;
            fuelmodeStr = "Thorium";
            ThermalPower = (float)(initial_thermal_power * GameConstants.thorium_power_output_ratio);
            resourceRate = (float)(initial_resource_rate * GameConstants.thorium_resource_burnrate_ratio);
            uranium_fuel = false;
        }

        protected void setUraniumFuel() {
            fuel_resource = uf4;
            fuelmodeStr = "Uranium";
            ThermalPower = (float)(initial_thermal_power);
            resourceRate = (float)(initial_resource_rate);
            uranium_fuel = true;
        }

        protected void defuelThorium() {
            List<PartResource> swap_resource_list = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ThF4").id, swap_resource_list);
            foreach (PartResource thf4_resource in swap_resource_list) {
                if (thf4_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double spare_capacity_for_thf4 = thf4_resource.maxAmount - thf4_resource.amount;
                    double thf4_added = Math.Min(thf4.amount, spare_capacity_for_thf4);
                    thf4.amount -= thf4_added;
                    thf4_resource.amount += thf4_added;
                }
            }
        }

        protected void defuelUranium() {
            List<PartResource> swap_resource_list = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("UF4").id, swap_resource_list);
            foreach (PartResource uf6_resource in swap_resource_list) {
                if (uf6_resource.part.FindModulesImplementing<FNNuclearReactor>().Count == 0) {
                    double spare_capacity_for_uf6 = uf6_resource.maxAmount - uf6_resource.amount;
                    double uf6_added = Math.Min(uf4.amount, spare_capacity_for_uf6);
                    uf4.amount -= uf6_added;
                    uf6_resource.amount += uf6_added;
                }
            }
        }

        
    }
}
