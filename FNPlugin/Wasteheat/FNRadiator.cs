using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("Radiator")]
    class StackFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
    class FlatFNRadiator : FNRadiator { }

    [KSPModule("Radiator")]
	class FNRadiator : FNResourceSuppliableModule	
    {
        // persitant
		[KSPField(isPersistant = true)]
		public bool radiatorIsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;
        [KSPField(isPersistant = true)]
        public bool isAutomated = true;
        [KSPField(isPersistant = true)]
        public bool showColorHeat = true;

        // non persistant
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass", guiUnits = " t")]
        public float partMass;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Upgrade Tech")]
        public string upgradeTechReq;
		[KSPField(isPersistant = false)]
		public bool isDeployable = true;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Converction Bonus", guiUnits = " K")]
		public float convectiveBonus = 1.0f;
		[KSPField(isPersistant = false)]
		public string animName;
        [KSPField(isPersistant = false)]
        public string thermalAnim;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Radiator Temp", guiUnits = " K")]
		public float radiatorTemp;
		[KSPField(isPersistant = false)]
		public string originalName;
		[KSPField(isPersistant = false)]
		public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public float emissiveColorPower = 3;
        [KSPField(isPersistant = false)]
		public string upgradedName;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Radiator Temp Upgraded")]
        public float upgradedRadiatorTemp;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public string colorHeat = "_EmissiveColor";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Pressure Load", guiFormat= "F2", guiUnits = "%")]
        public float pressureLoad;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
		public string radiatorType;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Rad Temp")]
		public string radiatorTempStr;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radiator Area")]
        public float radiatorArea = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Radiator Area Upgraded")]
        public float upgradedRadiatorArea = 1;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Radiated")]
		public string thermalPowerDissipStr;
		[KSPField(isPersistant = false, guiActive = true, guiName = "Power Convected")]
		public string thermalPowerConvStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Rad Upgrade Cost")]
		public string upgradeCostStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Radiator Start Temp")]
        public float radiator_temperature_temp_val;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Rad Temp")]
        public float instantaneous_rad_temp;
        [KSPField(isPersistant = false, guiActive = false, guiName = "WasteHeat Ratio")]
        public float wasteheatRatio;

        const float rad_const_h = 1000;
        const String kspShader = "KSP/Emissive/Bumped Specular";

		protected Animation deployAnim;
		protected float radiatedThermalPower;
		protected float convectedThermalPower;
		protected float current_rad_temp;
		protected float directionrotate = 1;
		protected Vector3 original_eulers;
		protected Transform pivot;
		protected long last_draw_update = 0;
        protected long update_count = 0;
		protected bool hasrequiredupgrade;
		protected int explode_counter = 0;

		protected static List<FNRadiator> list_of_all_radiators = new List<FNRadiator>();

        private List<IThermalSource> list_of_thermal_sources;
        private Renderer[] array;
        private AnimationState[] heatStates;
        private ModuleDeployableRadiator moduleDeployableRadiator;

		public static List<FNRadiator> getRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

            List<FNRadiator> list_of_radiators_for_vessel = new List<FNRadiator>();
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					list_of_radiators_for_vessel.Add (radiator);
			}
			return list_of_radiators_for_vessel;
		}

		public static bool hasRadiatorsForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);

			bool has_radiators = false;
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
					has_radiators = true;
			}
			return has_radiators;
		}

		public static double getAverageRadiatorTemperatureForVessel(Vessel vess) 
        {
			list_of_all_radiators.RemoveAll(item => item == null);
			double average_temp = 0;
			double n_radiators = 0;
			foreach (FNRadiator radiator in list_of_all_radiators) 
            {
				if (radiator.vessel == vess) 
                {
					average_temp += radiator.getRadiatorTemperature ();
					n_radiators+=1.0f;
				}
			}

			if (n_radiators > 0) 
				average_temp = average_temp / n_radiators;
			else 
				average_temp = 0;

			return average_temp;
		}

        public static double getAverageMaximumRadiatorTemperatureForVessel(Vessel vess) 
        {
            list_of_all_radiators.RemoveAll(item => item == null);
            double average_temp = 0;
            double n_radiators = 0;

            foreach (FNRadiator radiator in list_of_all_radiators) 
            {
                if (radiator.vessel == vess) 
                {
                    average_temp += radiator.radiatorTemp;
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0) 
                average_temp = average_temp / n_radiators;
            else 
                average_temp = 0;

            return average_temp;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Enable Automation", active = true)]
        public void SwitchAutomation()
        {
            isAutomated = !isAutomated;

            UpdateEnableAutomation();
        }


		[KSPEvent(guiActive = true, guiName = "Deploy Radiator", active = true)]
		public void DeployRadiator() 
        {
            isAutomated = false;

            Deploy();
		}

        private void Deploy()
        {
            if (!isDeployable) return;

            deployAnim[animName].speed = 0.5f;
            deployAnim[animName].normalizedTime = 0f;
            deployAnim.Blend(animName, 2f);
            radiatorIsEnabled = true;
        }

		[KSPEvent(guiActive = true, guiName = "Retract Radiator", active = false)]
		public void RetractRadiator() 
        {
            isAutomated = false;

            Retract();
		}

        private void Retract()
        {
            if (!isDeployable) return;

            deployAnim[animName].speed = -0.5f;
            deployAnim[animName].normalizedTime = 1f;
            deployAnim.Blend(animName, 2f);
            radiatorIsEnabled = false;
        }

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitRadiator() 
        {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

			isupgraded = true;
			radiatorType = upgradedName;
			radiatorTemp = upgradedRadiatorTemp;
			radiatorTempStr = radiatorTemp + "K";

            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
		}

        [KSPAction("Switch Automation")]
        public void SwitchAutomationAction(KSPActionParam param)
        {
            SwitchAutomation();
        }

		[KSPAction("Deploy Radiator")]
		public void DeployRadiatorAction(KSPActionParam param) 
        {
            DeployRadiator();
		}

		[KSPAction("Retract Radiator")]
		public void RetractRadiatorAction(KSPActionParam param) 
        {
			RetractRadiator();
		}

		[KSPAction("Toggle Radiator")]
		public void ToggleRadiatorAction(KSPActionParam param) 
        {
			if (radiatorIsEnabled) 
				RetractRadiator();
			else 
				DeployRadiator();
		}

        public float CurrentRadiatorArea  {  get { return isupgraded ? upgradedRadiatorArea : radiatorArea; } }

		public override void OnStart(StartState state) 
        {
            radiatedThermalPower = 0;
		    convectedThermalPower = 0;
		    current_rad_temp = 0;
		    directionrotate = 1;
		    last_draw_update = 0;
            update_count = 0;
		    hasrequiredupgrade = false;
		    explode_counter = 0;
            UpdateEnableAutomation();

            if (upgradedRadiatorArea == 1)
                upgradedRadiatorArea = radiatorArea * 1.7f;

    		Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = "Deploy Radiator";
			Actions["RetractRadiatorAction"].guiName = Events["RetractRadiator"].guiName = "Retract Radiator";
			Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");

            var wasteheatPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);
            // calculate WasteHeat Capacity
            if (wasteheatPowerResource != null)
            {
                var ratio =  Math.Min(1, Math.Max(0, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                wasteheatPowerResource.maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;
                wasteheatPowerResource.amount = wasteheatPowerResource.maxAmount * ratio;
            }

            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                Fields["partMass"].guiActiveEditor = true;
                Fields["upgradeTechReq"].guiActiveEditor = true;
                Fields["convectiveBonus"].guiActiveEditor = true;
                Fields["upgradedRadiatorTemp"].guiActiveEditor = true;
            }

            if (state == StartState.Editor) 
            {
                if (hasTechsRequiredToUpgrade()) 
                {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                }
                return;
            }

            // find all eal thermal sources
            list_of_thermal_sources = vessel.FindPartModulesImplementing<IThermalSource>().Where(tc => tc.IsThermalSource).ToList();

            if (ResearchAndDevelopment.Instance != null)
                upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString("0") + " Science";

            if (state == PartModule.StartState.Docked)
            {
                base.OnStart(state);
                return;
            }

            // add to static list of all radiators
            FNRadiator.list_of_all_radiators.Add(this);

            moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
            array = part.FindModelComponents<Renderer>();

			deployAnim = part.FindModelAnimators (animName).FirstOrDefault ();
			if (deployAnim != null) 
            {
				deployAnim [animName].layer = 1;

				if (radiatorIsEnabled) 
                {
					deployAnim[animName].normalizedTime = 1.0f;
					deployAnim[animName].enabled = true;
					deployAnim.Sample();
				} 

			}

            if (!String.IsNullOrEmpty(thermalAnim))
                heatStates = SetUpAnimation(thermalAnim, this.part);

			if (isDeployable) 
            {
				pivot = part.FindModelTransform ("suntransform");
				original_eulers = pivot.transform.localEulerAngles;
			} 
            else 
				radiatorIsEnabled = true;
			

			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) 
            {
				if(PluginHelper.hasTech(upgradeTechReq)) 
					hasrequiredupgrade = true;
			}
            else
				hasrequiredupgrade = true;
			

			if (radiatorInit == false) 
				radiatorInit = true;

			if (!isupgraded) 
				radiatorType = originalName;
			else 
            {
				radiatorType = upgradedName;
				radiatorTemp = upgradedRadiatorTemp;
			}

			radiatorTempStr = radiatorTemp + "K";
		}

        public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
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

        private void UpdateEnableAutomation()
        {
            Events["SwitchAutomation"].active = isDeployable;
            Events["SwitchAutomation"].guiName = isAutomated ? "Disable Automation" : "Enable Automation";
        }

		public override void OnUpdate() 
        {
            if (update_count - last_draw_update > 8)
            {
                UpdateEnableAutomation();

                Events["DeployRadiator"].active = !radiatorIsEnabled && isDeployable;
                Events["RetractRadiator"].active = radiatorIsEnabled && isDeployable;

                if (ResearchAndDevelopment.Instance != null)
                    Events["RetrofitRadiator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
                else
                    Events["RetrofitRadiator"].active = false;

                Fields["upgradeTechReq"].guiActive = !isupgraded;
                Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;
                Fields["radiatorArea"].guiActive = !isupgraded;
                Fields["thermalPowerConvStr"].guiActive = convectedThermalPower > 0;
                if ((moduleDeployableRadiator != null && moduleDeployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED) || moduleDeployableRadiator == null)
                {
                    thermalPowerDissipStr = radiatedThermalPower.ToString("0.000") + "MW";
                    thermalPowerConvStr = convectedThermalPower.ToString("0.000") + "MW";
                }
                else
                {
                    thermalPowerDissipStr = "disabled";
                    thermalPowerConvStr = "disabled";
                }

                radiatorTempStr = current_rad_temp.ToString("0.0") + "K / " + radiatorTemp.ToString("0.0") + "K";

                last_draw_update = update_count;

                if (showColorHeat)
                    ColorHeat();
            }

            update_count++;
		}

        
        public void FixedUpdate() // FixedUpdate is also called when not activated
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

	        if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) 
            {
				float pressure = ((float) FlightGlobals.getStaticPressure (vessel.transform.position) / 100f);
				float dynamic_pressure = (float) (0.5f * pressure * 1.2041f * vessel.srf_velocity.sqrMagnitude / 101325.0);
				pressure += dynamic_pressure;
				float low_temp = (float)FlightGlobals.getExternalTemperature (vessel.transform.position);

                float delta_temp = Mathf.Max(0, (float)current_rad_temp - low_temp);
                double conv_power_dissip = pressure * delta_temp * CurrentRadiatorArea * rad_const_h / 1e6f * TimeWarp.fixedDeltaTime * convectiveBonus;
				if (!radiatorIsEnabled) 
					conv_power_dissip = conv_power_dissip / 2.0f;
				
                convectedThermalPower = consumeWasteHeat(conv_power_dissip);

                if (isDeployable)
                    DeployMentControl(dynamic_pressure);
			}
            else
            {
                pressureLoad = 0;
                if (!radiatorIsEnabled && isAutomated)
                    Deploy();
            }

            wasteheatRatio = (float)getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT);

            radiator_temperature_temp_val = radiatorTemp * Mathf.Pow(wasteheatRatio, 0.25f);
            var activeThermalSources = GetActiveThermalSources();
            if (activeThermalSources.Any())
                radiator_temperature_temp_val = Math.Min(GetAverageTemperatureofOfThermalSource(activeThermalSources) / 1.01f, radiator_temperature_temp_val);

			if (radiatorIsEnabled) 
            {
                if (wasteheatRatio >= 1 && current_rad_temp >= radiatorTemp) 
                {
					explode_counter ++;
					if (explode_counter > 25) 
						part.explode ();
				} 
                else 
					explode_counter = 0;

                float fixed_thermal_power_dissip = Mathf.Pow(radiator_temperature_temp_val, 4) * GameConstants.stefan_const * CurrentRadiatorArea / 1e6f * TimeWarp.fixedDeltaTime;

                if (Single.IsNaN(fixed_thermal_power_dissip))
					Debug.LogWarning("FNRadiator: OnFixedUpdate Single.IsNaN detected in fixed_thermal_power_dissip");

                radiatedThermalPower = consumeWasteHeat(fixed_thermal_power_dissip);

                if (Single.IsNaN(radiatedThermalPower))
					Debug.LogError("FNRadiator: OnFixedUpdate Single.IsNaN detected in radiatedThermalPower after call consumeWasteHeat (" + fixed_thermal_power_dissip + ")");

                instantaneous_rad_temp = Mathf.Min(radiator_temperature_temp_val * 1.014f, radiatorTemp);
                instantaneous_rad_temp = Mathf.Max(instantaneous_rad_temp, Mathf.Max((float)FlightGlobals.getExternalTemperature(vessel.altitude, vessel.mainBody), 2.7f));

				if (Single.IsNaN(instantaneous_rad_temp))
					Debug.LogError("FNRadiator: OnFixedUpdate Single.IsNaN detected in instantaneous_rad_temp after reading external temperature");

                current_rad_temp = instantaneous_rad_temp;
                
				if (isDeployable) 
                {
					pivot.Rotate (Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

					Vector3 sunpos = FlightGlobals.Bodies [0].transform.position;
					Vector3 flatVectorToTarget = sunpos - transform.position;

					flatVectorToTarget = flatVectorToTarget.normalized;
					float dot = Mathf.Asin (Vector3.Dot (pivot.transform.right, flatVectorToTarget)) / Mathf.PI * 180.0f;

					float anglediff = -dot;
					directionrotate = anglediff / 5 / TimeWarp.fixedDeltaTime;
					directionrotate = Mathf.Min (3, directionrotate);
					directionrotate = Mathf.Max (-3, directionrotate);
			
					part.maximum_drag = 0.8f;
					part.minimum_drag = 0.8f;
				}

			} 
            else 
            {
				if (isDeployable) 
					pivot.transform.localEulerAngles = original_eulers;

                float fixed_thermal_power_dissip = Mathf.Pow(radiator_temperature_temp_val, 4) * GameConstants.stefan_const * CurrentRadiatorArea / 0.5e7f * TimeWarp.fixedDeltaTime;

                radiatedThermalPower = consumeWasteHeat(fixed_thermal_power_dissip);

                instantaneous_rad_temp = Mathf.Min(radiator_temperature_temp_val * 1.014f, radiatorTemp);
                instantaneous_rad_temp = Mathf.Max(instantaneous_rad_temp, Mathf.Max((float)FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody), 2.7f));

                current_rad_temp = instantaneous_rad_temp;
                
				part.maximum_drag = 0.2f;
				part.minimum_drag = 0.2f;
			}

		}

        private void DeployMentControl(float dynamic_pressure)
        {
            if (dynamic_pressure <= 0) return;

            pressureLoad = (dynamic_pressure / 1.4854428818159e-3f) * 100;
            if (pressureLoad > 100)
            {
                if (radiatorIsEnabled)
                {
                    if (isAutomated)
                        Retract();
                    else
                    {
                        part.deactivate();
                        part.decouple(1);
                    }
                }
            }
            else if (!radiatorIsEnabled && isAutomated)
                Deploy();
        }

        public float GetAverageTemperatureofOfThermalSource(List<IThermalSource> active_thermal_sources)
        {
            return active_thermal_sources.Any() 
                ? active_thermal_sources.Sum(r => r.HotBathTemperature) / active_thermal_sources.Count 
                : 3500;
        }

        public List<IThermalSource> GetActiveThermalSources()
        {
            return list_of_thermal_sources.Where(ts => ts.IsActive).ToList();
        }

        private float consumeWasteHeat(double wasteheatToConsume)
        {
            if ((moduleDeployableRadiator != null && moduleDeployableRadiator.panelState == ModuleDeployableRadiator.panelStates.EXTENDED) || moduleDeployableRadiator == null)
            {
                var consumedWasteheat = consumeFNResource(wasteheatToConsume, FNResourceManager.FNRESOURCE_WASTEHEAT);

                if (Single.IsNaN(consumedWasteheat))
                    return 0;
                    
                return consumedWasteheat / TimeWarp.fixedDeltaTime;
            }

            return 0;
        }

        public bool hasTechsRequiredToUpgrade()
        {
            if (HighLogic.CurrentGame == null) return false;

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return true;

            if (upgradeTechReq != null && PluginHelper.hasTech(upgradeTechReq)) return true;

            return false;
        }

		public float getRadiatorTemperature() 
        {
			return current_rad_temp;
		}

		public override string GetInfo() 
        {
            float thermal_power_dissip =  GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(radiatorTemp, 4) / 1e6f;
            float thermal_power_dissip2 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(upgradedRadiatorTemp, 4) / 1e6f;
            float thermal_power_dissip3 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(600, 4) / 1e6f;
            float thermal_power_dissip4 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(1200, 4) / 1e6f;
            float thermal_power_dissip5 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(1800, 4) / 1e6f;
            float thermal_power_dissip6 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(2400, 4) / 1e6f;
            float thermal_power_dissip7 = GameConstants.stefan_const * CurrentRadiatorArea * Mathf.Pow(3000, 4) / 1e6f;
            return String.Format("Maximum Waste Heat Radiated\n Base: {0} MW\n Upgraded: {1} MW\n-----\nRadiator Performance at:\n600K: {2} MW\n1200K: {3} MW\n1800K: {4} MW\n2400K: {5} MW\n3000K: {6} MW\n", thermal_power_dissip, thermal_power_dissip2, thermal_power_dissip3, thermal_power_dissip4, thermal_power_dissip5, thermal_power_dissip6, thermal_power_dissip7);
		}

        public override int getPowerPriority() 
        {
            return 3;
        }

        private void ColorHeat()
        {
            float currentTemperature = getRadiatorTemperature();

            float partTempRatio = Mathf.Min((float)(part.temperature / part.maxTemp), 1);

            float radiatorTempRatio = Mathf.Min(currentTemperature / radiatorTemp * 1.05f, 1);

            float colorRatio = Mathf.Pow(Math.Max(partTempRatio, radiatorTempRatio), emissiveColorPower);

            if (heatStates != null)
            {
                foreach (AnimationState anim in heatStates)
                {
                    anim.normalizedTime = colorRatio;
                }
                return;
            }

            var emissiveColor = new Color(colorRatio, 0.0f, 0.0f, 1.0f);

            foreach (Renderer renderer in array) 
            {
                if (renderer.material.shader.name != kspShader)
                    renderer.material.shader = Shader.Find(kspShader);

                if (part.name.StartsWith("circradiator"))
                {
                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_e", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_n", false));
                } 
                else if (part.name.StartsWith("RadialRadiator"))
                {
                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/RadialHeatRadiator/d_glow", false));
                } 
                else if (part.name.StartsWith("LargeFlatRadiator"))
                {

                    if (renderer.material.shader.name != kspShader)
                        renderer.material.shader = Shader.Find(kspShader);

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/glow", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/radtex_n", false));
                }

                if (string.IsNullOrEmpty(colorHeat))
                    return;

                renderer.material.SetColor(colorHeat, emissiveColor);
            }
        }

	}
}

