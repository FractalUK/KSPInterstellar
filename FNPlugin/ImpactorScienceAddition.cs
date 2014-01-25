using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ImpactorScienceAddition : MonoBehaviour {
        protected bool callback_added = false;
        protected Part cur_part;
        protected string name;

        public void Update() {

            if (FlightGlobals.fetch.activeVessel != null) {
                if (FlightGlobals.fetch.activeVessel.vesselName != name) {
                    callback_added = false;
                }
            }
            if (!callback_added) {
                if (FlightGlobals.fetch.activeVessel != null) {
                    cur_part = FlightGlobals.fetch.activeVessel.rootPart;
                    name = FlightGlobals.fetch.activeVessel.vesselName;
                    if (FlightGlobals.fetch.activeVessel.rootPart.FindModulesImplementing<FNImpactorModule>().Count == 0) {
                        print("[KSP Interstellar] Setting Up Impactor Callback On " + FlightGlobals.fetch.activeVessel.rootPart.name + " " + name);
                        ConfigNode config = new ConfigNode();
                        config.AddValue("name", "FNImpactorModule");
                        FlightGlobals.fetch.activeVessel.rootPart.AddModule(config);
                        callback_added = true;
                    } else {
                        callback_added = true;
                    }
                }
            }
        }


    }
}
