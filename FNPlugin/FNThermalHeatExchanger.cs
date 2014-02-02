using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin  {
	class FNThermalHeatExchanger : FNResourceSuppliableModule, FNThermalSource {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;

        //Persistent False
        [KSPField(isPersistant = false)]
        public float radius;

        //GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
		public string thermalpower;

        // internal
		protected float ThermalPower;

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

		int activeExchangers = 0;

		public void setupThermalPower(){
			activeExchangers = FNThermalHeatExchanger.getActiveExchangersForVessel(vessel);
            ThermalPower = getStableResourceSupply(FNResourceManager.FNRESOURCE_THERMALPOWER) / activeExchangers;
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

            thermalpower = ThermalPower.ToString() + "MW";
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			setupThermalPower ();
		}

		public float getCoreTemp() {
            return 1500;
		}

        public float getCoreTempAtRadiatorTemp(float rad_temp) {
            return 1500;
        }

        public float getThermalPowerAtTemp(float temp) {
            return ThermalPower;
        }

		public float getThermalPower() {
			return ThermalPower;
		}

        public float getChargedPower() {
            return 0;
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

        public void enableIfPossible() {
            IsEnabled = true;
        }

        public bool shouldScaleDownJetISP() {
            return false;
        }

        public bool isVolatileSource() {
            return false;
        }

        public float getMinimumThermalPower() {
            return 0;
        }

        public static int getActiveExchangersForVessel(Vessel vess) {
            int activeExchangers = 0;
            List<FNThermalHeatExchanger> mthes = vess.FindPartModulesImplementing<FNThermalHeatExchanger>();
            foreach (FNThermalHeatExchanger mthe in mthes) {
                if (mthe.isActive()) {
                    activeExchangers++;
                }
            }
            return activeExchangers;
        }

	}
}

