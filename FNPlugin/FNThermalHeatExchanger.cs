using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin  {
	class FNThermalHeatExchanger : FNResourceSuppliableModule, FNThermalSource {
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
		public string thermalpower;

		[KSPField(isPersistant = true)]
		public bool IsEnabled = false;
		[KSPField(isPersistant = false)]
		public float ThermalPower;
		[KSPField(isPersistant = false)]
		public float ThermalTemp;
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

		//int activeExchangers = 0;
		int activeThermalEngines = 0;

		public void setupThermalPower(){
			if (vessel != FlightGlobals.ActiveVessel)
			{
				return;
			}
			ThermalPower = 0;
			//activeExchangers = 0;
			activeThermalEngines = 0;

			/*List<FNThermalHeatExchanger> thes = vessel.FindPartModulesImplementing<FNThermalHeatExchanger>();
			foreach (FNThermalHeatExchanger the in thes) {
				if (the.IsEnabled == true) {
					activeExchangers++;
				}
			}*/

			List<FNNozzleController> fnncs = vessel.FindPartModulesImplementing<FNNozzleController>();
			foreach (FNNozzleController fnnc in fnncs) {
				ModuleEngines me = fnnc.part.Modules["ModuleEngines"] as ModuleEngines;
				if (me != null) {
					if (me.isOperational == true) {
						activeThermalEngines++;
					}
				}
			}

			List<FNThermalSource> fntss = vessel.FindPartModulesImplementing<FNThermalSource>();
			foreach (FNThermalSource fnts in fntss) {
				if (fnts.getThermalPower() > 0 && activeThermalEngines > 0 && !fnts.getIsThermalHeatExchanger()) {
					ThermalPower += fnts.getThermalPower() / activeThermalEngines;
				}
			}

			if (ThermalPower > 1500) { ThermalTemp = 1500; } else { ThermalTemp = ThermalPower; }

			thermalpower = ThermalPower.ToString () + "MW";

			print ("ActiveThermalEngines : " + activeThermalEngines + " ThermalPower " + ThermalPower);
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
			//supplyFNResource(ThermalPower * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_THERMALPOWER);
			//consumeFNResource(ThermalPower * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_THERMALPOWER);
		}

		public float getThermalTemp() {
			return ThermalTemp;
		}

		public float getThermalPower() {
			return ThermalPower;
		}

		public bool getIsNuclear() {
			return false;
		}

		public float getRadius() {
			return radius;
		}

		public bool isActive() {
			return IsEnabled;
		}

		public bool getIsThermalHeatExchanger() {
			return true;
		}


		public void enableIfPossible() {
			if (!IsEnabled) {
				IsEnabled = true;
			}
		}

	}
}

