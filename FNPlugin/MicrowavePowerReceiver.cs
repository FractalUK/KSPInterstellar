using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
	class MicrowavePowerReceiver : FNResourceSuppliableModule{
        [KSPField(isPersistant = false, guiActive = true, guiName = "Input Power")]
        public string beamedpower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Satellites Connected")]
        public string connectedsats;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Relay Connected")]
		public string connectedrelays;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Total Efficiency")]
        public string toteff;
		[KSPField(isPersistant = true)]
		bool IsEnabled;
		bool aIsRelay;
        public float powerInput;
        private int connectedsatsf = 0;
		private int connectedrelaysf = 0;
        private int activeCount = 0;
        private static int dishcount;
        private int mycount = -1;
        private float totefff;
        private float rangelosses;
        const float angle = 3.64773814E-10f;
        const float efficiency = 0.85f;

		[KSPField(isPersistant = false)]
		public string animName;
		[KSPField(isPersistant = false)]
		public float collectorArea = 1;

		protected Animation anim;

        //protected bool responsible_for_megajoulemanager;
        //protected FNResourceManager megamanager;

		protected bool play_down = true;
		protected bool play_up = true;

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

			base.OnStart (state);
            if (state == StartState.Editor) { return; }
            this.part.force_activate();

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) {
				anim [animName].layer = 1;
				if (connectedsatsf > 0 || connectedrelaysf > 0) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}

            if (mycount == -1) {
                mycount = dishcount;
                dishcount++;
            }
        }

        public override void OnUpdate() {
			Events["ActivateReceiver"].active = !IsEnabled;
			Events["DisableReceiver"].active = IsEnabled;
			Fields["toteff"].guiActive = (connectedsatsf > 0 || connectedrelaysf > 0);

			if (IsEnabled) {
				if (powerInput > 1000) {
					beamedpower = (powerInput/1000).ToString () + "MW";
				} else {
					beamedpower = powerInput.ToString () + "KW";
				}
			} else {
				beamedpower = "Offline.";
			}
            connectedsats = connectedsatsf.ToString();
			connectedrelays = connectedrelaysf.ToString();
            toteff = totefff.ToString() + "%";

			if (connectedsatsf > 0 || connectedrelaysf > 0) {
				if (play_up) {
					play_down = true;
					play_up = false;
					anim [animName].speed = 1f;
					anim [animName].normalizedTime = 0f;
					anim.Blend (animName, 2f);
				}
			} else {
				if (play_down) {
					play_down = false;
					play_up = true;
					anim [animName].speed = -1f;
					anim [animName].normalizedTime = 1f;
					anim.Blend (animName, 2f);
				}

			}
        }

        public override void OnFixedUpdate() {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES,FNResourceManager.FNRESOURCE_WASTEHEAT};
			this.resources_to_supply = resources_to_supply;

			base.OnFixedUpdate ();
			                                   
			ConfigNode config = PluginHelper.getPluginSaveFile();
            float powerInputIncr = 0;
			float powerInputRelay = 0;
            int activeSatsIncr = 0;
            float rangelosses = 0;
            if (config != null && IsEnabled) {

				//Check to see if active vessel is a relay - for now we do not want a relay to connect to another relay to prevent energy loops
				String aid = vessel.id.ToString ();
				if (config.HasValue (aid) == true) {
					String agenType = config.GetValue (aid + "type");
					if (agenType == "relay") {
						aIsRelay = true;
					} else {
						aIsRelay = false;
					}
				}

                //if (activeCount % 100 == 0) {
                    List<Vessel> vessels = FlightGlobals.Vessels;
                    //print(vessels.Count.ToString() + "\n");

					//loop through vessels and attempt to add any active sattilites
                    foreach (Vessel vess in vessels) {
						String vid = vess.id.ToString();
						String vname = vess.vesselName.ToString().ToLower();
                        //print(vid + "\n");

						//prevent adding active vessel as sat, skip calculations on debris, only add vessels with config value and line of sight to active vessel
						if (vess.isActiveVessel == false && vname.IndexOf("debris") == -1 && config.HasValue(vid) == true && lineOfSightTo(vess) == true) {
							String powerinputsat = config.GetValue (vid);
							String vgenType = config.GetValue (vid + "type");
							// if sat is not relay/nuclear check that it has line of site to sun
							// NOTE: we need to add a check for relay to check lineOfSiteToSource(vess), and if solar a lineOfSiteFromSourceToSun - to check that the source which it is relaying is still attached to it, and if it is a solar source that it is recieving solar energy
							if((vgenType == "solar" && PluginHelper.lineOfSightToSun(vess)) || vgenType == "relay" || vgenType == "nuclear") {
								float inputPowerFixedAlt = float.Parse (powerinputsat) * PluginHelper.getSatFloatCurve ().Evaluate ((float)FlightGlobals.Bodies [0].GetAltitude (vess.transform.position));
								float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
								float powerdissip = (float)(Math.Tan (angle) * distance * Math.Tan (angle) * distance);
								powerdissip = Math.Max (powerdissip/collectorArea, 1); 
								if (vgenType != "relay" && inputPowerFixedAlt > 0) {
									rangelosses += powerdissip;
									//Scale energy reception based on angle of reciever to transmitter
									Vector3d direction_vector = (vess.transform.position-vessel.transform.position).normalized;
									float facing_factor = Vector3.Dot (part.transform.up, direction_vector);
									facing_factor = Mathf.Max (0, facing_factor);
									powerInputIncr += inputPowerFixedAlt / powerdissip*facing_factor;
									activeSatsIncr++;
									connectedrelaysf = 0;
									//print ("warp: sat added - genType: " + vgenType);
								}
								// only attach to one relay IF no sattilites are available for direct connection
								else if(aIsRelay == false && activeSatsIncr < 1 && inputPowerFixedAlt > 0){
									rangelosses = powerdissip;
									//Scale energy reception based on angle of reciever to transmitter
									Vector3d direction_vector = (vess.transform.position-vessel.transform.position).normalized;
									float facing_factor = Vector3.Dot (part.transform.up, direction_vector);
									facing_factor = Mathf.Max (0, facing_factor);
									powerInputRelay = inputPowerFixedAlt / powerdissip*facing_factor;
									connectedrelaysf = 1;
									activeSatsIncr = 0;
									//print ("warp: relay added");
								}
							}
						}
                    }

                    float atmosphericefficiency = (float) Math.Exp(-FlightGlobals.getStaticPressure(vessel.transform.position) / 5);

					if (activeSatsIncr > 0 && powerInputIncr > 0) {
						this.rangelosses = rangelosses / activeSatsIncr;
						totefff = efficiency * atmosphericefficiency*100/rangelosses;
						powerInput = powerInputIncr * efficiency * atmosphericefficiency;
						connectedsatsf = activeSatsIncr;
						//print ("warp: connected sat");
					}
					else if (connectedrelaysf > 0 && powerInputRelay > 0) {
						this.rangelosses = rangelosses / connectedrelaysf;
						totefff = efficiency * atmosphericefficiency*100/rangelosses;
						powerInput = powerInputRelay * efficiency * atmosphericefficiency;
						connectedsatsf = 0;
						//print("warp: connected relay");
					}
					else {
						connectedrelaysf = 0;
						connectedsatsf = 0;
						powerInput = 0;
						//print ("warp: no active sats or relays available");
					}
                //}
            }


			float powerInputMegajoules = powerInput/1000.0f;
			supplyFNResource(powerInputMegajoules * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
			float waste_head_production = powerInput/1000.0f/ efficiency * (1.0f - efficiency);
			supplyFNResource (waste_head_production * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
            //activeCount++;
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
    }

    
}
