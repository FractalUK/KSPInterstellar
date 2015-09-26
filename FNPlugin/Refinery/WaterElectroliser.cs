using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class WaterElectroliser : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        const double hydrogenMassByFraction = (2 * 1.008) / (15.999 + (2 * 1.008));
        const double oxygenMassByFraction = 1 - hydrogenMassByFraction;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        
        protected double _water_consumption_rate;
        protected double _hydrogen_production_rate;
        protected double _oxygen_production_rate;
        protected double _current_power;
        protected double _fixedMaxConsumptionWaterRate;
        protected double _current_rate;
        protected double _consumptionStorageRatio;

        protected double _water_density;
        protected double _oxygen_density;
        protected double _hydrogen_density;

        protected double _availableWaterMass;
        protected double _spareRoomOxygenMass;
        protected double _spareRoomHydrogenMass;

        protected double _maxCapacityWaterMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityOxygenMass;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Water Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements {  get  {  return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0);  } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public WaterElectroliser(Part part) 
        {
            _part = part;

            _vessel = part.vessel;
            _water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water);
            var partsThatContainOxygen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Oxygen);
            var partsThatContainHydrogen = _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;

            _availableWaterMass = partsThatContainWater.Sum(p => p.amount) * _water_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;

            // determine how much water we can consume
            _fixedMaxConsumptionWaterRate = Math.Min(_current_rate * TimeWarp.fixedDeltaTime, _availableWaterMass);

            if (_fixedMaxConsumptionWaterRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomHydrogenMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedMaxConsumptionWaterRate * hydrogenMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionWaterRate * oxygenMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleHydrogenRatio = fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _water_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Water, _consumptionStorageRatio * _fixedMaxConsumptionWaterRate / _water_density) / TimeWarp.fixedDeltaTime * _water_density;

                var hydrogen_rate_temp = _water_consumption_rate * hydrogenMassByFraction;
                var oxygen_rate_temp = _water_consumption_rate * oxygenMassByFraction;

                _hydrogen_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, -hydrogen_rate_temp * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                _oxygen_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Oxygen, -oxygen_rate_temp * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _water_consumption_rate = 0;
                _hydrogen_production_rate = 0;
                _oxygen_production_rate = 0;
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
            GUILayout.Label("Water Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Consumption Storage Ratio", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_water_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0 && _oxygen_production_rate > 0)
                _status = "Electrolysing Water";
            else if (_fixedMaxConsumptionWaterRate <= 0.0000000001)
                _status = "Out of water";
            else if (_hydrogen_production_rate > 0)
                _status = "Insufficient " + InterstellarResourcesConfiguration.Instance.Oxygen + " Storage";
            else if (_oxygen_production_rate > 0)
                _status = "Insufficient " + InterstellarResourcesConfiguration.Instance.Hydrogen + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
