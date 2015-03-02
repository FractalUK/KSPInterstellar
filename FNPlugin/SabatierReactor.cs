extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class SabatierReactor : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;

        protected double _oxygen_density;
        protected double _methane_density;
        protected double _hydrogen_density;

        protected double _hydrogen_consumption_rate;
        protected double _methane_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_rate;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Sabatier ISRU"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0) && FlightGlobals.getStaticPressure(_vessel.transform.position) * ORSAtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(_vessel.mainBody.flightGlobalsIndex, "Carbon Dioxide") >= 0.01; } }

        public double PowerRequirements { get { return GameConstants.baseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public SabatierReactor(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _methane_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Methane).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / GameConstants.electrolysisEnergyPerTon * _vessel.atmDensity;
            double h_rate_temp = _current_rate / (1 + GameConstants.electrolysisMassRatio);
            double o_rate_temp = h_rate_temp * (GameConstants.electrolysisMassRatio - 1.0);
            _hydrogen_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, h_rate_temp * TimeWarp.fixedDeltaTime / _hydrogen_density / 2);
            if (_hydrogen_consumption_rate > 0)
            {
                _oxygen_production_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -o_rate_temp * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime / _oxygen_density;
                _methane_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Methane, -o_rate_temp * 2.0 / _oxygen_density * TimeWarp.fixedDeltaTime / _methane_density) * _methane_density / TimeWarp.fixedDeltaTime;
            }
            updateStatusMessage();
        }

        public void UpdateGUI()
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(150));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_hydrogen_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_methane_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_oxygen_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_methane_production_rate > 0 && _oxygen_production_rate > 0)
            {
                _status = "Sabatier Process Ongoing";
            } else if (_oxygen_production_rate > 0)
            {
                _status = "Ongoing: Insufficient Oxygen Storage";
            } else if (_methane_production_rate > 0)
            {
                _status = "Ongoing: Insufficient Methane Storage";
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = "Insufficient Power";
            } else
            {
                _status = "Insufficient Storage";
            }
        }
    }
}
