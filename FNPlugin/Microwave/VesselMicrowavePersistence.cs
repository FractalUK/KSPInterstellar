using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    class VesselMicrowavePersistence
    {
        Vessel vessel;
        double nuclear_power;
        double solar_power;

        public VesselMicrowavePersistence(Vessel vessel)
        {
            this.vessel = vessel;
        }

        public double getAvailablePower()
        {
            double power = 0;
            if (PluginHelper.lineOfSightToSun(vessel) && solar_power > 0)
            {
                var distanceBetweenVesselAndSun = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                var distanceBetweenSunAndKerbin = Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
                double inv_square_mult = Math.Pow(distanceBetweenVesselAndSun, 2) / Math.Pow(distanceBetweenSunAndKerbin, 2);
                power = nuclear_power + solar_power / inv_square_mult;
            }
            else
                power = nuclear_power;

            return power;
        }

        public double getNuclearPower()
        {
            return nuclear_power;
        }

        public double getSolarPower()
        {
            return solar_power;
        }

        public Vessel getVessel()
        {
            return vessel;
        }

        public void setNuclearPower(double nuclear_power)
        {
            this.nuclear_power = nuclear_power;
        }

        public void setSolarPower(double solar_power)
        {
            this.solar_power = solar_power;
        }
    }
}
