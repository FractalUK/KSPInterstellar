using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ReactorFuelMode {
        protected int _reactor_type;
        protected string _mode_gui_name;
        protected List<ReactorFuel> _fuels;
        protected bool _aneutronic;
        protected double _normreactionrate;
        protected double _normpowerrequirements;

        public ReactorFuelMode(ConfigNode node) {
            _reactor_type = Convert.ToInt32(node.GetValue("ReactorType"));
            _mode_gui_name = node.GetValue("GUIName");
            _aneutronic = Boolean.Parse(node.GetValue("Aneutronic"));
            _normreactionrate = Double.Parse(node.GetValue("NormalisedReactionRate"));
            _normpowerrequirements = Double.Parse(node.GetValue("NormalisedPowerConsumption"));
            ConfigNode[] fuel_nodes = node.GetNodes("FUEL");
            _fuels = fuel_nodes.Select(nd => new ReactorFuel(nd)).ToList();
        }

        public int SupportedReactorTypes { get { return _reactor_type; } }

        public string ModeGUIName { get { return _mode_gui_name; } }

        public IList<ReactorFuel> ReactorFuels { get { return _fuels; } }

        public bool Aneutronic { get { return _aneutronic; } }

        public double NormalisedReactionRate { get { return _normreactionrate; } }

        public double NormalisedPowerRequirements { get { return _normpowerrequirements; } }

    }
}
