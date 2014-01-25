using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNInfraredTelescope : PartModule{
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool telescopeIsEnabled;
        [KSPField(isPersistant = true)]
        public double lastActiveTime;
        [KSPField(isPersistant = true)]
        public double lastMaintained;
        [KSPField(isPersistant = true)]
        public bool telescopeInit;
        [KSPField(isPersistant = true)]
        public bool dpo;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Performance")]
        public string performPcnt = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Science")]
        public string sciencePerDay = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "G-Lens")]
        public string gLensStr = "";

        //Internal
        protected double perform_factor_d = 0;
        protected double perform_exponent = 0;
        protected double science_rate = 0;
        protected double science_awaiting_addition = 0;


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
            lastMaintained = Planetarium.GetUniversalTime();
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

            if (telescopeInit == false) {
                telescopeInit = true;
                lastMaintained = Planetarium.GetUniversalTime();
            }

            if (telescopeIsEnabled && lastActiveTime > 0) {
                double t0 = lastActiveTime - lastMaintained;
                double t1 = Planetarium.GetUniversalTime() - lastMaintained;
                double a = -GameConstants.telescopePerformanceTimescale;
                double base_science = dpo ? GameConstants.telescopeGLensScience : GameConstants.telescopeBaseScience;
                double avg_science_rate = base_science / a / a * (Math.Exp(a * t1) * (a * t1 - 1) - Math.Exp(a * t0) * (a * t0 - 1));
                double time_diff = Planetarium.GetUniversalTime() - lastActiveTime;
                double science_to_add = avg_science_rate / 86400 * time_diff;
                lastActiveTime = Planetarium.GetUniversalTime();
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
                if (current_au >= 548 && !inAtmos) {
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
        }

        public override void OnFixedUpdate() {
            if (ResearchAndDevelopment.Instance != null) {
                if (!double.IsNaN(science_awaiting_addition) && !double.IsInfinity(science_awaiting_addition) && science_awaiting_addition > 0) {
                    ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)science_awaiting_addition;
                    ScreenMessages.PostScreenMessage(science_awaiting_addition.ToString("0") + " science has been added to the R&D centre.", 2.5f, ScreenMessageStyle.UPPER_CENTER);
                    science_awaiting_addition = 0;
                }
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
                lastActiveTime = Planetarium.GetUniversalTime();
            }
        }
    }
}
