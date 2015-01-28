extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

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

		protected PartResource _intake_atm = null;

        public override void OnStart(PartModule.StartState state) {
            _intake_atm = part.Resources.Contains(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere) ? part.Resources[InterstellarResourcesConfiguration.Instance.IntakeAtmosphere] : null;
        }

        public override void OnUpdate() {
            intakeval = airf.ToString("0.00") + " kg";
        }

        public void FixedUpdate() {
            if (HighLogic.LoadedSceneIsFlight && _intake_atm != null)
            {
                double resourcedensity = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere).density;
                double airdensity = part.vessel.atmDensity / 1000;
                double airspeed = part.vessel.srf_velocity.magnitude + 100.0;
                double air = airspeed * airdensity * area / resourcedensity * TimeWarp.fixedDeltaTime;
                airf = (float)(1000.0 * air / TimeWarp.fixedDeltaTime * resourcedensity);

                air = _intake_atm.amount = Math.Min(air / TimeWarp.fixedDeltaTime, _intake_atm.maxAmount);
                part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere, -air);
            }
        }
    }
}
