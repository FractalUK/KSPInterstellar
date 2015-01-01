using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class UF4Ammonolysiser : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;

        protected double _ammonia_density;
        protected double _uranium_tetraflouride_density;
        protected double _uranium_nitride_density;

        protected double _ammonia_consumption_rate;
        protected double _uranium_tetraflouride_consumption_rate;
        protected double _uranium_nitride_production_rate;
        protected double _current_rate;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Uranium Tetraflouride Ammonolysis"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).Any(rs => rs.amount > 0) && _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Ammonia).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return GameConstants.baseUraniumAmmonolysisConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public UF4Ammonolysiser(Part part) 
        {
            _part = part;
            _vessel = part.vessel;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _uranium_tetraflouride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride).density;
            _uranium_nitride_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.UraniumNitride).density;
        }

        public void UpdateFrame(double rate_multiplier)
        {
            _current_power = PowerRequirements * rate_multiplier;
            _current_rate = CurrentPower / GameConstants.baseUraniumAmmonolysisRate;
            double uf4persec = _current_rate * 1.24597 / _uranium_tetraflouride_density;
            double ammoniapersec = _current_rate * 0.901 / _ammonia_density;
            _uranium_tetraflouride_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.UraniumTetraflouride, uf4persec * TimeWarp.fixedDeltaTime)/_uranium_tetraflouride_density/TimeWarp.fixedDeltaTime;
            _ammonia_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, ammoniapersec * TimeWarp.fixedDeltaTime) / _ammonia_density / TimeWarp.fixedDeltaTime;

            if(_ammonia_consumption_rate > 0 && _uranium_tetraflouride_consumption_rate > 0) 
                _uranium_nitride_production_rate = -_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.UraniumNitride, -_uranium_tetraflouride_consumption_rate / 1.24597 / _uranium_nitride_density*TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime * _uranium_nitride_density;

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
            GUILayout.Label("Uranium Tetraflouride Consumption Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_uranium_tetraflouride_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Uranium Nitride Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label(_uranium_nitride_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_uranium_nitride_production_rate > 0)
            {
                _status = "Uranium Tetraflouride Ammonolysis Process Ongoing";
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = "Insufficient Power";
            } else
            {
                if (_ammonia_consumption_rate > 0 && _uranium_tetraflouride_consumption_rate > 0)
                {
                    _status = "Insufficient Storage";
                } else if (_ammonia_consumption_rate > 0)
                {
                    _status = "Uranium Tetraflouride Deprived";
                } else if (_uranium_tetraflouride_consumption_rate > 0)
                {
                    _status = "Ammonia Deprived";
                } else
                {
                    _status = "UF4 and Ammonia Deprived";
                }
            }
        }
    }
}
