using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class MicrowavePowerTransmitter : FNResourceSuppliableModule
    {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool relay;

        [KSPField(isPersistant = true)]
        protected float nuclear_power = 0;
        [KSPField(isPersistant = true)]
        protected float solar_power = 0;

        //Persistent False
        [KSPField(isPersistant = false)]
        public string animName;

        //GUI 
        [KSPField(isPersistant = false, guiActive = true, guiName = "Transmitter")]
        public string statusStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Beamed Power")]
        public string beamedpower;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Transmission"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float transmitPower = 100;

        //Internal
        protected Animation anim;
        protected float displayed_solar_power = 0;
        protected List<FNGenerator> generators;
        protected List<MicrowavePowerReceiver> receivers;
        protected List<ModuleDeployableSolarPanel> panels;
        protected MicrowavePowerReceiver part_receiver;
        protected bool has_receiver = false;

        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = true)]
        public void ActivateTransmitter()
        {
            if (relay) { return; }
            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim.Blend(animName, 2f);
            }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter()
        {
            if (relay) { return; }
            if (anim != null)
            {
                if (anim[animName].normalizedTime == 0.0f)
                    anim[animName].normalizedTime = 1.0f;
                anim[animName].speed = -1f;
                anim.Blend(animName, 2f);
            }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Activate Relay", active = true)]
        public void ActivateRelay()
        {
            if (IsEnabled) { return; }
            if (anim != null)
            {
                anim[animName].speed = 1f;
                anim.Blend(animName, 2f);
            }
            IsEnabled = true;
            relay = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Relay", active = true)]
        public void DeactivateRelay()
        {
            if (!relay) { return; }
            if (anim != null)
            {
                if (anim[animName].normalizedTime == 0.0f)
                    anim[animName].normalizedTime = 1.0f;
                anim[animName].speed = -1.0f;
                anim.Blend(animName, 2f);
            }
            IsEnabled = false;
            relay = false;
        }

        [KSPAction("Activate Transmitter")]
        public void ActivateTransmitterAction(KSPActionParam param)
        {
            ActivateTransmitter();
        }

        [KSPAction("Deactivate Transmitter")]
        public void DeactivateTransmitterAction(KSPActionParam param)
        {
            DeactivateTransmitter();
        }

        [KSPAction("Activate Relay")]
        public void ActivateRelayAction(KSPActionParam param)
        {
            ActivateRelay();
        }

        [KSPAction("Deactivate Relay")]
        public void DeactivateRelayAction(KSPActionParam param)
        {
            DeactivateRelay();
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) { return; }

            generators = vessel.FindPartModulesImplementing<FNGenerator>();
            receivers = vessel.FindPartModulesImplementing<MicrowavePowerReceiver>();
            panels = vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>();
            if (part.FindModulesImplementing<MicrowavePowerReceiver>().Count == 1)
            {
                part_receiver = part.FindModulesImplementing<MicrowavePowerReceiver>().First();
                has_receiver = true;
            }

            anim = part.FindModelAnimators(animName).FirstOrDefault();
            if (anim != null)
            {
                anim[animName].layer = 1;
                if (IsEnabled)
                {
                    anim[animName].normalizedTime = 1f;
					anim[animName].enabled = true;
					anim.Sample();
                }
            }

            this.part.force_activate();
        }

        public override void OnUpdate()
        {
            bool receiver_on = false;
            if (has_receiver)
            {
                if (part_receiver.isActive())
                {
                    receiver_on = true;
                }
            }
            Events["ActivateTransmitter"].active = !IsEnabled && !relay && !receiver_on;
            Events["DeactivateTransmitter"].active = IsEnabled && !relay;
            Events["ActivateRelay"].active = !IsEnabled && !relay && !receiver_on;
            Events["DeactivateRelay"].active = IsEnabled && relay;
            Fields["beamedpower"].guiActive = IsEnabled && !relay;
            Fields["transmitPower"].guiActive = IsEnabled && !relay;

            if (IsEnabled)
            {
                if (relay)
                {
                    statusStr = "Relay Active";
                }
                else
                {
                    statusStr = "Transmitter Active";
                }
            }
            else
            {
                statusStr = "Inactive.";
            }

            double inputPower = nuclear_power + displayed_solar_power;
            if (inputPower > 1000)
            {
                if (inputPower > 1e6)
                {
                    beamedpower = (inputPower / 1e6).ToString("0.000") + " GW";
                }
                else
                {
                    beamedpower = (inputPower / 1000).ToString("0.000") + " MW";
                }
            }
            else
            {
                beamedpower = inputPower.ToString("0.000") + " KW";
            }
        }

        public override void OnFixedUpdate()
        {
            nuclear_power = 0;
            solar_power = 0;
            displayed_solar_power = 0;

            base.OnFixedUpdate();
            if (IsEnabled && !relay)
            {
                foreach (FNGenerator generator in generators)
                {
                    if (generator.isActive())
                    {
                        IThermalSource thermal_source = generator.getThermalSource();
                        if (thermal_source != null && !thermal_source.IsVolatileSource)
                        {
                            float output = generator.getMaxPowerOutput();
                            if (thermal_source is InterstellarFusionReactor)
                            {
                                InterstellarFusionReactor fusion_reactor = thermal_source as InterstellarFusionReactor;
                                output = output * 0.92f;
                            }
                            output = output * transmitPower / 100.0f;
                            float gpower = consumeFNResource(output * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                            nuclear_power += gpower * 1000 / TimeWarp.fixedDeltaTime;
                        }
                    }
                }

                foreach (ModuleDeployableSolarPanel panel in panels)
                {
                    float output = panel.flowRate;

                    // attempt to retrieve all solar power output
                    if (output == 0.0)
                    {
                        var solarpanels = panel.part.parent.FindModulesImplementing<ModuleDeployableSolarPanel>();
                        solarpanels.ForEach(s => output += s.flowRate);
                    }

                    float spower = part.RequestResource("ElectricCharge", output * TimeWarp.fixedDeltaTime);
                    double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
                    displayed_solar_power += spower / TimeWarp.fixedDeltaTime;
                    //scale solar power to what it would be in Kerbin orbit for file storage
                    solar_power += (float)(spower / TimeWarp.fixedDeltaTime / inv_square_mult);
                }
            }

            if (double.IsInfinity(nuclear_power) || double.IsNaN(nuclear_power))
            {
                nuclear_power = 0;
            }

            if (double.IsInfinity(solar_power) || double.IsNaN(solar_power))
            {
                solar_power = 0;
            }
        }

        public double getNuclearPower()
        {
            return nuclear_power;
        }

        public double getSolarPower()
        {
            return solar_power;
        }

        public bool getIsRelay()
        {
            return relay;
        }

        public bool isActive()
        {
            return IsEnabled;
        }

        public override string getResourceManagerDisplayName()
        {
            return "Microwave Transmitter";
        }

        public static double getEnumeratedNuclearPowerForVessel(Vessel vess)
        {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            double total_nuclear_power = 0;
            foreach (MicrowavePowerTransmitter transmitter in transmitters)
            {
                total_nuclear_power += transmitter.getNuclearPower();
            }
            return total_nuclear_power;
        }

        public static double getEnumeratedSolarPowerForVessel(Vessel vess)
        {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            double total_solar_power = 0;
            foreach (MicrowavePowerTransmitter transmitter in transmitters)
            {
                total_solar_power += transmitter.getSolarPower();
            }
            return total_solar_power;
        }

        public static bool vesselIsRelay(Vessel vess)
        {
            List<MicrowavePowerTransmitter> transmitters = vess.FindPartModulesImplementing<MicrowavePowerTransmitter>();
            foreach (MicrowavePowerTransmitter transmitter in transmitters)
            {
                if (transmitter.getIsRelay() && transmitter.isActive())
                {
                    return true;
                }
            }
            return false;
        }

        public static double getEnumeratedNuclearPowerForVessel(ProtoVessel vess)
        {
            double total_nuclear_power = 0;
            foreach (var ppart in vess.protoPartSnapshots)
            {
                foreach (var pmodule in ppart.modules)
                {
                    if (pmodule.moduleName == "MicrowavePowerTransmitter")
                    {
                        string nuclear_power = pmodule.moduleValues.GetValue("nuclear_power");
                        if (nuclear_power != null)
                            total_nuclear_power += double.Parse(nuclear_power);
                    }
                }
            }
            return total_nuclear_power;
        }

        public static double getEnumeratedSolarPowerForVessel(ProtoVessel vess)
        {
            double total_solar_power = 0;
            foreach (var ppart in vess.protoPartSnapshots)
            {
                foreach (var pmodule in ppart.modules)
                {
                    if (pmodule.moduleName == "MicrowavePowerTransmitter")
                    {
                        string solar_power = pmodule.moduleValues.GetValue("solar_power");
                        if (solar_power != null)
                            total_solar_power += double.Parse(solar_power);
                    }
                }
            }
            return total_solar_power;
        }

        public static bool vesselIsRelay(ProtoVessel vess)
        {
            foreach (var ppart in vess.protoPartSnapshots)
            {
                foreach (var pmodule in ppart.modules)
                {
                    if (pmodule.moduleName == "MicrowavePowerTransmitter")
                    {
                        string relay_value = pmodule.moduleValues.GetValue("relay");
                        if (relay_value != null)
                            return bool.Parse(relay_value);
                    }
                }
            }
            return false;
        }

    }
}
