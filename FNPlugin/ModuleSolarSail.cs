using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ModuleSolarSail : PartModule {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;

        // Persistent False
        [KSPField]
        public float reflectedPhotonRatio = 1f;
        [KSPField]
        public float surfaceArea; // Surface area of the panel.
        [KSPField]
        public string animName;

        // GUI
        [KSPField(guiActive = true, guiName = "Force")]
        protected string forceAcquired = "";
        [KSPField(guiActive = true, guiName = "Acceleration")]
        protected string solarAcc = "";

        protected Transform surfaceTransform = null;
        protected Animation solarSailAnim = null;

        const double kerbin_distance = 13599840256;
        const double thrust_coeff = 9.08e-6;

        protected double solar_force_d = 0;
        protected double solar_acc_d = 0;
        protected long count = 0;

        [KSPEvent(guiActive = true, guiName = "Deploy Sail", active = true)]
        public void DeploySail() {
            if (animName != null && solarSailAnim != null) {
                solarSailAnim[animName].speed = 1f;
                solarSailAnim[animName].normalizedTime = 0f;
                solarSailAnim.Blend(animName, 2f);
            }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Retract Sail", active = false)]
        public void RetractSail() {
            if (animName != null && solarSailAnim != null) {
                solarSailAnim[animName].speed = -1f;
                solarSailAnim[animName].normalizedTime = 1f;
                solarSailAnim.Blend(animName, 2f);
            }
            IsEnabled = false;
        }

        public override void OnStart(StartState state) {
            if (state != StartState.None && state != StartState.Editor) {
                //surfaceTransform = part.FindModelTransform(surfaceTransformName);
                //solarSailAnim = (ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"];
                if (animName != null) {
                    solarSailAnim = part.FindModelAnimators(animName).FirstOrDefault();
                }
                if (IsEnabled) {
                    solarSailAnim[animName].speed = 1f;
                    solarSailAnim[animName].normalizedTime = 0f;
                    solarSailAnim.Blend(animName, 0.1f);
                }

                this.part.force_activate();
            }
        }

        public override void OnUpdate() {
            Events["DeploySail"].active = !IsEnabled;
            Events["RetractSail"].active = IsEnabled;
            Fields["solarAcc"].guiActive = IsEnabled;
            Fields["forceAcquired"].guiActive = IsEnabled;
            forceAcquired = solar_force_d.ToString("E") + " N";
            solarAcc = solar_acc_d.ToString("E") + " m/s";
        }

        public override void OnFixedUpdate() {
            if (FlightGlobals.fetch != null) {
                solar_force_d = 0;
                if (!IsEnabled) { return; }
                double sunlightFactor = 1.0;
                Vector3 sunVector = FlightGlobals.fetch.bodies[0].position - part.orgPos;

                if (!PluginHelper.lineOfSightToSun(vessel)) {
                    sunlightFactor = 0.0f;
                }

                //Debug.Log("Detecting sunlight: " + sunlightFactor.ToString());
                Vector3d solarForce = CalculateSolarForce() * sunlightFactor;
                //print(surfaceArea);

                Vector3d solar_accel = solarForce / vessel.GetTotalMass() / 1000.0 * TimeWarp.fixedDeltaTime;
                if (!this.vessel.packed) {
                    vessel.ChangeWorldVelocity(solar_accel);
                } else {
                    if (sunlightFactor > 0) {
                        double temp1 = solar_accel.y;
                        solar_accel.y = solar_accel.z;
                        solar_accel.z = temp1;
                        Vector3d position = vessel.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime());
                        Orbit orbit2 = new Orbit(vessel.orbit.inclination, vessel.orbit.eccentricity, vessel.orbit.semiMajorAxis, vessel.orbit.LAN, vessel.orbit.argumentOfPeriapsis, vessel.orbit.meanAnomalyAtEpoch, vessel.orbit.epoch, vessel.orbit.referenceBody);
                        orbit2.UpdateFromStateVectors(position, vessel.orbit.vel + solar_accel, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
                        //print(orbit2.timeToAp);
                        if (!double.IsNaN(orbit2.inclination) && !double.IsNaN(orbit2.eccentricity) && !double.IsNaN(orbit2.semiMajorAxis) && orbit2.timeToAp > TimeWarp.fixedDeltaTime) {
                            vessel.orbit.inclination = orbit2.inclination;
                            vessel.orbit.eccentricity = orbit2.eccentricity;
                            vessel.orbit.semiMajorAxis = orbit2.semiMajorAxis;
                            vessel.orbit.LAN = orbit2.LAN;
                            vessel.orbit.argumentOfPeriapsis = orbit2.argumentOfPeriapsis;
                            vessel.orbit.meanAnomalyAtEpoch = orbit2.meanAnomalyAtEpoch;
                            vessel.orbit.epoch = orbit2.epoch;
                            vessel.orbit.referenceBody = orbit2.referenceBody;
                            vessel.orbit.Init();

                            //vessel.orbit.UpdateFromOrbitAtUT(orbit2, Planetarium.GetUniversalTime(), orbit2.referenceBody);
                            vessel.orbit.UpdateFromUT(Planetarium.GetUniversalTime());
                        }

                    }
                }
                solar_force_d = solarForce.magnitude;
                solar_acc_d = solar_accel.magnitude / TimeWarp.fixedDeltaTime;
                //print(solarForce.x.ToString() + ", " + solarForce.y.ToString() + ", " + solarForce.z.ToString());
            }
            count++;
        }

        private Vector3d CalculateSolarForce() {
            if (this.part != null) {
                Vector3d sunPosition = FlightGlobals.fetch.bodies[0].position;
                Vector3d ownPosition = this.part.transform.position;
				Vector3d ownsunPosition = ownPosition - sunPosition;
                Vector3d normal = this.part.transform.up;
                if (surfaceTransform != null) {
                    normal = surfaceTransform.forward;
                }
				// If normal points away from sun, negate so our force is always away from the sun
				// so that turning the backside towards the sun thrusts correctly
				if (Vector3d.Dot (normal, ownsunPosition) < 0) {
					normal = -normal;
				}
				// Magnitude of force proportional to cosine-squared of angle between sun-line and normal
				double cosConeAngle = Vector3.Dot (ownsunPosition.normalized, normal);
                Vector3d force = normal * cosConeAngle * cosConeAngle * surfaceArea * reflectedPhotonRatio * solarForceAtDistance();
				return force;
            } else {
                return Vector3d.zero;
            }
        }

        private double solarForceAtDistance() {
            double distance_from_sun = Vector3.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position, vessel.transform.position);
            double force_to_return = thrust_coeff * kerbin_distance * kerbin_distance / distance_from_sun / distance_from_sun;
            return force_to_return;
        }

    }
}

