using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class VesselRelayPersistence {
        Vessel vessel;
        bool relay;

        public VesselRelayPersistence(Vessel vessel) {
            this.vessel = vessel;
        }

        public bool isActive() {
            return relay;
        }

        public Vessel getVessel() {
            return vessel;
        }

        public void setActive(bool active) {
            relay = active;
        }

        public bool lineOfSightTo(Vessel vess) {
            Vector3d a = vessel.transform.position;
            Vector3d b = vess.transform.position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
                Vector3d refminusa = referenceBody.position - a;
                Vector3d bminusa = b - a;
                if (Vector3d.Dot(refminusa, bminusa) > 0) {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude) {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
