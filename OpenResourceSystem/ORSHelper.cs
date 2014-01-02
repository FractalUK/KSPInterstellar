using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSHelper {
        public static float getMaxAtmosphericAltitude(CelestialBody body) {
            if (!body.atmosphere) {
                return 0;
            }
            return (float)-body.atmosphereScaleHeight * 1000.0f * Mathf.Log(1e-6f);
        }
    }
}
