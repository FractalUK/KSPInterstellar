using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin  {
	class AtmosphericIntake : FNResourceSuppliableModule {
        [KSPField(isPersistant = false)]
        public float area;
		[KSPField(isPersistant = false)]
		public bool hasAirIntake;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Intake Atmosphere")]
        public string intakeval;
        protected float airf;
		protected float airo;

		protected PartResource intake_atm = null;

        public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_INTAKEATM};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            intakeval = airf.ToString("0.00") + " kg";

			PartResourceList prl = part.Resources;

			foreach (PartResource wanted_resource in prl) {
				if (wanted_resource.resourceName == "IntakeAtm") {
					intake_atm = wanted_resource;
				}
			}
        }

        public override void OnFixedUpdate() {
			base.OnFixedUpdate ();

            double resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
			double airdensity =  part.vessel.atmDensity/1000;

			double airspeed = part.vessel.srf_velocity.magnitude+40.0;
			double air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;

            airf = (float) (1000.0*air/TimeWarp.fixedDeltaTime*resourcedensity);


			air = intake_atm.amount = Math.Min (air/TimeWarp.fixedDeltaTime, intake_atm.maxAmount);

            //part.RequestResource("IntakeAtm",-air);
			airo = (float) supplyFNResource(air,FNResourceManager.FNRESOURCE_INTAKEATM);

			//Consume IntakeAir to prevent people from using IntakeAir at the same time as IntakeAtm...
			if (hasAirIntake == true) {
				this.part.RequestResource ("IntakeAir", air);
			}
        }

        public float getAtmosphericOutput() {
			return airo * 9.81f;
        }
    }
}
