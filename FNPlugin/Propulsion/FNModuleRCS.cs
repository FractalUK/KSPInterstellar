using System;
using System.Collections.Generic;
using UnityEngine;
using KSP;

namespace FNPlugin
{
    public class FNModuleRCSFX : ModuleRCSFX
    {
        // FNModuleRCSFX is a clone of ModuleRCSFX with a small fix in Update where y is switch by x

        float inputAngularX;
        float inputAngularY;
        float inputAngularZ;
        float inputLinearX;
        float inputLinearY;
        float inputLinearZ;

        private Vector3 inputLinear;
        private Vector3 inputAngular;
        private bool precision;
        private double exhaustVel = 0d;
        private double invG = 1d / 9.80665d;

        /// <summary>
        /// If control actuation < this, ignore.
        /// </summary>
        [KSPField]
        float EPSILON = 0.05f; // 5% control actuation

        new public void Update()
        {
            if (this.part.vessel == null)
                return;

            inputLinear = vessel.ReferenceTransform.rotation * new Vector3(enableX ? vessel.ctrlState.X : 0f, enableZ ? vessel.ctrlState.Z : 0f, enableY ? vessel.ctrlState.Y : 0f);
            inputAngular = vessel.ReferenceTransform.rotation * new Vector3(enablePitch ? vessel.ctrlState.pitch : 0f, enableRoll ? vessel.ctrlState.roll : 0f, enableYaw ? vessel.ctrlState.yaw : 0);
            if (useThrottle)
            {
                //inputLinear.y -= vessel.ctrlState.mainThrottle;
                inputLinear.x -= vessel.ctrlState.mainThrottle;
                //inputLinear.y = Mathf.Clamp(inputLinear.y, -1f, 1f);
                inputLinear.x = Mathf.Clamp(inputLinear.x, -1f, 1f);
            }

            // Epsilon checks (min values)
            float EPSILON2 = EPSILON * EPSILON;
            inputAngularX = inputAngular.x;
            inputAngularY = inputAngular.y;
            inputAngularZ = inputAngular.z;
            inputLinearX = inputLinear.x;
            inputLinearY = inputLinear.y;
            inputLinearZ = inputLinear.z;
            if (inputAngularX * inputAngularX < EPSILON2)
                inputAngularX = 0f;
            if (inputAngularY * inputAngularY < EPSILON2)
                inputAngularY = 0f;
            if (inputAngularZ * inputAngularZ < EPSILON2)
                inputAngularZ = 0f;
            if (inputLinearX * inputLinearX < EPSILON2)
                inputLinearX = 0f;
            if (inputLinearY * inputLinearY < EPSILON2)
                inputLinearY = 0f;
            if (inputLinearZ * inputLinearZ < EPSILON2)
                inputLinearZ = 0f;
            inputLinear.x = inputLinearX;
            inputLinear.y = inputLinearY;
            inputLinear.z = inputLinearZ;
            inputAngular.x = inputAngularX;
            inputAngular.y = inputAngularY;
            inputAngular.z = inputAngularZ;

            precision = FlightInputHandler.fetch.precisionMode;
        }

