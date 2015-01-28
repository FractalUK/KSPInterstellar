extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FNPlugin {
    [KSPModule("Fission Reactor")]
    class InterstellarFissionMSRGC : InterstellarReactor, INuclearFuelReprocessable {
        [KSPField(isPersistant = true)]
        public int fuel_mode = 0;

        public double WasteToReprocess { get { return part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides) ? part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount : 0; } }

        [KSPEvent(guiName = "Swap Fuel", externalToEVAOnly = true, guiActiveUnfocused = true, guiActive = false, unfocusedRange = 3.5f)]
        public void SwapFuelMode()
        {
            if (part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount <= 0.01)
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

        [KSPEvent(guiName = "Swap Fuel", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = false)]
        public void EditorSwapFuel()
        {
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels) part.Resources[fuel.FuelName].amount = 0;
            fuel_mode++;
            if (fuel_mode >= fuel_modes.Count) fuel_mode = 0;
            current_fuel_mode = fuel_modes[fuel_mode];
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels) part.Resources[fuel.FuelName].amount = part.Resources[fuel.FuelName].maxAmount;
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
                if (!part.Resources.Contains(fuel.FuelName) || !part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides)) return; // avoid exceptions, just in case
                PartResource fuel_reactor = part.Resources[fuel.FuelName];
                PartResource actinides_reactor = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
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

        public override bool IsNeutronRich { get { return !current_fuel_mode.Aneutronic; } }

        public override bool IsNuclear { get { return true; } }

        public override float MaximumThermalPower
        {
            get
            {
                try
                {
                    if (part.Resources[InterstellarResourcesConfiguration.Instance.Actinides] != null)
                    {
                        double fuel_mass = current_fuel_mode.ReactorFuels.Sum(fuel => getFuelAvailability(fuel) * fuel.Density);
                        double actinide_mass = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount;
                        double fuel_actinide_mass_ratio = Math.Min(fuel_mass / (actinide_mass * current_fuel_mode.NormalisedReactionRate * current_fuel_mode.NormalisedReactionRate * current_fuel_mode.NormalisedReactionRate * 2.5), 1.0);
                        fuel_actinide_mass_ratio = (double.IsInfinity(fuel_actinide_mass_ratio) || double.IsNaN(fuel_actinide_mass_ratio)) ? 1.0 : fuel_actinide_mass_ratio;
                        return (float)(base.MaximumThermalPower * Math.Sqrt(fuel_actinide_mass_ratio));
                    }
                    return base.MaximumThermalPower;
                }
                catch (Exception error)
                {
                    UnityEngine.Debug.Log("[KSPI] - InterstellarFissionMSRGC.MaximumThermalPower exception: " + error.Message);
                    return base.MaximumThermalPower;
                }
            }
        }

        public override float MinimumPower { get { return MaximumPower * minimumThrottle; } }

        public override float CoreTemperature
        {
            get
            {
                if (HighLogic.LoadedSceneIsFlight && !isupgraded)
                {
                    double temp_scale;
                    if (vessel != null && FNRadiator.hasRadiatorsForVessel(vessel))
                    {
                        temp_scale = FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
                    }
                    else
                    {
                        temp_scale = base.CoreTemperature / 2.0;
                    }
                    double temp_diff = (base.CoreTemperature - temp_scale) * Math.Sqrt(powerPcnt / 100.0);
                    return (float)(temp_scale + temp_diff);
                }
                else
                    return base.CoreTemperature;
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

        public override void OnStart(PartModule.StartState state)
        {
            // start as normal
            base.OnStart(state);

            // auto switch if current fuel mode is depleted
            if (isCurrentFuelDepleted())
                SwapFuelMode();
        }

        public override void OnFixedUpdate()
        {
            // if reactor is overloaded with actinides, stop functioning
            if (IsEnabled && part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                if (part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount >= part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount)
                {
                    part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].maxAmount;
                    IsEnabled = false;
                }
            }
            base.OnFixedUpdate();
        }

        public override bool shouldScaleDownJetISP()
        {
            return true;
        }

        public double ReprocessFuel(double rate)
        {
            if (part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
            {
                PartResource actinides = part.Resources[InterstellarResourcesConfiguration.Instance.Actinides];
                double new_actinides_amount = Math.Max(actinides.amount - rate, 0);
                double actinides_change = actinides.amount - new_actinides_amount;
                actinides.amount = new_actinides_amount;

                double depleted_fuels_change = actinides_change * 0.2;
                depleted_fuels_change = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.DepletedFuel, -depleted_fuels_change);

                double sum_useage_per_mw = current_fuel_mode.ReactorFuels.Sum(fuel => fuel.FuelUsePerMJ);

                foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
                {
                    PartResource fuel_resource = part.Resources[fuel.FuelName];
                    double fraction = sum_useage_per_mw > 0.0 ? fuel.FuelUsePerMJ / sum_useage_per_mw : 1;
                    double new_fuel_amount = Math.Min(fuel_resource.amount + depleted_fuels_change * 4.0*fraction, fuel_resource.maxAmount);
                    fuel_resource.amount = new_fuel_amount;
                }

                return actinides_change;
            }
            return 0;
        }

        protected override double consumeReactorFuel(ReactorFuel fuel, double consume_amount)
        {
            if (!consumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName) && part.Resources.Contains(InterstellarResourcesConfiguration.Instance.Actinides))
                {
                    double amount = Math.Min(consume_amount, part.Resources[fuel.FuelName].amount / FuelEfficiency);
                    part.Resources[fuel.FuelName].amount -= amount;
                    part.Resources[InterstellarResourcesConfiguration.Instance.Actinides].amount += amount;
                    return amount;
                } else return 0;
            } else
            {
                return part.ImprovedRequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
            }
        }

        protected override void setDefaultFuelMode()
        {
            current_fuel_mode = (fuel_mode < fuel_modes.Count) ? fuel_modes[fuel_mode] : fuel_modes.FirstOrDefault();
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
