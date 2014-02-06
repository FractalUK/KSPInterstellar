using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ModuleSabreHeating : PartModule {
        [KSPField(isPersistant = true)]
        public bool IsEnabled;

        protected ModuleEnginesFX rapier_engine = null;
        protected ModuleEngines rapier_engine2 = null;
        protected int pre_coolers_active = 0;
        protected int intakes_open = 0;
        protected double proportion = 0;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            List<ModuleEnginesFX> mefxs = part.FindModulesImplementing<ModuleEnginesFX>().Where(e => e.engineID == "AirBreathing").ToList();
            List<ModuleEngines> mes = part.FindModulesImplementing<ModuleEngines>().ToList();
            if (mefxs.Count > 0) {
                rapier_engine = mefxs.First();
            }
            if (mes.Count > 0) {
                rapier_engine2 = mes.First();
            }

        }

        public override void OnUpdate() {
            if (rapier_engine != null) {
                if (rapier_engine.isOperational && !IsEnabled) {
                    IsEnabled = true;
                    part.force_activate();
                }
            }

            if (rapier_engine2 != null) {
                if (rapier_engine2.isOperational && !IsEnabled) {
                    IsEnabled = true;
                    part.force_activate();
                }
            }
        }

        public override void OnFixedUpdate() {
            try {
                pre_coolers_active = vessel.FindPartModulesImplementing<FNModulePreecooler>().Where(prc => prc.isFunctional()).Count();
                intakes_open = vessel.FindPartModulesImplementing<ModuleResourceIntake>().Where(mre => mre.intakeEnabled).Count();
                double proportion = Math.Pow((double)(intakes_open - pre_coolers_active) / (double)intakes_open, 0.1);
                if (double.IsNaN(proportion) || double.IsInfinity(proportion)) {
                    proportion = 1;
                }

                if (rapier_engine != null) {
                    if (rapier_engine.isOperational && rapier_engine.currentThrottle > 0 && rapier_engine.useVelocityCurve) {
                        part.temperature = (float)Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * proportion, 1);
                    } else {
                        part.temperature = 1;
                    }
                }

                if (rapier_engine2 != null) {
                    if (rapier_engine2.isOperational && rapier_engine2.currentThrottle > 0 && rapier_engine2.useVelocityCurve) {
                        part.temperature = (float)Math.Max((Math.Sqrt(vessel.srf_velocity.magnitude) * 20.0 / GameConstants.atmospheric_non_precooled_limit) * part.maxTemp * proportion, 1);
                    } else {
                        part.temperature = 1;
                    }
                }
            } catch (Exception ex) {
                Debug.Log("[KSP Interstellar] ModuleSabreHeating threw Exception in OnFixedUpdate(): " + ex);
            }
        }
    }
}
