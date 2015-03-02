extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class WaterElectroliser : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_rate;
        protected double _water_density;
        protected double _oxygen_density;
        protected double _hydrogen_density;

        protected double _water_consumption_rate;
        protected double _hydrogen_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_power;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Water Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return GameConstants.baseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public WaterElectroliser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / GameConstants.electrolysisEnergyPerTon;
            _water_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Water, _current_rate * TimeWarp.fixedDeltaTime / _water_density) / TimeWarp.fixedDeltaTime * _water_density;
            double h_rate_temp = _water_consumption_rate / (1 + GameConstants.electrolysisMassRatio);
            double o_rate_temp = h_rate_temp * GameConstants.electrolysisMassRatio;
            _hydrogen_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -h_rate_temp * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime*_hydrogen_density;
            _oxygen_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -o_rate_temp * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime*_oxygen_density;
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
            GUILayout.Label("Water Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label((_water_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label((_oxygen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0 && _oxygen_production_rate > 0)
            {
                _status = "Electrolysing";
            } else if (_hydrogen_production_rate > 0)
            {
                _status = "Electrolysing: Insufficient Oxygen Storage";
            } else if (_oxygen_production_rate > 0)
            {
                _status = "Electrolysing: Insufficient Hydrogen Storage";
            } else if (CurrentPower <= 0.01 * PowerRequirements)
            {
                _status = "Insufficient Power";
            } else
            {
                _status = "Insufficient Storage";
            }
        }
    }
}
