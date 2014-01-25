using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class VABThermalUI : MonoBehaviour {
        protected int windowID = 825462;
        protected Rect windowPosition = new Rect(300, 60, 300, 100);
        protected GUIStyle bold_label;
        protected GUIStyle green_label;
        protected GUIStyle red_label;
        protected GUIStyle orange_label;
        protected bool render_window = true;
        protected double total_source_power = 0;
        protected double min_source_temp = 0;
        protected double rad_max_dissip = 0;
        protected double min_source_power = 0;
        protected double resting_radiator_temp_at_100pcnt = 0;
        protected double resting_radiator_temp_at_30pcnt = 0;
        protected double average_rad_temp = 0;
        protected double au_scale = 1;
        protected bool has_generators = false;
        protected double generator_efficiency_at_100pcnt = 0;
        protected double generator_efficiency_at_30pcnt = 0;

        public void Start() {
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        public void Update() {
            // thermal logic
            List<FNThermalSource> thermal_sources = new List<FNThermalSource>();
            List<FNRadiator> radiators = new List<FNRadiator>();
            List<ModuleDeployableSolarPanel> panels = new List<ModuleDeployableSolarPanel>();
            List<FNGenerator> generators = new List<FNGenerator>();
            foreach (Part p in EditorLogic.fetch.ship.parts) {
                thermal_sources.AddRange(p.FindModulesImplementing<FNThermalSource>());
                radiators.AddRange(p.FindModulesImplementing<FNRadiator>());
                panels.AddRange(p.FindModulesImplementing<ModuleDeployableSolarPanel>());
                generators.AddRange(p.FindModulesImplementing<FNGenerator>());
            }

            total_source_power = 0;
            min_source_power = 0;
            min_source_temp = double.MaxValue;
            foreach (FNThermalSource tsource in thermal_sources) {
                total_source_power += tsource.getThermalPower();
                min_source_temp = Math.Min(tsource.getCoreTemp(), min_source_temp);
                min_source_power += tsource.getMinimumThermalPower();
            }

            foreach (ModuleDeployableSolarPanel panel in panels) {
                total_source_power += panel.chargeRate * 0.0005/au_scale/au_scale;
            }

            double n_rads = 0;
            rad_max_dissip = 0;
            average_rad_temp = 0;
            foreach (FNRadiator radiator in radiators) {
                double area = radiator.radiatorArea;
                double temp = radiator.isupgraded ? radiator.upgradedRadiatorTemp : radiator.radiatorTemp;
                temp = Math.Min(temp, min_source_temp);
                n_rads += 1;
                rad_max_dissip += GameConstants.stefan_const * area * Math.Pow(temp, 4) / 1e6;
                average_rad_temp += temp;
            }
            average_rad_temp = average_rad_temp / n_rads;

            double rad_ratio = total_source_power / rad_max_dissip;
            resting_radiator_temp_at_100pcnt = ((!double.IsInfinity(rad_ratio) && !double.IsNaN(rad_ratio)) ? Math.Pow(rad_ratio,0.25) : 0) * average_rad_temp;
            resting_radiator_temp_at_30pcnt = ((!double.IsInfinity(rad_ratio) && !double.IsNaN(rad_ratio)) ? Math.Pow(0.3*rad_ratio, 0.25) : 0) * average_rad_temp;

            if (generators.Count > 0) {
                has_generators = true;
                generator_efficiency_at_100pcnt = (double.MaxValue == min_source_temp || (double.IsInfinity(resting_radiator_temp_at_100pcnt) || double.IsNaN(resting_radiator_temp_at_100pcnt))) ? 0 : 1 - resting_radiator_temp_at_100pcnt / min_source_temp;
                generator_efficiency_at_100pcnt = Math.Max(((generators[0].isupgraded) ? generators[0].upgradedpCarnotEff : generators[0].pCarnotEff)*generator_efficiency_at_100pcnt,0);
                generator_efficiency_at_30pcnt = (double.MaxValue == min_source_temp || (double.IsInfinity(resting_radiator_temp_at_30pcnt) || double.IsNaN(resting_radiator_temp_at_30pcnt))) ? 0 : 1 - resting_radiator_temp_at_30pcnt / min_source_temp;
                generator_efficiency_at_30pcnt = Math.Max(((generators[0].isupgraded) ? generators[0].upgradedpCarnotEff : generators[0].pCarnotEff) * generator_efficiency_at_30pcnt, 0);
            } else {
                has_generators = false;
            }

            if (min_source_temp == double.MaxValue) {
                min_source_temp = -1;
            }
        }

        protected void OnGUI() {
            windowPosition = GUILayout.Window(windowID, windowPosition, Window, "KSP Interstellar Thermal Mechanics Helper");
        }

        private void Window(int windowID) {
            green_label = new GUIStyle(GUI.skin.label);
            green_label.normal.textColor = Color.green;
            red_label = new GUIStyle(GUI.skin.label);
            red_label.normal.textColor = Color.red;
            orange_label = new GUIStyle(GUI.skin.label);
            orange_label.normal.textColor = Color.yellow;
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x")) {
                render_window = false;
            }
            GUIStyle radiator_label = green_label;
            if (rad_max_dissip < total_source_power) {
                radiator_label = orange_label;
                if (rad_max_dissip < min_source_power) {
                    radiator_label = red_label;
                }
            }
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Distance from Kerbol: /AU (Kerbin = 1)", GUILayout.ExpandWidth(false), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            au_scale = GUILayout.HorizontalSlider((float)au_scale, 0.001f, 8f, GUILayout.ExpandWidth(true));
            GUILayout.Label(au_scale.ToString("0.000")+ " AU", GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Heat Production:", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(total_source_power), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Minimum Thermal Source Temperature:", bold_label, GUILayout.ExpandWidth(true));
            string source_temp_string = (min_source_temp < 0) ? "N/A" : min_source_temp.ToString("0.0") + " K";
            GUILayout.Label(source_temp_string, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Radiator Maximum Dissipation:", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(rad_max_dissip), radiator_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            string resting_radiator_temp_at_100pcntStr = (!double.IsInfinity(resting_radiator_temp_at_100pcnt) && !double.IsNaN(resting_radiator_temp_at_100pcnt)) ? resting_radiator_temp_at_100pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label("Radiator Resting Temperature at 100% Power:", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(resting_radiator_temp_at_100pcntStr, radiator_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            string resting_radiator_temp_at_30pcntStr = (!double.IsInfinity(resting_radiator_temp_at_30pcnt) && !double.IsNaN(resting_radiator_temp_at_30pcnt)) ? resting_radiator_temp_at_30pcnt.ToString("0.0") + " K" : "N/A";
            GUILayout.Label("Radiator Resting Temperature at 30% Power:", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(resting_radiator_temp_at_30pcntStr, radiator_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            if (has_generators) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Resting Generator Efficiency at 100% Power:", bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((generator_efficiency_at_100pcnt*100).ToString("0.00") + "%", radiator_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Resting Generator Efficiency at 30% Power:", bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((generator_efficiency_at_30pcnt * 100).ToString("0.00") + "%", radiator_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        protected string getPowerFormatString(double power) {
            if (Math.Abs(power) >= 1000) {
                if (Math.Abs(power) > 20000) {
                    return (power / 1000).ToString("0.0") + " GW";
                } else {
                    return (power / 1000).ToString("0.00") + " GW";
                }
            } else {
                if (Math.Abs(power) > 20) {
                    return power.ToString("0.0") + " MW";
                } else {
                    if (Math.Abs(power) >= 1) {
                        return power.ToString("0.00") + " MW";
                    } else {
                        return (power * 1000).ToString("0.00") + " KW";
                    }
                }
            }
        }
    }
}
