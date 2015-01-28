using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ElectricRCSController : FNResourceSuppliableModule {
        //persistant false
        [KSPField(isPersistant = false)]
        public float maxThrust;

        //Config settings settings
        protected double g0 = PluginHelper.GravityConstant;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";

        // internal
        protected ModuleRCS attachedRCS;
        protected float electrical_consumption_f = 0;
        protected float heat_production_f = 0;

        public override void OnStart(PartModule.StartState state) {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            attachedRCS = this.part.Modules["ModuleRCS"] as ModuleRCS;
            this.resources_to_supply = resources_to_supply;
            base.OnStart(state);
            if (state == StartState.Editor) return;
        }

        public override void OnUpdate() {
            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                electricalPowerConsumptionStr = electrical_consumption_f.ToString("0.00") + " MW";
                heatProductionStr = heat_production_f.ToString("0.00") + " MW";
            } else {
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
            }
        }

        public void FixedUpdate() {
            if (attachedRCS != null && HighLogic.LoadedSceneIsFlight && vessel.ActionGroups[KSPActionGroup.RCS]) {
                double total_thrust = attachedRCS.thrustForces.Sum(frc => frc);
                float curve_eval_point = (float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position), 1.0);
                double currentIsp = attachedRCS.atmosphereCurve.Evaluate(curve_eval_point);

                double power_required = total_thrust * currentIsp * g0 * 0.5 / 1000.0;
                double power_received = consumeFNResource(power_required * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                electrical_consumption_f = (float)power_received;
                double power_ratio = power_required > 0 ? Math.Min(power_received / power_required, 1.0) : 1;
                attachedRCS.thrusterPower = Mathf.Max(maxThrust * ((float)power_ratio), 0.0001f);
                float thrust_ratio = Mathf.Min(Mathf.Min((float)power_ratio, (float)(total_thrust / maxThrust)), 1.0f)*0.125f;
            }
        }

        public override string getResourceManagerDisplayName() {
            return "Electrical Reaction Control System";
        }
    }
}
