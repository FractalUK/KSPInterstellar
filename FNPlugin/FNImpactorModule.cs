using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
            double initial_science = 0;
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
                    double theta = vessel.longitude;
                    double phi = vessel.latitude;
                    Vector3d up = vessel.mainBody.GetSurfaceNVector(phi, theta).normalized;
                    double surface_height = vessel.mainBody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(theta, Vector3d.down) * QuaternionD.AngleAxis(phi, Vector3d.forward) * Vector3d.right)-vessel.mainBody.Radius;
                    double height_diff = vessel.pqsAltitude - surface_height;
                    // record science if we have crashed into the surface at velocity > 40m/s
                    if (is_active && planet == body && vessel.heightFromSurface <= 0.75 && vessel.srf_velocity.magnitude > 40 && height_diff <= 1) {
                        // do sciency stuff
                        Vector3d surface_vector = (conf_vess.transform.position - FlightGlobals.Bodies[body].transform.position);
                        surface_vector = surface_vector.normalized;
                        if (first) {
                            first = false;
                            net_vector = surface_vector;
                            net_science = 50 * PluginHelper.getImpactorScienceMultiplier(body);
                            initial_science = net_science;
                        } else {
                            net_science += (1.0 - Vector3d.Dot(surface_vector, net_vector.normalized)) * 50 * PluginHelper.getImpactorScienceMultiplier(body);
                            net_vector = net_vector + surface_vector;
                        }
                    } else {
                        if (vessel.heightFromSurface > 0.5) {
                            print("[KSP Interstellar] Impactor: Ignored due to vessel being destroyed at too high altitude.");
                        }
                        if (vessel.srf_velocity.magnitude <= 40) {
                            print("[KSP Interstellar] Impactor: Ignored due to vessel being at too low velocity.");
                        }
                    }
                }
            }
            net_science = Math.Min(net_science, initial_science * 3.5); // no more than 3.5x boost to science by using multiple detectors
            if (net_science > 0 && !double.IsInfinity(net_science) && !double.IsNaN(net_science)) {

                ConfigNode science_node;
                int science_experiment_number = 0;
                if (config.HasNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper())) {
                    science_node = config.GetNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    science_experiment_number = science_node.nodes.Count;
                } else {
                    science_node = config.AddNode("SEISMIC_SCIENCE_" + vessel.mainBody.name.ToUpper());
                    science_node.AddValue("name", "interstellarseismicarchive");
                }


                double science_coeff = -science_experiment_number / 2.0;
                net_science = net_science * Math.Exp(science_coeff);
                ScreenMessages.PostScreenMessage("Impact Recorded, science report can now be accessed from one of your accelerometers deployed on this body.", 5f, ScreenMessageStyle.UPPER_CENTER);
                //science_node.AddValue(vessel.id.ToString(), net_science);
                if (!science_node.HasNode("IMPACT_" + vessel.id.ToString())) {
                    ConfigNode impact_node = new ConfigNode("IMPACT_" + vessel.id.ToString());
                    impact_node.AddValue("transmitted", "False");
                    impact_node.AddValue("vesselname", vessel.vesselName);
                    impact_node.AddValue("science", net_science);
                    impact_node.AddValue("number", (science_experiment_number+1).ToString("0"));
                    science_node.AddNode(impact_node);
                }
                //if (ResearchAndDevelopment.Instance != null) {
                //    ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science + (float)net_science;
                //}

                config.Save(PluginHelper.getPluginSaveFilePath());
            }
        }
    }
}
