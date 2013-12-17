using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
	class ModuleSolarSail : PartModule 	{
		// How much thrust will be gain when certain amount of light is projected onto the panel.

		// How much part of particles are reflected to transfer momentum to the panel.
		// 1.0 makes it a pure reflecting solar sail.
		// Otherwise you can use magnetic field to deflect the rest of the particles to gain extra momentum.
		[KSPField]	
		public float reflectedPhotonRatio = 0.5f;

		[KSPField(guiActive = true, guiName = "Force")]
		private string forceAcquired;

		// Surface area of the panel.
		[KSPField]	
		public float surfaceArea;

		[KSPField]	
		public string animName;

		// Solar power curve (distance's function).
		[KSPField]
		public FloatCurve solarPowerCurve;// = PluginHelper.getSatFloatCurve();

		[KSPField(isPersistant = true)]
		public bool IsEnabled = false;

		//[KSPField]
		//public string surfaceTransformName;

		private List<bool> isSunLightReached = new List<bool>();

		private Transform surfaceTransform = null;
		private Animation solarSailAnim = null;

		const double kerbin_distance = 13599840256;
		const double thrust_coeff = 9.08e-6;

		[KSPEvent(guiActive = true, guiName = "Deploy Sail", active = true)]
		public void DeploySail() {
			solarSailAnim [animName].speed = 1f;
			solarSailAnim [animName].normalizedTime = 0f;
			solarSailAnim.Blend (animName, 2f);
			IsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Retract Sail", active = false)]
		public void RetractSail() {
			solarSailAnim [animName].speed = -1f;
			solarSailAnim [animName].normalizedTime = 1f;
			solarSailAnim.Blend (animName, 2f);
			IsEnabled = false;
		}

		public override void OnStart(StartState state) {
			if (state != StartState.None && state != StartState.Editor)	{
				//surfaceTransform = part.FindModelTransform(surfaceTransformName);
				//solarSailAnim = (ModuleAnimateGeneric)part.Modules["ModuleAnimateGeneric"];
				solarSailAnim = part.FindModelAnimators (animName).FirstOrDefault ();
				this.part.force_activate ();
			}
		}

		public override void OnFixedUpdate()	{
			if(FlightGlobals.fetch != null)	{
				if(!isEnabled) {return;}
				double sunlightFactor = 1.0;
				Vector3 sunVector = FlightGlobals.fetch.bodies[0].position - part.orgPos;

				if (!PluginHelper.lineOfSightToSun(vessel)) {
					sunlightFactor = 0.0f;
				}

				Debug.Log("Detecting sunlight: " + sunlightFactor.ToString());
				Vector3 solarForce = CalculateSolarForce() * sunlightFactor;

				Vector3d solar_accel = CalculateSolarForce () / vessel.GetTotalMass ()/1000.0;
				if (!this.vessel.packed) {
					vessel.ChangeWorldVelocity (solar_accel);
				} else {
					double temp1 = solar_accel.y;
					solar_accel.y = solar_accel.z;
					solar_accel.z = temp1;
					Vector3d position = vessel.orbit.pos + vessel.orbit.vel*TimeWarp.fixedDeltaTime;
					Vector3d mod_acceleration = solar_accel * TimeWarp.fixedDeltaTime;
					
					vessel.orbit.UpdateFromStateVectors(position, vessel.orbit.vel + solar_accel, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
				}

				forceAcquired = solarForce.magnitude.ToString ("E") + " N";
			}
		}

		private Vector3d CalculateSolarForce() {
			if (this.part != null) {
				Vector3d sunPosition = FlightGlobals.fetch.bodies [0].position;
				Vector3d ownPosition = this.part.transform.position;
				Vector3d normal = this.part.transform.up;
				if (surfaceTransform != null) {
					normal = surfaceTransform.forward;
				}
				Vector3d force = normal * Vector3.Dot ((ownPosition - sunPosition).normalized, normal);
				return force * surfaceArea * reflectedPhotonRatio * solarForceAtDistance ();
			} else {
				return Vector3d.zero;
			}
		}

		private double solarForceAtDistance() {
			double distance_from_sun = Vector3.Distance (FlightGlobals.Bodies [PluginHelper.REF_BODY_KERBOL].transform.position, vessel.transform.position);
			double force_to_return = thrust_coeff * kerbin_distance * kerbin_distance / distance_from_sun / distance_from_sun;
			return force_to_return;
		}

	}
}

