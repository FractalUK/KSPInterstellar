using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	class MicrowavePowerReceiver : FNResourceSuppliableModule, FNThermalSource {
		[KSPField(isPersistant = false, guiActive = true, guiName = "Input Power")]
		public string beamedpower;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Satellites Connected")]
		public string connectedsats;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Relay Connected")]
		public string connectedrelays;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Total Efficiency")]
		public string toteff;
		[KSPField(isPersistant = true)]
		public bool IsEnabled;
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
		public string animRName;
		[KSPField(isPersistant = false)]
		public string animTName;
		[KSPField(isPersistant = false)]
		public float collectorArea = 1;
		[KSPField(isPersistant = false)]
		public bool isThermalReciever;
		[KSPField(isPersistant = false)]
		public bool isInlineReciever;


		[KSPField(isPersistant = false)]
		public float ThermalTemp;
		[KSPField(isPersistant = false)]
		public float ThermalPower;
		[KSPField(isPersistant = false)]
		public float radius;

		protected Animation anim;
		protected Animation animR;
		protected Animation animT;

		protected String maxPowerSource;
		protected float maxPowerSourceAmt;
		protected float maxPowerVector;
		protected int deactivate_timer = 0;

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

			animR = part.FindModelAnimators (animRName).FirstOrDefault ();
			if (animR != null) {
				animR [animRName].layer = 1;
				animR [animRName].normalizedTime = 0f;
				animR [animRName].speed = 0.001f;
				animR.Play ();
			}

			animT = part.FindModelAnimators (animTName).FirstOrDefault ();
			if (animT != null) {
				animT [animTName].layer = 1;
				animT [animTName].normalizedTime = 0f;
				animT [animTName].speed = 0.001f;
				animT.Play ();
			}

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
			if (vessel != FlightGlobals.ActiveVessel)
			{
				return;
			}
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_MEGAJOULES,FNResourceManager.FNRESOURCE_WASTEHEAT,FNResourceManager.FNRESOURCE_THERMALPOWER};
			this.resources_to_supply = resources_to_supply;

			base.OnFixedUpdate ();

			ConfigNode config = PluginHelper.getPluginSaveFile();
			float powerInputIncr = 0;
			float powerInputRelay = 0;
			int activeSatsIncr = 0;
			float rangelosses = 0;
			if (config != null && IsEnabled) {
				if (getResourceBarRatio (FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && !isThermalReciever) {
					IsEnabled = false;
					deactivate_timer++;
					if (deactivate_timer > 2) {
						ScreenMessages.PostScreenMessage ("Warning Dangerous Overheating Detected: Emergency microwave power shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}
					return;
				}
				deactivate_timer = 0;

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
					String vid = vess.id.ToString ();
					String vname = vess.vesselName.ToString ().ToLower ();
					//print(vid + "\n");

					//prevent adding active vessel as sat, skip calculations on debris, only add vessels with config value and line of sight to active vessel
					if (vess.isActiveVessel == false && vname.IndexOf ("debris") == -1 && config.HasValue (vid) == true && lineOfSightTo (vess) == true) {
						String powerinputsat = config.GetValue (vid);
						String vgenType = config.GetValue (vid + "type");
						// if sat is not relay/nuclear check that it has line of site to sun
						// NOTE: we need to add a check for relay to check lineOfSiteToSource(vess), and if solar a lineOfSiteFromSourceToSun - to check that the source which it is relaying is still attached to it, and if it is a solar source that it is recieving solar energy
						if ((vgenType == "solar" && PluginHelper.lineOfSightToSun (vess)) || vgenType == "relay" || vgenType == "nuclear") {
							float inputPowerFixedAlt = float.Parse (powerinputsat) * PluginHelper.getSatFloatCurve ().Evaluate ((float)FlightGlobals.Bodies [0].GetAltitude (vess.transform.position));
							float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
							float powerdissip = (float)(Math.Tan (angle) * distance * Math.Tan (angle) * distance);
							powerdissip = Math.Max (powerdissip / collectorArea, 1); 
							if (vgenType != "relay" && inputPowerFixedAlt > 0) {
								rangelosses += powerdissip;
								if (!isInlineReciever) {
									//Scale energy reception based on angle of reciever to transmitter
									Vector3d direction_vector = (vess.transform.position - vessel.transform.position).normalized;
									float facing_factor = Vector3.Dot (part.transform.up, direction_vector);
									facing_factor = Mathf.Max (0, facing_factor);
									powerInputIncr += inputPowerFixedAlt / powerdissip * facing_factor;
								} else {
									Vector3d direction_vector = (vess.transform.position - vessel.transform.position).normalized;
									float facing_factorl = Vector3.Dot (-part.transform.right, direction_vector);
									float facing_factorf = Vector3.Dot (part.transform.forward, direction_vector);
									float facing_factorr = Vector3.Dot (part.transform.right, direction_vector);
									float facing_factorb = Vector3.Dot (-part.transform.forward, direction_vector);
									facing_factorl = Mathf.Max (0, facing_factorl);
									facing_factorf = Mathf.Max (0, facing_factorf);
									facing_factorr = Mathf.Max (0, facing_factorr);
									facing_factorb = Mathf.Max (0, facing_factorb);
									float facing_factor = facing_factorl + facing_factorf + facing_factorr + facing_factorb;

									//collector area is now based on the surface area of cylinder / surface area of 3m circle (to scale for standard 3m circular collector area = 1) / 4 (to divide by sides). max facing_factor is now 4 which will scale power vs collector area accurately.
									/*if (facing_factor > 1) {
										facing_factor = 1;
									}*/ 

									/* //animation testing
									float[] ff = { facing_factorl, facing_factorf, facing_factorr, facing_factorb };
									float sff = ff.Max ();

									if (facing_factorl == sff) {
										maxPowerVector = 0f;
									} else if (facing_factorf == sff) {
										maxPowerVector = 0.25f;
									} else if (facing_factorr == sff) {
										maxPowerVector = 0.5f;
									} else if (facing_factorb == sff) {
										maxPowerVector = 0.75f;
									}


									if (inputPowerFixedAlt > maxPowerSourceAmt) {
										maxPowerSourceAmt = inputPowerFixedAlt;
										maxPowerSource = vid;
										if (maxPowerVector > animR[animRName].normalizedTime) {
											animR [animRName].speed = 0.01f;
										} else {
											animR [animRName].speed = -0.01f;
										}
									} else if (maxPowerSource == vid) {
										if (maxPowerVector > animR[animRName].normalizedTime) {
											animR [animRName].speed = 0.01f;
										} else {
											animR [animRName].speed = -0.01f;
										}
										maxPowerSourceAmt = inputPowerFixedAlt;
									}

									//print ("Source " + maxPowerSource + " Amount " + maxPowerSourceAmt + " Vector " + maxPowerVector);


									animR [animRName].normalizedTime = maxPowerVector;
									animR.Blend (animRName, 2f);
									*/

									powerInputIncr += inputPowerFixedAlt / powerdissip * facing_factor;
								}
								activeSatsIncr++;
								connectedrelaysf = 0;
								//print ("warp: sat added - genType: " + vgenType);
							}
							// only attach to one relay IF no sattilites are available for direct connection
							else if (aIsRelay == false && activeSatsIncr < 1 && inputPowerFixedAlt > 0) {
								rangelosses = powerdissip;
								if (!isInlineReciever) {
									//Scale energy reception based on angle of reciever to transmitter
									Vector3d direction_vector = (vess.transform.position - vessel.transform.position).normalized;
									float facing_factor = Vector3.Dot (part.transform.up, direction_vector);
									facing_factor = Mathf.Max (0, facing_factor);
									powerInputIncr += inputPowerFixedAlt / powerdissip * facing_factor;
								} else {
									Vector3d direction_vector = (vess.transform.position - vessel.transform.position).normalized;
									float facing_factorl = Vector3.Dot (-part.transform.right, direction_vector);
									float facing_factorf = Vector3.Dot (part.transform.forward, direction_vector);
									float facing_factorr = Vector3.Dot (part.transform.right, direction_vector);
									float facing_factorb = Vector3.Dot (-part.transform.forward, direction_vector);
									facing_factorl = Mathf.Max (0, facing_factorl);
									facing_factorf = Mathf.Max (0, facing_factorf);
									facing_factorr = Mathf.Max (0, facing_factorr);
									facing_factorb = Mathf.Max (0, facing_factorb);
									float facing_factor = facing_factorl + facing_factorf + facing_factorr + facing_factorb;

									//collector area is now based on the surface area of cylinder / surface area of 3m circle (to scale for standard 3m circular collector area = 1) / 4 (to divide by sides). max facing_factor is now 4 which will scale power vs collector area accurately.
									/*if (facing_factor > 1) {
										facing_factor = 1;
									}*/ 
									powerInputIncr += inputPowerFixedAlt / powerdissip * facing_factor;
								}
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
					totefff = efficiency * atmosphericefficiency * 100 / rangelosses;
					powerInput = powerInputIncr * efficiency * atmosphericefficiency;
					connectedsatsf = activeSatsIncr;
					//print ("warp: connected sat");
				} else if (connectedrelaysf > 0 && powerInputRelay > 0) {
					this.rangelosses = rangelosses / connectedrelaysf;
					totefff = efficiency * atmosphericefficiency * 100 / rangelosses;
					powerInput = powerInputRelay * efficiency * atmosphericefficiency;
					connectedsatsf = 0;
					//print("warp: connected relay");
				} else {
					connectedrelaysf = 0;
					connectedsatsf = 0;
					powerInput = 0;
					//print ("warp: no active sats or relays available");
				}
				//}
			}else{
				connectedrelaysf = 0;
				connectedsatsf = 0;
				powerInput = 0;
			}

			float powerInputMegajoules = powerInput/1000.0f;

			float animateTemp = powerInputMegajoules / 3000; //3000K is max temp
			if (animateTemp > 1) {
				animateTemp = 1;
			}

			animT [animTName].speed = 0.001f;
			animT [animTName].normalizedTime = animateTemp;
			animT.Blend (animTName, 2f);

			if (!isThermalReciever) {
				supplyFNResource (powerInputMegajoules * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
				float waste_head_production = powerInputMegajoules / efficiency * (1.0f - efficiency);
				supplyFNResource (waste_head_production * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
				//activeCount++;
			} else {
				ThermalPower = powerInputMegajoules;
				if (ThermalPower - 1 > 0) {
					supplyFNResource (ThermalPower - 1 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER);
					supplyFNResource (1 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES); //revise this later to only supply megajoules as needed
				} else {
					supplyFNResource (ThermalPower * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_THERMALPOWER);
				}
				if(ThermalPower > 3000) { ThermalTemp = 3000; } else { ThermalTemp = ThermalPower; };
				//vessel.FindPartModulesImplementing<FNNozzleController> ().ForEach (fnnc => fnnc.setupPropellants ());
			}

		}

		public float getMegajoules() {
			return powerInput/1000;
		}

        public float getThermalTemp() {
            return ThermalTemp;
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
	}


}
