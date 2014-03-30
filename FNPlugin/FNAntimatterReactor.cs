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
                return String.Format("[Base Part Information]\nPart Name: {0}\nCore Temperature: {1:n0}K\nTotal Power Output: {2:n0}MW\nAntimatter Consumption Rate (Max):\n{3}mg/sec\n\n[Upgrade Information]\nScience Tech Required:\n- {4}\nPart Name: {5}\nCore Temperature: {6:n0}K\nTotal Power Output: {7:n0}MW\nAntimatter Consumption (Max):\n{8}mg/sec", originalName, ReactorTemp, ThermalPower, resourceRate, upgradeTechReq, upgradedName, upgradedReactorTemp, upgradedThermalPower, upgradedResourceRate);
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
