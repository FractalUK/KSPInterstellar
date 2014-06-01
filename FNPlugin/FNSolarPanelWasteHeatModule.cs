using KSP;
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

		[KSPField(isPersistant = false, guiActive = true, guiName = "Energy Flow", guiUnits = "KW", guiFormat = "F2")]
		public float energyFlow;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Efficiency", guiFormat = "P0")]
		public float panelEfficiency = 2f / 3f;

		protected float chargeRate;

        protected ModuleDeployableSolarPanel solarPanel;

		public override void OnStart(PartModule.StartState state) {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);

			if (state == StartState.Editor) { return; }

            isEnabled = true;
			solarPanel = (ModuleDeployableSolarPanel)this.part.Modules["ModuleDeployableSolarPanel"];
			if (solarPanel != null)
			{
				ModuleDeployableSolarPanel panelPrefab = PartLoader.getPartInfoByName(this.part.partInfo.name).partPrefab.Modules["ModuleDeployableSolarPanel"] as ModuleDeployableSolarPanel;
				solarPanel.Fields["flowRate"].guiActive = false;
				this.chargeRate = panelPrefab.chargeRate * 1.5f;
				solarPanel.chargeRate = 0f;
			}
		}

		public void Update() {
			heatProductionStr = wasteheat_production_f.ToString ("0.00") + " KW";
		}

		public void FixedUpdate() {
			if (HighLogic.LoadedSceneIsFlight && solarPanel != null) {
				base.OnFixedUpdate();
				double kerbinToKerbolSqr = (FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBIN].transform.position - FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position).sqrMagnitude;
				double vesselToKerbolSqr = (vessel.transform.position - FlightGlobals.Bodies[PluginHelper.REF_BODY_KERBOL].transform.position).sqrMagnitude;
				float inv_square_mult = (float)(kerbinToKerbolSqr / vesselToKerbolSqr);

				float solar_rate = this.chargeRate * TimeWarp.fixedDeltaTime * solarPanel.sunAOA * inv_square_mult;
				float heat_rate = solar_rate * (1f - panelEfficiency) / 1000.0f;
				float power_rate = solar_rate * panelEfficiency / 1000.0f;
				float max_rate = power_rate / solarPanel.sunAOA;

				if (getResourceBarRatio (FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.98 && solarPanel.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED && solarPanel.sunTracking) {
					solarPanel.Retract ();
					if (FlightGlobals.ActiveVessel == vessel) {
						ScreenMessages.PostScreenMessage ("Warning Dangerous Overheating Detected: Solar Panel retraction occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					}
					return;
				}

				wasteheat_production_f = supplyFNResource(heat_rate,FNResourceManager.FNRESOURCE_WASTEHEAT)/TimeWarp.fixedDeltaTime*1000.0f;
				energyFlow = supplyFNResourceFixedMax(power_rate, max_rate, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime * 1000f;
			}
		}
	}
}

