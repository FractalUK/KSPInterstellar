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
        protected String resource_name;
        //protected Dictionary<MegajouleSuppliable, float> power_returned;
        protected float powersupply = 0;
        protected float stable_supply = 0;

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

        public float getStableResourceSupply() {
            return stable_supply;
        }

        public Vessel getVessel() {
            return my_vessel;
        }

        public void update() {
            stable_supply = powersupply;
            //stored power
            List<PartResource> partresources = new List<PartResource>();
            my_part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resource_name).id, partresources);
            float currentmegajoules = 0;
            foreach (PartResource partresource in partresources) {
                currentmegajoules += (float)partresource.amount;
            }
            powersupply += currentmegajoules;
            
            // check engines
            foreach (KeyValuePair<FNResourceSuppliable, float> power_kvp in power_draws) {
                FNResourceSuppliable ms = power_kvp.Key;
                if (ms is ElectricEngineController || ms is FNNozzleController) {
                    float power = power_kvp.Value;
                    float power_supplied = Math.Min(powersupply, power);
                    powersupply -= power_supplied;
                    //notify of supply
                    ms.supplyPower(power_supplied * TimeWarp.fixedDeltaTime,this.resource_name);
                }

            }
            // check others
            foreach (KeyValuePair<FNResourceSuppliable, float> power_kvp in power_draws) {
                FNResourceSuppliable ms = power_kvp.Key;
                if (!(ms is ElectricEngineController) && !(ms is FNNozzleController)) {
                    float power = power_kvp.Value;
                    float power_supplied = Math.Min(powersupply, power);
                    powersupply -= power_supplied;
                    //notify of supply
                    ms.supplyPower(power_supplied * TimeWarp.fixedDeltaTime, this.resource_name);
                }

            }
            powersupply -= currentmegajoules;
            my_part.RequestResource(this.resource_name, -powersupply * TimeWarp.fixedDeltaTime);
            powersupply = 0;
            power_draws.Clear();
        }
    }
}
