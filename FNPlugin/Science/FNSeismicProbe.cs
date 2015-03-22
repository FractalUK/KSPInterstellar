using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNSeismicProbe : ModuleModableScienceGenerator {
        // Persistent True
        [KSPField(isPersistant = true)]
        public bool probeIsEnabled;

        protected long active_count = 0;
        protected string science_vess_ref;

        [KSPEvent(guiActive = true, guiName = "Record Seismic Data", active = true)]
        public void ActivateProbe() {
            if (vessel.Landed) {
                PopupDialog.SpawnPopupDialog("Seismic Probe", "Surface will be monitored for impact events.", "OK", false, HighLogic.Skin);
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

        public override void OnUpdate() {
            base.OnUpdate();
            Events["ActivateProbe"].active = !probeIsEnabled;
            Events["DeactivateProbe"].active = probeIsEnabled;
            
        }

        protected override bool generateScienceData() {
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("FNSeismicProbeExperiment");
            if (experiment == null) {
                return false;
            }
            //ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, vessel.mainBody, "surface");
            //if (subject == null) {
            //    return false;
            //}
            //subject.scientificValue = 1;
            //subject.scienceCap = float.MaxValue;
            //subject.science = 1;
            //subject.subjectValue = 1;
            result_title = "Impactor Experiment";
            result_string = "No useful seismic data has been recorded.";
            transmit_value = 0;
            recovery_value = 0;
            data_size = 0;
            xmit_scalar = 1;
            ref_value = 1;
            
           // science_data = new ScienceData(0, 1, 0, subject.id, "data");

            ConfigNode config = PluginHelper.getPluginSaveFile();
            if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper())) {
                ConfigNode planet_data = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                foreach (ConfigNode probe_data in planet_data.nodes) {
                    if (probe_data.name.Contains("IMPACT_")) {
                        science_vess_ref = probe_data.name;
                        bool transmitted = false;
                        string vessel_name = "";
                        float science_amount = 0;
                        int exp_number = 1;
                        if (probe_data.HasValue("transmitted")) {
                            transmitted = bool.Parse(probe_data.GetValue("transmitted"));
                        }
                        if (probe_data.HasValue("vesselname")) {
                            vessel_name = probe_data.GetValue("vesselname");
                        }
                        if (probe_data.HasValue("science")) {
                            science_amount = float.Parse(probe_data.GetValue("science"));
                        }
                        if (probe_data.HasValue("number")) {
                            exp_number = int.Parse(probe_data.GetValue("number"));
                        }
                        if (!transmitted) {
                            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ExperimentSituations.SrfLanded, vessel.mainBody, vessel.mainBody.name + "'s surface.");
                            if (subject == null) {
                                return false;
                            }
                            result_string =  vessel_name + " impacted into " + vessel.mainBody.name + " producing seismic activity.  From this data, information on the structure of " + vessel.mainBody.name + "'s crust can be determined.";
                            transmit_value = science_amount;
                            recovery_value = science_amount;
                            subject.subjectValue = 1;
                            subject.scientificValue = 1;
                            subject.scienceCap = 50 * PluginHelper.getImpactorScienceMultiplier(vessel.mainBody.flightGlobalsIndex)*10;
                            //subject.science = 0;
                            data_size = science_amount * 2.5f;
                            science_data = new ScienceData(science_amount, 1, 0, subject.id, "Impactor Data");
                            ref_value = 50*PluginHelper.getImpactorScienceMultiplier(vessel.mainBody.flightGlobalsIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected override void cleanUpScienceData() {
            if (science_vess_ref != null) {
                ConfigNode config = PluginHelper.getPluginSaveFile();
                if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper())) {
                    ConfigNode planet_data = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    if (planet_data.HasNode(science_vess_ref)) {
                        ConfigNode impact_node = planet_data.GetNode(science_vess_ref);
                        if (impact_node.HasValue("transmitted")) {
                            impact_node.SetValue("transmitted", "True");
                        }
                        config.Save(PluginHelper.PluginSaveFilePath);
                    }
                }
            }
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
                config.Save(PluginHelper.PluginSaveFilePath);
            }

        }




    }
}
