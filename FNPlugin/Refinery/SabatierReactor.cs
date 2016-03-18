using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class SabatierReactor : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;
        protected double _fixedConsumptionRate;

        protected double _carbondioxide_density;
        protected double _methane_density;
        protected double _hydrogen_density;
        protected double _oxygen_density;

        protected double _hydrogen_consumption_rate;
        protected double _carbondioxide_consumption_rate;

        protected double _methane_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_rate;
        

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Sabatier Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements {
            get
            {
                return _part.GetConnectedResources(_hydrogen_resource_name).Any(rs => rs.amount > 0) &&
                    _part.GetConnectedResources(_carbondioxide_resource_name).Any(rs => rs.amount > 0);
            }
        }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        protected string _carbondioxide_resource_name;
        protected string _methane_resource_name;
        protected string _hydrogen_resource_name;
        protected string _oxygen_resource_name;

        public SabatierReactor(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _carbondioxide_resource_name = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _hydrogen_resource_name = InterstellarResourcesConfiguration.Instance.Hydrogen;
            _methane_resource_name = InterstellarResourcesConfiguration.Instance.Methane;
            _oxygen_resource_name = InterstellarResourcesConfiguration.Instance.Oxygen;

            _carbondioxide_density = PartResourceLibrary.Instance.GetDefinition(_carbondioxide_resource_name).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_resource_name).density;
            _methane_density = PartResourceLibrary.Instance.GetDefinition(_methane_resource_name).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygen_resource_name).density;
        }

        protected double _maxCapacityCarbondioxideMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityMethaneMass;
        protected double _maxCapacityOxygenMass;

        protected double _availableCarbondioxideMass;
        protected double _availableHydrogenMass;
        protected double _spareRoomMethaneMass;
        protected double _spareRoomOxygenMass;

        protected double _carbonDioxideMassByFraction = 44.01 / (44.01 + (8 * 1.008));
        protected double _hydrogenMassByFraction = (8 * 1.008) / (44.01 + (8 * 1.008));
        protected double _oxygenMassByFraction = 32.0 / 52.0;
        protected double _methaneMassByFraction = 20.0 / 52.0;

        private double combined_consumption_rate;

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon; //* _vessel.atmDensity;

            // determine how much resource we have
            var partsThatContainCarbonDioxide = _part.GetConnectedResources(_carbondioxide_resource_name);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogen_resource_name);
            var partsThatContainMethane = _part.GetConnectedResources(_methane_resource_name);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygen_resource_name);

            _maxCapacityCarbondioxideMass = partsThatContainCarbonDioxide.Sum(p => p.maxAmount) * _carbondioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMethaneMass = partsThatContainMethane.Sum(p => p.maxAmount) * _methane_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;

            _availableCarbondioxideMass = partsThatContainCarbonDioxide.Sum(r => r.amount) * _carbondioxide_density;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogen_density;
            _spareRoomMethaneMass = partsThatContainMethane.Sum(r => r.maxAmount - r.amount) * _methane_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;

            var fixedMaxCarbondioxideConsumptionRate = _current_rate * _carbonDioxideMassByFraction * TimeWarp.fixedDeltaTime;
            var carbondioxideConsumptionRatio = fixedMaxCarbondioxideConsumptionRate > 0 
                ? Math.Min(fixedMaxCarbondioxideConsumptionRate, _availableCarbondioxideMass) / fixedMaxCarbondioxideConsumptionRate 
                : 0;

            var fixedMaxHydrogenConsumptionRate = _current_rate * _hydrogenMassByFraction * TimeWarp.fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(carbondioxideConsumptionRatio, hydrogenConsumptionRatio);

            if (_fixedConsumptionRate > 0 && _spareRoomMethaneMass > 0)
            {
                var fixedMaxPossibleProductionRate = Math.Min(_spareRoomMethaneMass, _fixedConsumptionRate);

                var carbonDioxide_consumption_rate = fixedMaxPossibleProductionRate * _carbonDioxideMassByFraction;
                var hydrogen_consumption_rate = fixedMaxPossibleProductionRate * _hydrogenMassByFraction;

                // consume the resource
                _hydrogen_consumption_rate = _part.RequestResource(_hydrogen_resource_name, hydrogen_consumption_rate / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                _carbondioxide_consumption_rate = _part.RequestResource(_carbondioxide_resource_name, carbonDioxide_consumption_rate / _carbondioxide_density) / TimeWarp.fixedDeltaTime * _carbondioxide_density;

                combined_consumption_rate = _hydrogen_consumption_rate + _carbondioxide_consumption_rate;

                var fixedMethaneProduction = combined_consumption_rate * _methaneMassByFraction * TimeWarp.fixedDeltaTime / _methane_density;
                var fixedOxygenProduction = combined_consumption_rate * _oxygenMassByFraction * TimeWarp.fixedDeltaTime / _oxygen_density;

                _methane_production_rate = -_part.RequestResource(_methane_resource_name, -fixedMethaneProduction) / TimeWarp.fixedDeltaTime * _methane_density;
                _oxygen_production_rate = -_part.RequestResource(_oxygen_resource_name, -fixedOxygenProduction) / TimeWarp.fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _hydrogen_consumption_rate = 0;
                _carbondioxide_consumption_rate = 0;
                _methane_production_rate = 0;
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
            GUILayout.Label("Overal Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((combined_consumption_rate / TimeWarp.fixedDeltaTime * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Dioxide Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableCarbondioxideMass.ToString("0.0000") + " mT / " + _maxCapacityCarbondioxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Carbon Dioxid Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_carbondioxide_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableHydrogenMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMethaneMass.ToString("0.0000") + " mT / " + _maxCapacityMethaneMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Methane Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_methane_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_methane_production_rate > 0)
                _status = "Sabatier Process Ongoing";
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
