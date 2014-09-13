extern alias ORSv1_2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_2::OpenResourceSystem;

namespace FNPlugin {
    class ReactorFuel {
        protected double fuel_usege_per_mw;
        protected string fuel_name;
        protected double density;
        protected string unit;

        public ReactorFuel(ConfigNode node) {
            fuel_name = node.GetValue("FuelName");
            fuel_usege_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            unit = node.GetValue("Unit");
            density = PartResourceLibrary.Instance.GetDefinition(fuel_name).density;
        }

        public double FuelUsePerMJ { get { return fuel_usege_per_mw/density; } }

        public string FuelName { get { return fuel_name; } }

        public string Unit { get { return unit; } }

        public double GetFuelUseForPower(InterstellarReactor reactor, double megajoules) {
            return fuel_usege_per_mw * megajoules/density/reactor.FuelEfficiency;
        }

        public double GetAvailabilityToReactor(InterstellarReactor reactor, bool global_draw) {
            if (!global_draw) {
                if(reactor.part.Resources.Contains(fuel_name)) return reactor.part.Resources[fuel_name].amount*reactor.FuelEfficiency;
                else return 0;
            }
            else {
                List<PartResource> resources = reactor.part.GetConnectedResources(fuel_name).ToList();
                return resources.Sum(rs => rs.amount)*reactor.FuelEfficiency;
            }
        }

        public double ConsumeReactorResource(InterstellarReactor reactor, bool global_draw, double amount) {
            if (!global_draw) {
                if (reactor.part.Resources.Contains(fuel_name)) {
                    amount = Math.Min(amount, reactor.part.Resources[fuel_name].amount/reactor.FuelEfficiency);
                    reactor.part.Resources[fuel_name].amount -= amount;
                    return amount;
                } else return 0;
            } else {
                return ORSHelper.fixedRequestResource(reactor.part, fuel_name, amount/reactor.FuelEfficiency);
            }
        }
    }
}
