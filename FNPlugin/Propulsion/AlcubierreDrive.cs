using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
	class AlcubierreDrive : FNResourceSuppliableModule 
	{
		// persistant
		[KSPField(isPersistant = true)]
		public bool IsEnabled = false;
		[KSPField(isPersistant = true)]
		public bool IsCharging = true;
		[KSPField(isPersistant = true)]
		private float existing_warpfactor;
		[KSPField(isPersistant = true)]
		public bool warpInit = false;
		[KSPField(isPersistant = true)]
		public int selected_factor = -1;
		
		// non persistant
		[KSPField(isPersistant = false)]
		public int InstanceID;
		[KSPField(isPersistant = false)]
		public bool IsSlave;
		[KSPField(isPersistant = false)]
		public string AnimationName = "";
		[KSPField(isPersistant = false)]
		public string upgradedName;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float effectSize1;
		[KSPField(isPersistant = false)]
		public float effectSize2;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;
        [KSPField(isPersistant = false)]
        public float powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Gravity Pull", guiUnits = "g", guiFormat = "F3")]
        public float gravityPull;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Maximum Warp Limit", guiUnits = "c", guiFormat = "F3")]
        public float maximumWarpForGravityPull;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Mass", guiUnits = "t")]
		public float partMass;
		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Total Warp Power", guiFormat = "F3", guiUnits = "t")]
		protected float sumOfAlcubierreDrives;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Vessel Total Mass", guiFormat = "F3", guiUnits = "t")]
		public float vesselTotalMass;
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Warp to Mass Ratio", guiFormat = "F3")]
		public float warpToMassRatio;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Magnitude Diff")]
		public float magnitudeDiff;
		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Magnitude Change")]
		public float magnitudeChange;
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Req Exotic Matter", guiUnits = " MW", guiFormat = "F2")]
		protected float exotic_power_required = 1000;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Abs Min Power Warp", guiFormat = "F2", guiUnits = "MW")]
		public float minPowerRequirementForLightSpeed;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Cur Power for Warp ", guiFormat = "F2", guiUnits = "MW")]
		public float currentPowerRequirementForWarp;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Power Max Speed", guiFormat = "F2", guiUnits = "MW")]
        public float PowerRequirementForMaximumAllowedLightSpeed;

		[KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
		public string warpdriveType = "Alcubierre Drive";

        [KSPField(isPersistant = false, guiActive = true, guiName = "Current Selected Throttle", guiUnits = "c", guiFormat = "F3")]
		public float WarpEngineThrottle;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Max Allowed Throtle", guiUnits = "c", guiFormat = "F3")]
        public float maximumAllowedWarpThrotle;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
		public string DriveStatus;

		[KSPField(isPersistant = true)]
		public bool isupgraded = false;

		[KSPField(isPersistant = true)]
		public string serialisedwarpvector;

		[KSPField(isPersistant = true)]
		public bool isDeactivatingWarpDrive = false;

		private float[] engine_throtle = { 0.01f, 0.016f, 0.025f, 0.04f, 0.063f, 0.1f, 0.16f, 0.25f, 0.40f, 0.63f, 1.0f, 1.6f, 2.5f, 4.0f, 6.3f, 10f, 16f, 25f, 40f, 63f, 100f };

		protected int old_selected_factor = 0;
		protected float tex_count;
		protected GameObject warp_effect;
		protected GameObject warp_effect2;
		protected Texture[] warp_textures;
		protected Texture[] warp_textures2;
		protected AudioSource warp_sound;
		protected const float warp_size = 50000;
		protected bool hasrequiredupgrade;
		
		private AnimationState[] animationState;
		private Vector3d heading_act;
		private Vector3d previous_Frame_heading;
		private Vector3d active_part_heading;
		private List<AlcubierreDrive> alcubierreDrives;
		private int minimum_selected_factor;
        private int maximumWarpSpeedFactor;
        private int minimumPowerAllowedFactor;
        private int insufficientPowerTimeout = 10;
        private bool vesselWasInOuterspace;


		[KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
		public void StartCharging() 
		{
			if (IsEnabled) return;

			if (warpToMassRatio < 1)
			{   
				ScreenMessages.PostScreenMessage("Warp Power to Vessel Mass is to low to create a stable warp field");
				return;
			}

			insufficientPowerTimeout = 10;
			IsCharging = true;
		}

		[KSPEvent(guiActive = true, guiName = "Stop Charging", active = false)]
		public void StopCharging() 
		{
			IsCharging = false;

			// flush all exotic matter
			List<PartResource> exoticResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
			float exotic_matter_available = (float) exoticResources.Sum(res => res.amount);
			part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, exotic_matter_available);
		}

		[KSPAction("Start Charging")]
		public void StartChargingAction(KSPActionParam param) 
		{
			StartCharging();
		}

		[KSPAction("Stop Charging")]
		public void StopChargingAction(KSPActionParam param) 
		{
			StopCharging();
		}

		[KSPAction("Toggle Charging")]
		public void ToggleChargingAction(KSPActionParam param) 
		{
			if (IsCharging) 
				StopCharging();
			else 
				StartCharging();
		}

		[KSPEvent(guiActive = true, guiName = "Activate Warp Drive", active = true)]
		public void ActivateWarpDrive() 
		{
			if (IsEnabled) return;

			isDeactivatingWarpDrive = false;

			if (warpToMassRatio < 1)
			{
				ScreenMessages.PostScreenMessage("Not enough warp power to warp vessel", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			Vessel vess = this.part.vessel;
			if (vess.altitude <= PluginHelper.getMaxAtmosphericAltitude(vess.mainBody) && vess.mainBody.flightGlobalsIndex != 0) 
			{
				ScreenMessages.PostScreenMessage("Cannot activate warp drive within the atmosphere!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			List<PartResource> resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
			float exotic_matter_available = (float) resources.Sum(res => res.amount);

			if (exotic_matter_available < exotic_power_required)
			{
				ScreenMessages.PostScreenMessage("Warp drive isn't fully charged yet for Warp!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

            if (maximumWarpSpeedFactor < selected_factor)
                selected_factor = minimumPowerAllowedFactor;

			float new_warpfactor = engine_throtle[selected_factor];

			currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warpfactor);

			if (currentPowerRequirementForWarp > getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES))
			{
				ScreenMessages.PostScreenMessage("Warp power requirement is higher that maximum power supply!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			IsCharging = false;
			initiateWarpTimeout = 10;
		}

		private int initiateWarpTimeout;

        private int GetMaximumFactor(float lightspeed)
        {
            int maxFactor = 0;

            for (int i = 0 ; i < engine_throtle.Count() ; i++ )
            {
                if (engine_throtle[i] > lightspeed )
                    return maxFactor;
                maxFactor = i;
            }
            return maxFactor;
        }

		private void InitiateWarp()
		{
            if (maximumWarpSpeedFactor < selected_factor)
                selected_factor = minimumPowerAllowedFactor;

			float new_warp_factor = engine_throtle[selected_factor];

			currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);

			float power_returned = consumeFNResource(currentPowerRequirementForWarp * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
			if (power_returned < 0.99 * currentPowerRequirementForWarp)
			{
				initiateWarpTimeout--;

				if (initiateWarpTimeout == 1)
				{
					while (selected_factor != minimum_selected_factor)
					{
						ReduceWarpPower();
						new_warp_factor = engine_throtle[selected_factor];
						currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);
						if (power_returned >= currentPowerRequirementForWarp)
							return;
					}
				}
				if (initiateWarpTimeout == 0)
				{

					ScreenMessages.PostScreenMessage("Not enough power to initiate warp " + power_returned + " " + currentPowerRequirementForWarp, 5.0f, ScreenMessageStyle.UPPER_CENTER);
					IsCharging = true;
					return;
				}
			}

            initiateWarpTimeout = 0; // stop initiating to warp
            vesselWasInOuterspace = (this.vessel.altitude > this.vessel.mainBody.atmosphereDepth * 10);

			// consume all exotic matter to create warp field
			part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, exotic_power_required);

			warp_sound.Play();
			warp_sound.loop = true;

			active_part_heading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

			heading_act = active_part_heading * GameConstants.warpspeed * new_warp_factor;
			serialisedwarpvector = ConfigNode.WriteVector(heading_act);

			vessel.GoOnRails();
			vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading_act, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
			vessel.GoOffRails();

			IsEnabled = true;
            
			existing_warpfactor = new_warp_factor;
			previous_Frame_heading = active_part_heading;
		}

		[KSPEvent(guiActive = true, guiName = "Deactivate Warp Drive", active = false)]
		public void DeactivateWarpDrive() 
		{
			if (!IsEnabled) 
				return;

            //float atmosphere_height = (float)this.vessel.mainBody.atmosphereDepth;
            //if (this.vessel.altitude <= atmosphere_height && vessel.mainBody.flightGlobalsIndex != 0) 
            //{
            //    ScreenMessages.PostScreenMessage("Cannot deactivate warp drive within the atmosphere!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
            //    return;
            //}

			IsEnabled = false;
			warp_sound.Stop();
			
			Vector3d heading = heading_act;
			heading.x = -heading.x;
			heading.y = -heading.y;
			heading.z = -heading.z;

			vessel.GoOnRails();
			vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
			vessel.GoOffRails();
		}

		[KSPEvent(guiActive = true, guiName = "Warp Throttle (+)", active = true)]
		public void ToggleWarpSpeedUp() 
		{
			selected_factor++;
			if (selected_factor >= engine_throtle.Length)
				selected_factor = engine_throtle.Length - 1;

			if (!IsEnabled)
				old_selected_factor = selected_factor;
		}

		[KSPEvent(guiActive = true, guiName = "Warp Throttle (-)", active = true)]
		public void ToggleWarpSpeedDown() 
		{
			selected_factor--;
			if (selected_factor < 0) 
				selected_factor = 0;

			if (!IsEnabled)
				old_selected_factor = selected_factor;
		}

		[KSPEvent(guiActive = true, guiName = "Reduce Warp Power", active = true)]
		public void ReduceWarpPower()
		{
			if (selected_factor == minimum_selected_factor) return;

			if (selected_factor < minimum_selected_factor)
				ToggleWarpSpeedUp();
            else if (selected_factor > minimum_selected_factor)
				ToggleWarpSpeedDown();
		}

		[KSPAction("Reduce Warp Drive")]
		public void ReduceWarpDriveAction(KSPActionParam param)
		{
			ReduceWarpPower();
		}

		[KSPAction("Activate Warp Drive")]
		public void ActivateWarpDriveAction(KSPActionParam param) 
		{
			ActivateWarpDrive();
		}

		[KSPAction("Deactivate Warp Drive")]
		public void DeactivateWarpDriveAction(KSPActionParam param) 
		{
			DeactivateWarpDrive();
		}

		[KSPAction("Warp Speed (+)")]
		public void ToggleWarpSpeedUpAction(KSPActionParam param) 
		{
			ToggleWarpSpeedUp();
		}

		[KSPAction("Warp Speed (-)")]
		public void ToggleWarpSpeedDownAction(KSPActionParam param) 
		{
			ToggleWarpSpeedDown();
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitDrive() 
		{
			if (ResearchAndDevelopment.Instance == null) return;

			if (isupgraded || ResearchAndDevelopment.Instance.Science < UpgradeCost()) return;

			isupgraded = true;
			warpdriveType = upgradedName;

			ResearchAndDevelopment.Instance.AddScience(-UpgradeCost(), TransactionReasons.RnDPartPurchase);
		}

		private float UpgradeCost()
		{
			return 0;
		}

		public override void OnStart(PartModule.StartState state) 
		{
			var exoticMatterResource = part.Resources.list.FirstOrDefault(r => r.resourceName == InterstellarResourcesConfiguration.Instance.ExoticMatter);
			// reset Exotic Matter Capacity
			if (exoticMatterResource != null)
			{
				part.mass = partMass;
				var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
				exoticMatterResource.maxAmount = 0.001;
				exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
			}

			InstanceID = GetInstanceID();

			if (IsSlave)
				UnityEngine.Debug.Log("KSPI - AlcubierreDrive Slave " + InstanceID + " Started");
			else
				UnityEngine.Debug.Log("KSPI - AlcubierreDrive Master " + InstanceID + " Started");

			if (!String.IsNullOrEmpty(AnimationName))
				animationState = SetUpAnimation(AnimationName, this.part);

			try
			{
				Events["StartCharging"].active = !IsSlave;
				Events["StopCharging"].active = !IsSlave;
				Events["ActivateWarpDrive"].active = !IsSlave;
				Events["DeactivateWarpDrive"].active = !IsSlave;
				Events["ToggleWarpSpeedUp"].active = !IsSlave;
				Events["ToggleWarpSpeedDown"].active = !IsSlave;
				Events["ReduceWarpPower"].active = !IsSlave;

				Fields["exotic_power_required"].guiActive = !IsSlave;
				Fields["WarpEngineThrottle"].guiActive = !IsSlave;
                Fields["maximumAllowedWarpThrotle"].guiActive = !IsSlave;
				Fields["warpToMassRatio"].guiActive = !IsSlave;
				Fields["vesselTotalMass"].guiActive = !IsSlave;
				Fields["DriveStatus"].guiActive = !IsSlave;
				Fields["minPowerRequirementForLightSpeed"].guiActive = !IsSlave;
				Fields["currentPowerRequirementForWarp"].guiActive = !IsSlave;
                Fields["sumOfAlcubierreDrives"].guiActive = !IsSlave;
                Fields["PowerRequirementForMaximumAllowedLightSpeed"].guiActive = !IsSlave;
			   
				Actions["StartChargingAction"].guiName = Events["StartCharging"].guiName = String.Format("Start Charging");
				Actions["StopChargingAction"].guiName = Events["StopCharging"].guiName = String.Format("Stop Charging");
				Actions["ToggleChargingAction"].guiName = String.Format("Toggle Charging");
				Actions["ActivateWarpDriveAction"].guiName = Events["ActivateWarpDrive"].guiName = String.Format("Activate Warp Drive");
				Actions["DeactivateWarpDriveAction"].guiName = Events["DeactivateWarpDrive"].guiName = String.Format("Deactivate Warp Drive");
				Actions["ToggleWarpSpeedUpAction"].guiName = Events["ToggleWarpSpeedUp"].guiName = String.Format("Warp Speed (+)");
				Actions["ToggleWarpSpeedDownAction"].guiName = Events["ToggleWarpSpeedDown"].guiName = String.Format("Warp Speed (-)");

				if (state == StartState.Editor) return;

				if (!IsSlave)
				{
					UnityEngine.Debug.Log("KSPI - AlcubierreDrive Create Slaves");
					alcubierreDrives = part.vessel.FindPartModulesImplementing<AlcubierreDrive>();
					foreach (var drive in alcubierreDrives)
					{
						var driveId = drive.GetInstanceID();
						if (driveId != InstanceID)
						{
							drive.IsSlave = true;
							UnityEngine.Debug.Log("KSPI - AlcubierreDrive " + driveId  + " != " + InstanceID);
						}
					}
				}

				UnityEngine.Debug.Log("KSPI - AlcubierreDrive OnStart step C ");

				this.part.force_activate();
				if (serialisedwarpvector != null)
					heading_act = ConfigNode.ParseVector3D(serialisedwarpvector);

				warp_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				warp_effect2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

				warp_effect.collider.enabled = false;
				warp_effect2.collider.enabled = false;

				Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
				Vector3 end_beam_pos = ship_pos + transform.up * warp_size;
				Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;

				warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
				warp_effect.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
				warp_effect.transform.rotation = part.transform.rotation;

				warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
				warp_effect2.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
				warp_effect2.transform.rotation = part.transform.rotation;

				//warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
				//warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));

				warp_effect.renderer.material.shader = Shader.Find("Unlit/Transparent");
				warp_effect2.renderer.material.shader = Shader.Find("Unlit/Transparent");

				warp_textures = new Texture[33];

				const string warp_tecture_path = "WarpPlugin/ParticleFX/warp";
				for (int i = 0; i < 11; i++)
				{
					warp_textures[i] = GameDatabase.Instance.GetTexture((i > 0)
						? warp_tecture_path + (i + 1).ToString()
						: warp_tecture_path, false);
				}

				warp_textures[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warp10", false);
				for (int i = 12; i < 33; i++)
				{
					int j = i > 17 ? 34 - i : i;
					warp_textures[i] = GameDatabase.Instance.GetTexture(j > 1 ?
						warp_tecture_path + (j + 1).ToString() : warp_tecture_path, false);
				}

				warp_textures2 = new Texture[33];

				const string warpr_tecture_path = "WarpPlugin/ParticleFX/warpr";
				for (int i = 0; i < 11; i++)
				{
					warp_textures2[i] = GameDatabase.Instance.GetTexture((i > 0)
						? warpr_tecture_path + (i + 1).ToString()
						: warpr_tecture_path, false);
				}

				warp_textures2[11] = GameDatabase.Instance.GetTexture("WarpPlugin/ParticleFX/warpr10", false);
				for (int i = 12; i < 33; i++)
				{
					int j = i > 17 ? 34 - i : i;
					warp_textures2[i] = GameDatabase.Instance.GetTexture(j > 1 ?
						warpr_tecture_path + (j + 1).ToString() : warpr_tecture_path, false);
				}

				warp_effect.renderer.material.color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, 0.5f);
				warp_effect2.renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 0.1f);
				warp_effect.renderer.material.mainTexture = warp_textures[0];
				warp_effect.renderer.receiveShadows = false;
				//warp_effect.layer = LayerMask.NameToLayer ("Ignore Raycast");
				//warp_effect.collider.isTrigger = true;
				warp_effect2.renderer.material.mainTexture = warp_textures2[0];
				warp_effect2.renderer.receiveShadows = false;
				warp_effect2.renderer.material.mainTextureOffset = new Vector2(-0.2f, -0.2f);
				//warp_effect2.layer = LayerMask.NameToLayer ("Ignore Raycast");
				//warp_effect2.collider.isTrigger = true;
				warp_effect2.renderer.material.renderQueue = 1000;
				warp_effect.renderer.material.renderQueue = 1001;
				/*gameObject.AddComponent<Light>();
				gameObject.light.color = Color.cyan;
				gameObject.light.intensity = 1f;
				gameObject.light.range = 4000f;
				gameObject.light.type = LightType.Spot;
				gameObject.light.transform.position = end_beam_pos;
				gameObject.light.cullingMask = ~0;*/

				//light.

				warp_sound = gameObject.AddComponent<AudioSource>();
				warp_sound.clip = GameDatabase.Instance.GetAudioClip("WarpPlugin/Sounds/warp_sound");
				warp_sound.volume = GameSettings.SHIP_VOLUME;
				warp_sound.panLevel = 0;
				warp_sound.rolloffMode = AudioRolloffMode.Linear;
				warp_sound.Stop();

				if (IsEnabled)
				{
					warp_sound.Play();
					warp_sound.loop = true;
				}

				bool manual_upgrade = false;
				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
				{
					if (upgradeTechReq != null)
					{
						if (PluginHelper.hasTech(upgradeTechReq))
							hasrequiredupgrade = true;
						else if (upgradeTechReq == "none")
							manual_upgrade = true;
					}
					else
						manual_upgrade = true;
				}
				else
					hasrequiredupgrade = true;

				if (warpInit == false)
				{
					warpInit = true;
					if (hasrequiredupgrade)
						isupgraded = true;
				}

				if (manual_upgrade)
					hasrequiredupgrade = true;


				if (isupgraded)
					warpdriveType = upgradedName;
				else
					warpdriveType = originalName;

				//warp_effect.transform.localScale.y = 2.5f;
				//warp_effect.transform.localScale.z = 200f;

				// disable charging at startup
				IsCharging = false;
			}
			catch (Exception e )
			{
				UnityEngine.Debug.LogError("KSPI - AlcubierreDrive OnStart 1 Exception " + e.Message);
			}

				warpdriveType = originalName;
				minimum_selected_factor = engine_throtle.ToList().IndexOf(engine_throtle.First(w => w == 1f));
				if (selected_factor == -1)
					selected_factor = minimum_selected_factor;
		}


		public override void OnUpdate() 
		{
			Events["StartCharging"].active = !IsSlave &&  !IsCharging;
			Events["StopCharging"].active = !IsSlave && IsCharging;
			Events["ActivateWarpDrive"].active = !IsSlave && !IsEnabled;
			Events["DeactivateWarpDrive"].active = !IsSlave && IsEnabled;

			if (ResearchAndDevelopment.Instance != null)
				Events["RetrofitDrive"].active = !IsSlave && !isupgraded && ResearchAndDevelopment.Instance.Science >= UpgradeCost() && hasrequiredupgrade;
			else 
				Events ["RetrofitDrive"].active = false;

            WarpEngineThrottle = engine_throtle[selected_factor];

			if (animationState != null)
			{
				foreach (AnimationState anim in animationState)
				{
					if ((IsEnabled || IsCharging) && anim.normalizedTime < 1) { anim.speed = 1; }
					if ((IsEnabled || IsCharging) && anim.normalizedTime >= 1)
					{
						anim.speed = 0;
						anim.normalizedTime = 1;
					}
					if (!IsEnabled && !IsCharging && anim.normalizedTime > 0) { anim.speed = -1; }
					if (!IsEnabled && !IsCharging && anim.normalizedTime <= 0)
					{
						anim.speed = 0;
						anim.normalizedTime = 0;
					}
				}
			}
		}

		public void FixedUpdate() // FixedUpdate is also called when not activated
		{
			if (alcubierreDrives != null)
				sumOfAlcubierreDrives = alcubierreDrives.Sum(p => p.partMass * (p.isupgraded ? 20 : 10));

            if (vessel != null)
            {
                vesselTotalMass = vessel.GetTotalMass();
                gravityPull = (float)FlightGlobals.getGeeForceAtPosition(vessel.GetWorldPos3D()).magnitude;
                maximumWarpForGravityPull = vessel.mainBody.flightGlobalsIndex != 0 
                    ? 1 / (Mathf.Max(gravityPull - 0.006f, 0.001f) * 10) 
                    : 1 / gravityPull;
                maximumWarpSpeedFactor = GetMaximumFactor(maximumWarpForGravityPull);
                maximumAllowedWarpThrotle = engine_throtle[maximumWarpSpeedFactor];
                minimumPowerAllowedFactor = maximumWarpSpeedFactor > minimum_selected_factor  ? maximumWarpSpeedFactor : minimum_selected_factor; 
            }

            if (sumOfAlcubierreDrives != 0 && vesselTotalMass != 0)
            {
                warpToMassRatio = sumOfAlcubierreDrives / vesselTotalMass;
                exotic_power_required = (GameConstants.initial_alcubierre_megajoules_required * vesselTotalMass * powerRequirementMultiplier) / warpToMassRatio;
            }

            minPowerRequirementForLightSpeed = GetPowerRequirementForWarp(1);
            PowerRequirementForMaximumAllowedLightSpeed = GetPowerRequirementForWarp(engine_throtle[maximumWarpSpeedFactor]);
			currentPowerRequirementForWarp = GetPowerRequirementForWarp(engine_throtle[selected_factor]);

			var exoticMatterResource = part.Resources.list.FirstOrDefault(r => r.resourceName == InterstellarResourcesConfiguration.Instance.ExoticMatter);
			// calculate Exotic Matter Capacity
			if (exoticMatterResource != null &&  !double.IsNaN(exotic_power_required) && !double.IsInfinity(exotic_power_required) && exotic_power_required > 0 )
			{
				var ratio = Math.Min(1, Math.Max(0, exoticMatterResource.amount / exoticMatterResource.maxAmount));
				exoticMatterResource.maxAmount = exotic_power_required;
				exoticMatterResource.amount = exoticMatterResource.maxAmount * ratio;
			}
		}

		public override void OnFixedUpdate()
		{
			if (initiateWarpTimeout > 0)
				InitiateWarp();
			
			Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
			Vector3 end_beam_pos = ship_pos + part.transform.up * warp_size;
			Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;

			warp_effect.transform.rotation = part.transform.rotation;
			warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
			warp_effect.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
			warp_effect.transform.rotation = part.transform.rotation;

			warp_effect2.transform.rotation = part.transform.rotation;
			warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
			warp_effect2.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
			warp_effect2.transform.rotation = part.transform.rotation;

			warp_effect.renderer.material.mainTexture = warp_textures[((int)tex_count) % warp_textures.Length];
			warp_effect2.renderer.material.mainTexture = warp_textures2[((int)tex_count + 8) % warp_textures.Length];
			tex_count += 1f * engine_throtle[selected_factor];

			WarpdriveCharging();

			UpdateWarpSpeed();
		}

		private void UpdateWarpDriveStatus(float currentExoticMatter, double lostWarpFieldForWarp)
		{
			double TimeLeftInSec = Math.Ceiling(currentExoticMatter / lostWarpFieldForWarp);
			DriveStatus = "Warp for " + (int)(TimeLeftInSec / 60) + " min " + (int)(TimeLeftInSec % 60) + " sec";
		}

		private void WarpdriveCharging()
		{
			float currentExoticMatter = 0;
			float maxExoticMatter = 0;

			List<PartResource> exoticResources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
			List<PartResource> partresources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();

			foreach (PartResource partresource in partresources)
			{
				currentExoticMatter += (float)partresource.amount;
				maxExoticMatter += (float)partresource.maxAmount;
			}

			if (IsCharging)
			{
                float available_power = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
				double powerDraw = Math.Max(minPowerRequirementForLightSpeed, Math.Min((maxExoticMatter - currentExoticMatter) / 0.001, available_power));

				float power_returned = consumeFNResource(powerDraw * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

				if (power_returned < 0.99 * minPowerRequirementForLightSpeed)
					insufficientPowerTimeout--;
				else
					insufficientPowerTimeout = 10;

				if (insufficientPowerTimeout < 0)
				{
					insufficientPowerTimeout--; 
					ScreenMessages.PostScreenMessage("Not enough MW power to initiate stable warp field!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
					StopCharging();
					return;
				}

				if (exoticResources.Sum(res => res.amount) < exotic_power_required)
				{
					part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, -power_returned * 0.001 * TimeWarp.fixedDeltaTime);
				}

				supplyFNResource(-power_returned * 0.999 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
			}

			if (!IsEnabled)
			{
				float exotic_matter_available = (float)exoticResources.Sum(res => res.amount);

				if (exotic_matter_available < exotic_power_required)
				{
					float electrical_current_pct = (float)(100.0f * exotic_matter_available / exotic_power_required);
					DriveStatus = String.Format("Charging: ") + electrical_current_pct.ToString("0.00") + String.Format("%");
				}
				else
					DriveStatus = "Ready.";

				warp_effect2.renderer.enabled = false;
				warp_effect.renderer.enabled = false;
			}
			else
			{
				DriveStatus = "Active.";
				warp_effect2.renderer.enabled = true;
				warp_effect.renderer.enabled = true;
			}
		}

		private float GetPowerRequirementForWarp(float lightspeedFraction)
		{
			var sqrtSpeed = Mathf.Sqrt(lightspeedFraction);
			var powerModifier = lightspeedFraction < 1 ? 1 / sqrtSpeed : sqrtSpeed;
			return powerModifier * exotic_power_required;
		}



		public void UpdateWarpSpeed()
		{
			if (!IsEnabled || exotic_power_required <= 0) return;

			float available_power = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);

            float new_warp_factor = engine_throtle[selected_factor];

			currentPowerRequirementForWarp = GetPowerRequirementForWarp(new_warp_factor);
			float power_returned = consumeFNResource(currentPowerRequirementForWarp * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
			supplyFNResource(-power_returned * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
			
			// retreive vessel heading
			Vector3d new_part_heading = new Vector3d(part.transform.up.x, part.transform.up.z, part.transform.up.y);

			// detect any changes in vessel heading and heading stability
			magnitudeDiff = (float)(active_part_heading - new_part_heading).magnitude;
			magnitudeChange = (float)(previous_Frame_heading - new_part_heading).magnitude;
			previous_Frame_heading = new_part_heading;

            // detect power shortage
            if (currentPowerRequirementForWarp > available_power)
                insufficientPowerTimeout = -1;
            else if (power_returned < 0.99 * currentPowerRequirementForWarp)
                insufficientPowerTimeout--;
            else
                insufficientPowerTimeout = 10;


            if (this.vessel.altitude < this.vessel.mainBody.atmosphereDepth * 2)
            {
                if (vesselWasInOuterspace)
                {
                    DeactivateWarpDrive();
                    return;
                }
            }
            else
                vesselWasInOuterspace = true;

            // determine if we need to change speed and heading
			var hasPowerShortage = insufficientPowerTimeout < 0;
			var hasHeadingChanged = magnitudeDiff > 0.05 && magnitudeChange < 0.0001;
			var hasWarpFactorChange = existing_warpfactor != new_warp_factor;
            var hasGavityPullInbalance = maximumWarpSpeedFactor < selected_factor;

            if (hasGavityPullInbalance)
            {
                selected_factor = maximumWarpSpeedFactor;
            }

            if (hasPowerShortage)
			{
                if (selected_factor == minimumPowerAllowedFactor || selected_factor == minimum_selected_factor ||  power_returned < 0.99 * PowerRequirementForMaximumAllowedLightSpeed)
				{
					ScreenMessages.PostScreenMessage("Critical Power shortage, deactivating warp");
					DeactivateWarpDrive();
					return;
				}

				ScreenMessages.PostScreenMessage("Insufficient Power " + power_returned.ToString("0.0") + " / " + currentPowerRequirementForWarp.ToString("0.0") + ", reducing power drain");
				ReduceWarpPower();
			}

            if (hasWarpFactorChange || hasPowerShortage || hasHeadingChanged || hasGavityPullInbalance)
			{
				new_warp_factor = engine_throtle[selected_factor];
				existing_warpfactor = new_warp_factor;

				Vector3d reverse_heading = new Vector3d(-heading_act.x, -heading_act.y, -heading_act.z);

				heading_act = new_part_heading * GameConstants.warpspeed * new_warp_factor;
				active_part_heading = new_part_heading;
				serialisedwarpvector = ConfigNode.WriteVector(heading_act);

				vessel.GoOnRails();
				vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + reverse_heading + heading_act, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
				vessel.GoOffRails();
			}

		}

		public override string getResourceManagerDisplayName() 
		{
			return "Alcubierre Drive";
		}

		public static AnimationState[] SetUpAnimation(string animationName, Part part)
		{
			var states = new List<AnimationState>();
			foreach (var animation in part.FindModelAnimators(animationName))
			{
				var animationState = animation[animationName];
				animationState.speed = 0;
				animationState.enabled = true;
				animationState.wrapMode = WrapMode.ClampForever;
				animation.Blend(animationName);
				states.Add(animationState);
			}
			return states.ToArray();
		}
	}

	
}
