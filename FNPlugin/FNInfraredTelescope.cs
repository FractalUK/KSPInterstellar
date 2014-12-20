extern alias ORSv1_4_1;
using ORSv1_4_1::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    [KSPModule("Infrared Telescope")]
    class FNInfraredTelescope : PartModule{
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
        public double helium_depleted_time;

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
        protected double science_awaiting_addition = 0;
        protected double helium_time_scale = 0;


        [KSPEvent(guiActive = true, guiName = "Deep Field Survey", active = false)]
        public void beginOberservations() {
            telescopeIsEnabled = true;
            dpo = false;
        }

        [KSPEvent(guiActive = true, guiName = "Direct Planetary Observation", active = false)]
        public void beginOberservations2() {
            telescopeIsEnabled = true;
            dpo = true;
        }

        [KSPEvent(guiActive = true, guiName = "Stop Survey", active = false)]
        public void stopOberservations() {
            telescopeIsEnabled = false;
        }

        [KSPEvent(guiName = "Perform Maintenance", externalToEVAOnly = true, guiActiveUnfocused = true, unfocusedRange = 2.5f)]
        public void maintainTelescope() {
            lastMaintained = (float) Planetarium.GetUniversalTime();
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

            if (telescopeInit == false || lastMaintained == 0) {
                telescopeInit = true;
                lastMaintained = (float) Planetarium.GetUniversalTime();
            }

            if (telescopeIsEnabled && lastActiveTime > 0) {
                double t0 = lastActiveTime - lastMaintained;
                double t1 = Math.Min(Planetarium.GetUniversalTime(),helium_depleted_time) - lastMaintained;
                if (t1 > t0) {
                    double a = -GameConstants.telescopePerformanceTimescale;
                    double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                    double avg_science_rate = base_science / a / a * (Math.Exp(a * t1) * (a * t1 - 1) - Math.Exp(a * t0) * (a * t0 - 1));
                    double time_diff = Planetarium.GetUniversalTime() - lastActiveTime;
                    double science_to_add = avg_science_rate / 86400 * time_diff;
                    lastActiveTime = (float) Planetarium.GetUniversalTime();
                    science_awaiting_addition = science_to_add;
                }
            }

            this.part.force_activate();
        }

        public override void OnUpdate() {
            bool inAtmos = false;
            if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                telescopeIsEnabled = false;
                inAtmos = true;
            }
            Events["beginOberservations"].active = !inAtmos && !telescopeIsEnabled;
            Events["stopOberservations"].active = telescopeIsEnabled;
            Fields["sciencePerDay"].guiActive = telescopeIsEnabled;
            performPcnt = (perform_factor_d * 100).ToString("0.0") + "%";
            sciencePerDay = (science_rate * 86400).ToString("0.00") + " Science/Day";
            double current_au = Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position) / Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position);
            if (vessel.FindPartModulesImplementing<ScienceModule>().Count > 0 || vessel.FindPartModulesImplementing<ComputerCore>().Count > 0) {
                List<ComputerCore> cores = vessel.FindPartModulesImplementing<ComputerCore>();
                List<ScienceModule> science_labs = vessel.FindPartModulesImplementing<ScienceModule>();
                bool upgraded_core = false;
                bool crewed_lab = false;
                foreach (ComputerCore core in cores) {
                    upgraded_core = upgraded_core ? upgraded_core : core.isupgraded;
                }
                foreach (ScienceModule science_lab in science_labs) {
                    crewed_lab = crewed_lab ? crewed_lab : (science_lab.part.protoModuleCrew.Count > 0);
                }

                if (current_au >= 548 && !inAtmos && (crewed_lab || upgraded_core)) {
                    if (vessel.orbit.eccentricity < 0.8) {
                        Events["beginOberservations2"].active = true;
                        if (telescopeIsEnabled && dpo) {
                            gLensStr = "Ongoing.";
                        } else {
                            gLensStr = "Available.";
                        }
                    } else {
                        Events["beginOberservations2"].active = false;
                        gLensStr = "Eccentricity: " + vessel.orbit.eccentricity.ToString("0.0") +"; < 0.8 Required";
                    }
                } else {
                    Events["beginOberservations2"].active = false;
                    gLensStr = current_au.ToString("0.0") + " AU; Required 548 AU";
                }
            } else {
                Events["beginOberservations2"].active = false;
                gLensStr = "Science Lab/Computer Core required";
            }

            if (helium_time_scale <= 0) {
                performPcnt = "Helium Coolant Deprived.";
            }
        }

        public override void OnFixedUpdate() {
            if (ResearchAndDevelopment.Instance != null) {
                if (!double.IsNaN(science_awaiting_addition) && !double.IsInfinity(science_awaiting_addition) && science_awaiting_addition > 0) {
                    ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)science_awaiting_addition;
                    ScreenMessages.PostScreenMessage(science_awaiting_addition.ToString("0") + " science has been added to the R&D centre.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                    science_awaiting_addition = 0;
                }
            }

            List<PartResource> prl = part.GetConnectedResources("LqdHelium").ToList();
            double max_helium = 0;
            double cur_helium = 0;
            double helium_fraction = 0;
            foreach (PartResource partresource in prl) {
                max_helium += partresource.maxAmount;
                cur_helium += partresource.amount;
            }

            helium_fraction = (max_helium > 0) ? cur_helium / max_helium : cur_helium;
            helium_time_scale = 1.0 / GameConstants.helium_boiloff_fraction * helium_fraction;
            helium_depleted_time = helium_time_scale + Planetarium.GetUniversalTime();

            if (helium_time_scale <= 0) {
                telescopeIsEnabled = false;
            }

            perform_exponent = -(Planetarium.GetUniversalTime() - lastMaintained) * GameConstants.telescopePerformanceTimescale;
            perform_factor_d = Math.Exp(perform_exponent);
            if (telescopeIsEnabled) {
                double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                science_rate = base_science * perform_factor_d/86400;
                if (ResearchAndDevelopment.Instance != null) {
                    if (!double.IsNaN(science_rate) && !double.IsInfinity(science_rate)) {
                        ResearchAndDevelopment.Instance.Science = (float) (ResearchAndDevelopment.Instance.Science + science_rate * TimeWarp.fixedDeltaTime);
                    }
                }
                lastActiveTime = (float) Planetarium.GetUniversalTime();
            }
        }

        public override string GetInfo() {
            string desc = "Requires Helium coolant.\n";
            desc = desc + "Science Rate: " + GameConstants.telescopeBaseScience.ToString("0.0") + " /day\n";
            desc = desc + "Gravitional Lens\n Required Altitude: 548AU\n";
            desc = desc + "G-Lens Science Rate:" + GameConstants.telescopeGLensScience.ToString("0.0") + " /day\n";
            return desc;
        }
    }
}
