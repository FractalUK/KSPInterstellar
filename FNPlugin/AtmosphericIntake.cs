using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin  {
	class AtmosphericIntake : FNResourceSuppliableModule {
        [KSPField(isPersistant = false)]
        public float area;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Intake Atmosphere")]
        public string intakeval;
        protected float airf;

        public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_INTAKEATM};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);

			if (state == StartState.Editor) { return; }
			this.part.force_activate();
        }

        public override void OnUpdate() {
            intakeval = airf.ToString("0.0000");
        }

        public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
            float resourcedensity = PartResourceLibrary.Instance.GetDefinition("IntakeAtm").density;
            float airdensity = (float) part.vessel.atmDensity;
            float airspeed = (float) part.vessel.srf_velocity.magnitude+10;
            float air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;
            airf = air/TimeWarp.fixedDeltaTime*resourcedensity;
            //part.RequestResource("IntakeAtm",-air);
			supplyFNResource(air,FNResourceManager.FNRESOURCE_INTAKEATM);
        }

        public float getAtmosphericOutput() {
            return airf;
        }
    }
}
