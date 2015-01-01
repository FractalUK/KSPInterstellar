using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	class FNSolarPanelWasteHeatModule : FNResourceSuppliableModule {
		protected float wasteheat_production_f = 0;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
		public string heatProductionStr = ":";

        protected ModuleDeployableSolarPanel solarPanel;
        private bool active = false;

		public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;
			base.OnStart (state);
			if (state == StartState.Editor) { return; }
			solarPanel = (ModuleDeployableSolarPanel)this.part.Modules["ModuleDeployableSolarPanel"];
		}

		public override void OnUpdate() {
			heatProductionStr = wasteheat_production_f.ToString ("0.00") + " KW";
		}

        public override void OnFixedUpdate() {
            active = true;
            base.OnFixedUpdate();
        }

		public void FixedUpdate() {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!active)
                {
                    base.OnFixedUpdate();
                }

                if (solarPanel != null)
                {
                    float solar_rate = solarPanel.flowRate * TimeWarp.fixedDeltaTime;
                    float heat_rate = solar_rate * 0.5f / 1000.0f;

                    double inv_square_mult = Math.Pow(Vector3d.Distance(FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2) / Math.Pow(Vector3d.Distance(vessel.transform.position, FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position), 2);
                    FloatCurve satcurve = new FloatCurve();
                    satcurve.Add(0.0f, (float)inv_square_mult);
                    solarPanel.powerCurve = satcurve;

                    if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.98 && solarPanel.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED && solarPanel.sunTracking)
                    {
                        solarPanel.Retract();
                        if (FlightGlobals.ActiveVessel == vessel)
                        {
                            ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Solar Panel retraction occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        }
                        return;
                    }

                    List<PartResource> prl = part.GetConnectedResources("ElectricCharge").ToList();
                    double current_charge = prl.Sum(pr => pr.amount);
                    double max_charge = prl.Sum(pr => pr.maxAmount);

                    supplyFNResourceFixedMax(current_charge >= max_charge ? solar_rate / 1000.0f : 0, solar_rate / 1000.0f, FNResourceManager.FNRESOURCE_MEGAJOULES);
                    wasteheat_production_f = supplyFNResource(heat_rate, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime * 1000.0f;
                }
            }
		}
	}
}

