using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Dissipated")]
		public string thermalPowerDissipStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
		public string upgradeCostStr;

		public const double stefan_const = 5.6704e-8;

		protected Animation anim;
		protected float radiatedThermalPower;
		protected float myScience = 0;
		protected Vector3 oldrot;
		protected float directionrotate = 1;
		protected float oldangle = 0;
		protected Vector3 original_eulers;
		protected Transform pivot;

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

				//Transform pivot = part.FindModelTransform("suntransform");
				Vector3 pivrot = pivot.rotation.eulerAngles;
				//print("x:" + pivrot.x + " y:" + pivrot.y + " z:" + pivrot.z);

				pivot.Rotate (Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

				//Vector3 lookdirection = Vector3.Cross (FlightGlobals.Bodies [0].transform.position, pivot.right);
				//pivot.LookAt (lookdirection);
				float x = 0;
				float y = 0;
				float z = 0;

				Vector3 sunpos = FlightGlobals.Bodies [0].transform.position;
				//sunpos.y = 0;
				Vector3 flatVectorToTarget = sunpos - transform.position;
				/*
				flatVectorToTarget.y = 0;
				flatVectorToTarget = flatVectorToTarget.normalized;

				Vector3 flatFacing = transform.up;
				flatFacing.y = 0;
				flatFacing = flatFacing.normalized;

				//var newRotation = Quaternion.LookRotation (flatVectorToTarget.normalized);
				float dot = Mathf.Acos(Vector3.Dot (pivot.transform.forward, Vector3.up))/Mathf.PI*180.0f;


				float dot2 = Mathf.Acos(Vector3.Dot (flatFacing, flatVectorToTarget))/Mathf.PI*180.0f;
				print (dot);

				if (dot >= dot2-1.5f && dot <=dot2+1.5f) {
					directionrotate = 0;
				} else {
					directionrotate = 1;
				}
				*/
				flatVectorToTarget = flatVectorToTarget.normalized;
				float dot = Mathf.Asin(Vector3.Dot (pivot.transform.right, flatVectorToTarget))/Mathf.PI*180.0f;

				float anglediff = -dot;
				oldangle = dot;
				print (dot);
				directionrotate = anglediff / 5 /TimeWarp.fixedDeltaTime;
				directionrotate = Mathf.Min (3, directionrotate);
				directionrotate = Mathf.Max (-3, directionrotate);
			
				//pivot.Rotate (Vector3.up * dot);
				//pivot.localEulerAngles = new Vector3 (dot, pivot.localEulerAngles.y, pivot.localEulerAngles.z);


				//Vector3 lookdirection = newRotation.eulerAngles;
				//lookdirection.z = transform.eulerAngles.z;
				//pivot.eulerAngles = lookdirection;
				//pivot.rotation.
				//pivot.eulerAngles = new Vector3 (lookdirection.x, lookdirection.y, 0);
				//
				//Vector3 oldlocaleulers = pivot.transform.localEulerAngles;
				//pivot.rotation = newRotation;
				//pivot.localEulerAngles = new Vector3 (pivot.localEulerAngles.x, oldlocaleulers.y, oldlocaleulers.z);

				//pivot.eulerAngles = new Vector3(0, newRotation.eulerAngles.y, newRotation.eulerAngles.z);
				//pivot.eulerAngles = new Vector3(180, 180, 180);
				//
				//pivot.transform.eulerAngles = new Vector3(newRotation.eulerAngles.x,newRotation.eulerAngles.y,newRotation.eulerAngles.z);
				//pivot.transform.localEulerAngles = new Vector3(pivot.transform.localEulerAngles.x, oldlocaleulers.y, oldlocaleulers.z);
				//pivot.eulerAngles = new Vector3(0,pivot.transform.eulerAngles.y,pivot.transform.eulerAngles.z);

				//newRotation.x = 0;
				//newRotation.y = 0;
				//pivot.rotation = newRotation;

				//Quaternion baseRotation = transform.rotation;
				//pivot.rotation = baseRotation*newRotation;

				//pivot.eulerAngles = new Vector3 (0, 0, 0);

				//print("x:" + pivot.InverseTransformDirection(transform.up).x + " y:" + pivot.InverseTransformDirection(transform.up).y + " z:" + pivot.InverseTransformDirection(transform.up).z);
				//print("x:" + pivot.eulerAngles.x + " y:" + pivot.eulerAngles.y + " z:" + pivot.eulerAngles.z);
				if (pivot.right.z > -0.1 && pivot.right.z < 0.1) {
					//directionrotate = 0;
				} else {
					//directionrotate = 1;
				}


			} else {
				pivot.transform.localEulerAngles = original_eulers;

				float thermal_power_dissip = (float)(stefan_const * radiatorArea * Math.Pow (radiatorTemp, 4) / 1e7) * TimeWarp.fixedDeltaTime;
				radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
			}

		}

	}
}

