using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class HaberProcess : IRefineryActivity
    {
        const int labelWidth = 200;
        const int valueWidth = 200;

        protected Part _part;
        protected Vessel _vessel;
        protected String _status = "";
        protected double _current_power;

        protected double _hydrogen_density;
        protected double _nitrogen_density;
        protected double _ammonia_density;

        protected double _hydrogen_consumption_rate;
        protected double _ammonia_production_rate;
        protected double _nitrogen_consumption_rate;

        protected double _atmospheric_nitrogen_rate;

        protected double _current_rate;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Haber Process ISRU"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements 
        { 
            get { return HasAccessToHydrogen() && HasAccessToNitrogen(); } 
        }

        private bool HasAccessToHydrogen()
        {
            return  _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Hydrogen).Any(rs => rs.amount > 0);
        }

        private bool HasAccessToNitrogen()
        {
            return (FlightGlobals.getStaticPressure(_vessel.transform.position) / 100) * ORSAtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(_vessel.mainBody.flightGlobalsIndex, "Nitrogen") >= 0.01
                    || _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Nitrogen).Any(rs => rs.amount > 0);
        }

        public double PowerRequirements { get { return PluginHelper.BaseHaberProcessPowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public HaberProcess(Part part)
        {
            _part = part;
            _vessel = part.vessel;

            _hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
            _ammonia_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Ammonia).density;
            _nitrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Nitrogen).density;
        }

        public void UpdateFrame(double rateMultiplier, bool allowOverflow)
        {
            _current_power = PowerRequirements * rateMultiplier;

            _current_rate = CurrentPower / PluginHelper.HaberProcessEnergyPerTon;

            double ammoniaNitrogenFractionByMass = (1 - GameConstants.ammoniaHydrogenFractionByMass);

            double hydrogen_rate = _current_rate * GameConstants.ammoniaHydrogenFractionByMass;
            double nitrogen_rate = _current_rate * ammoniaNitrogenFractionByMass;

            _hydrogen_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, hydrogen_rate * TimeWarp.fixedDeltaTime / _hydrogen_density) * _hydrogen_density / TimeWarp.fixedDeltaTime;

            _atmospheric_nitrogen_rate = (FlightGlobals.getStaticPressure(_vessel.transform.position) /100) * ORSAtmosphericResourceHandler.getAtmosphericResourceContentByDisplayName(_vessel.mainBody.flightGlobalsIndex, "Nitrogen") * _current_rate * 10;
            if (_atmospheric_nitrogen_rate > nitrogen_rate)
                _nitrogen_consumption_rate = nitrogen_rate;
            else
                _nitrogen_consumption_rate = _part.RequestResource(InterstellarResourcesConfiguration.Instance.Nitrogen, nitrogen_rate * TimeWarp.fixedDeltaTime / _nitrogen_density) * _nitrogen_density / TimeWarp.fixedDeltaTime;

            if (_hydrogen_consumption_rate > 0 && _nitrogen_consumption_rate > 0)
                _ammonia_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.Ammonia, -_nitrogen_consumption_rate / ammoniaNitrogenFractionByMass * TimeWarp.fixedDeltaTime / _ammonia_density) * _ammonia_density / TimeWarp.fixedDeltaTime;
            
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
            GUILayout.Label("Current Rate ", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_current_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Avaialble Atmospheric Nitrogen ", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_atmospheric_nitrogen_rate.ToString(), GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Nitrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_nitrogen_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_hydrogen_consumption_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ammonia Production Rate", _bold_label, GUILayout.Width(labelWidth));
            GUILayout.Label(_ammonia_production_rate * GameConstants.HOUR_SECONDS + " mT/hour", GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void updateStatusMessage()
        {
            if (_ammonia_production_rate > 0) 
                _status = "Haber Process Ongoing";
            else if (CurrentPower <= 0.01 * PowerRequirements)
                _status = "Insufficient Power";
            else
                _status = "Insufficient Storage";
        }
    }
}
