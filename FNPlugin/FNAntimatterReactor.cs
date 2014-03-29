using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : FNReactor {
        public override bool getIsNuclear() {
            return false;
        }

        public override string GetInfo() {
            if (!hasTechsRequiredToUpgrade()) {
                return String.Format(originalName + "\nCore Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Max Consumption Rate: {2}mg/sec\n -Upgrade Information-\n Upgraded Core Temperature: {3}K\n Upgraded Power: {4}MW\n Upgraded Antimatter Consumption: {5}mg/sec", ReactorTemp, ThermalPower, resourceRate, upgradedReactorTemp, upgradedThermalPower, upgradedResourceRate);
            } else {
                return String.Format(upgradedName + "\nThis part is available automatically upgraded\nCore Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Max Consumption Rate: {2}mg/sec\n", upgradedReactorTemp, upgradedThermalPower, upgradedResourceRate);
            }
        }

        protected override double consumeReactorResource(double resource) {
            List<PartResource> antimatter_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, antimatter_resources);
            double antimatter_provided = 0;
            foreach (PartResource antimatter_resource in antimatter_resources) {
                double antimatter_consumed_here = Math.Min(antimatter_resource.amount, resource);
                antimatter_provided += antimatter_consumed_here;
                antimatter_resource.amount -= antimatter_consumed_here;
                resource -= antimatter_consumed_here;
            }
            return antimatter_provided;
        }

        protected override double returnReactorResource(double resource) {
            List<PartResource> antimatter_resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Antimatter").id, antimatter_resources);
            double antimatter_returned = 0;
            foreach (PartResource antimatter_resource in antimatter_resources) {
                double antimatter_returned_here = Math.Min(antimatter_resource.maxAmount - antimatter_resource.amount, resource);
                antimatter_returned += antimatter_returned_here;
                antimatter_resource.amount += antimatter_returned_here;
                resource -= antimatter_returned_here;
            }
            return antimatter_returned;
        }
        
        protected override string getResourceDeprivedMessage() {
            return "Antimatter Deprived";
        }

    }
}
