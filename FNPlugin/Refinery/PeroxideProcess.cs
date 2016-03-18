using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class PeroxideProcess : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;
        protected double _current_rate;
        protected double _fixedConsumptionRate;
        protected double _consumptionStorageRatio;

        protected string _ammonia_resource_name;
        protected string _hydrazine_resource_name;
        protected string _hydrogen_peroxide_name;
        protected string _water_resource_name;

        protected double _ammonia_density;
        protected double _water_density;
        protected double _hydrogen_peroxide_density;
        protected double _hydrazine_density;

        protected double _ammonia_consumption_rate;
        protected double _hydrogen_peroxide_consumption_rate;
        protected double _water_production_rate;
        protected double _hydrazine_production_rate;

        protected double _maxCapacityAmmoniaMass;
        protected double _maxCapacityHydrogenPeroxideMass;
        protected double _maxCapacityHydrazineMass;
        protected double _maxCapacityWaterMass;

        protected double _availableAmmoniaMass;
        protected double _availableHydrogenPeroxideMass;
        protected double _spareRoomHydrazineMass;
        protected double _spareRoomWaterMass;

        private double ammona_mass_consumption_ratio = (2 * 35.04) / (2 * 35.04 + 34.0147);
        private double hydrogen_peroxide_mass_consumption_ratio = 34.0147 / (2 * 35.04 + 34.0147);
        private double hydrazine_mass_production_ratio = 32.04516 / (32.04516 + 2 * 18.01528);
        private double water_mass_production_ratio = (2 * 18.01528) / (32.04516 + 2 * 18.01528);

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Peroxide Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements 
        { 
            get 
            {
                return _part.GetConnectedResources(_hydrogen_peroxide_name).Any(rs => rs.amount > 0)
                && _part.GetConnectedResources(_ammonia_resource_name).Any(rs => rs.amount > 0); 
            } 
        }

        public double PowerRequirements { get { return PluginHelper.BasePechineyUgineKuhlmannPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public PeroxideProcess(Part part) 
        {
            _part = part;
            _vessel = part.vessel;

            _ammonia_resource_name = InterstellarResourcesConfiguration.Instance.Ammonia;
            _hydrazine_resource_name = InterstellarResourcesConfiguration.Instance.Hydrazine;
            _water_resource_name = InterstellarResourcesConfiguration.Instance.Water;
            _hydrogen_peroxide_name = InterstellarResourcesConfiguration.Instance.HydrogenPeroxide;

            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(_ammonia_resource_name).density;
            _water_density = PartResourceLibrary.Instance.GetDefinition(_water_resource_name).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(_hydrogen_peroxide_name).density;
            _hydrazine_density = PartResourceLibrary.Instance.GetDefinition(_hydrazine_resource_name).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / PluginHelper.PechineyUgineKuhlmannEnergyPerTon;

            // determine how much resource we have
            var partsThatContainAmmonia = _part.GetConnectedResources(_ammonia_resource_name);
            var partsThatContainHydrogenPeroxide = _part.GetConnectedResources(_hydrogen_peroxide_name);
            var partsThatContainHydrazine = _part.GetConnectedResources(_hydrazine_resource_name);
            var partsThatContainWater = _part.GetConnectedResources(_water_resource_name);

            _maxCapacityAmmoniaMass = partsThatContainAmmonia.Sum(p => p.maxAmount) * _ammonia_density;
            _maxCapacityHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrogen_peroxide_density;
            _maxCapacityHydrazineMass = partsThatContainHydrogenPeroxide.Sum(p => p.maxAmount) * _hydrazine_density;
            _maxCapacityWaterMass = partsThatContainWater.Sum(p => p.maxAmount) * _water_density;

            _availableAmmoniaMass = partsThatContainAmmonia.Sum(r => r.amount) * _ammonia_density;
            _availableHydrogenPeroxideMass = partsThatContainHydrogenPeroxide.Sum(r => r.amount) * _hydrogen_peroxide_density;

            _spareRoomHydrazineMass = partsThatContainHydrazine.Sum(r => r.maxAmount - r.amount) * _hydrazine_density;
            _spareRoomWaterMass = partsThatContainWater.Sum(r => r.maxAmount - r.amount) * _water_density;

            // determine how much we can consume
            var fixedMaxAmmoniaConsumptionRate = _current_rate * ammona_mass_consumption_ratio * TimeWarp.fixedDeltaTime;
            var ammoniaConsumptionRatio = fixedMaxAmmoniaConsumptionRate > 0 ? Math.Min(fixedMaxAmmoniaConsumptionRate, _availableAmmoniaMass) / fixedMaxAmmoniaConsumptionRate : 0;

            var fixedMaxHydrogenPeroxideConsumptionRate = _current_rate * hydrogen_peroxide_mass_consumption_ratio * TimeWarp.fixedDeltaTime;
            var hydrogenPeroxideConsumptionRatio = fixedMaxHydrogenPeroxideConsumptionRate > 0 ? Math.Min(fixedMaxHydrogenPeroxideConsumptionRate, _availableHydrogenPeroxideMass) / fixedMaxHydrogenPeroxideConsumptionRate : 0;

            _fixedConsumptionRate = _current_rate * TimeWarp.fixedDeltaTime * Math.Min(ammoniaConsumptionRatio, hydrogenPeroxideConsumptionRatio);

            if (_fixedConsumptionRate > 0 && (_spareRoomHydrazineMass > 0 || _spareRoomWaterMass > 0))
            {
                // calculate consumptionStorageRatio
                var fixedMaxHydrazineRate = _fixedConsumptionRate * hydrazine_mass_production_ratio;
                var fixedMaxWaterRate = _fixedConsumptionRate * water_mass_production_ratio;

                var fixedMaxPossibleydrazineRate = allowOverflow ? fixedMaxHydrazineRate : Math.Min(_spareRoomHydrazineMass, fixedMaxHydrazineRate);
                var fixedMaxPossibleWaterRate = allowOverflow ? fixedMaxWaterRate : Math.Min(_spareRoomWaterMass, fixedMaxWaterRate);

                _consumptionStorageRatio = Math.Min(fixedMaxPossibleydrazineRate / fixedMaxHydrazineRate, fixedMaxPossibleWaterRate / fixedMaxWaterRate);

                // now we do the real consumption
                var ammonia_request = _fixedConsumptionRate * ammona_mass_consumption_ratio * _consumptionStorageRatio / _ammonia_density;
                _ammonia_consumption_rate = _part.RequestResource(_ammonia_resource_name, ammonia_request) * _ammonia_density / TimeWarp.fixedDeltaTime;

                var hydrogen_peroxide_request = _fixedConsumptionRate * hydrogen_peroxide_mass_consumption_ratio * _consumptionStorageRatio / _hydrogen_peroxide_density;
                _hydrogen_peroxide_consumption_rate = _part.RequestResource(_hydrogen_peroxide_name, hydrogen_peroxide_request) * _hydrogen_peroxide_density / TimeWarp.fixedDeltaTime;

                var combined_consumption_rate = _ammonia_consumption_rate + _hydrogen_peroxide_consumption_rate;

                var fixed_hydrazine_production = combined_consumption_rate * hydrazine_mass_production_ratio * TimeWarp.fixedDeltaTime / _hydrazine_density;
                    _hydrazine_production_rate = -_part.RequestResource(_hydrazine_resource_name, -fixed_hydrazine_production) * _hydrazine_density / TimeWarp.fixedDeltaTime;

                var fixed_water_production = combined_consumption_rate * water_mass_production_ratio * TimeWarp.fixedDeltaTime / _water_density;
                    _water_production_rate = -_part.RequestResource(_water_resource_name, -fixed_water_production) * _water_density / TimeWarp.fixedDeltaTime;
            }
            else
            {
                _ammonia_consumption_rate = 0;
                _hydrogen_peroxide_consumption_rate = 0;
                _hydrazine_production_rate = 0;
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
            GUILayout.Label("Ammonia Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableAmmoniaMass.ToString("0.0000") + " mT / " + _maxCapacityAmmoniaMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammona Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_ammonia_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Available", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_availableHydrogenPeroxideMass.ToString("0.0000") + " mT / " + _maxCapacityHydrogenPeroxideMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrogen_peroxide_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
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
            GUILayout.Label("Hydrazine Storage", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_spareRoomHydrazineMass.ToString("0.0000") + " mT / " + _maxCapacityHydrazineMass.ToString("0.0000") + " mT", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrazine Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label((_hydrazine_production_rate * GameConstants.HOUR_SECONDS).ToString("0.0000") + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_water_production_rate > 0 && _hydrazine_production_rate > 0)
                _status = "Peroxide Process Ongoing";
            else if (_hydrazine_production_rate > 0)
                _status = "Ongoing: Insufficient Monopropellant Storage";
            else if (_water_production_rate > 0)
                _status = "Ongoing: Insufficient Water Storage";
            else if (CurrentPower <= 0.01*PowerRequirements)
                _status = "Insufficient Power";
            else
            {
                if (_ammonia_consumption_rate > 0 && _hydrogen_peroxide_consumption_rate > 0)
                    _status = "Insufficient Storage";
                else if (_ammonia_consumption_rate > 0)
                    _status = "Hydrogen Peroxide Deprived";
                else if (_hydrogen_peroxide_consumption_rate > 0)
                    _status = "Ammonia Deprived";
                else
                    _status = "Hydrogen Peroxide and Ammonia Deprived";
            }
        }
    }
}
