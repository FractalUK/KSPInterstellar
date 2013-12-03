using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class DTMagnetometer : PartModule {
		[KSPField(isPersistant = true)]
		bool IsEnabled;
		[KSPField(isPersistant = false)]
		public string animName;
        [KSPField(isPersistant = false, guiActive = true, guiName = "|B|")]
        public string Bmag;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_r")]
        public string Brad;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_T")]
        public string Bthe;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;


        private bool init = false;
		protected Animation anim;

		[KSPEvent(guiActive = true, guiName = "Activate Magnetometer", active = true)]
		public void ActivateMagnetometer() {
			anim [animName].speed = 1f;
			anim [animName].normalizedTime = 0f;
			anim.Blend (animName, 2f);
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Deactivate Magnetometer", active = false)]
		public void DeactivateMagnetometer() {
			anim [animName].speed = -1f;
			anim [animName].normalizedTime = 1f;
			anim.Blend (animName, 2f);
			IsEnabled = false;
		}

        [KSPAction("Activate Magnetometer")]
        public void ActivateMagnetometerAction(KSPActionParam param) {
            ActivateMagnetometer();
        }

        [KSPAction("Deactivate Magnetometer")]
        public void DeactivateMagnetometerAction(KSPActionParam param) {
            DeactivateMagnetometer();
        }

        [KSPAction("Toggle Magnetometer")]
        public void ToggleMagnetometerAction(KSPActionParam param) {
            if (IsEnabled) {
                DeactivateMagnetometer();
            } else {
                ActivateMagnetometer();
            }
        }

        public override void OnStart(PartModule.StartState state) {
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
        }

        public override void OnUpdate() {
			Events["ActivateMagnetometer"].active = !IsEnabled;
			Events["DeactivateMagnetometer"].active = IsEnabled;
			Fields["Bmag"].guiActive = IsEnabled;
			Fields["Brad"].guiActive = IsEnabled;
			Fields["Bthe"].guiActive = IsEnabled;
			Fields["ParticleFlux"].guiActive = IsEnabled;

            float lat = (float)vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            float Bmagf = VanAllen.getBeltMagneticFieldMag(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            float Bradf = VanAllen.getBeltMagneticFieldRadial(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            float Bthef = VanAllen.getBeltMagneticFieldAzimuthal(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            float flux = VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            Bmag = Bmagf.ToString("E") + "T";
            Brad = Bradf.ToString("E") + "T";
            Bthe = Bthef.ToString("E") + "T";
            ParticleFlux = flux.ToString("E");
        }

        public override void OnFixedUpdate() {
            
            

        }
    }
}
