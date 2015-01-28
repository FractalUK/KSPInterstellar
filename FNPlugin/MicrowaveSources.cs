using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class MicrowaveSources : MonoBehaviour
    {
        class VesselComparator : IEqualityComparer<Vessel>
        {
            public int GetHashCode(Vessel foo) { return foo.id.GetHashCode().GetHashCode(); }
            public bool Equals(Vessel foo1, Vessel foo2) { return foo1.id == foo2.id; }
        }

        public Dictionary<Vessel, VesselMicrowavePersistence> transmitters = 
            new Dictionary<Vessel, VesselMicrowavePersistence>(new VesselComparator());
        
        public Dictionary<Vessel, VesselRelayPersistence> relays =
            new Dictionary<Vessel, VesselRelayPersistence>(new VesselComparator());

        public static MicrowaveSources instance
        {
            get;
            private set;
        }

        void Start()
        {
            instance = this;
            Debug.Log("[KSP Interstellar]: MicrowaveSources initialized");
        }

        uint unloaded_counter = 0;

        public void calculateTransmitters()
        {
            unloaded_counter++;

            foreach (var vessel in FlightGlobals.Vessels)
            {
                if (vessel.state == Vessel.State.DEAD)
                    continue;

                // if vessek is offloaded to rails, parse protovessel
                if (vessel.state == Vessel.State.INACTIVE)
                {
                    if (unloaded_counter % 30 != 1)                // sometimes rebuild unloaded vessels as transmitters and relays
                        continue;
                    // parse transmitter
                    var trans_pers = new VesselMicrowavePersistence(vessel);
                    trans_pers.setNuclearPower(MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel.protoVessel));
                    trans_pers.setSolarPower(MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel.protoVessel));
                    if (trans_pers.getAvailablePower() > 1.0)
                    {
                        transmitters[vessel] = trans_pers;
                    }
                    else
                        transmitters.Remove(vessel);
                    // parse relay
                    var persistence = new VesselRelayPersistence(vessel);
                    persistence.setActive(MicrowavePowerTransmitter.vesselIsRelay(vessel.protoVessel));
                    if (persistence.isActive())
                        relays[vessel] = persistence;
                    else
                        relays.Remove(vessel);
                    continue;
                }

                // if vessel is loaded
                var transes = vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>();
                if (transes.Count > 0)
                {
                    var persistence = new VesselMicrowavePersistence(vessel);
                    persistence.setNuclearPower(MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel));
                    persistence.setSolarPower(MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel));
                    if (persistence.getAvailablePower() > 0.1)
                    {
                        transmitters[vessel] = persistence;
                    }
                    else
                        transmitters.Remove(vessel);
                }

                if (MicrowavePowerTransmitter.vesselIsRelay(vessel))
                {
                    var persistence = new VesselRelayPersistence(vessel);
                    persistence.setActive(true);
                    relays[vessel] = persistence;
                }
                else
                    relays.Remove(vessel);
            }
        }

        uint counter = 0;
        void Update()                  // update every 40 frames
        {
            if (counter++ % 30 == 0 && HighLogic.LoadedSceneIsGame)
                calculateTransmitters();
        }
    }
}
