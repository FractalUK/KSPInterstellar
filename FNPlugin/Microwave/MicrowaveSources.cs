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
        public Dictionary<Vessel, VesselRelayPersistence> relays = new Dictionary<Vessel, VesselRelayPersistence>();

        public static MicrowaveSources instance
        {
            get;
            private set;
        }

        void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            instance = this;
            Debug.Log("[KSP Interstellar]: MicrowaveSources initialized");
        }

        uint unloaded_counter = 0;

        public void calculateTransmitters()
        {
            unloaded_counter++;
            foreach (var vessel in FlightGlobals.Vessels)
            {
                // if vessek is offloaded to rails, parse file system
                if (vessel.state == Vessel.State.INACTIVE)
                {
                    if (unloaded_counter % 100 != 1)                // sometimes rebuild unloaded vessels as transmitters and relays
                        continue;
                    // parse transmitter
                    var trans_pers = new VesselMicrowavePersistence(vessel);
                    trans_pers.setNuclearPower(MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel.protoVessel));
                    trans_pers.setSolarPower(MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel.protoVessel));

                    if (trans_pers.getAvailablePower() > 1.0)
                        transmitters[vessel] = trans_pers;
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

                // if vessel is dead
                if (vessel.state == Vessel.State.DEAD)
                {
                    transmitters.Remove(vessel);
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

                    if (persistence.getAvailablePower() > 1.0)
                        transmitters[vessel] = persistence;
                    else
                        transmitters.Remove(vessel);
                }

                if (MicrowavePowerTransmitter.vesselIsRelay(vessel))
                {
                    var persistence = new VesselRelayPersistence(vessel);
                    persistence.setActive(true);
                    if (persistence.isActive())
                        relays[vessel] = persistence;
                    else
                        relays.Remove(vessel);
                }
            }
        }

        uint counter = 0;
        void Update()                  // update every 40 frames
        {
            if (counter++ % 40 == 0 && HighLogic.LoadedSceneIsGame)
                calculateTransmitters();
        }
    }
}
