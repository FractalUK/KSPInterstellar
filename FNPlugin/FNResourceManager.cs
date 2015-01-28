extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    public class FNResourceManager : ORSResourceManager {
        

        public FNResourceManager(PartModule pm, String resource_name) : base(pm, resource_name) {
            windowPosition = new Rect(200, 200, 350, 100);

            
        }

        protected override void pluginSpecificImpl() {
            if (resource_name == FNRESOURCE_CHARGED_PARTICLES) {
                flow_type = FNRESOURCE_FLOWTYPE_EVEN;
            }

            if (String.Equals(this.resource_name, FNResourceManager.FNRESOURCE_WASTEHEAT) && !PluginHelper.IsThermalDissipationDisabled) 
            {   // passive dissip of waste heat - a little bit of this
                double vessel_mass = my_vessel.GetTotalMass();
                double passive_dissip = passive_temp_p4 * GameConstants.stefan_const * vessel_mass * 2.0;
                internl_power_extract += passive_dissip * TimeWarp.fixedDeltaTime;

                if (my_vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(my_vessel.mainBody)) 
                { // passive convection - a lot of this
                    double pressure = FlightGlobals.getStaticPressure(my_vessel.transform.position);
                    double delta_temp = 20;
                    double conv_power_dissip = pressure * delta_temp * vessel_mass * 2.0 * GameConstants.rad_const_h / 1e6 * TimeWarp.fixedDeltaTime;
                    internl_power_extract += conv_power_dissip;
                }
            }
        }
                
        protected override void doWindow(int windowID) {
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            green_label = new GUIStyle(GUI.skin.label);
            green_label.normal.textColor = Color.green;
            red_label = new GUIStyle(GUI.skin.label);
            red_label.normal.textColor = Color.red;
            //right_align = new GUIStyle(GUI.skin.label);
            //right_align.alignment = TextAnchor.UpperRight;
            GUIStyle net_style;
            GUIStyle net_style2;
            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x")) {
                render_window = false;
            }
            GUILayout.Space(2);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Theoretical Supply",bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_stable_supply), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current Supply", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_supply), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Demand", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_resource_demand), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            double demand_supply = stored_supply - stored_resource_demand;
            double demand_stable_supply = stored_resource_demand / stored_stable_supply;
            if (demand_supply < -0.001) {
                net_style = red_label;
            } else {
                net_style = green_label;
            }
            if (demand_stable_supply > 1) {
                net_style2 = red_label;
            } else {
                net_style2 = green_label;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Net Power", bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(demand_supply), net_style, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.EndHorizontal();
            if (!double.IsNaN(demand_stable_supply) && !double.IsInfinity(demand_stable_supply)) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Utilisation", bold_label, GUILayout.ExpandWidth(true));
                GUILayout.Label((demand_stable_supply).ToString("P3"), net_style2, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Component",bold_label, GUILayout.ExpandWidth(true));
            GUILayout.Label("Demand", bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.Label("Priority", bold_label, GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
            GUILayout.EndHorizontal();
            if (power_draw_list_archive != null) {
                foreach (KeyValuePair<ORSResourceSuppliable, double> power_kvp in power_draw_list_archive) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(power_kvp.Key.getResourceManagerDisplayName(), GUILayout.ExpandWidth(true));
                    GUILayout.Label(getPowerFormatString(power_kvp.Value), GUILayout.ExpandWidth(false),GUILayout.MinWidth(80));
                    GUILayout.Label(power_kvp.Key.getPowerPriority().ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("DC Electrical System", GUILayout.ExpandWidth(true));
            GUILayout.Label(getPowerFormatString(stored_charge_demand), GUILayout.ExpandWidth(false), GUILayout.MinWidth(80));
            GUILayout.Label("0", GUILayout.ExpandWidth(false), GUILayout.MinWidth(50));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
            
        }

    }
}
