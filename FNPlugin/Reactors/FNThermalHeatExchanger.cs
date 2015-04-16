using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin  {
	class FNThermalHeatExchanger : FNResourceSuppliableModule, IThermalSource {
        //Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled = true;

        //Persistent False
        [KSPField(isPersistant = false)]
        public float radius;
        [KSPField(isPersistant = false)]
        public float heatTransportationEfficiency = 0.7f;

        //GUI
		[KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
		public string thermalpower;

        // internal
		protected float _thermalpower;


        // reference types
        protected Dictionary<Guid, float> connectedRecievers = new Dictionary<Guid, float>();
        protected Dictionary<Guid, float> connectedRecieversFraction = new Dictionary<Guid, float>();
        protected float connectedRecieversSum;

        public void AttachThermalReciever(Guid key, float radius)
        {
            try
            {
                UnityEngine.Debug.Log("[KSPI] - InterstellarReactor.ConnectReciever: Guid: " + key + " radius: " + radius);

                if (!connectedRecievers.ContainsKey(key))
                {
                    connectedRecievers.Add(key, radius);
                    connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                    connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
                }
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError("[KSPI] - InterstellarReactor.ConnectReciever exception: " + error.Message);
            }
        }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Remove(key);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }
        }

        public float GetFractionThermalReciever(Guid key)
        {
            float result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        //properties

        public float ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public bool IsSelfContained { get { return false; } }

        public float CoreTemperature { get { return 1500; } }

        public float StableMaximumThermalPower { get { return MaximumThermalPower; } }

        public float MaximumPower { get { return MaximumThermalPower; } }

        public float MaximumThermalPower { get { return _thermalpower; } }

        public float MinimumPower { get { return 0; } }

        public bool IsVolatileSource { get { return false; } }

        public bool IsActive { get { return IsEnabled; } }

        public bool IsNuclear { get { return false; } }

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
            _thermalpower = getStableResourceSupply(FNResourceManager.FNRESOURCE_THERMALPOWER) / activeExchangers;
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

            thermalpower = _thermalpower.ToString() + "MW";
		}

		public override void OnFixedUpdate() {
			base.OnFixedUpdate ();
			setupThermalPower ();
		}

		public float getCoreTemp() {
            return 1500;
		}

        public float GetCoreTempAtRadiatorTemp(float rad_temp) {
            return 1500;
        }

        public float GetThermalPowerAtTemp(float temp) {
            return _thermalpower;
        }

		public float getThermalPower() {
			return _thermalpower;
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

