using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class FNResourceManager {
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
		public const string FNRESOURCE_WASTEHEAT = "WasteHeat";
		public const int FNRESOURCE_FLOWTYPE_SMALLEST_FIRST = 0;
		public const int FNRESOURCE_FLOWTYPE_EVEN = 1;
		protected const double passive_temp_p4 = 2947.295521;
               
        protected Vessel my_vessel;
        protected Part my_part;
        protected PartModule my_partmodule;
        protected Dictionary<FNResourceSuppliable, double> power_draws;
		List<PartResource> partresources;
        protected String resource_name;
        //protected Dictionary<MegajouleSuppliable, float> power_returned;
        protected double powersupply = 0;
		protected double stable_supply = 0;
		protected double stored_stable_supply = 0;
		protected double current_resource_demand = 0;
		protected double high_priority_resource_demand = 0;
		protected double charge_resource_demand = 0;
		protected int flow_type = 0;

		protected double resource_bar_ratio = 0;

        public FNResourceManager(PartModule pm,String resource_name) {
            my_vessel = pm.vessel;
            my_part = pm.part;
            my_partmodule = pm;
            power_draws = new Dictionary<FNResourceSuppliable,double>();
            this.resource_name = resource_name;

			if (resource_name == FNRESOURCE_WASTEHEAT || resource_name == FNRESOURCE_THERMALPOWER) {
				flow_type = FNRESOURCE_FLOWTYPE_EVEN;
			} else {
				flow_type = FNRESOURCE_FLOWTYPE_SMALLEST_FIRST;
			}
        }

        public void powerDraw(FNResourceSuppliable pm, double power_draw) {
            if (power_draws.ContainsKey(pm)) {
                power_draw = power_draw / TimeWarp.fixedDeltaTime + power_draws[pm];
                power_draws[pm] = power_draw;
            }else {
                power_draws.Add(pm, power_draw / TimeWarp.fixedDeltaTime);
            }
        }

        public float powerSupply(float power) {
            return (float) powerSupply ((double)power);
        }

        public double powerSupply(double power) {
            powersupply += (power / TimeWarp.fixedDeltaTime);
			stable_supply += (power / TimeWarp.fixedDeltaTime);
            return power;
        }

		public float powerSupplyFixedMax(float power, float maxpower) {
			return (float) powerSupplyFixedMax ((double)power,(double)maxpower);
		}

		public double powerSupplyFixedMax(double power, double maxpower) {
			powersupply += (power / TimeWarp.fixedDeltaTime);
			stable_supply += (maxpower / TimeWarp.fixedDeltaTime);
			return power;
		}

		public float managedPowerSupply(float power) {
			return managedPowerSupplyWithMinimum (power, 0);
		}

		public double managedPowerSupply(double power) {
			return managedPowerSupplyWithMinimum (power, 0);
		}

		public double getSpareResourceCapacity() {
			partresources = new List<PartResource>();
			my_part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resource_name).id, partresources);
			double spare_capacity = 0;
			foreach (PartResource partresource in partresources) {
				spare_capacity += partresource.maxAmount - partresource.amount;
			}
			return spare_capacity;
		}

		public float managedPowerSupplyWithMinimum(float power, float rat_min) {
            return (float) managedPowerSupplyWithMinimum((double)power, (double)rat_min);
		}

		public double managedPowerSupplyWithMinimum(double power, double rat_min) {
			double power_seconds_units = power / TimeWarp.fixedDeltaTime;
			double power_min_seconds_units = power_seconds_units * rat_min;
			double managed_supply_val_add = Math.Min (power_seconds_units, Math.Max(getCurrentUnfilledResourceDemand()+getSpareResourceCapacity()/TimeWarp.fixedDeltaTime,power_min_seconds_units));
			powersupply += managed_supply_val_add;
			stable_supply += power_seconds_units;
			return managed_supply_val_add*TimeWarp.fixedDeltaTime;
		}

        public float getStableResourceSupply() {
            return (float) stored_stable_supply;
        }

		public float getCurrentResourceDemand() {
			return (float) current_resource_demand;
		}

		public float getCurrentUnfilledResourceDemand() {
			return (float) (current_resource_demand-powersupply);
		}

		public double getResourceBarRatio() {
			return resource_bar_ratio;
		}

        public Vessel getVessel() {
            return my_vessel;
        }

		public void updatePartModule(PartModule pm) {
			my_vessel = pm.vessel;
			my_part = pm.part;
			my_partmodule = pm;
		}

		public PartModule getPartModule() {
			return my_partmodule;
		}

        public void update() {
			stored_stable_supply = stable_supply;
			double stored_current_demand = current_resource_demand;
			double stored_current_hp_demand = high_priority_resource_demand;
			double stored_current_charge_demand = charge_resource_demand;

			current_resource_demand = 0;
			high_priority_resource_demand = 0;
			charge_resource_demand = 0;

			//Debug.Log ("Early:" + powersupply);

            //stored power
            List<PartResource> partresources = new List<PartResource>();
            my_part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resource_name).id, partresources);
            double currentmegajoules = 0;
			double maxmegajoules = 0;
            foreach (PartResource partresource in partresources) {
                currentmegajoules += partresource.amount;
				maxmegajoules += partresource.maxAmount;
            }
			if (maxmegajoules > 0) {
				resource_bar_ratio = currentmegajoules / maxmegajoules;
			} else {
				resource_bar_ratio = 0;
			}
			double missingmegajoules = maxmegajoules - currentmegajoules;
            powersupply += currentmegajoules;
			//Debug.Log ("Current:" + currentmegajoules);

			double demand_supply_ratio = 0;
			double high_priority_demand_supply_ratio = 0;

			if (high_priority_resource_demand > 0) {
				high_priority_demand_supply_ratio = Math.Min ((powersupply-stored_current_charge_demand) / stored_current_hp_demand, 1.0);
			} else {
				high_priority_demand_supply_ratio = 1.0;
			}

			if (stored_current_demand > 0) {
				demand_supply_ratio = Math.Min ((powersupply-stored_current_charge_demand-stored_current_hp_demand) / stored_current_demand, 1.0);
			} else {
				demand_supply_ratio = 1.0;
			}

			//Debug.Log ("Late:" + powersupply);

			//Prioritise supplying stock ElectricCharge resource
			if (String.Equals(this.resource_name,FNResourceManager.FNRESOURCE_MEGAJOULES) && stored_stable_supply > 0) {
				List<PartResource> partresources2 = new List<PartResource> ();
				my_part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition ("ElectricCharge").id, partresources2); 
				double stock_electric_charge_needed = 0;
				foreach (PartResource partresource in partresources2) {
					stock_electric_charge_needed += partresource.maxAmount - partresource.amount;
				}
				double power_supplied = Math.Min(powersupply*1000*TimeWarp.fixedDeltaTime, stock_electric_charge_needed);
                if (stock_electric_charge_needed > 0) {
                    current_resource_demand += stock_electric_charge_needed / 1000.0;
                    charge_resource_demand += stock_electric_charge_needed / 1000.0;
                }
				if (power_supplied > 0) {
					powersupply += my_part.RequestResource ("ElectricCharge", -power_supplied)/1000/TimeWarp.fixedDeltaTime;
				}
			}

			//sort by power draw
			//var power_draw_items = from pair in power_draws orderby pair.Value ascending select pair;
			List<KeyValuePair<FNResourceSuppliable, double>> power_draw_items = power_draws.ToList();

            power_draw_items.Sort(delegate(KeyValuePair<FNResourceSuppliable, double> firstPair, KeyValuePair<FNResourceSuppliable, double> nextPair) { return firstPair.Value.CompareTo(nextPair.Value); });
            
            // check engines
            foreach (KeyValuePair<FNResourceSuppliable, double> power_kvp in power_draw_items) {
                FNResourceSuppliable ms = power_kvp.Key;

                if (ms is ElectricEngineController || ms is FNNozzleController || ms is AntimatterStorageTank) {
                    double power = power_kvp.Value;
					current_resource_demand += power;
					high_priority_resource_demand += power;
					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) {
						power = power * high_priority_demand_supply_ratio;
					}
                    double power_supplied = Math.Max(Math.Min(powersupply, power),0.0);
					//Debug.Log (power + ", " + powersupply + "::: " + power_supplied);
                    powersupply -= power_supplied;
					//notify of supply
                    ms.receiveFNResource(power_supplied, this.resource_name);
                }

            }
            // check others
            foreach (KeyValuePair<FNResourceSuppliable, double> power_kvp in power_draw_items) {
                FNResourceSuppliable ms = power_kvp.Key;
                
                if (!(ms is ElectricEngineController) && !(ms is FNNozzleController) && !(ms is FNRadiator) && !(ms is AntimatterStorageTank)) {
                    double power = power_kvp.Value;
					current_resource_demand += power;
					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) {
						power = power * demand_supply_ratio;
					}
					double power_supplied = Math.Max(Math.Min(powersupply, power),0.0);
                    powersupply -= power_supplied;

					//notify of supply
					ms.receiveFNResource(power_supplied, this.resource_name);
                }

            }
			// check radiators
            foreach (KeyValuePair<FNResourceSuppliable, double> power_kvp in power_draw_items) {
				FNResourceSuppliable ms = power_kvp.Key;
				if (ms is FNRadiator) {
					double power = power_kvp.Value;
					current_resource_demand += power;
					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) {
						power = power * demand_supply_ratio;
					}
					double power_supplied = Math.Max(Math.Min(powersupply, power),0.0);
					powersupply -= power_supplied;

					//notify of supply
                    ms.receiveFNResource(power_supplied, this.resource_name);
				}
			}


            powersupply -= Math.Max(currentmegajoules,0.0);

			double power_extract = -powersupply * TimeWarp.fixedDeltaTime;

			if (String.Equals (this.resource_name, FNResourceManager.FNRESOURCE_WASTEHEAT)) { // passive dissip of waste heat
				double vessel_mass = my_vessel.GetTotalMass ();
                double passive_dissip = passive_temp_p4 * GameConstants.stefan_const * vessel_mass * 2.0;
				power_extract += passive_dissip*TimeWarp.fixedDeltaTime;
				if (power_extract < 0 && PluginHelper.isThermalDissipationDisabled ()) {
					power_extract = 0;
				}
			}

			if (power_extract > 0) {
				power_extract = Math.Min (power_extract, currentmegajoules);
			} else if (power_extract < 0) {
				power_extract = Math.Max (power_extract, -missingmegajoules);
			}

			my_part.RequestResource(this.resource_name, power_extract);
            powersupply = 0;
			stable_supply = 0;
            power_draws.Clear();
        }
    }
}
