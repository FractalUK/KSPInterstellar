using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin  {
	class FNThermalHeatExchanger : FNResourceSuppliableModule {
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
		public string thermalpower;

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

		float availableThermalPower = 0;
		int activeExchangers = 0;

		public void setupThermalPower(){
			//skip calculations on other vessels
			if (vessel != FlightGlobals.ActiveVessel)
			{
				return;
			}

			availableThermalPower = 0;
			activeExchangers = 0;

			List<FNThermalHeatExchanger> mthes = vessel.FindPartModulesImplementing<FNThermalHeatExchanger>();
			foreach (FNThermalHeatExchanger mthe in mthes) {
				if (mthe.IsEnabled == true) {
					activeExchangers++;
				}
			}

			List<MicrowavePowerReceiver> mprs = vessel.FindPartModulesImplementing<MicrowavePowerReceiver>();
			foreach (MicrowavePowerReceiver mpr in mprs) {
				if (mpr.isThermalReciever) {
					if (mpr.getMegajoules () > 0 && activeExchangers > 0) {
						availableThermalPower += mpr.getMegajoules () / activeExchangers;
					}
				}
			}

			List<FNReactor> fnrs = vessel.FindPartModulesImplementing<FNReactor>();
			foreach (FNReactor fnr in fnrs) {
				if (fnr.IsEnabled) {
					if (fnr.getReactorThermalPower() > 0 && activeExchangers > 0) {
						availableThermalPower += fnr.getReactorThermalPower() / activeExchangers;
					}
				}
			}

			//availableThermalPower = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES) / activeExchangers;
			//availableThermalPower = (getStableResourceSupply(FNResourceManager.FNRESOURCE_THERMALPOWER) / activeExchangers) * TimeWarp.fixedDeltaTime;

			thermalpower = availableThermalPower.ToString () + "MW";
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

			setupThermalPower ();
		}

		public override void OnUpdate() {
			Events["ActivateHeatExchanger"].active = !IsEnabled;
			Events["DeactivateHeatExchanger"].active = IsEnabled;
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			setupThermalPower ();
			//supplyFNResource(availableThermalPower * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_THERMALPOWER);
			//consumeFNResource(availableThermalPower * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
		}

		public float getRadius() {
			return radius;
		}

		public float getThermalPower() {
			return availableThermalPower;
		}

	}
}

