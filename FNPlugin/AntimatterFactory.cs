using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenResourceSystem;

namespace FNPlugin {
    class AntimatterFactory {
        protected Part part;
        protected Vessel vessel;
        protected double current_rate = 0;
        protected double efficiency = 0.01149;

        public AntimatterFactory(Part part) {
            this.part = part;
            vessel = part.vessel;
        }

        public void produceAntimatterFrame(double rate_multiplier) {
            double energy_provided = rate_multiplier * GameConstants.baseAMFPowerConsumption * 1E6f;
            double antimatter_density = PartResourceLibrary.Instance.GetDefinition("Antimatter").density;
            double antimatter_mass = energy_provided / GameConstants.warpspeed / GameConstants.warpspeed / 200000.0f / antimatter_density*efficiency;
            current_rate = -ORSHelper.fixedRequestResource(part, "Antimatter", -antimatter_mass * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
        }

        public double getAntimatterProductionRate() {
            return current_rate;
        }

        public double getAntimatterProductionEfficiency() {
            return efficiency;
        }
    }
}