        new public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;
            int fxC = thrusterFX.Count;
            if (TimeWarp.CurrentRate > 1.0f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
            {

                for (int i = 0; i < fxC; ++i)
                {
                    FXGroup fx = thrusterFX[i];
                    fx.setActive(false);
                    fx.Power = 0f;
                }
                return;
            }

            // set starting params for loop
            bool success = false;
            curThrust = 0f;

            // set Isp/EV
            realISP = atmosphereCurve.Evaluate((float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres));
            exhaustVel = (double)realISP * (double)G * ispMult;

            thrustForces.Clear();

            if (rcsEnabled && !part.ShieldedFromAirstream)
            {
                if (vessel.ActionGroups[KSPActionGroup.RCS] != rcs_active)
                {
                    rcs_active = vessel.ActionGroups[KSPActionGroup.RCS];
                }
                if (vessel.ActionGroups[KSPActionGroup.RCS] && (inputAngular != Vector3.zero || inputLinear != Vector3.zero))
                {

                    // rb_velocity should include timewarp, right?
                    Vector3 CoM = vessel.CoM + vessel.rb_velocity * Time.fixedDeltaTime;

                    float effectPower = 0f;
                    int xformCount = thrusterTransforms.Count;
                    for (int i = 0; i < xformCount; ++i)
                    {
                        Transform xform = thrusterTransforms[i];
                        if (xform.position != Vector3.zero)
                        {
                            Vector3 position = xform.position;
                            Vector3 torque = Vector3.Cross(inputAngular.normalized, (position - CoM).normalized);

                            Vector3 thruster;
                            if (useZaxis)
                                thruster = xform.forward;
                            else
                                thruster = xform.up;
                            float thrust = Mathf.Max(Vector3.Dot(thruster, torque), 0f);
                            thrust += Mathf.Max(Vector3.Dot(thruster, inputLinear.normalized), 0f);

                            // thrust should now be normalized 0-1.

                            if (thrust > 0f)
                            {
                                if (fullThrust && thrust >= fullThrustMin)
                                    thrust = 1f;

                                if (precision)
                                {
                                    if (useLever)
                                    {
                                        //leverDistance = GetLeverDistanceOriginal(predictedCOM);
                                        float leverDistance = GetLeverDistance(-thruster, CoM);

                                        if (leverDistance > 1)
                                        {
                                            thrust /= leverDistance;
                                        }
                                    }
                                    else
                                    {
                                        thrust *= precisionFactor;
                                    }
                                }

                                UpdatePropellantStatus();
                                float thrustForce = CalculateThrust(thrust, out success);

                                if (success)
                                {
                                    curThrust += thrustForce;
                                    thrustForces.Add(thrustForce);
                                    if (!isJustForShow)
                                    {
                                        Vector3 force = -thrustForce * thruster;

                                        part.Rigidbody.AddForceAtPosition(force, position, ForceMode.Force);
                                        //Debug.Log("Part " + part.name + " adding force " + force.x + "," + force.y + "," + force.z + " at " + position);
                                    }

                                    thrusterFX[i].Power = Mathf.Clamp(thrust, 0.1f, 1f);
                                    if (effectPower < thrusterFX[i].Power)
                                        effectPower = thrusterFX[i].Power;
                                    thrusterFX[i].setActive(thrustForce > 0f);
                                }
                                else
                                {
                                    thrusterFX[i].Power = 0f;

                                    /*if (!(flameoutEffectName.Equals("")))
                                        part.Effect(flameoutEffectName, 1.0f);*/
                                }
                            }
                            else
                            {
                                thrusterFX[i].Power = 0f;
                            }
                        }
                    }
                    /*if(!(runningEffectName.Equals("")))
                        part.Effect(runningEffectName, effectPower);*/
                }
            }
            if (!success)
            {
                for (int i = 0; i < fxC; ++i)
                {
                    FXGroup fx = thrusterFX[i];
                    fx.setActive(false);
                    fx.Power = 0f;
                }
            }

        }

        private void UpdatePropellantStatus()
        {
            if ((object)propellants != null)
            {
                int pCount = propellants.Count;
                for (int i = 0; i < pCount; ++i)
                    propellants[i].UpdateConnectedResources(part);
            }
        }
        new public float CalculateThrust(float totalForce, out bool success)
        {
            double massFlow = flowMult * fuelFlow * (double)totalForce;

            double propAvailable = 1.0d;

            if (!CheatOptions.InfiniteRCS)
                propAvailable = RequestPropellant(massFlow * TimeWarp.fixedDeltaTime);

            totalForce = (float)(massFlow * exhaustVel * propAvailable);

            success = (propAvailable > 0f); // had some fuel
            return totalForce;
        }

    }
}