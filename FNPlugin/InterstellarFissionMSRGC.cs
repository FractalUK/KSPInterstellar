extern alias ORSv1_3;
using ORSv1_3::OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FNPlugin {
    [KSPModule("Fission Reactor")]
    class InterstellarFissionMSRGC : InterstellarReactor {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public override float MaximumThermalPower
        {
            get
            {
                if (part.Resources["Actinides"] != null)
                {
                    double fuel_mass = current_fuel_mode.ReactorFuels.Sum(fuel => getFuelAvailability(fuel) * fuel.Density);
                    double actinide_mass = part.Resources["Actinides"].amount;
                    double fuel_actinide_mass_ratio = Math.Min(fuel_mass / (actinide_mass * current_fuel_mode.NormalisedReactionRate * current_fuel_mode.NormalisedReactionRate * current_fuel_mode.NormalisedReactionRate * 2.5), 1.0);
                    fuel_actinide_mass_ratio = (double.IsInfinity(fuel_actinide_mass_ratio) || double.IsNaN(fuel_actinide_mass_ratio)) ? 1.0 : fuel_actinide_mass_ratio;
                    return (float)(base.MaximumThermalPower * Math.Sqrt(fuel_actinide_mass_ratio));
                }
                return base.MaximumThermalPower;
            }
        }

        public override float MinimumThermalPower { get { return MaximumThermalPower * minimumThrottle; } }

        [KSPEvent(guiName = "Swap Fuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]
        public void SwapFuelMode()
        {
            if (part.Resources["Actinides"].amount <= 0.01)
            {
                defuelCurrentFuel();
                if (isCurrentFuelDepleted())
                {
                    fuel_mode++;
                    if (fuel_mode >= fuel_modes.Count) fuel_mode = 0;
                    current_fuel_mode = fuel_modes[fuel_mode];
                    Refuel();
                }
            }
        }

        [KSPEvent(guiName = "Manual Restart", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualRestart() 
        {
            if (current_fuel_mode.ReactorFuels.All(fuel => getFuelAvailability(fuel) > 0.0001)) IsEnabled = true;
        }

        [KSPEvent(guiName = "Manual Shutdown", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ManualShutdown() 
        {
            IsEnabled = false;
        }

        [KSPEvent(guiName = "Refuel", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 3.5f)]
        public void Refuel() 
        {
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels) 
            {
                if (!part.Resources.Contains(fuel.FuelName) || !part.Resources.Contains("Actinides")) return; // avoid exceptions, just in case
                PartResource fuel_reactor = part.Resources[fuel.FuelName];
                PartResource actinides_reactor = part.Resources["Actinides"];
                List<PartResource> fuel_resources = part.GetConnectedResources(fuel.FuelName).ToList();
                double spare_capacity_for_fuel = fuel_reactor.maxAmount - actinides_reactor.amount;
                fuel_resources.ForEach(res =>
                {
                    double resource_available = res.amount;
                    double resource_added = Math.Min(resource_available, spare_capacity_for_fuel);
                    fuel_reactor.amount += resource_added;
                    res.amount -= resource_added;
                    spare_capacity_for_fuel -= resource_added;
                });
            }
        }

        public override void OnUpdate()
        {
            Events["ManualRestart"].active = Events["ManualRestart"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["ManualShutdown"].active = Events["ManualShutdown"].guiActiveUnfocused = IsEnabled;
            Events["Refuel"].active = Events["Refuel"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["SwapFuelMode"].active = Events["SwapFuelMode"].guiActiveUnfocused = !IsEnabled && !decay_ongoing;
            Events["Refuel"].guiName = "Refuel " + (current_fuel_mode != null ? current_fuel_mode.ModeGUIName : "");
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

        protected override double consumeReactorFuel(ReactorFuel fuel, double consume_amount)
        {
            if (!consumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName) && part.Resources.Contains("Actinides"))
                {
                    double amount = Math.Min(consume_amount, part.Resources[fuel.FuelName].amount / FuelEfficiency);
                    part.Resources[fuel.FuelName].amount -= amount;
                    part.Resources["Actinides"].amount += amount;
                    return amount;
                } else return 0;
            } else
            {
                return part.ImprovedRequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
            }
        }

        private void defuelCurrentFuel()
        {
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
            {
                PartResource fuel_reactor = part.Resources[fuel.FuelName];
                List<PartResource> swap_resource_list = part.GetConnectedResources(fuel.FuelName).ToList();
                swap_resource_list.ForEach(res =>
                {
                    double spare_capacity_for_fuel = res.maxAmount - res.amount;
                    double fuel_added = Math.Min(fuel_reactor.amount, spare_capacity_for_fuel);
                    fuel_reactor.amount -= fuel_added;
                    res.amount += fuel_added;
                });
            }
        }

        private bool isCurrentFuelDepleted()
        {
            return current_fuel_mode.ReactorFuels.Any(fuel => getFuelAvailability(fuel) < 0.001);
        }

    }
}
