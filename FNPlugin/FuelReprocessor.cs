extern alias ORSv1_4_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_1::OpenResourceSystem;

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
            List<INuclearFuelReprocessable> nuclear_reactors = vessel.FindPartModulesImplementing<INuclearFuelReprocessable>();
            double remaining_capacity_to_reprocess = GameConstants.baseReprocessingRate * TimeWarp.fixedDeltaTime / 86400.0 * rate_multiplier;
            double enum_actinides_change = 0;
            foreach (INuclearFuelReprocessable nuclear_reactor in nuclear_reactors)
            {
                double actinides_change = nuclear_reactor.ReprocessFuel(remaining_capacity_to_reprocess);
                enum_actinides_change += actinides_change;
                remaining_capacity_to_reprocess = Math.Max(0, remaining_capacity_to_reprocess-actinides_change);
            }
            remaining_to_reprocess = nuclear_reactors.Sum(nfr => nfr.WasteToReprocess);
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
