using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AnthraquinoneProcessor : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_rate;
        protected double _fixedConsumptionRate;

		protected double _hydrogen_density;
		protected double _oxygen_density;
        protected double _hydrogen_peroxide_density;

        private string _oxygenResourceName;
        private string _hydrogenResourceName;
        private string _hydrogenPeroxideResourceName;

        protected double _maxCapacityOxygenMass;
        protected double _maxCapacityHydrogenMass;
        protected double _maxCapacityPeroxideMass;

        private double _availableOxygenMass;
        private double _availableHydrogenMass;
        private double _spareRoomHydrogenPeroxideMass;

		protected double _hydrogen_consumption_rate;
		protected double _oxygen_consumption_rate;
        protected double _hydrogen_peroxide_production_rate;

        protected double _current_power;
		protected double _hydrogenMassByFraction = (1.0079 * 2)/ 34.01468;
        protected double _oxygenMassByFraction = 1 - ((1.0079 * 2) / 34.01468);
        private GUIStyle _bold_label;
       

        public String ActivityName { get { return "Anthraquinone Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements 
        { 
            get 
            {
                return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0) &&
                    _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Oxygen).Any(rs => rs.amount > 0); 
            } 
        }

        public double PowerRequirements { get { return PluginHelper.BaseAnthraquiononePowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AnthraquinoneProcessor(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _oxygenResourceName = InterstellarResourcesConfiguration.Instance.Oxygen;
            _hydrogenResourceName = InterstellarResourcesConfiguration.Instance.Hydrogen;
            _hydrogenPeroxideResourceName = InterstellarResourcesConfiguration.Instance.HydrogenPeroxide;

			_hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
			_oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
	    {
            // determine how much resource we have
		    _current_power = PowerRequirements * rateMultiplier;
		    _current_rate = CurrentPower / PluginHelper.AnthraquinoneEnergyPerTon;

            var partsThatContainOxygen = _part.GetConnectedResources(_oxygenResourceName);
            var partsThatContainHydrogen = _part.GetConnectedResources(_hydrogenResourceName);
            var partsThatContainPeroxide = _part.GetConnectedResources(_hydrogenPeroxideResourceName);

            _maxCapacityOxygenMass = partsThatContainOxygen.Sum(p => p.maxAmount) * _oxygen_density;
            _maxCapacityHydrogenMass = partsThatContainHydrogen.Sum(p => p.maxAmount) * _hydrogen_density;
            _maxCapacityPeroxideMass = partsThatContainPeroxide.Sum(p => p.maxAmount) * _hydrogen_peroxide_density;

            _availableOxygenMass = partsThatContainOxygen.Sum(r => r.amount) * _oxygen_density;
            _availableHydrogenMass = partsThatContainHydrogen.Sum(r => r.amount) * _hydrogen_density;
            _spareRoomHydrogenPeroxideMass = partsThatContainPeroxide.Sum(r => r.maxAmount - r.amount) * _hydrogen_peroxide_density;

            // determine how much we can consume
            var fixedMaxOxygenConsumptionRate = _current_rate * _oxygenMassByFraction * TimeWarp.fixedDeltaTime;
            var oxygenConsumptionRatio = fixedMaxOxygenConsumptionRate > 0 ? Math.Min(fixedMaxOxygenConsumptionRate, _availableOxygenMass) / fixedMaxOxygenConsumptionRate : 0;

            var fixedMaxHydrogenConsumptionRate = _current_rate * _hydrogenMassByFraction * TimeWarp.fixedDeltaTime;
            var hydrogenConsumptionRatio = fixedMaxHydrogenConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenConsumptionRate, _availableHydrogenMass) / fixedMaxHydrogenConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(oxygenConsumptionRatio, hydrogenConsumptionRatio);

            if (_fixedConsumptionRate > 0 && _spareRoomHydrogenPeroxideMass > 0)
            {
                var fixedMaxPossibleHydrogenPeroxidenRate = Math.Min(_spareRoomHydrogenPeroxideMass, _fixedConsumptionRate);

                var hydrogen_consumption_rate = fixedMaxPossibleHydrogenPeroxidenRate * _hydrogenMassByFraction;
                var oxygen_consumption_rate = fixedMaxPossibleHydrogenPeroxidenRate * _oxygenMassByFraction;

                // consume the resource
                _hydrogen_consumption_rate = _part.RequestResource(_hydrogenResourceName, hydrogen_consumption_rate / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;
                _oxygen_consumption_rate = _part.RequestResource(_oxygenResourceName, oxygen_consumption_rate / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;

                var combined_consumption_rate = (_hydrogen_consumption_rate + _oxygen_consumption_rate) * TimeWarp.fixedDeltaTime / _hydrogen_peroxide_density;

                _hydrogen_peroxide_production_rate = -_part.RequestResource(_hydrogenPeroxideResourceName, -combined_consumption_rate) / TimeWarp.fixedDeltaTime * _hydrogen_peroxide_density;
            }
            else
            {
                _hydrogen_consumption_rate = 0;
                _oxygen_consumption_rate = 0;
                _hydrogen_peroxide_production_rate = 0;
            }


			updateStatusMessage();
        }

        public void UpdateGUI()
        {
            if (_bold_label == null)
                 _bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Overal Consumption", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(((_fixedConsumptionRate / TimeWarp.fixedDeltaTime * GameConstants.HOUR_SECONDS).ToString("0.0000")) + " mT/hour", GUILayout.Width(valueWidth));
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
            GUILayout.Label("Oxygen Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableOxygenMass.ToString("0.0000") + " mT / " + _maxCapacityOxygenMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_oxygen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
			GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrogenPeroxideMass.ToString("0.0000") + " mT / " + _maxCapacityPeroxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_peroxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_hydrogen_peroxide_production_rate > 0)
                _status = "Electrolysing";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
