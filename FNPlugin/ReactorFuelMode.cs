using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ReactorFuelMode {
        protected int reactor_type;
        protected string mode_gui_name;
        protected List<ReactorFuel> fuels;

        public ReactorFuelMode(ConfigNode node) {
            reactor_type = Convert.ToInt32(node.GetValue("ReactorType"));
            mode_gui_name = node.GetValue("GUIName");
            ConfigNode[] fuel_nodes = node.GetNodes("FUEL");
            fuels = fuel_nodes.Select(nd => new ReactorFuel(nd)).ToList();
        }

        public int SupportedReactorTypes { get { return reactor_type; } }

        public string ModeGUIName { get { return mode_gui_name; } }

        public List<ReactorFuel> ReactorFuels { get { return fuels; } }

    }
}
