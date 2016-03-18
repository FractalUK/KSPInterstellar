using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class ReverseWaterGasShift : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        const double waterMassByFraction = 18.01528 / (18.01528 + 28.010);
        const double monoxideMassByFraction = 1 - waterMassByFraction;

        const double hydrogenMassByFraction = (2 * 1.008) / (44.01 + (2 * 1.008));
        const double dioxideMassByFraction = 1 - hydrogenMassByFraction;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected bool _allowOverflow;
        
        protected double _current_power;
        protected double _current_rate;
        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;

        protected double _dioxide_consumption_rate;
        protected double _hydrogen_consumption_rate;
        protected double _monoxide_production_rate;
        protected double _water_production_rate;

        protected string _waterResourceName;
        protected string _monoxideResourceName;
        protected string _dioxideResourceName;
        protected string _hydrogenResourceName;

        protected double _water_density;
        protected double _dioxide_density;
        protected double _hydrogen_density;
        protected double _monoxide_density;

        protected double _availableDioxideMass;
        protected double _availableHydrogenMass;
        protected double _spareRoomWaterMass;
        protected double _spareRoomMonoxideMass;

        protected double _maxCapacityWaterMass;
        protected double _maxCapacityDioxideMass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityHydrogenMass;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Reverse Water Gas Shift"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements 
        {
            get 
            {
                return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0) && _part.GetConnectedResources(_hydrogenResourceName).Any(rs => rs.amount > 0); 
            } 
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption * 5; } }

        public String Status { get { return String.Copy(_status); } }

        public ReverseWaterGasShift(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _waterResourceName = InterstellarResourcesConfiguration.Instance.Water;
            _monoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            _dioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _hydrogenResourceName = InterstellarResourcesConfiguration.Instance.Hydrogen;

            _water_density = PartResourceLibrary.Instance.GetDefinition(_waterResourceName).density;
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(_hydrogenResourceName).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }
        
        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _allowOverflow = allowOverflow;
            
            // determine overal maximum production rate
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            // determine how much resource we have
            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName);
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableDioxideMass = partsThatContainDioxide.Sum(r => r.amount) * _dioxide_density;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogen_density;

            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _water_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;

            // determine how much we can consume
            var fixedMaxDioxideConsumptionRate = _current_rate * dioxideMassByFraction * TimeWarp.fixedDeltaTime;
            var dioxideConsumptionRatio = fixedMaxDioxideConsumptionRate > 0 ? Math.Min(fixedMaxDioxideConsumptionRate, _availableDioxideMass) / fixedMaxDioxideConsumptionRate : 0;

            var fixedMaxHydrogenConsumptionRate =  _current_rate * hydrogenMassByFraction * TimeWarp.fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(dioxideConsumptionRatio, hydrogenConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomMonoxideMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedConsumptionRate * monoxideMassByFraction;
                var fixedMaxWaterRate = _fixedConsumptionRate * waterMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                _dioxide_consumption_rate = _part.RequestResource(_dioxideResourceName, dioxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _dioxide_density) / TimeWarp.fixedDeltaTime * _dioxide_density;
                _hydrogen_consumption_rate = _part.RequestResource(_hydrogenResourceName, hydrogenMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                var combined_consumption_rate = _dioxide_consumption_rate + _hydrogen_consumption_rate;

                var monoxide_rate_temp = combined_consumption_rate * monoxideMassByFraction;
                var water_rate_temp = combined_consumption_rate * waterMassByFraction;

                _monoxide_production_rate = -_part.RequestResource(_monoxideResourceName, -monoxide_rate_temp * TimeWarp.fixedDeltaTime / _monoxide_density) / TimeWarp.fixedDeltaTime * _monoxide_density;
                _water_production_rate = -_part.RequestResource(_waterResourceName, -water_rate_temp * TimeWarp.fixedDeltaTime / _water_density) / TimeWarp.fixedDeltaTime * _water_density;
            }
            else
            {
                _dioxide_consumption_rate = 0;
                _hydrogen_consumption_rate = 0;
                _monoxide_production_rate = 0;
                _water_production_rate = 0;
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
            GUILayout.Label("Current Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_fixedConsumptionRate / TimeWarp.fixedDeltaTime * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Consumption Storage Ratio", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_consumptionStorageRatio * 100).ToString("0.0000") + "%"), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_dioxide_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_water_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MonoxideMonoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("MonoxideMonoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_monoxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_monoxide_production_rate > 0 && _water_production_rate > 0)
                _status = "Water Gas Swifting";
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                if (_availableDioxideMass <= 0.0000000001)
                    _status = "Out of CarbonDioxide";
                else
                    _status = "Out of Hydrogen";
            }
            else if (_monoxide_production_rate > 0)
                _status = _allowOverflow ? "Overflowing " : "Insufficient " + _waterResourceName + " Storage";
            else if (_water_production_rate > 0)
                _status = _allowOverflow ? "Overflowing " : "Insufficient " + _monoxideResourceName + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
