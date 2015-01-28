extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class AlcubierreDrive : FNResourceSuppliableModule 
    {
        [KSPField(isPersistant = true)]
        public bool IsEnabled = false;
		[KSPField(isPersistant = true)]
		public bool IsCharging = true;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
		[KSPField(isPersistant = false)]
		public float effectSize1;
		[KSPField(isPersistant = false)]
		public float effectSize2;
        private Vector3d heading_act;

		[KSPField(isPersistant = true)]
		public bool warpInit = false;
		[KSPField(isPersistant = false)]
		public string upgradeTechReq;
        
        //private float warpspeed = 30000000.0f;
        //public const float warpspeed = 29979245.8f;
        protected double megajoules_required = 1000;
                
        private float[] warp_factors = {0.1f,0.25f,0.5f,0.75f,1.0f,2.0f,3.0f,4.0f,5.0f,7.5f,10.0f,15f,20.0f};
		[KSPField(isPersistant = true)]
        public int selected_factor = 0;
        protected float mass_divisor = 10f;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string warpdriveType;
        
        [KSPField(isPersistant = false, guiActive = true, guiName = "Light Speed Factor")]
        public string LightSpeedFactor;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string DriveStatus;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Upgrade")]
        public string upgradeCostStr;

        [KSPField(isPersistant = true)]
        public bool isupgraded = false;

        [KSPField(isPersistant = true)]
        public string serialisedwarpvector;

        [KSPField(isPersistant = true)]
        public bool isDeactivatingWarpDrive = false;

        protected GameObject warp_effect;
        protected GameObject warp_effect2;
        protected Texture[] warp_textures;
        protected Texture[] warp_textures2;
        protected AudioSource warp_sound;
        protected float tex_count;
        const float warp_size = 50000;
		protected bool hasrequiredupgrade;

		[KSPEvent(guiActive = true, guiName = "Start Charging", active = true)]
		public void StartCharging() 
        {
			IsCharging = true;
		}

		[KSPEvent(guiActive = true, guiName = "Stop Charging", active = false)]
		public void StopCharging() 
        {
			IsCharging = false;
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
            Vessel vess = this.part.vessel;
            //float atmosphere_height = vess.mainBody.maxAtmosphereAltitude;
            if (vess.altitude <= PluginHelper.getMaxAtmosphericAltitude(vess.mainBody) && vess.mainBody.flightGlobalsIndex != 0) 
            {
                ScreenMessages.PostScreenMessage("Cannot activate warp drive within the atmosphere!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            List<PartResource> resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
            float exotic_matter_available = (float) resources.Sum(res => res.amount);
            var powerRequiredForWarp = megajoules_required * warp_factors[selected_factor];

            if (exotic_matter_available < powerRequiredForWarp)
            {
                ScreenMessages.PostScreenMessage("Warp drive isn't fully charged yet for Warp!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }

            var totalConsumedPower = PluginHelper.LimitedWarpTravel
                ? (0.5 * powerRequiredForWarp) + (exotic_matter_available - powerRequiredForWarp)
                : powerRequiredForWarp;

            part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, totalConsumedPower);
            warp_sound.Play();
            warp_sound.loop = true;
            
            //Orbit planetOrbit = vessel.orbit.referenceBody.orbit;
            Vector3d heading = part.transform.up;
            heading.y = heading.z;
            heading.z = heading.y;
            
            Vector3d position = vessel.orbit.pos;
            heading = heading * GameConstants.warpspeed * warp_factors[selected_factor];
            heading_act = heading;
            serialisedwarpvector = ConfigNode.WriteVector(heading);
            
            vessel.GoOnRails();
            vessel.orbit.UpdateFromStateVectors(position, vessel.orbit.vel + heading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
            vessel.GoOffRails();
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Warp Drive", active = false)]
        public void DeactivateWarpDrive() 
        {
			if (!IsEnabled) 
                return;

            if (PluginHelper.LimitedWarpTravel)
            {
                // retrieve current strength of warpfield
                List<PartResource> resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
                float warpFieldStrenth = (float)resources.Sum(res => res.amount);

                // wait untill warp field has collapsed
                if (warpFieldStrenth > 0)
                {
                    isDeactivatingWarpDrive = true;
                    return;
                }
                // deactive charging
                IsCharging = false;
                isDeactivatingWarpDrive = false;
            }

            float atmosphere_height = this.vessel.mainBody.maxAtmosphereAltitude;
            if (this.vessel.altitude <= atmosphere_height && vessel.mainBody.flightGlobalsIndex != 0) {
				ScreenMessages.PostScreenMessage("Cannot deactivate warp drive within the atmosphere!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                return;
            }
            IsEnabled = false;
            warp_sound.Stop();
            
            Vector3d heading = heading_act;
            heading.x = -heading.x;
            heading.y = -heading.y;
            heading.z = -heading.z;
            vessel.GoOnRails();
            //PatchedConicSolver.
            //this.vessel.ChangeWorldVelocity(heading);
            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + heading, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());
            vessel.GoOffRails();

            //CheatOptions.UnbreakableJoints = false;
            //CheatOptions.NoCrashDamage = false;           
        }

        [KSPEvent(guiActive = true, guiName = "Warp Speed (+)", active = true)]
        public void ToggleWarpSpeed() 
        {
            if (IsEnabled) return;

            selected_factor++;
            if (selected_factor >= warp_factors.Length) 
                selected_factor = 0;
        }

		[KSPEvent(guiActive = true, guiName = "Warp Speed (-)", active = true)]
		public void ToggleWarpSpeedDown() 
        {
			if (IsEnabled) return;

			selected_factor-=1;
			if (selected_factor < 0) 
				selected_factor = warp_factors.Length-1;
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
        public void ToggleWarpSpeedAction(KSPActionParam param) 
        {
            ToggleWarpSpeed();
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
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            isupgraded = true;
            warpdriveType = upgradedName;
            mass_divisor = 40f;
            //recalculatePower();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
            //IsEnabled = false;
        }

        public override void OnStart(PartModule.StartState state) 
        {
			Actions["StartChargingAction"].guiName = Events["StartCharging"].guiName = String.Format("Start Charging");
			Actions["StopChargingAction"].guiName = Events["StopCharging"].guiName = String.Format("Stop Charging");
			Actions["ToggleChargingAction"].guiName = String.Format("Toggle Charging");
			Actions["ActivateWarpDriveAction"].guiName = Events["ActivateWarpDrive"].guiName = String.Format("Activate Warp Drive");
            Actions["DeactivateWarpDriveAction"].guiName = Events["DeactivateWarpDrive"].guiName = String.Format("Deactivate Warp Drive");
			Actions["ToggleWarpSpeedAction"].guiName = Events["ToggleWarpSpeed"].guiName = String.Format("Warp Speed (+)");
			Actions["ToggleWarpSpeedDownAction"].guiName = Events["ToggleWarpSpeedDown"].guiName = String.Format("Warp Speed (-)");
            
            if (state == StartState.Editor) return;
 
            this.part.force_activate();
            if (serialisedwarpvector != null) 
                heading_act = ConfigNode.ParseVector3D(serialisedwarpvector);
            
            warp_effect2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warp_effect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            warp_effect.collider.enabled = false;
            warp_effect2.collider.enabled = false;
            Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
            Vector3 end_beam_pos = ship_pos + transform.up * warp_size;
            Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f;
			warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
            warp_effect.transform.position = new Vector3(mid_pos.x, ship_pos.y+mid_pos.y, mid_pos.z);
            warp_effect.transform.rotation = part.transform.rotation;
			warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
            warp_effect2.transform.position = new Vector3(mid_pos.x, ship_pos.y + mid_pos.y, mid_pos.z);
            warp_effect2.transform.rotation = part.transform.rotation;
            //warp_effect.layer = LayerMask.NameToLayer("Ignore Raycast");
            //warp_effect.renderer.material = new Material(KSP.IO.File.ReadAllText<AlcubierreDrive>("AlphaSelfIllum.shader"));
            //KSP.IO.File.
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
			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) 
            {
				if(upgradeTechReq != null) 
                {
					if (PluginHelper.hasTech(upgradeTechReq)) 
						hasrequiredupgrade = true;
					else if(upgradeTechReq == "none") 
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
				if(hasrequiredupgrade) 
					isupgraded = true;
			}

			if(manual_upgrade) 
				hasrequiredupgrade = true;
			

            if (isupgraded) 
            {
                warpdriveType = upgradedName;
                mass_divisor = 40f;
            }
            else 
            {
                warpdriveType = originalName;
                mass_divisor = 10f;
            }
            
            //warp_effect.transform.localScale.y = 2.5f;
            //warp_effect.transform.localScale.z = 200f;

            // disable charging at startup
            IsCharging = false;
        }

        public override void OnUpdate() 
        {
			Events["StartCharging"].active = !IsCharging;
			Events["StopCharging"].active = IsCharging;
            Events["ActivateWarpDrive"].active = !IsEnabled;
            Events["DeactivateWarpDrive"].active = IsEnabled;
            Events["ToggleWarpSpeed"].active = !IsEnabled;
			Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;

			if (ResearchAndDevelopment.Instance != null) 
				Events ["RetrofitDrive"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
            else 
				Events ["RetrofitDrive"].active = false;
			
            LightSpeedFactor = warp_factors[selected_factor].ToString("0.00") + "c";

			if (ResearchAndDevelopment.Instance != null) 
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
        }

        public override void OnFixedUpdate() 
        {
            //if (!IsEnabled) { return; }
            megajoules_required =  GameConstants.initial_alcubierre_megajoules_required * vessel.GetTotalMass() / mass_divisor;
            Vector3 ship_pos = new Vector3(part.transform.position.x, part.transform.position.y, part.transform.position.z);
            Vector3 end_beam_pos = ship_pos + part.transform.up * warp_size;
            Vector3 mid_pos = (ship_pos - end_beam_pos) / 2.0f ;
            warp_effect.transform.rotation = part.transform.rotation;
			warp_effect.transform.localScale = new Vector3(effectSize1, mid_pos.magnitude, effectSize1);
            warp_effect.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect.transform.rotation = part.transform.rotation;
            warp_effect2.transform.rotation = part.transform.rotation;
			warp_effect2.transform.localScale = new Vector3(effectSize2, mid_pos.magnitude, effectSize2);
            warp_effect2.transform.position = new Vector3(ship_pos.x + mid_pos.x, ship_pos.y + mid_pos.y, ship_pos.z + mid_pos.z);
            warp_effect2.transform.rotation = part.transform.rotation;
            
            //if (tex_count < warp_textures.Length) {
            warp_effect.renderer.material.mainTexture = warp_textures[((int)tex_count)%warp_textures.Length];
            warp_effect2.renderer.material.mainTexture = warp_textures2[((int)tex_count+8) % warp_textures.Length];
            tex_count+=1f*warp_factors[selected_factor];
            //}else {
            //    tex_count = 0;
            //}

            if (PluginHelper.LimitedWarpTravel)
                LimitedWarpDriveCharging();
            else
                SimpleWarpdriveCharging();
        }

        private void LimitedWarpDriveCharging()
        {
            float currentExoticMatter = 0;
            float maxExoticMatter = 0;
            List<PartResource> partresources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();

            foreach (PartResource partresource in partresources)
            {
                currentExoticMatter += (float)partresource.amount;
                maxExoticMatter += (float)partresource.maxAmount;
            }

            double naturalWarpfieldDecay = megajoules_required / 500.0;
            double warpSpeedModifier = Math.Max(1.0 + warp_factors[selected_factor] / 2.0, warp_factors[selected_factor]);
            double warpfieldDelta;

            if (IsCharging)
            {
                float maxPowerDrawForExoticMatter = (maxExoticMatter - currentExoticMatter) * GameConstants.BaseMaxPowerDrawForExoticMatter * PluginHelper.MaxPowerDrawForExoticMatterMult;
                float available_power = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                float power_returned = consumeFNResource(Math.Min(maxPowerDrawForExoticMatter * TimeWarp.fixedDeltaTime, available_power * TimeWarp.fixedDeltaTime), FNResourceManager.FNRESOURCE_MEGAJOULES);
                double normalisedReturnedPower = (double)power_returned / (double)TimeWarp.fixedDeltaTime;

                if (IsEnabled)
                {
                    // maintain or collapse warpfield
                    double lostWarpField = isDeactivatingWarpDrive
                        ? Math.Max(((normalisedReturnedPower - megajoules_required / 100.0) * 100.0) / megajoules_required + naturalWarpfieldDecay, naturalWarpfieldDecay)
                        : Math.Min(1.0 / ((normalisedReturnedPower * 50.0) / megajoules_required), naturalWarpfieldDecay);

                    double lostWarpFieldForWarp = lostWarpField * warpSpeedModifier;
                    UpdateWarpDriveStatus(currentExoticMatter, lostWarpFieldForWarp);
                    warpfieldDelta = lostWarpFieldForWarp * TimeWarp.fixedDeltaTime;
                }
                else
                {
                    // charge warp engine
                    double warpfieldTreshHold = (megajoules_required / 100.0);
                    double WarpFieldCharge = Math.Min(-(normalisedReturnedPower - warpfieldTreshHold) / 25.0f, warpfieldTreshHold);
                    warpfieldDelta = WarpFieldCharge * warpSpeedModifier * TimeWarp.fixedDeltaTime;
                }
            }
            else
            {
                double lostWarpFieldForWarp = naturalWarpfieldDecay * warpSpeedModifier;
                UpdateWarpDriveStatus(currentExoticMatter, lostWarpFieldForWarp);
                warpfieldDelta = lostWarpFieldForWarp * TimeWarp.fixedDeltaTime;  // discharge warp engine
            }

            // modilfy warpfield/warpengine
            part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, warpfieldDelta);

            // get curent available exotic matter
            double exotic_matter_available = partresources.Sum(res => res.amount);

            if (!IsEnabled)
            {
                if (exotic_matter_available < megajoules_required * warp_factors[selected_factor])
                {
                    float electrical_current_pct = (float)(100.0f * exotic_matter_available / (megajoules_required * warp_factors[selected_factor]));
                    DriveStatus = String.Format("Charging: ") + electrical_current_pct.ToString("0.00") + String.Format("%");
                }
                else
                    DriveStatus = "Ready.";

                warp_effect2.renderer.enabled = false;
                warp_effect.renderer.enabled = false;
            }
            else
            {
                // check if warp field is still stable
                if (exotic_matter_available == 0)
                {
                    ScreenMessages.PostScreenMessage("Warp field has collaped, dropping out of Warp!", 5.0f, ScreenMessageStyle.LOWER_CENTER);
                    DeactivateWarpDrive();
                }
                else
                {
                    //DriveStatus = "Active.";
                    warp_effect2.renderer.enabled = true;
                    warp_effect.renderer.enabled = true;
                }
            }
        }

        private void UpdateWarpDriveStatus(float currentExoticMatter, double lostWarpFieldForWarp)
        {
            double TimeLeftInSec = Math.Ceiling(currentExoticMatter / lostWarpFieldForWarp);
            DriveStatus = "Warp for " + (int)(TimeLeftInSec / 60) + " min " + (int)(TimeLeftInSec % 60) + " sec";
        }

        private void SimpleWarpdriveCharging()
        {
            float currentExoticMatter = 0;
            float maxExoticMatter = 0;
            List<PartResource> partresources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
            foreach (PartResource partresource in partresources)
            {
                currentExoticMatter += (float)partresource.amount;
                maxExoticMatter += (float)partresource.maxAmount;
            }

            if (IsCharging)
            {
                float maxPowerDrawForExoticMatter = (maxExoticMatter - currentExoticMatter) * GameConstants.BaseMaxPowerDrawForExoticMatter * PluginHelper.MaxPowerDrawForExoticMatterMult;
                float available_power = getStableResourceSupply(FNResourceManager.FNRESOURCE_MEGAJOULES);
                float power_returned = consumeFNResource(Math.Min(maxPowerDrawForExoticMatter * TimeWarp.fixedDeltaTime, available_power * TimeWarp.fixedDeltaTime), FNResourceManager.FNRESOURCE_MEGAJOULES);
                part.RequestResource(InterstellarResourcesConfiguration.Instance.ExoticMatter, -power_returned / (GameConstants.BaseMaxPowerDrawForExoticMatter * PluginHelper.MaxPowerDrawForExoticMatterMult));
            }

            if (!IsEnabled)
            {
                //ChargeStatus = "";
                List<PartResource> resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.ExoticMatter).ToList();
                float exotic_matter_available = (float)resources.Sum(res => res.amount);

                if (exotic_matter_available < megajoules_required * warp_factors[selected_factor])
                {
                    float electrical_current_pct = (float)(100.0f * exotic_matter_available / (megajoules_required * warp_factors[selected_factor]));
                    DriveStatus = String.Format("Charging: ") + electrical_current_pct.ToString("0.00") + String.Format("%");
                }
                else
                    DriveStatus = "Ready.";

                //light.intensity = 0;
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

        public override string getResourceManagerDisplayName() 
        {
            return "Alcubierre Drive";
        }

    }

    
}
