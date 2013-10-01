using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
	class MicrowavePowerTransmitter : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        bool transIsEnabled;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Beamed Power")]
        public string beamedpower;
        float inputPower = 0;
        private int activeCount = 0;

		bool nuclear = false;
		bool microwave = false;
		bool solar = false;

		//[KSPField(isPersistant = false)]
		//public string animName;

		//protected Animation anim;

                
        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = true)]
        public void ActivateTransmitter() {
			/*anim [animName].speed = 1f;
			anim [animName].normalizedTime = 0f;
			anim.Blend (animName, 2f);*/
            transIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter() {
			/*anim [animName].speed = -1f;
			anim [animName].normalizedTime = 1f;
			anim.Blend (animName, 2f);*/
            transIsEnabled = false;
        }

        [KSPAction("Activate Transmitter")]
        public void ActivateTransmitterAction(KSPActionParam param) {
            ActivateTransmitter();
        }

        [KSPAction("Deactivate Transmitter")]
        public void DeactivateTransmitterAction(KSPActionParam param) {
            DeactivateTransmitter();
        }

        public override void OnStart(PartModule.StartState state) {
            Actions["ActivateTransmitterAction"].guiName = Events["ActivateTransmitter"].guiName = String.Format("Activate Transmitter");
            Actions["DeactivateTransmitterAction"].guiName = Events["DeactivateTransmitter"].guiName = String.Format("Deactivate Transmitter");
            
            if (state == StartState.Editor) { return; }
            this.part.force_activate();

			/*anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) {
				anim [animName].layer = 1;
				if (!transIsEnabled) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}*/
                        
            List<Part> vesselparts = vessel.parts;
            for (int i = 0; i < vesselparts.Count; ++i) {
                Part cPart = vesselparts.ElementAt(i);
                PartModuleList pml = cPart.Modules;
                for (int j = 0; j < pml.Count; ++j) {
                    var curSolarPan = pml.GetModule(j) as ModuleDeployableSolarPanel;
                    if (curSolarPan != null) {
                        curSolarPan.powerCurve = PluginHelper.getSatFloatCurve();
                    }
                }
            }


        }

        public override void OnUpdate() {
            Events["ActivateTransmitter"].active = !transIsEnabled;
            Events["DeactivateTransmitter"].active = transIsEnabled;
			if (inputPower > 1000) {
				beamedpower = (inputPower / 1000).ToString ("0.000") + "MW";
			} else {
				beamedpower = inputPower.ToString ("0.000") + "KW";
			}
        }

        public override void OnFixedUpdate() {
            activeCount++;


			if (transIsEnabled) {
				//List<PartResource> resources = new List<PartResource>();
				//part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, resources);
				//float electrical_current_available = 0;
				//for (int i = 0; i < resources.Count; ++i) {
				//    electrical_current_available += (float)resources.ElementAt(i).amount;
				//}
				List<Part> vesselparts = vessel.parts;
				float electrical_current_available = 0;
				for (int i = 0; i < vesselparts.Count; ++i) {
					Part cPart = vesselparts.ElementAt (i);
					PartModuleList pml = cPart.Modules;
					for (int j = 0; j < pml.Count; ++j) {
						var curFNGen = pml.GetModule (j) as FNGenerator;
						var curMwRec = pml.GetModule (j) as MicrowavePowerReceiver;
						var curSolarPan = pml.GetModule (j) as ModuleDeployableSolarPanel;
						if (curFNGen != null) {
							electrical_current_available += curFNGen.tPower * 1000;
							//print ("warp: current available " + electrical_current_available);
							List<PartResource> partresources = new List<PartResource> ();
							part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition ("Megajoules").id, partresources);
							float currentMJ = 0;
							foreach (PartResource partresource in partresources) {
								currentMJ += (float)partresource.amount;
							}
							if (currentMJ > electrical_current_available / 1000) {
								part.RequestResource ("Megajoules", electrical_current_available / 1000 * TimeWarp.fixedDeltaTime);
							}
							nuclear = true;
						} 
						else if (curMwRec != null && nuclear == false) {
							electrical_current_available = curMwRec.powerInput;
							part.RequestResource ("ElectricCharge", electrical_current_available * TimeWarp.fixedDeltaTime);
							microwave = true;
						}
						else if (curSolarPan != null && nuclear == false && microwave == false) {
							electrical_current_available += curSolarPan.flowRate;
							part.RequestResource ("ElectricCharge", electrical_current_available * TimeWarp.fixedDeltaTime);
							solar = true;
						}
					}
				}
				inputPower = electrical_current_available;
				//print ("warp: inputPower " + inputPower);
			} else {
				inputPower = 0;
			}

            if (activeCount % 1000 == 9) {
                ConfigNode config = ConfigNode.Load(PluginHelper.getPluginSaveFilePath());
				string genType = "undefined";
                if (config == null) {
                    config = new ConfigNode();
                }
                //float inputPowerFixedAlt = (float) ((double)inputPower * (Math.Pow(FlightGlobals.Bodies[0].GetAltitude(vessel.transform.position), 2)) / PluginHelper.FIXED_SAT_ALTITUDE / PluginHelper.FIXED_SAT_ALTITUDE);
				float inputPowerFixedAlt = 0;
				if (nuclear == true) {
					inputPowerFixedAlt = inputPower;
					print ("warp: nuclear inputPower " + inputPowerFixedAlt);
					genType = "nuclear";
				} else if (microwave == true) {
					inputPowerFixedAlt = inputPower;
					print ("warp: relay inputPower " + inputPowerFixedAlt);
					genType = "relay";
				} else if (solar == true) {
					inputPowerFixedAlt = inputPower / PluginHelper.getSatFloatCurve ().Evaluate ((float)FlightGlobals.Bodies [0].GetAltitude (vessel.transform.position));
					print ("warp: solar inputPower " + inputPowerFixedAlt);
					genType = "solar";
				}
                
				if (genType != "undefined") {
					string vesselIDSolar = vessel.id.ToString ();
					string outputPower = inputPowerFixedAlt.ToString ("0.000");
					if (!config.HasValue (vesselIDSolar)) {
						config.AddValue (vesselIDSolar, outputPower);
						config.AddValue (vesselIDSolar + "type", genType);
					} else {
						config.SetValue (vesselIDSolar, outputPower);
						config.AddValue (vesselIDSolar + "type", genType);
					}
                
					config.Save (PluginHelper.getPluginSaveFilePath ());
				}
            }
        }
    }
}
