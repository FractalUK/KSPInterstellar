using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class WaterGasShift : IRefineryActivity
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

        protected double _water_consumption_rate;
        protected double _monoxide_consumption_rate;
        protected double _hydrogen_production_rate;
        protected double _dioxide_production_rate;

        protected string _waterResourceName;
        protected string _monoxideResourceName;
        protected string _dioxideResourceName;
        protected string _hydrogenResourceName;

        protected double _water_density;
        protected double _dioxide_density;
        protected double _hydrogen_density;
        protected double _monoxide_density;

        protected double _availableWaterMass;
        protected double _availableMonoxideMass;
        protected double _spareRoomDioxideMass;
        protected double _spareRoomHydrogenMass;

        protected double _maxCapacityWaterMass;
        protected double _maxCapacityDioxideMass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityHydrogenMass;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Water Gas Shift"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements 
        {
            get 
            {
                return _part.GetConnectedResources(_waterResourceName).Any(rs => rs.amount > 0) && _part.GetConnectedResources(_monoxideResourceName).Any(rs => rs.amount > 0); 
            } 
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption * 5; } }

        public String Status { get { return String.Copy(_status); } }

        public WaterGasShift(Part part) 
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
            
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            var partsThatContainWater = _part.GetConnectedResources(_waterResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName);
            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);

            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;
            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableWaterMass = partsThatContainWater.Sum(r => r.amount) * _water_density;
            _availableMonoxideMass = partsThatContainMonoxide.Sum(r => r.amount) * _monoxide_density;
            _spareRoomDioxideMass = partsThatContainDioxide.Sum(r => r.maxAmount - r.amount) * _dioxide_density;
            _spareRoomHydrogenMass = partsThatContainHydrogen.Sum(r => r.maxAmount - r.amount) * _hydrogen_density;

            // determine how much carbondioxide we can consume
            var fixedMaxWaterConsumptionRate = _current_rate * waterMassByFraction * TimeWarp.fixedDeltaTime;
            var waterConsumptionRatio = fixedMaxWaterConsumptionRate > 0 ? Math.Min(fixedMaxWaterConsumptionRate, _availableWaterMass) / fixedMaxWaterConsumptionRate : 0;

            var fixedMaxMonoxideConsumptionRate =  _current_rate * monoxideMassByFraction * TimeWarp.fixedDeltaTime;
            var monoxideConsumptionRatio = fixedMaxMonoxideConsumptionRate > 0 ? Math.Min(fixedMaxMonoxideConsumptionRate, _availableMonoxideMass) / fixedMaxMonoxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(waterConsumptionRatio, monoxideConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrogenMass > 0 || _spareRoomDioxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrogenRate = _fixedConsumptionRate * hydrogenMassByFraction;
                var fixedMaxDioxideRate = _fixedConsumptionRate * dioxideMassByFraction;

                var fixedMaxPossibleHydrogenRate = allowOverflow ? fixedMaxHydrogenRate : Math.Min(_spareRoomHydrogenMass, fixedMaxHydrogenRate);
                var fixedMaxPossibleDioxideRate = allowOverflow ? fixedMaxDioxideRate : Math.Min(_spareRoomDioxideMass, fixedMaxDioxideRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleHydrogenRate / fixedMaxHydrogenRate, fixedMaxPossibleDioxideRate / fixedMaxDioxideRate);

                // now we do the real elextrolysis
                _water_consumption_rate = _part.RequestResource(_waterResourceName, waterMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _water_density) / TimeWarp.fixedDeltaTime * _water_density;
                _monoxide_consumption_rate = _part.RequestResource(_monoxideResourceName, monoxideMassByFraction * _consumptionStorageRatio * _fixedConsumptionRate / _monoxide_density) / TimeWarp.fixedDeltaTime * _monoxide_density;
                var combined_consumption_rate = _water_consumption_rate + _monoxide_consumption_rate;

                var hydrogen_rate_temp = combined_consumption_rate * hydrogenMassByFraction;
                var dioxide_rate_temp = combined_consumption_rate * dioxideMassByFraction;

                _hydrogen_production_rate = -_part.RequestResource(_hydrogenResourceName, -hydrogen_rate_temp * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                _dioxide_production_rate = -_part.RequestResource(_dioxideResourceName, -dioxide_rate_temp * TimeWarp.fixedDeltaTime / _dioxide_density) / TimeWarp.fixedDeltaTime * _dioxide_density;
            }
            else
            {
                _water_consumption_rate = 0;
                _monoxide_consumption_rate = 0;
                _hydrogen_production_rate = 0;
                _dioxide_production_rate = 0;
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
            GUILayout.Label("Water Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableWaterMass.ToString("0.0000") + " mT / " + _maxCapacityWaterMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Water Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_water_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonMonoxide Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableMonoxideMass.ToString("0.0000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonMonoxide Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_monoxide_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomDioxideMass.ToString("0.0000") + " mT / " + _maxCapacityDioxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonDioxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_dioxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrogenMass.ToString("0.00000") + " mT / " + _maxCapacityHydrogenMass.ToString("0.00000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_dioxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.00000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_production_rate > 0 && _dioxide_production_rate > 0)
                _status = "Water Gas Swifting";
            else if (_fixedConsumptionRate <= 0.0000000001)
            {
                if (_availableWaterMass <= 0.0000000001)
                    _status = "Out of Water";
                else
                    _status = "Out of CarbonMonoxide";
            }
            else if (_hydrogen_production_rate > 0)
                _status = _allowOverflow ? "Overflowing " : "Insufficient " + _dioxideResourceName + " Storage";
            else if (_dioxide_production_rate > 0)
                _status = _allowOverflow ? "Overflowing " : "Insufficient " + _hydrogenResourceName + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
