using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace FNPlugin  {
    class AtmosphericIntake : PartModule {
        [KSPField(isPersistant = false)]
        public float area;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Intake Atmosphere")]
        public string intakeval;
        protected float airf;

        public override void OnStart(PartModule.StartState state) {

            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            intakeval = airf.ToString("0.0000");
        }

        public override void OnFixedUpdate() {
            float resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
            float airdensity = (float) part.vessel.atmDensity;
            float airspeed = (float) part.vessel.srf_velocity.magnitude+10;
            float air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;
            airf = air/TimeWarp.fixedDeltaTime*resourcedensity;
            part.RequestResource("IntakeAtm",-air);
        }

        public float getAtmosphericOutput() {
            return airf;
        }

    }
}
