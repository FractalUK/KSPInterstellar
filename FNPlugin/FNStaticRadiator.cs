using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
	class FNStaticRadiator : FNResourceSuppliableModule{
		[KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
		public string radiatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
		public string radiatorTempStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Radiated")]
		public string thermalPowerDissipStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Convected")]
		public string thermalPowerConvStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
		public string upgradeCostStr;

		[KSPField(isPersistant = false)]
		public float radiatorTemp;
		[KSPField(isPersistant = false)]
		public float radiatorArea;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = true)]
		public bool isupgraded = false;
		[KSPField(isPersistant = false)]
		public float upgradeCost = 100;
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public float upgradedRadiatorTemp;

		protected float radiatedThermalPower;
		protected float convectedThermalPower;
		protected float myScience = 0;
		protected float current_rad_temp = 0;

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitRadiator() {
			if (isupgraded || myScience < upgradeCost) { return; }

			isupgraded = true;
			radiatorType = upgradedName;
			radiatorTemp = upgradedRadiatorTemp;
			radiatorTempStr = radiatorTemp + "K";
		}

		public override void OnStart(PartModule.StartState state) {

			if (state == StartState.Editor) { return; }
			this.part.force_activate();

			if (!isupgraded) {
				radiatorType = originalName;
			} else {
				radiatorType = upgradedName;
				radiatorTemp = upgradedRadiatorTemp;
			}
			radiatorTempStr = radiatorTemp + "K";
		}

		public override void OnUpdate() {
			Events["RetrofitRadiator"].active = !isupgraded && myScience >= upgradeCost;

			thermalPowerDissipStr = radiatedThermalPower.ToString ("0.000") + "MW";
			thermalPowerConvStr = convectedThermalPower.ToString ("0.000") + "MW";
			radiatorTempStr = current_rad_temp.ToString("0.0") + "K / " + radiatorTemp.ToString("0.0") + "K";

			float currentscience = 0;
			List<PartResource> partresources = new List<PartResource>();
			part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition("Science").id, partresources);
			foreach (PartResource partresource in partresources) {
				currentscience += (float)partresource.amount;
			}

			myScience = currentscience;
			upgradeCostStr = currentscience.ToString("0") + "/" + upgradeCost.ToString("0") + " Science";
		}

		public override void OnFixedUpdate() {
			float thermal_power_dissip = (float)(FNRadiator.stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e6) * TimeWarp.fixedDeltaTime;
			radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

			current_rad_temp = (float) (Math.Min(Math.Pow (radiatedThermalPower*1e6 / (FNRadiator.stefan_const * radiatorArea), 0.25),radiatorTemp));
			current_rad_temp = Mathf.Max(current_rad_temp,FlightGlobals.getExternalTemperature((float)vessel.altitude,vessel.mainBody)+273.16f);

			float vessel_height = (float) vessel.mainBody.GetAltitude (vessel.transform.position);
			float conv_power_dissip = 0;
			if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude (vessel.mainBody)) {
				float pressure = (float)FlightGlobals.getStaticPressure (vessel.transform.position);
				float dynamic_pressure = (float)(0.5 * pressure * 1.2041 * vessel.srf_velocity.sqrMagnitude / 101325.0);
				pressure += dynamic_pressure;
				float low_temp = FlightGlobals.getExternalTemperature (vessel.transform.position);

				float delta_temp = Mathf.Max (0, radiatorTemp - low_temp);
				conv_power_dissip = pressure * delta_temp * radiatorArea * FNRadiator.h / 1e6f * TimeWarp.fixedDeltaTime * 20.0f;

				convectedThermalPower = consumeFNResource (conv_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
			}
		}

		public override string GetInfo() {
			float thermal_power_dissip = (float)(FNRadiator.stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e6);
			float thermal_power_dissip2 = (float)(FNRadiator.stefan_const * radiatorArea * Math.Pow (upgradedRadiatorTemp, 4) / 1e6);
			return String.Format("Waste Heat Radiated\n Present: {0} MW\n After Upgrade: {1} MW\n Upgrade Cost: {2} Science", thermal_power_dissip,thermal_power_dissip2,upgradeCost);
		}


	}
}

