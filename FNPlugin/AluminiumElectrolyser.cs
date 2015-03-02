extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class AluminiumElectrolyser : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_rate;
        protected double _alumina_density;
        protected double _aluminium_density;
        protected double _oxygen_density;

        protected double _alumina_consumption_rate;
        protected double _aluminium_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_power;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Aluminium Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Alumina).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return GameConstants.baseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AluminiumElectrolyser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _alumina_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Alumina).density;
            _aluminium_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Aluminium).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / GameConstants.electrolysisEnergyPerTon;
            _alumina_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Alumina, _current_rate * TimeWarp.fixedDeltaTime / _alumina_density) / TimeWarp.fixedDeltaTime * _alumina_density;
            _aluminium_production_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Aluminium, -_alumina_consumption_rate * TimeWarp.fixedDeltaTime / _aluminium_density) * _aluminium_density / TimeWarp.fixedDeltaTime;
            _oxygen_production_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -GameConstants.aluminiumElectrolysisMassRatio * _alumina_consumption_rate * TimeWarp.fixedDeltaTime / _oxygen_density) * _oxygen_density / TimeWarp.fixedDeltaTime;
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
            GUILayout.Label("Alumina Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_alumina_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Aluminium Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_aluminium_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_oxygen_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_aluminium_production_rate > 0 && _oxygen_production_rate > 0)
            {
                _status = "Electrolysing";
            } else if (_alumina_consumption_rate > 0)
            {
                _status = "Electrolysing: Insufficient Oxygen Storage";
            } else if (_oxygen_production_rate > 0)
            {
                _status = "Electrolysing: Insufficient Aluminium Storage";
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
