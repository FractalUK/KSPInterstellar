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
        public static void Perturb(this Orbit orbit, Vector3d deltaVV, double universalTime, double deltaTime)
        {
            // If there is a deltaV, perturb orbit
            if (deltaVV.magnitude <= 0) return;

            // Transpose deltaVV Y and Z to match orbit frame
            Vector3d deltaVV_orbit = deltaVV.xzy;
            Vector3d position = orbit.getRelativePositionAtUT(universalTime);
            Orbit orbit2 = orbit.Clone();
            orbit2.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(universalTime) + deltaVV_orbit, orbit.referenceBody, universalTime);
            if (!double.IsNaN(orbit2.inclination) && !double.IsNaN(orbit2.eccentricity) && !double.IsNaN(orbit2.semiMajorAxis) && orbit2.timeToAp > deltaTime)
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
                orbit.UpdateFromUT(universalTime);
            }
            else
            {
                orbit.UpdateFromStateVectors(position, orbit.getOrbitalVelocityAtUT(universalTime) + deltaVV_orbit, orbit.referenceBody, universalTime);
                orbit.Init();
                orbit.UpdateFromUT(universalTime);
            }
        }
    }

}

