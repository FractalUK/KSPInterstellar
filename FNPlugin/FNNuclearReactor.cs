using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNNuclearReactor : FNReactor {
        public override bool getIsNuclear() {
            return true;
        }

        public override string GetInfo() {
            float uf6_rate_per_day = resourceRate * 86400;
            float up_uf6_rate_per_day = upgradedResourceRate * 86400;
            return String.Format("Core Temperature: {0}K\n Thermal Power: {1}MW\n UF6 Max Consumption Rate: {2}L/day\n -Upgrade Information-\n Upgraded Core Temperate: {3}K\n Upgraded Power: {4}MW\n Upgraded UF6 Consumption: {5}L/day", ReactorTemp, ThermalPower, uf6_rate_per_day, upgradedReactorTemp, upgradedThermalPower, up_uf6_rate_per_day);
        }

        protected override double consumeReactorResource(double resource) {
            if(part.Resources["UF6"].flowState == false) {
                part.Resources["UF6"].flowState = true;
            }
            double uf6_current_amount = part.Resources["UF6"].amount;
            resource = Math.Min(uf6_current_amount, resource);
            double uf6_provided = part.RequestResource("UF6", resource);
            part.RequestResource("DUF6", -uf6_provided);
            return uf6_provided;
        }

        protected override double returnReactorResource(double resource) {
            double uf6_returned = part.RequestResource("UF6", -resource);
            part.RequestResource("DUF6", -uf6_returned);
            return uf6_returned;
        }
        
        protected override string getResourceDeprivedMessage() {
            return "UF6 Deprived";
        }

        
    }
}
