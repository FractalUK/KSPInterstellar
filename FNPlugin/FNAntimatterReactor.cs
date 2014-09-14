using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor {

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public override bool IsNuclear { get { return false; } }

        /*public override string GetInfo() {
            if (!hasTechsRequiredToUpgrade()) {
                return String.Format(originalName + "\nCore Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Max Consumption Rate: {2}mg/sec\n -Upgrade Information-\n Upgraded Core Temperature: {3}K\n Upgraded Power: {4}MW\n Upgraded Antimatter Consumption: {5}mg/sec", ReactorTemp, PowerOutput, resourceRate, upgradedReactorTemp, upgradedPowerOutput, upgradedResourceRate);
            } else {
                return String.Format(upgradedName + "\nThis part is available automatically upgraded\nCore Temperature: {0}K\n Thermal Power: {1}MW\n Antimatter Max Consumption Rate: {2}mg/sec\n", upgradedReactorTemp, upgradedPowerOutput, upgradedResourceRate);
            }
        }*/

        public override string getResourceManagerDisplayName() {
            return TypeName;
        }

    }
}
