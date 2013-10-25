using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNAntimatterReactor : FNReactor {
        public override bool getIsNuclear() {
            return false;
        }

        public override string GetInfo() {
            return String.Format("Core Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Max Consumption Rate: {2}mg/sec\n -Upgrade Information-\n Upgraded Core Temperature: {3}K\n Upgraded Power: {4}MW\n Upgraded Antimatter Consumption: {5}mg/sec", ReactorTemp, ThermalPower, resourceRate, upgradedReactorTemp, upgradedThermalPower, upgradedResourceRate);
        }

        protected override double consumeReactorResource(double resource) {
            List<PartResource> antimatter_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, antimatter_resources);
            double antimatter_current_amount = 0;
            foreach (PartResource antimatter_resource in antimatter_resources) {
                antimatter_current_amount += antimatter_resource.amount;
            }
            resource = Math.Min(antimatter_current_amount, resource);
            double antimatter_provided = part.RequestResource("Antimatter", resource);
            return antimatter_provided;
        }

        protected override double returnReactorResource(double resource) {
            resource = part.RequestResource("Antimatter", -resource);
            return resource;
        }
        
        protected override string getResourceDeprivedMessage() {
            return "Antimatter Deprived";
        }

    }
}
