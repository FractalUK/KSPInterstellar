using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNSeismicProbe : PartModule {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool probeIsEnabled;

        protected long active_count = 0;

        [KSPEvent(guiActive = true, guiName = "Start Recording Seismic Data", active = true)]
        public void ActivateProbe() {
            if (vessel.Landed) {
                probeIsEnabled = true;
            } else {
                ScreenMessages.PostScreenMessage("Must be landed to activate seismic probe.", 5f, ScreenMessageStyle.UPPER_CENTER);
            }
            saveState();
        }

        [KSPEvent(guiActive = true, guiName = "Stop Recording", active = false)]
        public void DeactivateProbe() {
            probeIsEnabled = false;
            saveState();
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
        }

        public override void OnUpdate() {
            Events["ActivateProbe"].active = !probeIsEnabled;
            Events["DeactivateProbe"].active = probeIsEnabled;
        }

        protected void saveState() {
            if (HighLogic.LoadedSceneIsFlight) {
                ConfigNode config = PluginHelper.getPluginSaveFile();
                string vesselID = vessel.id.ToString();
                if (config.HasNode("VESSEL_SEISMIC_PROBE_" + vesselID)) {
                    ConfigNode probe_node = config.GetNode("VESSEL_SEISMIC_PROBE_" + vesselID);
                    if (probe_node.HasValue("is_active")) {
                        probe_node.SetValue("is_active", probeIsEnabled.ToString());
                    } else {
                        probe_node.AddValue("is_active", probeIsEnabled.ToString());
                    }
                    if (probe_node.HasValue("celestial_body")) {
                        probe_node.SetValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                    } else {
                        probe_node.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                    }
                } else {
                    ConfigNode probe_node = config.AddNode("VESSEL_SEISMIC_PROBE_" + vesselID);
                    probe_node.AddValue("is_active", probeIsEnabled.ToString());
                    probe_node.AddValue("celestial_body", vessel.mainBody.flightGlobalsIndex.ToString());
                }
                config.Save(PluginHelper.getPluginSaveFilePath());
            }
            
        }


    }
}
