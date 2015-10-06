using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class CarbonDioxideElectroliser : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        const double carbonMonoxideMassByFraction = 28.010 / (28.010 + 15.999);
        const double oxygenMassByFraction = 1 - carbonMonoxideMassByFraction;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        
        protected double _current_power;
        protected double _fixedMaxConsumptionDioxideRate;
        protected double _current_rate;
        protected double _consumptionStorageRatio;

        protected double _dioxide_consumption_rate;
        protected double _monoxide_production_rate;
        protected double _oxygen_production_rate;

        protected string _dioxideResourceName;
        protected string _oxygenResourceName;
        protected string _monoxideResourceName;

        protected double _dioxide_density;
        protected double _oxygen_density;
        protected double _monoxide_density;

        protected double _availableDioxideMass;
        protected double _spareRoomOxygenMass;
        protected double _spareRoomMonoxideMass;

        protected double _maxCapacityDioxideMass;
        protected double _maxCapacityMonoxideMass;
        protected double _maxCapacityOxygenMass;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "CarbonDioxide Electrolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(_dioxideResourceName).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseELCPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public CarbonDioxideElectroliser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _dioxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonDioxide;
            _oxygenResourceName = InterstellarResourcesConfiguration.Instance.Oxygen;
            _monoxideResourceName = InterstellarResourcesConfiguration.Instance.CarbonMoxoxide;
            
            _dioxide_density = PartResourceLibrary.Instance.GetDefinition(_dioxideResourceName).density;
            _oxygen_density = PartResourceLibrary.Instance.GetDefinition(_oxygenResourceName).density;
            _monoxide_density = PartResourceLibrary.Instance.GetDefinition(_monoxideResourceName).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            // determine how much mass we can produce at max
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.ElectrolysisEnergyPerTon;

            var partsThatContainDioxide = _part.GetConnectedResources(_dioxideResourceName);
            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName);
            var partsThatContainMonoxide = _part.GetConnectedResources(_monoxideResourceName);

            _maxCapacityDioxideMass = partsThatContainDioxide.Sum(p => p.maxAmount) * _dioxide_density;
            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityMonoxideMass = partsThatContainMonoxide.Sum(p => p.maxAmount) * _monoxide_density;

            _availableDioxideMass = partsThatContainDioxide.Sum(p => p.amount) * _dioxide_density;
            _spareRoomOxygenMass = partsThatContainOxygen.Sum(r => r.maxAmount - r.amount) * _oxygen_density;
            _spareRoomMonoxideMass = partsThatContainMonoxide.Sum(r => r.maxAmount - r.amount) * _monoxide_density;

            // determine how much carbondioxide we can consume
            _fixedMaxConsumptionDioxideRate = Math.Min(_current_rate * TimeWarp.fixedDeltaTime, _availableDioxideMass);

            if (_fixedMaxConsumptionDioxideRate > 0 && (_spareRoomOxygenMass > 0 || _spareRoomMonoxideMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxMonoxideRate = _fixedMaxConsumptionDioxideRate * carbonMonoxideMassByFraction;
                var fixedMaxOxygenRate = _fixedMaxConsumptionDioxideRate * oxygenMassByFraction;

                var fixedMaxPossibleMonoxideRate = allowOverflow ? fixedMaxMonoxideRate : Math.Min(_spareRoomMonoxideMass, fixedMaxMonoxideRate);
                var fixedMaxPossibleOxygenRate = allowOverflow ? fixedMaxOxygenRate : Math.Min(_spareRoomOxygenMass, fixedMaxOxygenRate);

                var fixedMaxPossibleMonoxideRatio = fixedMaxPossibleMonoxideRate / fixedMaxMonoxideRate;
                var fixedMaxPossibleOxygenRatio = fixedMaxPossibleOxygenRate / fixedMaxOxygenRate;
                _consumptionStorageRatio = Math.Min(fixedMaxPossibleMonoxideRatio, fixedMaxPossibleOxygenRatio);

                // now we do the real elextrolysis
                _dioxide_consumption_rate = _part.RequestResource(_dioxideResourceName, _consumptionStorageRatio * _fixedMaxConsumptionDioxideRate / _dioxide_density) / TimeWarp.fixedDeltaTime * _dioxide_density;

                var monoxide_rate_temp = _dioxide_consumption_rate * carbonMonoxideMassByFraction;
                var oxygen_rate_temp = _dioxide_consumption_rate * oxygenMassByFraction;

                _monoxide_production_rate = -_part.ImprovedRequestResource(_monoxideResourceName, -monoxide_rate_temp * TimeWarp.fixedDeltaTime / _monoxide_density) / TimeWarp.fixedDeltaTime * _monoxide_density;
                _oxygen_production_rate = -_part.ImprovedRequestResource(_oxygenResourceName, -oxygen_rate_temp * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;
            }
            else
            {
                _dioxide_consumption_rate = 0;
                _monoxide_production_rate = 0;
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
            GUILayout.Label("CarbonMonoxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomMonoxideMass.ToString("0.00000") + " mT / " + _maxCapacityMonoxideMass.ToString("0.00000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("CarbonMonoxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_monoxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
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
            if (_monoxide_production_rate > 0 && _oxygen_production_rate > 0)
                _status = "Electrolysing CarbonDioxide";
            else if (_fixedMaxConsumptionDioxideRate <= 0.0000000001)
                _status = "Out of CarbonDioxide";
            else if (_monoxide_production_rate > 0)
                _status = "Insufficient " + _oxygenResourceName + " Storage";
            else if (_oxygen_production_rate > 0)
                _status = "Insufficient " + _monoxideResourceName + " Storage";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
