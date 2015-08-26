using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public static class OrbitExtensions
    {

        // Dublicate an orbit
        public static Orbit Clone(this Orbit orbit0)
        {
            return new Orbit(orbit0.inclination, orbit0.eccentricity, orbit0.semiMajorAxis, orbit0.LAN, orbit0.argumentOfPeriapsis, orbit0.meanAnomalyAtEpoch, orbit0.epoch, orbit0.referenceBody);
        }

        // Perturb an orbit by a deltaV vector
        public static void Perturb(this Orbit orbit, Vector3d deltaVV, double UT, double dT)
        {

            // If there is a deltaV, perturb orbit
            if (deltaVV.magnitude > 0)
            {
                // Transpose deltaVV Y and Z to match orbit frame
                Vector3d deltaVV_orbit = deltaVV.xzy;
                Vector3d position = orbit.getRelativePositionAtUT(UT);
                Orbit orbit2 = orbit.Clone();
                orbit2.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(UT) + deltaVV_orbit, orbit.referenceBody, UT);
                if (!double.IsNaN(orbit2.inclination) && !double.IsNaN(orbit2.eccentricity) && !double.IsNaN(orbit2.semiMajorAxis) && orbit2.timeToAp > dT)
                {
                    orbit.inclination = orbit2.inclination;
                    orbit.eccentricity = orbit2.eccentricity;
                    orbit.semiMajorAxis = orbit2.semiMajorAxis;
                    orbit.LAN = orbit2.LAN;
                    orbit.argumentOfPeriapsis = orbit2.argumentOfPeriapsis;
                    orbit.meanAnomalyAtEpoch = orbit2.meanAnomalyAtEpoch;
                    orbit.epoch = orbit2.epoch;
                    orbit.referenceBody = orbit2.referenceBody;
                    orbit.Init();
                    orbit.UpdateFromUT(UT);
                }
                else
                {
                    orbit.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(UT) + deltaVV_orbit, orbit.referenceBody, UT);
                    orbit.Init();
                    orbit.UpdateFromUT(UT);
                }
            }
        }
    }

}

