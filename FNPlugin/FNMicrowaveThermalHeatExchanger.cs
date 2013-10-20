using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin  {
	class FNMicrowaveThermalHeatExchanger : FNResourceSuppliableModule {
		[KSPField(isPersistant = true)]
		public bool IsEnabled = true;
		[KSPField(isPersistant = false)]
		public float radius;

		[KSPEvent(guiActive = true, guiName = "Activate Heat Exchanger", active = false)]
		public void ActivateHeatExchanger() {
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Deactivate Heat Exchanger", active = true)]
		public void DeactivateHeatExchanger() {
			IsEnabled = false;
		}

		[KSPAction("Activate Heat Exchanger")]
		public void ActivateHeatExchangerAction(KSPActionParam param) {
			ActivateHeatExchanger();
		}

		[KSPAction("Deactivate Heat Exchanger")]
		public void DeactivateHeatExchangerAction(KSPActionParam param) {
			DeactivateHeatExchanger();
		}

		[KSPAction("Toggle Heat Exchanger")]
		public void ToggleHeatExchangerAction(KSPActionParam param) {
			IsEnabled = !IsEnabled;
		}

		float availableMegajoules = 0;
		int activeExchangers = 0;

		public void setupMicrowaveThermalPower(){
			//skip calculations on other vessels
			if (vessel != FlightGlobals.ActiveVessel)
			{
				return;
			}

			availableMegajoules = 0;
			activeExchangers = 0;

			List<FNMicrowaveThermalHeatExchanger> mthes = vessel.FindPartModulesImplementing<FNMicrowaveThermalHeatExchanger>();
			foreach (FNMicrowaveThermalHeatExchanger mthe in mthes) {
				if (mthe.IsEnabled == true) {
					activeExchangers++;
				}
			}

			/*List<MicrowavePowerReceiver> mprs = vessel.FindPartModulesImplementing<MicrowavePowerReceiver>();
			foreach (MicrowavePowerReceiver mpr in mprs) {
				if (mpr.getMegajoules () > 0 && activeExchangers > 0) {
					availableMegajoules = mpr.getMegajoules () / activeExchangers;
				} else {
					availableMegajoules = 0;
				}
			}*/

			availableMegajoules = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) / activeExchangers;
		}

		public override void OnStart(PartModule.StartState state) {
			Actions["ActivateHeatExchangerAction"].guiName = Events["ActivateHeatExchanger"].guiName = String.Format("Activate Heat Exchanger");
			Actions["DeactivateHeatExchangerAction"].guiName = Events["DeactivateHeatExchanger"].guiName = String.Format("Deactivate Heat Exchanger");
			Actions["ToggleHeatExchangerAction"].guiName = String.Format("Toggle Heat Exchanger");

			String[] resources_to_supply = {FNResourceManager.FNRESOURCE_THERMALPOWER};
			this.resources_to_supply = resources_to_supply;

			base.OnStart (state);

			if (state == StartState.Editor) { return; }
			this.part.force_activate();

			setupMicrowaveThermalPower ();
		}

		public override void OnUpdate() {
			Events["ActivateHeatExchanger"].active = !IsEnabled;
			Events["DeactivateHeatExchanger"].active = IsEnabled;
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			setupMicrowaveThermalPower ();
			supplyFNResource(availableMegajoules * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_THERMALPOWER);
			consumeFNResource(availableMegajoules * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
		}

		public float getRadius() {
			return radius;
		}

		public float getThermalPower() {
			return availableMegajoules;
		}

	}
}

