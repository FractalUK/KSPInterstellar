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

		protected PartResource intake_atm = null;

        public override void OnStart(PartModule.StartState state) {

            if (state == StartState.Editor) { return; }
            this.part.force_activate();

			PartResourceList prl = part.Resources;

			foreach (PartResource wanted_resource in prl) {
				if (wanted_resource.resourceName == "IntakeAtm") {
					intake_atm = wanted_resource;
				}
			}
        }

        public override void OnUpdate() {
            intakeval = airf.ToString("0.00") + " kg";
        }

        public override void OnFixedUpdate() {
            double resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
			double airdensity =  part.vessel.atmDensity/1000;

			double airspeed = part.vessel.srf_velocity.magnitude+40.0;
			double air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;

            airf = (float) (1000.0*air/TimeWarp.fixedDeltaTime*resourcedensity);


			air = intake_atm.amount = Math.Min (air/TimeWarp.fixedDeltaTime, intake_atm.maxAmount);

            part.RequestResource("IntakeAtm",-air);
        }

        public float getAtmosphericOutput() {
            return airf;
        }

    }
}
