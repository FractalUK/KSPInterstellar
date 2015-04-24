extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    class AnthraquinoneProcessor : IRefineryActivity
    {
        protected Part _part;
        //protected Vessel _vessel;
        protected String _status = "";
        protected double _current_rate;
        //protected double _water_density;
		protected double _hydrogen_density;
		protected double _oxygen_density;
        protected double _hydrogen_peroxide_density;
        
        //protected double _water_consumption_rate;
		protected double _hydrogen_consumption_rate;
		protected double _oxygen_consumption_rate;

        protected double _hydrogen_peroxide_production_rate;
        protected double _current_power;


		protected double _hydrogenMollFractionInHydrogenPeroxide = (1.0079 * 2)/ 34.01468;

        private GUIStyle _bold_label;

        public String ActivityName { get { return "Anthraquinone Process"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Water).Any(rs => rs.amount > 0); } }

        public double PowerRequirements { get { return PluginHelper.BaseAnthraquiononePowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public AnthraquinoneProcessor(Part part) 
        {
            _part = part;
            //_vessel = part.vessel;
            //_water_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Water).density;
			_hydrogen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen).density;
			_oxygen_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Oxygen).density;
            _hydrogen_peroxide_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide).density;
            
        }

	    public void UpdateFrame(double rateMultiplier)
	    {
		    _current_power = PowerRequirements * rateMultiplier;
		    _current_rate = CurrentPower/PluginHelper.AnthraquinoneEnergyPerTon;
		    //_water_consumption_rate = _part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Water, _current_rate * TimeWarp.fixedDeltaTime / _water_density) / TimeWarp.fixedDeltaTime * _water_density;

		    _hydrogen_consumption_rate =
				_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, _hydrogenMollFractionInHydrogenPeroxide * _current_rate * TimeWarp.fixedDeltaTime / _hydrogen_density) / TimeWarp.fixedDeltaTime * _hydrogen_density;

		    _oxygen_consumption_rate =
				_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen, (1 - _hydrogenMollFractionInHydrogenPeroxide) * _current_rate * TimeWarp.fixedDeltaTime / _oxygen_density) / TimeWarp.fixedDeltaTime * _oxygen_density;

			if (Math.Abs(_hydrogen_consumption_rate) < 0.001 || Math.Abs(_oxygen_consumption_rate) < 0.001)
		    {
			    _hydrogen_peroxide_production_rate = 0;

				if (_hydrogen_consumption_rate > 0)
					_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen,
					-_hydrogen_consumption_rate * _current_rate * TimeWarp.fixedDeltaTime / _hydrogen_density);

				if (_oxygen_consumption_rate > 0)
					_part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Hydrogen,
						-_oxygen_consumption_rate * _current_rate * TimeWarp.fixedDeltaTime / _oxygen_density);
		    }
		    else
		    {
		    	_hydrogen_peroxide_production_rate = -_part.RequestResource(InterstellarResourcesConfiguration.Instance.HydrogenPeroxide,
				    -(_hydrogen_consumption_rate + _oxygen_consumption_rate) * TimeWarp.fixedDeltaTime/_hydrogen_peroxide_density) *
			      _hydrogen_peroxide_density/TimeWarp.fixedDeltaTime;
			}

			updateStatusMessage();
        }

        public void UpdateGUI()
        {
            if (_bold_label == null)
                 _bold_label = new GUIStyle(GUI.skin.label) {fontStyle = FontStyle.Bold};
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power", _bold_label, GUILayout.Width(150));
            GUILayout.Label(PluginHelper.getFormattedPowerString(CurrentPower) + "/" + PluginHelper.getFormattedPowerString(PowerRequirements), GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Consumption Rate", _bold_label, GUILayout.Width(150));
			GUILayout.Label((_hydrogen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
            GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Oxygen Consumption Rate", _bold_label, GUILayout.Width(150));
			GUILayout.Label((_oxygen_consumption_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
			GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Hydrogen Peroxide Production Rate", _bold_label, GUILayout.Width(150));
            GUILayout.Label((_hydrogen_peroxide_production_rate * GameConstants.HOUR_SECONDS).ToString("0.000") + " mT/hour", GUILayout.Width(150));
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
