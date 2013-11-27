using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class MicrowavePowerReceiver : FNResourceSuppliableModule, FNThermalSource {
        //Persistent True
        [KSPField(isPersistant = true)]
        bool IsEnabled;

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
        public const float angle = 3.64773814E-10f;
        public const float efficiency = 0.85f;
        public const float alpha = 0.00399201596806387225548902195609f;

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

        [KSPEvent(guiActive = true, guiName = "Activate Receiver", active = true)]
        public void ActivateReceiver() {
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Receiver", active = true)]
        public void DisableReceiver() {
            IsEnabled = false;
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
            IsEnabled = !IsEnabled;
        }

        public override void OnStart(PartModule.StartState state) {
            Actions["ActivateReceiverAction"].guiName = Events["ActivateReceiver"].guiName = String.Format("Activate Receiver");
            Actions["DisableReceiverAction"].guiName = Events["DisableReceiver"].guiName = String.Format("Disable Receiver");
            Actions["ToggleReceiverAction"].guiName = String.Format("Toggle Receiver");
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_MEGAJOULES, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_THERMALPOWER };
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);
            if (state == StartState.Editor) { return; }
            
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
                        VesselMicrowavePersistence vmp = new VesselMicrowavePersistence(vess);
                        if (power_node.HasValue("nuclear_power")) {
                            double nuclear_power = double.Parse(power_node.GetValue("nuclear_power"));
                            vmp.setNuclearPower(nuclear_power);
                        }
                        if (power_node.HasValue("solar_power")) {
                            double solar_power = double.Parse(power_node.GetValue("solar_power"));
                            vmp.setSolarPower(solar_power);
                        }
                        vmps.Add(vmp);
                    }

                    if (config.HasNode("VESSEL_MICROWAVE_RELAY_" + vesselID)) {
                        ConfigNode relay_node = config.GetNode("VESSEL_MICROWAVE_RELAY_" + vesselID);
                        VesselRelayPersistence vrp = new VesselRelayPersistence(vess);
                        if (relay_node.HasValue("relay")) {
                            bool relay = bool.Parse(relay_node.GetValue("relay"));
                            vrp.setActive(relay);
                        }
                        vrps.Add(vrp);
                    }
                }
            }

            this.part.force_activate();
        }

        public override void OnUpdate() {
            Events["ActivateReceiver"].active = !IsEnabled;
            Events["DisableReceiver"].active = IsEnabled;
            Fields["toteff"].guiActive = (connectedsatsi > 0 || connectedrelaysi > 0);

            if (IsEnabled) {
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
            base.OnFixedUpdate();
            if (IsEnabled) {
                if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && !isThermalReceiver) {
                    IsEnabled = false;
                    deactivate_timer++;
                    if (FlightGlobals.ActiveVessel == vessel && deactivate_timer > 2) {
                        ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency microwave power shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    return;
                }
                deactivate_timer = 0;
                foreach (VesselMicrowavePersistence vmp in vmps) {
                    if (lineOfSightTo(vmp.getVessel())) { // we can see the satellite
                        double sat_power = getSatPower(vmp);
                        total_power += sat_power;
                        activeSatsIncr++;
                    } else {
                        foreach (VesselRelayPersistence vrp in vrps) {
                            if(lineOfSightTo(vrp.getVessel()) && vrp.isActive()) { // we can see relay and the relay is active
                                if(vrp.lineOfSightTo(vmp.getVessel())) { // relay can see satellite
                                    double sat_power = getSatPower(vmp);
                                    total_power += sat_power;
                                    activeRelsIncr++;
                                    break;
                                }
                            }
                        }
                    }
                }

                double atmosphericefficiency = Math.Exp(-FlightGlobals.getStaticPressure(vessel.transform.position) / 5);
                connectedsatsi = activeSatsIncr;
                connectedrelaysi = activeRelsIncr;

                double powerInputMegajoules = total_power / 1000.0*efficiency*atmosphericefficiency;
                powerInput = powerInputMegajoules * 1000.0f;
                efficiency_d = efficiency * atmosphericefficiency;

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
                    double waste_head_production = powerInputMegajoules / efficiency * (1.0f - efficiency);
                    supplyFNResource(waste_head_production * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
                } else {
                    double cur_thermal_power = supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER) / TimeWarp.fixedDeltaTime;
                    if (ThermalPower <= 0) {
                        ThermalPower = (float)(cur_thermal_power);
                    } else {
                        ThermalPower = (float)(cur_thermal_power * alpha + (1.0f - alpha) * ThermalPower);
                    }
                }
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
            return IsEnabled;
        }

        public bool shouldScaleDownJetISP() {
            return false;
        }

        public void enableIfPossible() {
            if (!IsEnabled) {
                IsEnabled = true;
            }
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
            double powerdissip = Math.Tan(angle) * distance * Math.Tan(angle) * distance;
            powerdissip = Math.Max(powerdissip / collectorArea, 1);
            if (!isInlineReceiver) {
                //Scale energy reception based on angle of reciever to transmitter
                Vector3d direction_vector = (vmp.getVessel().transform.position - vessel.transform.position).normalized;
                double facing_factor = Math.Abs(Vector3d.Dot(part.transform.up, direction_vector));
                facing_factor = Math.Max(0, facing_factor);
                available_power = available_power / powerdissip * facing_factor;
            } else {
                Vector3d direction_vector = (vmp.getVessel().transform.position - vessel.transform.position).normalized;
                double facing_factor = 1.0 - Math.Abs(Vector3d.Dot(part.transform.up, direction_vector));
                if (facing_factor > 1) {
                    facing_factor = 1;
                }
                available_power = available_power / powerdissip * facing_factor;
            }
            return available_power;
        }


    }


}
