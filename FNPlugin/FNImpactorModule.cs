using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class FNImpactorModule : PartModule {
        protected bool impactor_running = false;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            part.OnJustAboutToBeDestroyed += onVesselAboutToBeDestroyed;
            impactor_running = true;
        }

        public override void OnUpdate() {
            if (!impactor_running) {
                impactor_running = true;
                part.OnJustAboutToBeDestroyed += onVesselAboutToBeDestroyed;
            }
        }

        public void onVesselAboutToBeDestroyed() {
            print("[KSP Interstellar] Handling Impactor");
            int body = vessel.mainBody.flightGlobalsIndex;
            //print(vessel.srf_velocity.magnitude);
            ConfigNode config = PluginHelper.getPluginSaveFile();
            Vector3d net_vector = Vector3d.zero;
            bool first = true;
            double net_science = 0;
            foreach (Vessel conf_vess in FlightGlobals.Vessels) {
                String conf_vess_ID = conf_vess.id.ToString();
                if (config.HasNode("VESSEL_SEISMIC_PROBE_" + conf_vess_ID)) {
                    ConfigNode probe_node = config.GetNode("VESSEL_SEISMIC_PROBE_" + conf_vess_ID);
                    bool is_active = false;
                    int planet = 0;
                    if (probe_node.HasValue("is_active")) {
                        is_active = bool.Parse(probe_node.GetValue("is_active"));
                    }
                    if (probe_node.HasValue("celestial_body")) {
                        planet = int.Parse(probe_node.GetValue("celestial_body"));
                    }

                    // record science if we have crashed into the surface at velocity > 100m/s
                    if (is_active && planet == body && vessel.heightFromSurface <= 0 && vessel.srf_velocity.magnitude > 40) {
                        // do sciency stuff
                        Vector3d surface_vector = (vessel.transform.position - FlightGlobals.Bodies[body].transform.position);
                        surface_vector = surface_vector.normalized;
                        if (first) {
                            first = false;
                            net_vector = surface_vector;
                            net_science = 50 * PluginHelper.getImpactorScienceMultiplier(body);
                        } else {
                            net_science += (1.0 - Vector3d.Dot(surface_vector, net_vector)) * 50 * PluginHelper.getImpactorScienceMultiplier(body);
                            net_vector = net_vector + surface_vector;
                        }
                    }
                }
            }
            if (net_science > 0 && !double.IsInfinity(net_science)) {
                
                ConfigNode science_node;
                int science_experiment_number = 0;
                if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper())) {
                    science_node = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    science_experiment_number = science_node.values.Count - 1;
                } else {
                    science_node = config.AddNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    science_node.AddValue("name", "interstellarseismicarchive");
                }

                if (!science_node.HasValue(vessel.id.ToString())) {
                    double science_coeff = -science_experiment_number / 2.0;
                    net_science = net_science * Math.Exp(science_coeff);
                    ScreenMessages.PostScreenMessage("Impact Recorded, " + net_science.ToString("0.0") + " science has been added to the R&D centre.", 5f, ScreenMessageStyle.UPPER_CENTER);
                    science_node.AddValue(vessel.id.ToString(), net_science);
                    if (ResearchAndDevelopment.Instance != null) {
                        ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)net_science;
                    }
                }
                config.Save(PluginHelper.getPluginSaveFilePath());
            }
        }
    }
}
