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
        public Dictionary<Vessel, VesselMicrowavePersistence> transmitters = new Dictionary<Vessel, VesselMicrowavePersistence>();
        public List<VesselRelayPersistence> relays = new List<VesselRelayPersistence>();

        public static MicrowaveSources instance
        {
            get;
            private set;
        }

        void Start()
        {
            instance = this;
            Debug.Log("[KSP Interstellar]: MicrowaveSources.Start called");
        }

        public void calculateTransmitters()
        {
            Debug.Log("[KSP Interstellar]: MicrowaveSources.calculateTransmitters called");
            transmitters.Clear();
            relays.Clear();
            foreach (var vessel in FlightGlobals.Vessels)
            {
                var transes = vessel.FindPartModulesImplementing<MicrowavePowerTransmitter>();
                if (transes.Count > 0)
                {
                    var persistence = new VesselMicrowavePersistence(vessel);
                    persistence.setNuclearPower( MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel));
                    persistence.setSolarPower(MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel));
                    transmitters[vessel] = persistence;
                    if (MicrowavePowerTransmitter.vesselIsRelay(vessel))
                    {
                        var persistence_relay = new VesselRelayPersistence(vessel);
                        persistence_relay.setActive(true);
                        relays.Add(persistence_relay);
                    }
                }
                Debug.Log("[KSP Interstellar]: MicrowaveSources.calculateTransmitters called -" + transmitters.ToString());
            }            
        }

        uint counter = 0;
        void Update()                  // update every 40 frames
        {
            if (counter++ % 40 == 0)
                calculateTransmitters();
        }
    }
}
