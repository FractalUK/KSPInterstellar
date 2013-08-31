using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    class DTMagnetometer : PartModule {
        [KSPField(isPersistant = false, guiActive = true, guiName = "|B|")]
        public string Bmag;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_r")]
        public string Brad;
        [KSPField(isPersistant = false, guiActive = true, guiName = "B_T")]
        public string Bthe;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Particle Flux")]
        public string ParticleFlux;

        private bool init = false;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();


        }

        public override void OnUpdate() {
            
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
