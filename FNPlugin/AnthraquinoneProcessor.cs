extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class AnthraquinoneProcessor : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_rate;
        protected double _water_density;
        protected double _hydrogen_peroxide_density;
        
        protected double _water_consumption_rate;
        protected double _hydrogen_peroxide_production_rate;
        protected double _current_power;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Anthraquinone Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseAnthraquiononePowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AnthraquinoneProcessor(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
            
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / PluginHelper.AnthraquinoneEnergyPerTon;
            _water_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Water, _current_rate * TimeWarp.fixedDeltaTime / _water_density) / TimeWarp.fixedDeltaTime * _water_density;
            _hydrogen_peroxide_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide, -_water_consumption_rate * TimeWarp.fixedDeltaTime / _hydrogen_peroxide_density) * _hydrogen_peroxide_density / TimeWarp.fixedDeltaTime;
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
            GUILayout.Label("Hydrogen Peroxide Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogen_peroxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_peroxide_production_rate > 0)
            {
                _status = "Electrolysing";
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
