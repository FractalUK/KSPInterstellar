using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
	class FNSolarPanelWasteHeatModule : FNResourceSuppliableModule 
    {
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true,  guiName = "Solar Power", guiUnits = " MW", guiFormat="F5")]
        public float megaJouleSolarPowerSupply;

		public string heatProductionStr = ":";

        protected ModuleDeployableSolarPanel solarPanel;
        private bool active = false;

		public override void OnStart(PartModule.StartState state) 
        {
			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_MEGAJOULES};
			this.resources_to_supply = resources_to_supply;
			base.OnStart (state);
			if (state == StartState.Editor) { return; }
			solarPanel = (ModuleDeployableSolarPanel)this.part.Modules["ModuleDeployableSolarPanel"];
		}

        public override void OnFixedUpdate() 
        {
            active = true;
            base.OnFixedUpdate();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!active)
                base.OnFixedUpdate();

            if (solarPanel == null) return;

            float solar_rate = solarPanel.flowRate * TimeWarp.fixedDeltaTime;

            List<PartResource> prl = part.GetConnectedResources("ElectricCharge").ToList();
            double current_charge = prl.Sum(pr => pr.amount);
            double max_charge = prl.Sum(pr => pr.maxAmount);

            var solar_supply = current_charge >= max_charge ? solar_rate / 1000.0f : 0;
            var solar_maxSupply = solar_rate / 1000.0f;

            megaJouleSolarPowerSupply = supplyFNResourceFixedMax(solar_supply, solar_maxSupply, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
        }
	}
}

