using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace FNPlugin {
	class FNRadiator : FNResourceSuppliableModule	{
		[KSPField(isPersistant = true)]
		bool IsEnabled;
		[KSPField(isPersistant = false)]
		public string animName;
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

		public const double stefan_const = 5.6704e-8;
		public const float h = 1000;

		protected const string emissive_property_name = "_Emission";

		protected Animation anim;
		protected float radiatedThermalPower;
		protected float convectedThermalPower;
		protected float current_rad_temp;
		protected float myScience = 0;
		protected Vector3 oldrot;
		protected float directionrotate = 1;
		protected float oldangle = 0;
		protected Vector3 original_eulers;
		protected Transform pivot;
		protected Texture orig_emissive_colour;


		[KSPEvent(guiActive = true, guiName = "Deploy Radiator", active = true)]
		public void DeployRadiator() {
			anim [animName].speed = 1f;
			anim [animName].normalizedTime = 0f;
			anim.Blend (animName, 2f);
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Retract Radiator", active = false)]
		public void RetractRadiator() {
			anim [animName].speed = -1f;
			anim [animName].normalizedTime = 1f;
			anim.Blend (animName, 2f);
			IsEnabled = false;
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitRadiator() {
			if (isupgraded || myScience < upgradeCost) { return; }

			isupgraded = true;
			radiatorType = upgradedName;
			radiatorTemp = upgradedRadiatorTemp;
			radiatorTempStr = radiatorTemp + "K";
		}

		[KSPAction("Deploy Radiator")]
		public void DeployRadiatorAction(KSPActionParam param) {
			DeployRadiator();
		}

		[KSPAction("Retract Radiator")]
		public void RetractRadiatorAction(KSPActionParam param) {
			RetractRadiator();
		}

		[KSPAction("Toggle Radiator")]
		public void ToggleRadiatorAction(KSPActionParam param) {
			if (IsEnabled) {
				RetractRadiator();
			} else {
				DeployRadiator();
			}
		}

		public override void OnStart(PartModule.StartState state) {
			Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = String.Format("Deploy Radiator");
			Actions["RetractRadiatorAction"].guiName = Events["RetractRadiator"].guiName = String.Format("Retract Radiator");
			Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");

			if (state == StartState.Editor) { return; }
			this.part.force_activate();

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			//orig_emissive_colour = part.renderer.material.GetTexture (emissive_property_name);
			if (anim != null) {
				anim [animName].layer = 1;
				if (!IsEnabled) {
					anim [animName].normalizedTime = 1f;
					anim [animName].speed = -1f;

				} else {
					anim [animName].normalizedTime = 0f;
					anim [animName].speed = 1f;

				}
				anim.Play ();
			}

			pivot = part.FindModelTransform("suntransform");
			original_eulers = pivot.transform.localEulerAngles;

			if (!isupgraded) {
				radiatorType = originalName;
			} else {
				radiatorType = upgradedName;
				radiatorTemp = upgradedRadiatorTemp;
			}
			radiatorTempStr = radiatorTemp + "K";
		}

		public override void OnUpdate() {
			Events["DeployRadiator"].active = !IsEnabled;
			Events["RetractRadiator"].active = IsEnabled;
			Events["RetrofitRadiator"].active = !isupgraded && myScience >= upgradeCost;
			Fields["upgradeCostStr"].guiActive = !isupgraded;

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
			if (IsEnabled) {
				float thermal_power_dissip = (float)(stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e6) * TimeWarp.fixedDeltaTime;
				radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

				current_rad_temp = (float) (Math.Min(Math.Pow (radiatedThermalPower*1e6 / (stefan_const * radiatorArea), 0.25),radiatorTemp));
				current_rad_temp = Mathf.Max(current_rad_temp,FlightGlobals.getExternalTemperature((float)vessel.altitude,vessel.mainBody)+273.16f);

				Vector3 pivrot = pivot.rotation.eulerAngles;

				pivot.Rotate (Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

				Vector3 sunpos = FlightGlobals.Bodies [0].transform.position;
				Vector3 flatVectorToTarget = sunpos - transform.position;

				flatVectorToTarget = flatVectorToTarget.normalized;
				float dot = Mathf.Asin(Vector3.Dot (pivot.transform.right, flatVectorToTarget))/Mathf.PI*180.0f;

				float anglediff = -dot;
				oldangle = dot;
				//print (dot);
				directionrotate = anglediff / 5 /TimeWarp.fixedDeltaTime;
				directionrotate = Mathf.Min (3, directionrotate);
				directionrotate = Mathf.Max (-3, directionrotate);
			
				part.maximum_drag = 0.8f;
				part.minimum_drag = 0.8f;

			} else {
				pivot.transform.localEulerAngles = original_eulers;

				float thermal_power_dissip = (float)(stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e7) * TimeWarp.fixedDeltaTime;
				radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

				current_rad_temp = (float) (Math.Min(Math.Pow (radiatedThermalPower*1e6 / (stefan_const * radiatorArea), 0.25),radiatorTemp));
				current_rad_temp = Mathf.Max(current_rad_temp,FlightGlobals.getExternalTemperature((float)vessel.altitude,vessel.mainBody)+273.16f);

				part.maximum_drag = 0.2f;
				part.minimum_drag = 0.2f;
			}

			float atmosphere_height = vessel.mainBody.maxAtmosphereAltitude;
			float vessel_height = (float) vessel.mainBody.GetAltitude (vessel.transform.position);
			float conv_power_dissip = 0;
			if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
				float pressure = (float) FlightGlobals.getStaticPressure (vessel.transform.position);
				float dynamic_pressure = (float) (0.5*pressure*1.2041*vessel.srf_velocity.sqrMagnitude/101325.0);
				pressure += dynamic_pressure;
				float low_temp = FlightGlobals.getExternalTemperature (vessel.transform.position);

				float delta_temp = Mathf.Max(0,radiatorTemp - low_temp);
				conv_power_dissip = pressure * delta_temp * radiatorArea * h/1e6f * TimeWarp.fixedDeltaTime;
				if (!IsEnabled) {
					conv_power_dissip = conv_power_dissip / 2.0f;
				}
				convectedThermalPower = consumeFNResource (conv_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

				if (IsEnabled && dynamic_pressure > 1.4854428818159388107574636072046e-3) {
					part.deactivate();

					//part.breakingForce = 1;
					//part.breakingTorque = 1;
					part.decouple();
				}
			}

		}

		public override string GetInfo() {
			float thermal_power_dissip = (float)(stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e6);
			float thermal_power_dissip2 = (float)(stefan_const * radiatorArea * Math.Pow (upgradedRadiatorTemp, 4) / 1e6);
			return String.Format("Waste Heat Radiated\n Present: {0} MW\n After Upgrade: {1} MW\n Upgrade Cost: {2} Science", thermal_power_dissip,thermal_power_dissip2,upgradeCost);
		}

	}
}

