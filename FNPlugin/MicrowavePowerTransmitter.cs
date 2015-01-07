using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class MicrowavePowerTransmitter : FNResourceSuppliableModule {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool relay;

        //Persistent False
        [KSPField(isPersistant = false)]
        public string animName;

        //GUI 
        [KSPField(isPersistant = false, guiActive = true, guiName = "Transmitter")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Beamed Power")]
        public string beamedpower;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Transmission"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float transmitPower;

        //Internal
        protected Animation anim;
        protected double nuclear_power;
        protected double solar_power;
        protected long activeCount = 0;
        protected double displayed_solar_power = 0;
        protected List<FNGenerator> generators;
        protected List<MicrowavePowerReceiver> receivers;
        protected List<ModuleDeployableSolarPanel> panels;
        protected MicrowavePowerReceiver part_receiver;
        protected bool has_receiver = false;

        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = true)]
        public void ActivateTransmitter() {
            if (relay) { return; }
            if (anim != null) {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, 2f);
            }
            transmitPower = 100;
            activeCount = 8;
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter() {
            if (relay) { return; }
            if (anim != null) {
                anim[animName].speed = -1f;
                anim[animName].normalizedTime = 1f;
                anim.Blend(animName, 2f);
            }
            activeCount = 8;
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Relay", active = true)]
        public void ActivateRelay() {
            if (IsEnabled) { return; }
            if (anim != null) {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, 2f);
            }
            activeCount = 8;
            IsEnabled = true;
            relay = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Relay", active = true)]
        public void DeactivateRelay() {
            if (!relay) { return; }
            if (anim != null) {
                anim[animName].speed = 1f;
                anim[animName].normalizedTime = 0f;
                anim.Blend(animName, 2f);
            }
            activeCount = 8;
            IsEnabled = false;
            relay = false;
        }

        [KSPAction("Activate Transmitter")]
        public void ActivateTransmitterAction(KSPActionParam param) {
            ActivateTransmitter();
        }

        [KSPAction("Deactivate Transmitter")]
        public void DeactivateTransmitterAction(KSPActionParam param) {
            DeactivateTransmitter();
        }

        [KSPAction("Activate Relay")]
        public void ActivateRelayAction(KSPActionParam param) {
            ActivateRelay();
        }

        [KSPAction("Deactivate Relay")]
        public void DeactivateRelayAction(KSPActionParam param) {
            DeactivateRelay();
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

            generators = vessel.FindPartModulesImplementing<FNGenerator>();
            receivers = vessel.FindPartModulesImplementing<MicrowavePowerReceiver>();
            panels = vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>();
            if (part.FindModulesImplementing<MicrowavePowerReceiver>().Count == 1) {
                part_receiver = part.FindModulesImplementing<MicrowavePowerReceiver>().First();
                has_receiver = true;
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null) {
                anim[animName].layer = 1;
                if (!IsEnabled) {
                    anim[animName].normalizedTime = 1f;
                    anim[animName].speed = -1f;

                } else {
                    anim[animName].normalizedTime = 0f;
                    anim[animName].speed = 1f;

                }
                anim.Play();
            }

            this.part.force_activate();
        }

        public override void OnUpdate() {
            bool receiver_on = false;
            if (has_receiver) {
                if (part_receiver.isActive()) {
                    receiver_on = true;
                }
            }
            Events["ActivateTransmitter"].active = !IsEnabled && !relay && !receiver_on;
            Events["DeactivateTransmitter"].active = IsEnabled && !relay;
            Events["ActivateRelay"].active = !IsEnabled && !relay && !receiver_on;
            Events["DeactivateRelay"].active = IsEnabled && relay;
            Fields["beamedpower"].guiActive = IsEnabled && !relay;
            Fields["transmitPower"].guiActive = IsEnabled && !relay;

            if (IsEnabled) {
                if (relay) {
                    statusStr = "Relay Active";
                } else {
                    statusStr = "Transmitter Active";
                }
            } else {
                statusStr = "Inactive.";
            }

            double inputPower = nuclear_power + displayed_solar_power;
            if (inputPower > 1000) {
                if (inputPower > 1e6) {
                    beamedpower = (inputPower / 1e6).ToString("0.000") + " GW";
                } else {
                    beamedpower = (inputPower / 1000).ToString("0.000") + " MW";
                }
            } else {
                beamedpower = inputPower.ToString("0.000") + " KW";
            }
        }

        public override void OnFixedUpdate() {
            activeCount++;
            nuclear_power = 0;
            solar_power = 0;
            displayed_solar_power = 0;
            if (IsEnabled && !relay) 
            {
                foreach (FNGenerator generator in generators) {
                    if (generator.isActive()) {
                        IThermalSource thermal_source = generator.getThermalSource();
                        if (thermal_source != null && !thermal_source.IsVolatileSource) {
                            double output = generator.getMaxPowerOutput();

                            if (thermal_source is InterstellarFusionReactor) {
                                InterstellarFusionReactor fusion_reactor = thermal_source as InterstellarFusionReactor;
                                output = output * 0.92;
                            }
                            output = output * transmitPower / 100.0;
                            double gpower = consumeFNResource(output * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                            nuclear_power += gpower * 1000 / TimeWarp.fixedDeltaTime;
                        }
                    }
                }

                foreach (ModuleDeployableSolarPanel panel in panels) 
                {
                    double output = panel.flowRate;

                    // attempt to retrieve all solar power output
                    if (output == 0.0)
                    {
                        var solarpanels = panel.part.parent.FindModulesImplementing<ModuleDeployableSolarPanel>();
                        solarpanels.ForEach(s => output += s.flowRate);
                    }

                    double spower = part.RequestResource("ElectricCharge", output * TimeWarp.fixedDeltaTime);
                    double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
                    displayed_solar_power += spower / TimeWarp.fixedDeltaTime;
                    //scale solar power to what it would be in Kerbin orbit for file storage
                    solar_power += spower / TimeWarp.fixedDeltaTime/inv_square_mult;
                }
            }

            if (double.IsInfinity(nuclear_power) || double.IsNaN(nuclear_power)) {
                nuclear_power = 0;
            }

            if (double.IsInfinity(solar_power) || double.IsNaN(solar_power)) {
                solar_power = 0;
            } 

            if (activeCount % 1000 == 9) {
                ConfigNode config = PluginHelper.getPluginSaveFile();
                string vesselID = vessel.id.ToString();
                if (config.HasNode("VESSEL_MICROWAVE_POWER_" + vesselID)) {
                    ConfigNode power_node = config.GetNode("VESSEL_MICROWAVE_POWER_" + vesselID);
                    if (power_node.HasValue("nuclear_power")) {
                        power_node.SetValue("nuclear_power", MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel).ToString("E"));
                    } else {
                        power_node.AddValue("nuclear_power", MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel).ToString("E"));
                    }
                    if (power_node.HasValue("solar_power")) {
                        power_node.SetValue("solar_power", MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel).ToString("E"));
                    } else {
                        power_node.AddValue("solar_power", MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel).ToString("E"));
                    }

                } else {
                    ConfigNode power_node = config.AddNode("VESSEL_MICROWAVE_POWER_" + vesselID);
                    power_node.AddValue("nuclear_power", MicrowavePowerTransmitter.getEnumeratedNuclearPowerForVessel(vessel).ToString("E"));
                    power_node.AddValue("solar_power", MicrowavePowerTransmitter.getEnumeratedSolarPowerForVessel(vessel).ToString("E"));
                }

                if (config.HasNode("VESSEL_MICROWAVE_RELAY_" + vesselID)) {
                    ConfigNode relay_node = config.GetNode("VESSEL_MICROWAVE_RELAY_" + vesselID);
                    if (relay_node.HasValue("relay")) {
                        relay_node.SetValue("relay", MicrowavePowerTransmitter.vesselIsRelay(vessel).ToString());
                    } else {
                        relay_node.AddValue("relay", MicrowavePowerTransmitter.vesselIsRelay(vessel).ToString());
                    }
                } else {
                    ConfigNode relay_node = config.AddNode("VESSEL_MICROWAVE_RELAY_" + vesselID);
                    relay_node.AddValue("relay", MicrowavePowerTransmitter.vesselIsRelay(vessel).ToString());
                }

                config.Save(PluginHelper.getPluginSaveFilePath());
            }
            activeCount++;
        }

        public double getNuclearPower() {
            return nuclear_power;
        }

        public double getSolarPower() {
            return solar_power;
        }

        public bool getIsRelay() {
            return relay;
        }

        public bool isActive() {
            return IsEnabled;
        }

        public override string getResourceManagerDisplayName() {
            return "Microwave Transmitter";
        }

        public static double getEnumeratedNuclearPowerForVessel(Vessel vess) {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            double total_nuclear_power = 0;
            foreach (MicrowavePowerTransmitter transmitter in transmitters) {
                total_nuclear_power += transmitter.getNuclearPower();
            }
            return total_nuclear_power;
        }

        public static double getEnumeratedSolarPowerForVessel(Vessel vess) {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            double total_solar_power = 0;
            foreach (MicrowavePowerTransmitter transmitter in transmitters) {
                total_solar_power += transmitter.getSolarPower();
            }
            return total_solar_power;
        }

        public static bool vesselIsRelay(Vessel vess) {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            foreach (MicrowavePowerTransmitter transmitter in transmitters) {
                if (transmitter.getIsRelay()) {
                    return true;
                }
            }
            return false;
        }

    }
}
