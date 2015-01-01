using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    class InterstellarTelescope : ModuleModableScienceGenerator
    {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool telescopeIsEnabled;
        [KSPField(isPersistant = true)]
        public float lastActiveTime;
        [KSPField(isPersistant = true)]
        public float lastMaintained;
        [KSPField(isPersistant = true)]
        public bool telescopeInit;
        [KSPField(isPersistant = true)]
        public bool dpo;
        [KSPField(isPersistant = true)]
        public float helium_depleted_time;
        [KSPField(isPersistant = true)]
        public float science_awaiting_addition;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Performance")]
        public string performPcnt = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Science")]
        public string sciencePerDay = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "G-Lens")]
        public string gLensStr = "";

        //Internal
        protected double perform_factor_d = 0;
        protected double perform_exponent = 0;
        protected double science_rate = 0;
        protected double helium_time_scale = 0;

        [KSPEvent(guiActive = true, guiName = "Deep Field Survey", active = false)]
        public void beginOberservations()
        {
            telescopeIsEnabled = true;
            dpo = false;
        }

        [KSPEvent(guiActive = true, guiName = "Direct Planetary Observation", active = false)]
        public void beginOberservations2()
        {
            telescopeIsEnabled = true;
            dpo = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Survey", active = false)]
        public void stopOberservations()
        {
            telescopeIsEnabled = false;
        }

        [KSPEvent(guiName = "Perform Maintenance", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void maintainTelescope()
        {
            lastMaintained = (float)Planetarium.GetUniversalTime();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) { return; }

            if (telescopeInit == false || lastMaintained == 0)
            {
                telescopeInit = true;
                lastMaintained = (float)Planetarium.GetUniversalTime();
            }

            if (telescopeIsEnabled && lastActiveTime > 0)
            {
                calculateTimeToHeliumDepletion();

                double t0 = lastActiveTime - lastMaintained;
                double t1 = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastMaintained;
                if (t1 > t0)
                {
                    double a = -GameConstants.telescopePerformanceTimescale;
                    double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                    double time_diff = Math.Min(Planetarium.GetUniversalTime(), helium_depleted_time) - lastActiveTime;
                    double avg_science_rate = 0.5*base_science * ( Math.Exp(a * t1)  + Math.Exp(a * t0) );
                    double science_to_add = avg_science_rate / 28800 * time_diff;
                    lastActiveTime = (float)Planetarium.GetUniversalTime();
                    science_awaiting_addition += (float)science_to_add;
                }
            }
        }

        protected override bool generateScienceData()
        {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("ExpInterstellarTelescope");
            if (experiment == null) return false;

            if (science_awaiting_addition > 0)
            {
                result_title = "Infrared Telescope Experiment";
                result_string = "Infrared telescope observations were recovered from the vicinity of " + vessel.mainBody.name + ".";

                transmit_value = science_awaiting_addition;
                recovery_value = science_awaiting_addition;
                data_size = science_awaiting_addition * 1.25f;
                xmit_scalar = 1;

                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.InSpaceHigh, vessel.mainBody, "");
                subject.scienceCap = 167*PluginHelper.getScienceMultiplier(vessel.mainBody.flightGlobalsIndex,false);
                ref_value = subject.scienceCap;

                science_data = new ScienceData(science_awaiting_addition, 1, 0, subject.id, "Infrared Telescope Data");

                return true;
            }
            return false;
        }

        protected override void cleanUpScienceData()
        {
            science_awaiting_addition = 0;
        }

        public override void OnUpdate()
        {
            if (vessel.IsInAtmosphere()) telescopeIsEnabled = false;

            Events["beginOberservations"].active = !vessel.IsInAtmosphere() && !telescopeIsEnabled;
            Events["stopOberservations"].active = telescopeIsEnabled;
            Fields["sciencePerDay"].guiActive = telescopeIsEnabled;
            performPcnt = (perform_factor_d * 100).ToString("0.0") + "%";
            sciencePerDay = (science_rate * 28800).ToString("0.00") + " Science/Day";
            double current_au = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position) / Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
            List<ITelescopeController> telescope_controllers = vessel.FindPartModulesImplementing<ITelescopeController>();

            if (telescope_controllers.Any(tscp => tscp.CanProvideTelescopeControl))
            {
                if (current_au >= 548 && !vessel.IsInAtmosphere())
                {
                    if (vessel.orbit.eccentricity < 0.8)
                    {
                        Events["beginOberservations2"].active = true;
                        gLensStr = (telescopeIsEnabled && dpo) ? "Ongoing." : "Available";
                    } else
                    {
                        Events["beginOberservations2"].active = false;
                        gLensStr = "Eccentricity: " + vessel.orbit.eccentricity.ToString("0.0") + "; < 0.8 Required";
                    }
                } else
                {
                    Events["beginOberservations2"].active = false;
                    gLensStr = current_au.ToString("0.0") + " AU; Required 548 AU";
                }
            } else
            {
                Events["beginOberservations2"].active = false;
                gLensStr = "Science Lab/Computer Core required";
            }

            if (helium_time_scale <= 0) performPcnt = "Helium Coolant Deprived.";
            
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                calculateTimeToHeliumDepletion();

                if (ResearchAndDevelopment.Instance != null)
                {
                    if (helium_time_scale <= 0) telescopeIsEnabled = false;

                    perform_exponent = -(Planetarium.GetUniversalTime() - lastMaintained) * GameConstants.telescopePerformanceTimescale;
                    perform_factor_d = Math.Exp(perform_exponent);

                    if (telescopeIsEnabled)
                    {
                        double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                        science_rate = base_science * perform_factor_d / 28800;
                        if (!double.IsNaN(science_rate) && !double.IsInfinity(science_rate))
                        {
                            science_awaiting_addition += (float)(science_rate * TimeWarp.fixedDeltaTime);
                        }
                        lastActiveTime = (float)Planetarium.GetUniversalTime();
                    }
                }
            }
        }

        private void calculateTimeToHeliumDepletion()
        {
            List<PartResource> helium_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Helium).ToList();
            double max_helium = helium_resources.Sum(hr => hr.maxAmount);
            double cur_helium = helium_resources.Sum(hr => hr.amount);
            double helium_fraction = (max_helium > 0) ? cur_helium / max_helium : cur_helium;
            helium_time_scale = 1.0 / GameConstants.helium_boiloff_fraction * helium_fraction;
            helium_depleted_time = (float)(helium_time_scale + Planetarium.GetUniversalTime());
        }
    }
}
