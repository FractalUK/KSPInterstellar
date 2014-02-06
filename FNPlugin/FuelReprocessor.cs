extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_1::OpenResourceSystem;

namespace FNPlugin {
    class FuelReprocessor {
        protected Part part;
        protected Vessel vessel;
        protected double current_rate = 0;
        protected double remaining_to_reprocess = 0;

        public FuelReprocessor(Part part) {
            this.part = part;
            vessel = part.vessel;
        }

        public void performReprocessingFrame(double rate_multiplier) {
            List<FNNuclearReactor> nuclear_reactors = vessel.FindPartModulesImplementing<FNNuclearReactor>();
            double remaining_capacity_to_reprocess = GameConstants.baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400.0 * rate_multiplier;
            double enum_actinides_change = 0;
            double amount_to_reprocess = 0;
            foreach (FNNuclearReactor nuclear_reactor in nuclear_reactors) {
                // reprocess each one
                PartResource actinides = nuclear_reactor.part.Resources["Actinides"];
                if (remaining_capacity_to_reprocess > 0) {
                    double new_actinides_amount = Math.Max(actinides.amount - remaining_capacity_to_reprocess, 0);
                    double actinides_change = actinides.amount - new_actinides_amount;
                    actinides.amount = new_actinides_amount;
                    if (nuclear_reactor.uranium_fuel) {
                        PartResource uf4 = nuclear_reactor.part.Resources["UF4"];
                        double depleted_fuels_change = actinides_change * 0.2;
                        depleted_fuels_change = -ORSHelper.fixedRequestResource(part, "DepletedFuel", -depleted_fuels_change);
                        double new_uf4_amount = Math.Min(uf4.amount + depleted_fuels_change*4, uf4.maxAmount);
                        double uf4_change = new_uf4_amount - uf4.amount;
                        uf4.amount = new_uf4_amount;
                        enum_actinides_change += depleted_fuels_change * 5;
                    } else {
                        PartResource thf4 = nuclear_reactor.part.Resources["ThF4"];
                        double depleted_fuels_change = actinides_change * 0.2;
                        depleted_fuels_change = -ORSHelper.fixedRequestResource(part, "DepletedFuel", -depleted_fuels_change);
                        double new_thf4_amount = Math.Min(thf4.amount + depleted_fuels_change * 4, thf4.maxAmount);
                        double thf4_change = new_thf4_amount - thf4.amount;
                        thf4.amount = new_thf4_amount;
                        enum_actinides_change += depleted_fuels_change * 5;
                    }
                    remaining_capacity_to_reprocess = Math.Max(0, actinides_change);
                    //enum_actinides_change += actinides_change;
                }
                amount_to_reprocess += actinides.amount;
            }
            remaining_to_reprocess = amount_to_reprocess;
            current_rate = enum_actinides_change;
        }

        public double getActinidesRemovedPerHour() {
            return current_rate / TimeWarp.fixedDeltaTime * 3600.0;
        }

        public double getRemainingAmountToReprocess() {
            return remaining_to_reprocess;
        }
    }
}
