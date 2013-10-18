using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            double resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
			double airdensity =  part.vessel.atmDensity;
			double airspeed = part.vessel.srf_velocity.magnitude+10;
			double air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;
            airf = (float) (air/TimeWarp.fixedDeltaTime*resourcedensity);

			List<PartResource> intake_atm_resources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("IntakeAtm").id, intake_atm_resources);
			double intake_atm_missing_amount = 0;
			foreach (PartResource intake_atm_resource in intake_atm_resources) {
				intake_atm_missing_amount += intake_atm_resource.maxAmount - intake_atm_resource.amount;
			}

			air = Math.Min (intake_atm_missing_amount, air);

            part.RequestResource("IntakeAtm",-air);
        }

        public float getAtmosphericOutput() {
            return airf;
        }

    }
}
