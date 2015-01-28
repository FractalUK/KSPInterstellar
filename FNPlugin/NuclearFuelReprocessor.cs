extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class NuclearFuelReprocessor : IRefineryActivity
    {
        protected Part _part;
        protected Vessel _vessel;
        protected double _current_rate = 0;
        protected double _remaining_to_reprocess = 0;
        protected double _remaining_seconds = 0;
        
        protected String _status = "";
        protected double _current_power;
        private GUIStyle _bold_label;

        public String ActivityName { get { return "Nuclear Fuel Reprocessing"; } }

        public double CurrentPower { get { return _current_power; } }

        public bool HasActivityRequirements { get { return _part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Actinides).Any(rs => rs.amount < rs.maxAmount); } }

        public double PowerRequirements { get { return PluginHelper.BasePowerConsumption; } }

        public String Status { get { return String.Copy(_status); } }

        public NuclearFuelReprocessor(Part part) 
        {
            this._part = part;
            _vessel = part.vessel;
        }

        public void UpdateFrame(double rate_multiplier) 
        {
            _current_power = PowerRequirements * rate_multiplier;
            List<INuclearFuelReprocessable> nuclear_reactors = _vessel.FindPartModulesImplementing<INuclearFuelReprocessable>();
            double remaining_capacity_to_reprocess = GameConstants.baseReprocessingRate * TimeWarp.fixedDeltaTime / GameConstants.EARH_DAY_SECONDS * rate_multiplier;
            double enum_actinides_change = 0;
            foreach (INuclearFuelReprocessable nuclear_reactor in nuclear_reactors)
            {
                double actinides_change = nuclear_reactor.ReprocessFuel(remaining_capacity_to_reprocess);
                enum_actinides_change += actinides_change;
                remaining_capacity_to_reprocess = Math.Max(0, remaining_capacity_to_reprocess-actinides_change);
            }
            _remaining_to_reprocess = nuclear_reactors.Sum(nfr => nfr.WasteToReprocess);
            _current_rate = enum_actinides_change;
            _remaining_seconds = _remaining_to_reprocess / _current_rate/ TimeWarp.fixedDeltaTime;
            _status = _current_rate > 0 ? "Online" : _remaining_to_reprocess > 0 ? "Power Deprived" : "No Fuel To Reprocess";
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
            if (_remaining_seconds > 0 && !double.IsNaN(_remaining_seconds) && !double.IsInfinity(_remaining_seconds))
            {
                int hrs = (int) (_remaining_seconds / 3600);
                int mins = (int) ((_remaining_seconds - hrs*3600)/60);
                int secs = (hrs * 60 + mins) % ((int)(_remaining_seconds / 60));
                GUILayout.Label("Time Remaining", _bold_label, GUILayout.Width(150));
                GUILayout.Label(hrs + " hours " + mins + " minutes " + secs + " seconds", GUILayout.Width(150));
            }
            GUILayout.EndHorizontal();
        }

        public double getActinidesRemovedPerHour() 
        {
            return _current_rate / TimeWarp.fixedDeltaTime * 3600.0;
        }

        public double getRemainingAmountToReprocess() 
        {
            return _remaining_to_reprocess;
        }
    }
}
