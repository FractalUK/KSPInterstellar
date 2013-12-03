using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class MicrowavePowerReceiver : FNResourceSuppliableModule, FNThermalSource {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool receiverIsEnabled;

        //Persistent False
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string animTName;
        [KSPField(isPersistant = false)]
        public float collectorArea = 1;
        [KSPField(isPersistant = false)]
        public bool isThermalReceiver;
        [KSPField(isPersistant = false)]
        public bool isInlineReceiver;
        [KSPField(isPersistant = false)]
        public float ThermalTemp;
        [KSPField(isPersistant = false)]
        public float ThermalPower;
        [KSPField(isPersistant = false)]
        public float radius;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Input Power")]
        public string beamedpower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Satellites Connected")]
        public string connectedsats;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Relay Connected")]
        public string connectedrelays;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Total Efficiency")]
        public string toteff;

        //Internal 

        protected Dictionary<Vessel, double> received_power = new Dictionary<Vessel, double>();

        //
        protected Animation anim;
        protected Animation animT;
        protected bool play_down = true;
        protected bool play_up = true;
        protected int connectedsatsi = 0;
        protected int connectedrelaysi = 0;
        protected double efficiency_d = 0;
        protected double powerInput = 0;
        protected long deactivate_timer = 0;
        protected List<VesselMicrowavePersistence> vmps;
        protected List<VesselRelayPersistence> vrps;
        protected MicrowavePowerTransmitter part_transmitter;
        protected bool has_transmitter = false;

        [KSPEvent(guiActive = true, guiName = "Activate Receiver", active = true)]
        public void ActivateReceiver() {
            receiverIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Receiver", active = true)]
        public void DisableReceiver() {
            receiverIsEnabled = false;
        }

        [KSPAction("Activate Receiver")]
        public void ActivateReceiverAction(KSPActionParam param) {
            ActivateReceiver();
        }

        [KSPAction("Disable Receiver")]
        public void DisableReceiverAction(KSPActionParam param) {
            DisableReceiver();
        }

        [KSPAction("Toggle Receiver")]
        public void ToggleReceiverAction(KSPActionParam param) {
            receiverIsEnabled = !receiverIsEnabled;
        }

        public override void OnStart(PartModule.StartState state) {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_THERMALPOWER };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);
            if (state == StartState.Editor) { return; }

            if (part.FindModulesImplementing<MicrowavePowerTransmitter>().Count == 1) {
                part_transmitter = part.FindModulesImplementing<MicrowavePowerTransmitter>().First();
                has_transmitter = true;
            }
            
            if (animTName != null) {
                animT = part.FindModelAnimators(animTName).FirstOrDefault();
                if (animT != null) {
                    animT[animTName].layer = 1;
                    animT[animTName].normalizedTime = 0f;
                    animT[animTName].speed = 0.001f;
                    animT.Play();
                }
            }

            if (animName != null) {
                anim = part.FindModelAnimators(animName).FirstOrDefault();
                if (anim != null) {
                    anim[animName].layer = 1;
                    if (connectedsatsi > 0 || connectedrelaysi > 0) {
                        anim[animName].normalizedTime = 1f;
                        anim[animName].speed = -1f;

                    } else {
                        anim[animName].normalizedTime = 0f;
                        anim[animName].speed = 1f;

                    }
                    anim.Play();
                }
            }
            vmps = new List<VesselMicrowavePersistence>();
            vrps = new List<VesselRelayPersistence>();
            foreach (Vessel vess in FlightGlobals.Vessels) {
                String vesselID = vess.id.ToString();
                
                if (vess.isActiveVessel == false && vess.vesselName.ToLower().IndexOf("debris") == -1) {
                    ConfigNode config = PluginHelper.getPluginSaveFile();
                    if (config.HasNode("VESSEL_MICROWAVE_POWER_" + vesselID)) {
                        ConfigNode power_node = config.GetNode("VESSEL_MICROWAVE_POWER_" + vesselID);
                        double nuclear_power = 0;
                        double solar_power = 0;
                        if (power_node.HasValue("nuclear_power")) {
                            nuclear_power = double.Parse(power_node.GetValue("nuclear_power"));
                            
                        }
                        if (power_node.HasValue("solar_power")) {
                            solar_power = double.Parse(power_node.GetValue("solar_power"));
                        }
                        if (nuclear_power > 0 || solar_power > 0) {
                            VesselMicrowavePersistence vmp = new VesselMicrowavePersistence(vess);
                            vmp.setSolarPower(solar_power);
                            vmp.setNuclearPower(nuclear_power);
                            vmps.Add(vmp);
                        }
                    }

                    if (config.HasNode("VESSEL_MICROWAVE_RELAY_" + vesselID)) {
                        ConfigNode relay_node = config.GetNode("VESSEL_MICROWAVE_RELAY_" + vesselID);
                        if (relay_node.HasValue("relay")) {
                            bool relay = bool.Parse(relay_node.GetValue("relay"));
                            if (relay) {
                                VesselRelayPersistence vrp = new VesselRelayPersistence(vess);
                                vrp.setActive(relay);
                                vrps.Add(vrp);
                            }
                        }
                    }
                }
            }

            this.part.force_activate();
        }

        public override void OnUpdate() {
            bool transmitter_on = false;
            if (has_transmitter) {
                if (part_transmitter.isActive()) {
                    transmitter_on = true;
                }
            }
            Events["ActivateReceiver"].active = !receiverIsEnabled && !transmitter_on;
            Events["DisableReceiver"].active = receiverIsEnabled;
            Fields["toteff"].guiActive = (connectedsatsi > 0 || connectedrelaysi > 0);

            if (receiverIsEnabled) {
                if (powerInput > 1000) {
                    beamedpower = (powerInput / 1000).ToString("0.00") + "MW";
                } else {
                    beamedpower = powerInput.ToString("0.00") + "KW";
                }
            } else {
                beamedpower = "Offline.";
            }
            connectedsats = connectedsatsi.ToString();
            connectedrelays = connectedrelaysi.ToString();
            toteff = (efficiency_d*100).ToString("0.00") + "%";

            if (anim != null) {
                if (connectedsatsi > 0 || connectedrelaysi > 0) {
                    if (play_up) {
                        play_down = true;
                        play_up = false;
                        anim[animName].speed = 1f;
                        anim[animName].normalizedTime = 0f;
                        anim.Blend(animName, 2f);
                    }
                } else {
                    if (play_down) {
                        play_down = false;
                        play_up = true;
                        anim[animName].speed = -1f;
                        anim[animName].normalizedTime = 1f;
                        anim.Blend(animName, 2f);
                    }
                }
            }
        }

        public override void OnFixedUpdate() {
            
            int activeSatsIncr = 0;
            int activeRelsIncr = 0;
            double total_power = 0;
            connectedsatsi = 0;
            connectedrelaysi = 0;
            
            base.OnFixedUpdate();
            if (receiverIsEnabled) {
                if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && !isThermalReceiver) {
                    receiverIsEnabled = false;
                    deactivate_timer++;
                    if (FlightGlobals.ActiveVessel == vessel && deactivate_timer > 2) {
                        ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency microwave power shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    return;
                }
                double atmosphericefficiency = Math.Exp(-FlightGlobals.getStaticPressure(vessel.transform.position) / 5);
                efficiency_d = GameConstants.microwave_dish_efficiency * atmosphericefficiency;
                deactivate_timer = 0;
                foreach (VesselMicrowavePersistence vmp in vmps) {
                    if (vmp.getAvailablePower() > 0) { // if satellite has no power, don't waste your time
                        if (received_power.ContainsKey(vmp.getVessel())) {
                            received_power[vmp.getVessel()] = 0;
                        } else {
                            received_power.Add(vmp.getVessel(), 0);
                        }
                        // calculate maximum power receivable from satellite
                        double sat_power_cap = vmp.getAvailablePower() * efficiency_d;
                        double current_power_from_sat = MicrowavePowerReceiver.getEnumeratedPowerFromSatelliteForAllVesssels(vmp);
                        double power_available_from_sat = (sat_power_cap - current_power_from_sat);
                        // line of sight stuff
                        if (lineOfSightTo(vmp.getVessel())) { // we can see the satellite
                            double sat_power = Math.Min(getSatPower(vmp), power_available_from_sat); // get sat power and make sure we conserve enegy
                            received_power[vmp.getVessel()] = sat_power * atmosphericefficiency;
                            total_power += sat_power;
                            activeSatsIncr++;
                        } else {
                            if (vmp.getVessel() != vessel) { // don't relay power back to ourself
                                foreach (VesselRelayPersistence vrp in vrps) {
                                    if (lineOfSightTo(vrp.getVessel()) && vrp.isActive()) { // we can see relay and the relay is active
                                        if (vrp.lineOfSightTo(vmp.getVessel())) { // relay can see satellite
                                            double sat_power = Math.Min(getSatPower(vmp, vrp), power_available_from_sat); // get sat power and make sure we conserve enegy
                                            received_power[vmp.getVessel()] = sat_power * atmosphericefficiency;
                                            total_power += sat_power;
                                            activeRelsIncr++;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                connectedsatsi = activeSatsIncr;
                connectedrelaysi = activeRelsIncr;

                double powerInputMegajoules = total_power / 1000.0 * GameConstants.microwave_dish_efficiency * atmosphericefficiency;
                powerInput = powerInputMegajoules * 1000.0f;


                float animateTemp = (float)powerInputMegajoules / 3000;
                if (animateTemp > 1) {
                    animateTemp = 1;
                }

                if (animT != null) {
                    animT[animTName].speed = 0.001f;
                    animT[animTName].normalizedTime = animateTemp;
                    animT.Blend(animTName, 2f);
                }

                if (!isThermalReceiver) {
                    supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    double waste_head_production = powerInputMegajoules / GameConstants.microwave_dish_efficiency * (1.0f - GameConstants.microwave_dish_efficiency);
                    supplyFNResource(waste_head_production * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
                } else {
                    double cur_thermal_power = supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
                    if (ThermalPower <= 0) {
                        ThermalPower = (float)(cur_thermal_power);
                    } else {
                        ThermalPower = (float)(cur_thermal_power * GameConstants.microwave_alpha + (1.0f - GameConstants.microwave_alpha) * ThermalPower);
                    }
                }
            } else {
                received_power.Clear();
            }
        }

        public float getMegajoules() {
            return (float) (powerInput / 1000);
        }

        public float getCoreTemp() {
            return 1500.0f;
        }

        public float getThermalPower() {
            return ThermalPower;
        }

        public bool getIsNuclear() {
            return false;
        }

        public float getRadius() {
            return radius;
        }

        public bool isActive() {
            return receiverIsEnabled;
        }

        public bool shouldScaleDownJetISP() {
            return false;
        }

        public void enableIfPossible() {
            if (!receiverIsEnabled) {
                receiverIsEnabled = true;
            }
        }

        public override string GetInfo() {
            return "Collector Area: " + collectorArea + " m^2";
        }

        public double getPowerFromSatellite(VesselMicrowavePersistence vmp) {
            if (received_power.ContainsKey(vmp.getVessel()) && receiverIsEnabled) {
                return received_power[vmp.getVessel()];
            }
            return 0;
        }

        protected bool lineOfSightTo(Vessel vess) {
            Vector3d a = vessel.transform.position;
            Vector3d b = vess.transform.position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
                Vector3d refminusa = referenceBody.position - a;
                Vector3d bminusa = b - a;
                if (Vector3d.Dot(refminusa, bminusa) > 0) {
                    if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude) {
                        Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                        if (tang.magnitude < referenceBody.Radius) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        protected double getSatPower(VesselMicrowavePersistence vmp) {
            double available_power = vmp.getAvailablePower();
            double distance = Vector3d.Distance(vessel.transform.position, vmp.getVessel().transform.position);
            double powerdissip = Math.Tan(GameConstants.microwave_angle) * distance * Math.Tan(GameConstants.microwave_angle) * distance;
            powerdissip = Math.Max(powerdissip / collectorArea, 1);
            if (!isInlineReceiver) {
                //Scale energy reception based on angle of reciever to transmitter
                Vector3d direction_vector = (vmp.getVessel().transform.position - vessel.transform.position).normalized;
                double facing_factor = Vector3d.Dot(part.transform.up, direction_vector);
                facing_factor = Math.Max(0, facing_factor);
                available_power = available_power / powerdissip * facing_factor;
            } else {
                if (vmp.getVessel() != vessel) {
                    Vector3d direction_vector = (vmp.getVessel().transform.position - vessel.transform.position).normalized;
                    double facing_factor = 1.0 - Math.Abs(Vector3d.Dot(part.transform.up, direction_vector));
                    if (facing_factor > 1) {
                        facing_factor = 1;
                    }
                    available_power = available_power / powerdissip * facing_factor;
                }
            }
            return available_power;
        }

        protected double getSatPower(VesselMicrowavePersistence vmp, VesselRelayPersistence vrp) {
            double available_power = vmp.getAvailablePower();
            double distance = Vector3d.Distance(vessel.transform.position, vmp.getVessel().transform.position) + Vector3d.Distance(vrp.getVessel().transform.position, vmp.getVessel().transform.position);
            double powerdissip = Math.Tan(GameConstants.microwave_angle) * distance * Math.Tan(GameConstants.microwave_angle) * distance;
            powerdissip = Math.Max(powerdissip / collectorArea, 1);
            if (!isInlineReceiver) {
                //Scale energy reception based on angle of reciever to transmitter
                Vector3d direction_vector = (vrp.getVessel().transform.position - vessel.transform.position).normalized;
                double facing_factor = Vector3d.Dot(part.transform.up, direction_vector);
                facing_factor = Math.Max(0, facing_factor);
                available_power = available_power / powerdissip * facing_factor;
            } else {
                if (vmp.getVessel() != vessel) {
                    Vector3d direction_vector = (vrp.getVessel().transform.position - vessel.transform.position).normalized;
                    double facing_factor = 1.0 - Math.Abs(Vector3d.Dot(part.transform.up, direction_vector));
                    if (facing_factor > 1) {
                        facing_factor = 1;
                    }
                    available_power = available_power / powerdissip * facing_factor;
                }
            }
            return available_power;
        }

        public static double getEnumeratedPowerFromSatelliteForAllVesssels(VesselMicrowavePersistence vmp) {
            
            double enumerated_power = 0;
            foreach (Vessel vess in FlightGlobals.Vessels) {
                List<MicrowavePowerReceiver> receivers = vess.FindPartModulesImplementing<MicrowavePowerReceiver>();
                foreach (MicrowavePowerReceiver receiver in receivers) {
                    enumerated_power += receiver.getPowerFromSatellite(vmp);
                }
            }
            return enumerated_power;
        }


    }


}
