using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin {
    class FNMassSpectrometer : PartModule{
        protected Rect windowPosition = new Rect(20, 20, 300, 100);
        protected int windowID = 346578435;
        protected bool render_window = false;
        protected GUIStyle bold_label;
        protected int analysis_count = 0;
        protected static int analysis_length = 1500;

        [KSPEvent(guiActive = true, guiName = "Show Spectrometry Results", active = true)]
        public void showWindow() {
            render_window = true;
        }

        [KSPEvent(guiActive = true, guiName = "Hide Spectrometry Results", active = true)]
        public void hideWindow() {
            render_window = false;
        }

        public override void OnStart(StartState state) {
            if (state == StartState.Editor) {
                return;
            }
            
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
            
        }

        public override void OnUpdate() {
            Events["showWindow"].active = !render_window;
            Events["hideWindow"].active = render_window;
            if (!vessel.isActiveVessel || part == null) {
                RenderingManager.RemoveFromPostDrawQueue(0, OnGUI);
            }
        }

        private void OnGUI() {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window) {
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "GC/MS Atmospheric Composition");
                if (analysis_count <= analysis_length) {
                    analysis_count++;
                }
            }
        }

        private void Window(int windowID) {
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x")) {
                render_window = false;
            }
            GUILayout.BeginVertical();
            if (vessel.altitude < PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                if (analysis_count > analysis_length) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Gas", bold_label, GUILayout.Width(150));
                    GUILayout.Label("Abundance", bold_label, GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                    foreach (ORSAtmosphericResource atmospheric_resource in ORSAtmosphericResourceHandler.getAtmosphericCompositionForBody(vessel.mainBody.flightGlobalsIndex)) {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(atmospheric_resource.getDisplayName(), GUILayout.Width(150));
                        string resource_abundance_str;
                        if (atmospheric_resource.getResourceAbundance() > 0.001) {
                            resource_abundance_str = (atmospheric_resource.getResourceAbundance() * 100.0).ToString() + "%";
                        } else {
                            if (atmospheric_resource.getResourceAbundance() > 0.000001) {
                                resource_abundance_str = (atmospheric_resource.getResourceAbundance() * 1e6).ToString() + " ppm";
                            } else {
                                resource_abundance_str = (atmospheric_resource.getResourceAbundance() * 1e9).ToString() + " ppb";
                            }
                        }
                        GUILayout.Label(resource_abundance_str, GUILayout.Width(150));
                        GUILayout.EndHorizontal();
                    }
                } else {
                    double percent_analysed = (double)analysis_count / analysis_length*100;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Analysing...", GUILayout.Width(150));
                    GUILayout.Label(percent_analysed.ToString("0.00") + "%", GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                }
                
            } else {
                GUILayout.Label("--No Atmosphere to Sample--", GUILayout.ExpandWidth(true));
                analysis_count = 0;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
