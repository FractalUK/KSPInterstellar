extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class MonopropellantProducer : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;

        protected double _ammonia_density;
        protected double _water_density;
        protected double _hydrogen_peroxide_density;
        protected double _hydrazine_density;

        protected double _ammonia_consumption_rate;
        protected double _hydrogen_peroxide_consumption_rate;
        protected double _water_production_rate;
        protected double _hydrazine_production_rate;
        protected double _current_rate;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Peroxide Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).Any(rs => rs.amount > 0) && _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return GameConstants.basePechineyUgineKuhlmannPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public MonopropellantProducer(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
            _hydrazine_density = PartResourceLibrary.Instance.GetDefinition("MonoPropellant").density;
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / GameConstants.pechineyUgineKuhlmannEnergyPerTon;
            _ammonia_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, 0.5 * _current_rate * (1 - GameConstants.pechineyUgineKuhlmannMassRatio) * TimeWarp.fixedDeltaTime / _ammonia_density) * _ammonia_density / TimeWarp.fixedDeltaTime;
            _hydrogen_peroxide_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide, 0.5 * _current_rate * GameConstants.pechineyUgineKuhlmannMassRatio * TimeWarp.fixedDeltaTime / _hydrogen_peroxide_density) * _hydrogen_peroxide_density / TimeWarp.fixedDeltaTime;
            if (_ammonia_consumption_rate > 0 && _hydrogen_peroxide_consumption_rate > 0)
            {
                double mono_prop_produciton_rate = _ammonia_consumption_rate + _hydrogen_peroxide_consumption_rate;
                _hydrazine_production_rate = -_part.ImprovedRequestResource("MonoPropellant", -mono_prop_produciton_rate * TimeWarp.fixedDeltaTime / _hydrazine_density * GameConstants.pechineyUgineKuhlmannMassRatio2) * _hydrazine_density / TimeWarp.fixedDeltaTime;
                _water_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Water, -mono_prop_produciton_rate * TimeWarp.fixedDeltaTime / _water_density * (1.0 - GameConstants.pechineyUgineKuhlmannMassRatio2)) * _water_density / TimeWarp.fixedDeltaTime;
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
            GUILayout.Label("Ammona Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_ammonia_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_hydrogen_peroxide_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_water_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrazine (Monopropellant) Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_hydrazine_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_water_production_rate > 0 && _hydrazine_production_rate > 0)
            {
                _status = "Peroxide Process Ongoing";
            } else if (_hydrazine_production_rate > 0)
            {
                _status = "Ongoing: Insufficient Monopropellant Storage";
            } else if (_water_production_rate > 0)
            {
                _status = "Ongoing: Insufficient Water Storage";
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = "Insufficient Power";
            } else
            {
                if (_ammonia_consumption_rate > 0 && _hydrogen_peroxide_consumption_rate > 0)
                {
                    _status = "Insufficient Storage";
                } else if (_ammonia_consumption_rate > 0)
                {
                    _status = "Hydrogen Peroxide Deprived";
                } else if (_hydrogen_peroxide_consumption_rate > 0)
                {
                    _status = "Ammonia Deprived";
                } else
                {
                    _status = "Hydrogen Peroxide and Ammonia Deprived";
                }
            }
        }
    }
}
