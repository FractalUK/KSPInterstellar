using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class MicrowavePowerReceiver : FNResourceSuppliableModule, IThermalSource {
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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Relays Connected")]
        public string connectedrelays;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Network Depth")]
        public string networkDepthString;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Total Efficiency")]
        public string toteff;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Reception"), UI_FloatRange(stepIncrement = 0.005f, maxValue = 100, minValue = 1)]
        public float receiptPower;

        //Internal 

        protected Dictionary<Vessel, double> received_power = new Dictionary<Vessel, double>();

        //
        protected Animation anim;
        protected Animation animT;
        protected bool play_down = true;
        protected bool play_up = true;
        protected int connectedsatsi = 0;
        protected int connectedrelaysi = 0;
        protected int networkDepth = 0;
        protected double efficiency_d = 0;
        protected double powerInput = 0;
        protected long deactivate_timer = 0;
        protected List<VesselMicrowavePersistence> vmps;
        protected List<VesselRelayPersistence> vrps;
        protected MicrowavePowerTransmitter part_transmitter;
        protected bool has_transmitter = false;
        static readonly double microwaveAngleTan = Math.Tan(GameConstants.microwave_angle);//this doesn't change during game so it's readonly 
        double penaltyFreeDistance = 1;//should be set to proper value by OnStart method

        public bool IsSelfContained { get { return false; } }

        public float CoreTemperature { get { return 1500; } }

        public float MaximumPower { get { return MaximumThermalPower; } }

        public float MaximumThermalPower { get { return ThermalPower; } }

        public float MinimumPower { get { return 0; } }

        public bool IsVolatileSource { get { return true; } }

        public bool IsActive { get { return receiverIsEnabled; } }

        public bool IsNuclear { get { return false; } }


        [KSPEvent(guiActive = true, guiName = "Activate Receiver", active = true)]
        public void ActivateReceiver() {
            receiverIsEnabled = true;
            receiptPower = 100;
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
            penaltyFreeDistance = Math.Sqrt(1 / ((microwaveAngleTan * microwaveAngleTan) / collectorArea));

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
            connectedsats = string.Format("{0}/{1}", connectedsatsi, vmps.Count);
            connectedrelays = string.Format("{0}/{1}", connectedrelaysi, vrps.Count);
            networkDepthString = networkDepth.ToString();
            toteff = (efficiency_d * 100).ToString("0.00") + "%";

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
            //int activeRelsIncr = 0;
            double total_power = 0;
            connectedsatsi = 0;
            connectedrelaysi = 0;
            networkDepth = 0;

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


                HashSet<VesselRelayPersistence> usedRelays = new HashSet<VesselRelayPersistence>();
                //Transmitters power calculation
                foreach (var connectedTransmitterEntry in GetConnectedTransmitters()) {
                    VesselMicrowavePersistence transmitter = connectedTransmitterEntry.Key;
                    Vessel transmitterVessel = connectedTransmitterEntry.Key.getVessel();
                    double routeEfficiency = connectedTransmitterEntry.Value.Key;
                    IEnumerable<VesselRelayPersistence> relays = connectedTransmitterEntry.Value.Value;

                    received_power[transmitterVessel] = 0;

                    // calculate maximum power receivable from satellite
                    double satPowerCap = transmitter.getAvailablePower() * efficiency_d;
                    double currentPowerFromSat = MicrowavePowerReceiver.getEnumeratedPowerFromSatelliteForAllVesssels(transmitter);
                    double powerAvailableFromSat = (satPowerCap - currentPowerFromSat);
                    double satPower = Math.Min(GetSatPower(transmitter, routeEfficiency), powerAvailableFromSat); // get sat power and make sure we conserve enegy
                    received_power[transmitterVessel] = satPower * atmosphericefficiency;
                    total_power += satPower;
                    if (satPower > 0) {
                        activeSatsIncr++;
                        if (relays != null) {
                            foreach (var relay in relays) {
                                usedRelays.Add(relay);
                            }
                            networkDepth = Math.Max(networkDepth, relays.Count());
                        }
                    }
                }


                connectedsatsi = activeSatsIncr;
                connectedrelaysi = usedRelays.Count;

                double powerInputMegajoules = total_power / 1000.0 * GameConstants.microwave_dish_efficiency * atmosphericefficiency;
                powerInput = powerInputMegajoules * 1000.0f * receiptPower/100.0f;


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
            return (float)(powerInput / 1000);
        }

        public float getCoreTemp() {
            return 1500.0f;
        }

        public virtual float GetCoreTempAtRadiatorTemp(float rad_temp) {
            if (isThermalReceiver) {
                return 1500;
            } else {
                return float.MaxValue;
            }
        }

        public float getThermalPower() {
            return ThermalPower;
        }

        public float GetThermalPowerAtTemp(float temp) {
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

        public bool isVolatileSource() {
            return true;
        }

        public float getChargedPower() {
            return 0;
        }

        public float getMinimumThermalPower() {
            return 0;
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

        protected double GetSatPower(VesselMicrowavePersistence transmitter, double efficiency) {
            double availablePower = transmitter.getAvailablePower();
            return availablePower * efficiency;
        }


        #region RelayRouting
        protected double ComputeVisibilityAndDistance(VesselRelayPersistence r, Vessel v) {
            return r.lineOfSightTo(v) ? Vector3d.Distance(r.getVessel().transform.position, v.transform.position) : -1;
        }

        protected double ComputeDistance(Vessel v1, Vessel v2) {
            return Vector3d.Distance(v1.transform.position, v2.transform.position);
        }

        protected double ComputeTransmissionEfficiency(double distance, double facingFactor) {
            double powerdissip = 1;

            if (distance > penaltyFreeDistance)//if distance is <= penaltyFreeDistance then powerdissip will always be 1
            {
                powerdissip = (microwaveAngleTan * distance * microwaveAngleTan * distance) / collectorArea;//dissip is always > 1 here
            }
            return facingFactor / powerdissip;
        }

        protected double ComputeFacingFactor(Vessel powerVessel) {
            double facingFactor = 1;

            Vector3d directionVector = (powerVessel.transform.position - vessel.transform.position).normalized;
            if (!isInlineReceiver) {
                //Scale energy reception based on angle of reciever to transmitter
                facingFactor = Vector3d.Dot(part.transform.up, directionVector);
                facingFactor = Math.Max(0, facingFactor);
            } else {
                facingFactor = 1.0 - Math.Abs(Vector3d.Dot(part.transform.up, directionVector));
                facingFactor = Math.Min(facingFactor, 1);
            }

            return facingFactor;
        }

        /// <summary>
        /// Returns transmitters which to which this vessel can connect, route efficiency and relays used for each one.
        /// </summary>
        /// <param name="maxHops">Maximum number of relays which can be used for connection to transmitter</param>
        protected IDictionary<VesselMicrowavePersistence, KeyValuePair<double, IEnumerable<VesselRelayPersistence>>> GetConnectedTransmitters(int maxHops = 25) {

            //these two dictionaries store transmitters and relays and best currently known route to them which is replaced if better one is found. 

            var transmitterRouteDictionary = new Dictionary<VesselMicrowavePersistence, MicrowaveRoute>();
            var relayRouteDictionary = new Dictionary<VesselRelayPersistence, MicrowaveRoute>();

            var transmittersToCheck = new List<VesselMicrowavePersistence>();//stores all transmiters to which we want to connect


            foreach (VesselMicrowavePersistence transmitter in vmps) { //first check for direct connection from current vessel to transmitters, will always be optimal
                if (transmitter.getAvailablePower() > 0) { //ignore if no power or transmitter is on the same vessel
                    if (!isInlineReceiver || transmitter.getVessel() != vessel) {
                        if (lineOfSightTo(transmitter.getVessel())) {
                            double distance = ComputeDistance(vessel, transmitter.getVessel());
                            double facingFactor = ComputeFacingFactor(transmitter.getVessel());
                            double efficiency = ComputeTransmissionEfficiency(distance, facingFactor);
                            transmitterRouteDictionary[transmitter] = new MicrowaveRoute(efficiency, distance, facingFactor); //store in dictionary that optimal route to this transmitter is direct connection, can be replaced if better route is found
                        }
                        transmittersToCheck.Add(transmitter);
                    }
                }
            }

            //this algorithm processes relays in groups in which elements of the first group must be visible from receiver, 
            //elements from the second group must be visible by at least one element from previous group and so on...


            var relaysToCheck = new List<VesselRelayPersistence>();//relays which we have to check - all active relays will be here
            var currentRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//relays which are in line of sight, and we have not yet checked what they can see. Their index in relaysToCheck is also stored

            int relayIndex = 0;
            foreach (VesselRelayPersistence relay in vrps) {
                if (relay.isActive()) {
                    if (lineOfSightTo(relay.getVessel())) {
                        double distance = ComputeDistance(vessel, relay.getVessel());
                        double facingFactor = ComputeFacingFactor(relay.getVessel());
                        double efficiency = ComputeTransmissionEfficiency(distance, facingFactor);
                        relayRouteDictionary[relay] = new MicrowaveRoute(efficiency, distance, facingFactor);//store in dictionary that optimal route to this relay is direct connection, can be replaced if better route is found
                        currentRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(relay, relayIndex));
                    }
                    relaysToCheck.Add(relay);
                    relayIndex++;
                }
            }



            int hops = 0; //number of hops between relays


            //pre-compute distances and visibility thus limiting number of checks to (Nr^2)/2 + NrNt +Nr + Nt
            if (hops < maxHops && transmittersToCheck.Any()) {
                double[,] relayToRelayDistances = new double[relaysToCheck.Count, relaysToCheck.Count];
                double[,] relayToTransmitterDistances = new double[relaysToCheck.Count, transmittersToCheck.Count];

                for (int i = 0; i < relaysToCheck.Count; i++) {
                    var relay = relaysToCheck[i];
                    for (int j = i + 1; j < relaysToCheck.Count; j++) {
                        double visibilityAndDistance = ComputeVisibilityAndDistance(relay, relaysToCheck[j].getVessel());
                        relayToRelayDistances[i, j] = visibilityAndDistance;
                        relayToRelayDistances[j, i] = visibilityAndDistance;
                    }
                    for (int t = 0; t < transmittersToCheck.Count; t++) {
                        relayToTransmitterDistances[i, t] = ComputeVisibilityAndDistance(relay,
                                                                                         transmittersToCheck[t].
                                                                                             getVessel());
                    }
                }

                HashSet<int> coveredRelays = new HashSet<int>();

                //runs as long as there is any relay to which we can connect and maximum number of hops have not been breached
                while (hops < maxHops && currentRelayGroup.Any()) {
                    var nextRelayGroup = new List<KeyValuePair<VesselRelayPersistence, int>>();//will put every relay which is in line of sight of any relay from currentRelayGroup here
                    foreach (var relayEntry in currentRelayGroup) //relays visible from receiver in first iteration, then relays visible from them etc....
                    {
                        var relay = relayEntry.Key;
                        MicrowaveRoute relayRoute = relayRouteDictionary[relay];// current best route for this relay
                        double relayRouteFacingFactor = relayRoute.FacingFactor;// it's always facing factor from the beggining of the route

                        for (int t = 0; t < transmittersToCheck.Count; t++)//check if this relay can connect to transmitters
                        {
                            var transmitter = transmittersToCheck[t];
                            double transmitterDistance = relayToTransmitterDistances[relayEntry.Value, t];
                            if (transmitterDistance > 0)//it's >0 if it can see
                            {
                                double newDistance = relayRoute.Distance + transmitterDistance;// total distance from receiver by this relay to transmitter
                                double efficiencyByThisRelay = ComputeTransmissionEfficiency(newDistance, relayRouteFacingFactor);//efficiency
                                MicrowaveRoute currentOptimalRoute;

                                //this will return true if there is already a route to this transmitter
                                if (transmitterRouteDictionary.TryGetValue(transmitter, out currentOptimalRoute)) {
                                    if (currentOptimalRoute.Efficiency < efficiencyByThisRelay)
                                        //if route using this relay is better then replace the old route
                                        transmitterRouteDictionary[transmitter] = new MicrowaveRoute(efficiencyByThisRelay, newDistance, relayRouteFacingFactor, relay);
                                } else {
                                    //there is no other route to this transmitter yet known so algorithm puts this one as optimal
                                    transmitterRouteDictionary[transmitter] = new MicrowaveRoute(efficiencyByThisRelay,
                                                                                                 newDistance,
                                                                                                 relayRouteFacingFactor,
                                                                                                 relay);
                                }
                            }
                        }

                        for (int r = 0; r < relaysToCheck.Count; r++) {
                            var nextRelay = relaysToCheck[r];
                            if (nextRelay == relay)
                                continue;

                            double distanceToNextRelay = relayToRelayDistances[relayEntry.Value, r];
                            if (distanceToNextRelay > 0) //any relay which is in LOS of this relay
                            {
                                double relayToNextRelayDistance = relayRoute.Distance + distanceToNextRelay;
                                double efficiencyByThisRelay =
                                    ComputeTransmissionEfficiency(relayToNextRelayDistance, relayRouteFacingFactor);

                                MicrowaveRoute currentOptimalPredecessor;

                                if (relayRouteDictionary.TryGetValue(nextRelay, out currentOptimalPredecessor))
                                //this will return true if there is already a route to next relay
                                {
                                    if (currentOptimalPredecessor.Efficiency < efficiencyByThisRelay)
                                        //if route using this relay is better

                                        relayRouteDictionary[nextRelay] = new MicrowaveRoute(efficiencyByThisRelay,
                                                                                             relayToNextRelayDistance,
                                                                                             relayRoute.FacingFactor,
                                                                                             relay);
                                    //we put it in dictionary as optimal

                                } else //there is no other route to this relay yet known so we put this one as optimal
                                {
                                    relayRouteDictionary[nextRelay] = new MicrowaveRoute(efficiencyByThisRelay,
                                                                                         relayToNextRelayDistance,
                                                                                         relayRoute.FacingFactor,
                                                                                         relay);
                                }

                                if (!coveredRelays.Contains(r)) {
                                    nextRelayGroup.Add(new KeyValuePair<VesselRelayPersistence, int>(nextRelay, r));
                                    //in next iteration we will check what next relay can see
                                    coveredRelays.Add(r);
                                }
                            }
                        }
                    }
                    currentRelayGroup = nextRelayGroup;
                    //we don't have to check old relays so we just replace whole List
                    hops++;
                }

            }

            //building final result
            var resultDictionary = new Dictionary<VesselMicrowavePersistence, KeyValuePair<double, IEnumerable<VesselRelayPersistence>>>();

            foreach (var transmitterEntry in transmitterRouteDictionary) {
                Stack<VesselRelayPersistence> relays = new Stack<VesselRelayPersistence>();//Last in, first out so relay visible from receiver will always be first
                VesselRelayPersistence relay = transmitterEntry.Value.PreviousRelay;
                while (relay != null) {
                    relays.Push(relay);
                    relay = relayRouteDictionary[relay].PreviousRelay;
                }
                resultDictionary.Add(transmitterEntry.Key, new KeyValuePair<double, IEnumerable<VesselRelayPersistence>>(transmitterEntry.Value.Efficiency, relays));
            }

            return resultDictionary; //connectedTransmitters;
        }
        #endregion RelayRouting
    }


}
