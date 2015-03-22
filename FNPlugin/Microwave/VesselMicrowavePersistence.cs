using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class VesselMicrowavePersistence {
        Vessel vessel;
        double nuclear_power;
        double solar_power;

        public VesselMicrowavePersistence(Vessel vessel) {
            this.vessel = vessel;
        }

        public double getAvailablePower() 
        {
            Vector3d vessel_pos = PluginHelper.getVesselPos(vessel);
            double power = 0;
            if (PluginHelper.lineOfSightToSun(vessel) && solar_power > 0) {
                double inv_square_mult = Math.Pow(Vector3d.Distance(vessel_pos, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
                power = nuclear_power + solar_power/inv_square_mult;
            } else {
                power = nuclear_power;
            }
            return power;
        }

        public double getNuclearPower() {
            return nuclear_power;
        }

        public double getSolarPower() {
            return solar_power;
        }

        public Vessel getVessel() {
            return vessel;
        }

        public void setNuclearPower(double nuclear_power) {
            this.nuclear_power = nuclear_power;
        }

        public void setSolarPower(double solar_power) {
            this.solar_power = solar_power;
        }
    }
}
