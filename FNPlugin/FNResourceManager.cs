using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    class FNResourceManager {
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
               
        protected Vessel my_vessel;
        protected Part my_part;
        protected PartModule my_partmodule;
        protected Dictionary<FNResourceSuppliable, float> power_draws;
		List<PartResource> partresources;
        protected String resource_name;
        //protected Dictionary<MegajouleSuppliable, float> power_returned;
        protected float powersupply = 0;
        protected float stable_supply = 0;
		protected float current_resource_demand = 0;

        protected float power_draw;

        public FNResourceManager(PartModule pm,String resource_name) {
            my_vessel = pm.vessel;
            my_part = pm.part;
            my_partmodule = pm;
            power_draws = new Dictionary<FNResourceSuppliable,float>();
            this.resource_name = resource_name;
        }

        public void powerDraw(FNResourceSuppliable pm, float power_draw) {
            if (power_draws.ContainsKey(pm)) {
                power_draw = power_draw / TimeWarp.fixedDeltaTime + power_draws[pm];
                power_draws[pm] = power_draw;
            }
            else {
                power_draws.Add(pm, power_draw / TimeWarp.fixedDeltaTime);
            }
        }

        public float powerSupply(float power) {
            powersupply += power / TimeWarp.fixedDeltaTime;
            return power;
        }

        public double powerSupply(double power) {
            powersupply += (float) (power / TimeWarp.fixedDeltaTime);
            return power;
        }

		public float managedPowerSupply(float power) {
			return managedPowerSupplyWithMinimum (power, 0);
		}

		public double managedPowerSupply(double power) {
			return managedPowerSupplyWithMinimum (power, 0);
		}

		public float getSpareResourceCapacity() {
			partresources = new List<PartResource>();
			my_part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resource_name).id, partresources);
			float spare_capacity = 0;
			foreach (PartResource partresource in partresources) {
				spare_capacity += (float)(partresource.maxAmount - partresource.amount);
			}
			return spare_capacity;
		}

		public float managedPowerSupplyWithMinimum(float power, float rat_min) {
			float power_seconds_units = power / TimeWarp.fixedDeltaTime;
			float power_min_seconds_units = power_seconds_units * rat_min;
			float managed_supply_val_add = Math.Min (power_seconds_units, Math.Max(current_resource_demand+getSpareResourceCapacity(),power_min_seconds_units));
			powersupply += managed_supply_val_add;
			return managed_supply_val_add*TimeWarp.fixedDeltaTime;
		}

		public double managedPowerSupplyWithMinimum(double power, double rat_min) {
			double power_seconds_units = power / TimeWarp.fixedDeltaTime;
			double power_min_seconds_units = power_seconds_units * rat_min;
			double managed_supply_val_add = Math.Min (power_seconds_units, Math.Max(current_resource_demand+getSpareResourceCapacity(),power_min_seconds_units));
			powersupply += (float) managed_supply_val_add;
			return managed_supply_val_add*TimeWarp.fixedDeltaTime;
		}

        public float getStableResourceSupply() {
            return stable_supply;
        }

		public float getCurrentResourceDemand() {
			return current_resource_demand;
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
            stable_supply = powersupply;
			current_resource_demand = 0;
			if (String.Equals(this.resource_name,FNResourceManager.FNRESOURCE_MEGAJOULES)) {
				current_resource_demand = 1;
			}
            //stored power
            List<PartResource> partresources = new List<PartResource>();
            my_part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resource_name).id, partresources);
            float currentmegajoules = 0;
            foreach (PartResource partresource in partresources) {
                currentmegajoules += (float)partresource.amount;
            }
            powersupply += currentmegajoules;

			//sort by power draw
			//var power_draw_items = from pair in power_draws orderby pair.Value ascending select pair;
			List<KeyValuePair<FNResourceSuppliable, float>> power_draw_items = power_draws.ToList();

			power_draw_items.Sort(delegate(KeyValuePair<FNResourceSuppliable, float> firstPair,KeyValuePair<FNResourceSuppliable, float> nextPair) {return firstPair.Value.CompareTo(nextPair.Value);});
            
            // check engines
			foreach (KeyValuePair<FNResourceSuppliable, float> power_kvp in power_draw_items) {
                FNResourceSuppliable ms = power_kvp.Key;
				current_resource_demand += power_kvp.Value;
                if (ms is ElectricEngineController || ms is FNNozzleController) {
                    float power = power_kvp.Value;
                    float power_supplied = Math.Min(powersupply, power);
                    powersupply -= power_supplied;
                    //notify of supply
                    ms.receiveFNResource(power_supplied * TimeWarp.fixedDeltaTime,this.resource_name);
                }

            }
            // check others
			foreach (KeyValuePair<FNResourceSuppliable, float> power_kvp in power_draw_items) {
                FNResourceSuppliable ms = power_kvp.Key;
                if (!(ms is ElectricEngineController) && !(ms is FNNozzleController)) {
                    float power = power_kvp.Value;
                    float power_supplied = Math.Min(powersupply, power);
                    powersupply -= power_supplied;
                    //notify of supply
                    ms.receiveFNResource(power_supplied * TimeWarp.fixedDeltaTime, this.resource_name);
                }

            }
            powersupply -= currentmegajoules;

            my_part.RequestResource(this.resource_name, -powersupply * TimeWarp.fixedDeltaTime);
            powersupply = 0;
            power_draws.Clear();
        }
    }
}
