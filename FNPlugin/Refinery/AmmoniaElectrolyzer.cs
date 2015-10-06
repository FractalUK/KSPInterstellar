using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AmmoniaElectrolyzer : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";

        protected double _current_mass_rate;
        protected double _current_power;
        protected double _ammonia_density;
        protected double _nitrogen_density;
        protected double _hydrogen_density;

        protected double _ammonia_consumption_mass_rate;
        protected double _hydrogen_production_mass_rate;
        protected double _nitrogen_production_mass_rate;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Ammonia Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AmmoniaElectrolyzer(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_mass_rate = (CurrentPower / PluginHelper.ElectrolysisEnergyPerTon) * 14.45;

            var spare_capacity_nitrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Nitrogen);
            var spare_capacity_hydrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Hydrogen);

            double max_nitrogen_mass_rate = (_current_mass_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass)) * TimeWarp.fixedDeltaTime / _nitrogen_density;
            double max_hydrogen_mass_rate = (_current_mass_rate * GameConstants.ammoniaHydrogenFractionByMass) * TimeWarp.fixedDeltaTime / _hydrogen_density;

            // prevent overflow
            if (spare_capacity_nitrogen <= max_nitrogen_mass_rate || spare_capacity_hydrogen <= max_hydrogen_mass_rate)
            {
                _ammonia_consumption_mass_rate = 0;
                _hydrogen_production_mass_rate = 0;
                _nitrogen_production_mass_rate = 0;
            }
            else
            {
                _ammonia_consumption_mass_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, _current_mass_rate * TimeWarp.fixedDeltaTime / _ammonia_density) / TimeWarp.fixedDeltaTime * _ammonia_density;
                double hydrogen_mass_rate = _ammonia_consumption_mass_rate * GameConstants.ammoniaHydrogenFractionByMass;
                double nitrogen_mass_rate = _ammonia_consumption_mass_rate * (1 - GameConstants.ammoniaHydrogenFractionByMass);

                _hydrogen_production_mass_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogen_mass_rate * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                _nitrogen_production_mass_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Nitrogen, -nitrogen_mass_rate * TimeWarp.fixedDeltaTime / _nitrogen_density) / TimeWarp.fixedDeltaTime * _nitrogen_density;
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
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammonia Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_ammonia_consumption_mass_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_production_mass_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_nitrogen_production_mass_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            var spare_capacity_nitrogen = _part.GetResourceSpareCapacity(InterstellarResourcesConfiguration.Instance.Nitrogen);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Spare Capacity Nitrogen", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(spare_capacity_nitrogen.ToString("0.000"), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_mass_rate > 0 && _nitrogen_production_mass_rate > 0)
                _status = "Electrolysing";
            else if (_hydrogen_production_mass_rate > 0)
                _status = "Electrolysing: Insufficient Nitrogen Storage";
            else if (_nitrogen_production_mass_rate > 0)
                _status = "Electrolysing: Insufficient Hydrogen Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
