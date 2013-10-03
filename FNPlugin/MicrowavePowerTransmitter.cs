using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
    class MicrowavePowerTransmitter : PartModule {
        [KSPField(isPersistant = true)]
        bool IsEnabled;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Beamed Power")]
        public string beamedpower;
        float inputPower = 0;
        private int activeCount = 0;

		[KSPField(isPersistant = false)]
		public string animName;

		protected Animation anim;

                
        [KSPEvent(guiActive = true, guiName = "Activate Transmitter", active = true)]
        public void ActivateTransmitter() {
			if (anim != null) {
				anim [animName].speed = 1f;
				anim [animName].normalizedTime = 0f;
				anim.Blend (animName, 2f);
			}
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Transmitter", active = false)]
        public void DeactivateTransmitter() {
			if (anim != null) {
				anim [animName].speed = -1f;
				anim [animName].normalizedTime = 1f;
				anim.Blend (animName, 2f);
			}
            IsEnabled = false;
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

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (anim != null) {
				anim [animName].layer = 1;
				if (!IsEnabled) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}
                        
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
            Events["ActivateTransmitter"].active = !IsEnabled;
            Events["DeactivateTransmitter"].active = IsEnabled;
            
            beamedpower = inputPower.ToString("0.000") + "KW";
        }

        public override void OnFixedUpdate() {
            activeCount++;


			if (IsEnabled) {
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
						var curSolarPan = pml.GetModule (j) as ModuleDeployableSolarPanel;
						if (curSolarPan != null) {
                            
							electrical_current_available += curSolarPan.flowRate;
						}
					}
				}

				//inputPower = (float)part.RequestResource("ElectricCharge", electrical_current_available * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
				part.RequestResource ("ElectricCharge", electrical_current_available * TimeWarp.fixedDeltaTime);
				inputPower = electrical_current_available;
			} else {
				inputPower = 0;
			}

            if (activeCount % 1000 == 9) {
				ConfigNode config = PluginHelper.getPluginSaveFile();
                //float inputPowerFixedAlt = (float) ((double)inputPower * (Math.Pow(FlightGlobals.Bodies[0].GetAltitude(vessel.transform.position), 2)) / PluginHelper.FIXED_SAT_ALTITUDE / PluginHelper.FIXED_SAT_ALTITUDE);
                float inputPowerFixedAlt = inputPower / PluginHelper.getSatFloatCurve().Evaluate((float)FlightGlobals.Bodies[0].GetAltitude(vessel.transform.position));
                string vesselIDSolar = vessel.id.ToString();

                string outputPower = inputPowerFixedAlt.ToString("0.000");
                if (!config.HasValue(vesselIDSolar)) {
                    config.AddValue(vesselIDSolar, outputPower);
                }else {
                    config.SetValue(vesselIDSolar, outputPower);
                }
                
                config.Save(PluginHelper.getPluginSaveFilePath());

            }


        }


    }
}
