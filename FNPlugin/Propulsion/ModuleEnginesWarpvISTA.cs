using System;
using System.Linq;
using UnityEngine;
using FNPlugin.Extensions;

namespace FNPlugin
{
    public class ModuleEnginesWarpVista : ModuleEngines
    {
        // GUI display values
        [KSPField(isPersistant = true)]
        bool IsForceActivated;

        [KSPField(guiActive = true, guiName = "Warp Thrust")]
        protected string Thrust = "";
        [KSPField(guiActive = true, guiName = "Warp Isp")]
        protected string Isp = "";
        [KSPField(guiActive = true, guiName = "Warp Throttle")]
        protected string Throttle = "";

        [KSPField(guiActive = true, guiName = "Mass Flow", guiFormat = "F8", guiUnits=" U")]
        public float totalMassFlow;
        [KSPField(guiActive = true, guiName = "Hydrogen Used", guiFormat = "F6")]
        public float propellantUsed;
        [KSPField(guiActive = true, guiName = "Deuterium Used", guiFormat = "F6")]
        public float deuteriumUsed;
        [KSPField(guiActive = true, guiName = "Tritium Used", guiFormat = "F6")]
        public float tritiumUsed;

        // Numeric display values
        protected double thrust_d = 0;
        protected double isp_d = 0;
        protected double throttle_d = 0;

        // Persistent values to use during timewarp
        float IspPersistent = 0;
        float ThrustPersistent = 0;
        float ThrottlePersistent = 0;
        float previousThrottle = 0;

        // Are we transitioning from timewarp to reatime?
        [KSPField]
        bool warpToReal = false;

        // Density of resource
        float densityHydrogen;
        float densityDeuterium;
        float densityTritium;

        Propellant hydrogenPropellant;
        Propellant deuteriumPropellant;
        Propellant tritiumPropellant;

        // Update
        public override void OnUpdate()
        {
            // When transitioning from timewarp to real update throttle
            if (warpToReal)
            {
                vessel.ctrlState.mainThrottle = ThrottlePersistent;
                warpToReal = false;
            }

            // Persistent thrust GUI
            Fields["Thrust"].guiActive = isEnabled;
            Fields["Isp"].guiActive = isEnabled;
            Fields["Throttle"].guiActive = isEnabled;

            // Update display values
            Thrust = FormatThrust(thrust_d);
            Isp = Math.Round(isp_d, 2).ToString() + " s";
            Throttle = Math.Round(throttle_d * 100).ToString() + "%";

            if (!IsForceActivated && isEnabled && isOperational)
            {
                IsForceActivated = true;
                part.force_activate();
            }
        }

        // Initialization
        public override void OnLoad(ConfigNode node)
        {
            // Run base OnLoad method
            base.OnLoad(node);

            // Initialize density of propellant used in deltaV and mass calculations
            densityHydrogen = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
            densityDeuterium = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Deuterium).density;
            densityTritium = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Tritium).density;

            hydrogenPropellant = this.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Hydrogen);
            deuteriumPropellant = this.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium);
            tritiumPropellant = this.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium);
        }

        // Physics update
        public override void OnFixedUpdate()
        {
            if (FlightGlobals.fetch == null || !isEnabled) return;

            totalMassFlow = this.requestedMassFlow;

            // retreive ratios
            var propellant_ratio = hydrogenPropellant.ratio;
            var deuterium_ratio = deuteriumPropellant.ratio;
            var tritium_ratio = tritiumPropellant.ratio;
            var sumOfRatios = propellant_ratio + deuterium_ratio + tritium_ratio;

            // Resource demand
            float demandReqPropellant = ((propellant_ratio / sumOfRatios) * this.requestedMassFlow) / densityHydrogen;
            float demandReqDeuterium = ((deuterium_ratio / sumOfRatios) * this.requestedMassFlow) / densityDeuterium;
            float demandReqTritium = ((tritium_ratio / sumOfRatios) * this.requestedMassFlow) / densityTritium;

            // Realtime mode
            if (!this.vessel.packed)
            {
                // Resource demand
                propellantUsed = demandReqPropellant;
                deuteriumUsed = demandReqDeuterium;
                tritiumUsed = demandReqTritium;

                // if not transitioning from warp to real
                // Update values to use during timewarp
                if (!warpToReal) //&& vessel.ctrlState.mainThrottle == previousThrottle)
                {
                    IspPersistent = realIsp;
                    ThrottlePersistent = vessel.ctrlState.mainThrottle;

                    this.CalculateThrust();
                    // verify we have thrust
                    if ((vessel.ctrlState.mainThrottle > 0 && finalThrust > 0) || (vessel.ctrlState.mainThrottle == 0 && finalThrust == 0))
                        ThrustPersistent = finalThrust;
                }
            }
            else if (part.vessel.situation != Vessel.Situations.SUB_ORBITAL)
            { 
                // Timewarp mode: perturb orbit using thrust
                warpToReal = true; // Set to true for transition to realtime
                double UT = Planetarium.GetUniversalTime(); // Universal time

                propellantUsed = (float)part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, demandReqPropellant * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                deuteriumUsed = (float)part.RequestResource(InterstellarResourcesConfiguration.Instance.Deuterium, demandReqDeuterium * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
                tritiumUsed = (float)part.RequestResource(InterstellarResourcesConfiguration.Instance.Tritium, demandReqTritium * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;

                // Calculate thrust and deltaV if demand output > 0
                if (propellantUsed > 0 && deuteriumUsed > 0 && tritiumUsed > 0)
                {
                    double vesselMass = this.vessel.GetTotalMass(); // Current mass
                    double m1 = vesselMass - (this.requestedMassFlow * TimeWarp.fixedDeltaTime); // Mass at end of burn

                    if (m1 <= 0 || vesselMass <= 0)
                        return;

                    double deltaV = IspPersistent * PluginHelper.GravityConstant * Math.Log(vesselMass / m1); // Delta V from burn

                    Vector3d thrustV = this.part.transform.up; // Thrust direction
                    Vector3d deltaVV = deltaV * thrustV; // DeltaV vector
                    vessel.orbit.Perturb(deltaVV, UT, TimeWarp.fixedDeltaTime); // Update vessel orbit
                }
                // Otherwise, if throttle is turned on, and demand out is 0, show warning
                else if (ThrottlePersistent > 0)
                {
                    ScreenMessages.PostScreenMessage("Out of resource", 5.0f);
                }
            }
            else //if (vessel.ctrlState.mainThrottle > 0)
            {
                ScreenMessages.PostScreenMessage("Cannot accelerate and timewarp durring sub orbital spaceflight!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            

            // Update display numbers
            thrust_d = ThrustPersistent;
            isp_d = IspPersistent;
            throttle_d = ThrottlePersistent;
            previousThrottle = vessel.ctrlState.mainThrottle;
        }

        // Format thrust into mN, N, kN
        public static string FormatThrust(double thrust)
        {
            if (thrust < 0.001)
                return Math.Round(thrust * 1000000.0, 3).ToString() + " mN";
            else if (thrust < 1.0)
                return Math.Round(thrust * 1000.0, 3).ToString() + " N";
            else
                return Math.Round(thrust, 3).ToString() + " kN";
        }
    }

}