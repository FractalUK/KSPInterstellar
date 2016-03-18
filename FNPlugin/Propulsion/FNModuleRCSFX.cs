using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace FNPlugin
{
    public class FNModuleRCSFX : ModuleRCS
    {
        // FNModuleRCSFX is a clone of ModuleRCSFX 0.4.2

        /// <summary>
        /// Always use the full thrust of the thruster, don't decrease it when off-alignment
        /// </summary>
        [KSPField]
        public bool fullThrust = false; // always use full thrust

        /// <summary>
        /// If fullThrust = true, if thrust ratio is < this, do not apply full thrust (leave thrust unchanged)
        /// </summary>
        [KSPField]
        public float fullThrustMin = 0.2f; // if thrust amount from dots < this, don't do full thrust

        [KSPField]
        public bool useEffects = false;

        [KSPField]
        string runningEffectName = "";
        [KSPField]
        string engageEffectName = "";
        [KSPField]
        string disengageEffectName = "";
        [KSPField]
        string flameoutEffectName = "";

        public bool rcs_active;

        [KSPField]
        public bool useZaxis = false;

        [KSPField(guiActiveEditor = true)]
        public string RCS = "Enable/Disable for:";

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Yaw"),  UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableYaw = true;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Pitch"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enablePitch = true;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Roll"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableRoll = true;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Port/Stbd"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableX = true;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Dorsal/Ventral"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableY = true;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Fore/Aft"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool enableZ = true;

        [KSPField]
        public bool useThrottle = false;

        /// <summary>
        /// Stock KSP lever compensation in precision mode (instead of just reduced thrsut
        /// Defaults to false (reduce thrust uniformly
        /// </summary>
        [KSPField]
        public bool useLever = false;

        /// <summary>
        /// The factor by which thrust is multiplied in precision mode (if lever compensation is off
        /// </summary>
        [KSPField]
        public float precisionFactor = 0.1f;

        //[KSPField(guiActive = true)]
        public float curThrust = 0f;

        /// <summary>
        /// Fuel flow in tonnes/sec
        /// </summary>
        public double fuelFlow = 0f;


        //[KSPField(guiActive = true)]
        float inputAngularX;
        //[KSPField(guiActive = true)]
        float inputAngularY;
        //[KSPField(guiActive = true)]
        float inputAngularZ;
        //[KSPField(guiActive = true)]
        float inputLinearX;
        //[KSPField(guiActive = true)]
        float inputLinearY;
        //[KSPField(guiActive = true)]
        float inputLinearZ;

        private Vector3 inputLinear;
        private Vector3 inputAngular;
        private bool precision;
        private double exhaustVel = 0d;

        public double flowMult = 1d;
        public double ispMult = 1d;

        private double invG = 1d / 9.80665d;

        /// <summary>
        /// If control actuation < this, ignore.
        /// </summary>
        [KSPField]
        float EPSILON = 0.05f; // 5% control actuation

        public float mixtureFactor;

        public override void OnLoad(ConfigNode node)
        {
            if (!node.HasNode("PROPELLANT") && node.HasValue("resourceName"))
            {
                ConfigNode c = new ConfigNode("PROPELLANT");
                c.AddValue("name", node.GetValue("resourceName"));
                c.AddValue("ratio", "1.0");
                if (node.HasValue("resourceFlowMode"))
                    c.AddValue("resourceFlowMode", node.GetValue("resourceFlowMode"));
                node.AddNode(c);
            }
            base.OnLoad(node);
            G = 9.80665f;
            fuelFlow = (double)thrusterPower / (double)atmosphereCurve.Evaluate(0f) * invG;
        }

        public override string GetInfo()
        {
            string text = base.GetInfo();
            return text;
        }

        public override void OnStart(StartState state)
        {
            if (useEffects) // use EFFECTS so don't do the base startup. That means we have to do this ourselves.
            {
                part.stackIcon.SetIcon(DefaultIcons.RCS_MODULE);
                part.stackIconGrouping = StackIconGrouping.SAME_TYPE;
                thrusterTransforms = new List<Transform>(part.FindModelTransforms(thrusterTransformName));
                if (thrusterTransforms == null || thrusterTransforms.Count == 0)
                {
                    Debug.Log("RCS module unable to find any transforms in part named " + thrusterTransformName);
                }

            }
            else
                base.OnStart(state);
        }

        new public void Update()
        {
            if (this.part.vessel == null)
                return;

            float ctrlZ = vessel.ctrlState.Z;
            if (useThrottle && ctrlZ < EPSILON && ctrlZ > -EPSILON) // only do this if not specifying axial thrust.
            {
                ctrlZ -= vessel.ctrlState.mainThrottle;
                ctrlZ = Mathf.Clamp(ctrlZ, -1f, 1f);
            }
            inputLinear = vessel.ReferenceTransform.rotation * new Vector3(enableX ? vessel.ctrlState.X : 0f, enableZ ? ctrlZ : 0f, enableY ? vessel.ctrlState.Y : 0f);
            inputAngular = vessel.ReferenceTransform.rotation * new Vector3(enablePitch ? vessel.ctrlState.pitch : 0f, enableRoll ? vessel.ctrlState.roll : 0f, enableYaw ? vessel.ctrlState.yaw : 0);

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

            //thrustForces.Clear();
			thrustForces = new List<float>().ToArray();

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
                            Vector3 torque = Vector3.Cross(inputAngular, (position - CoM).normalized);

                            Vector3 thruster;
                            if (useZaxis)
                                thruster = xform.forward;
                            else
                                thruster = xform.up;
                            float thrust = Mathf.Max(Vector3.Dot(thruster, torque), 0f);
                            thrust += Mathf.Max(Vector3.Dot(thruster, inputLinear), 0f);

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
                                    //thrustForces.Add(thrustForce);
	                                var newForces = thrustForces.ToList();
									newForces.Add(thrustForce);
									thrustForces = newForces.ToArray();
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