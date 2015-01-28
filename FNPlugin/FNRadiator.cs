using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPModule("Radiator")]
	class FNRadiator : FNResourceSuppliableModule	{
		[KSPField(isPersistant = true)]
		public bool radiatorIsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool radiatorInit;

        [KSPField(isPersistant = false)]
        public string upgradeTechReq;
		[KSPField(isPersistant = false)]
		public bool isDeployable = true;
		[KSPField(isPersistant = false)]
		public float convectiveBonus = 1.0f;
		[KSPField(isPersistant = false)]
		public string animName;
		[KSPField(isPersistant = false)]
		public float radiatorTemp;
		[KSPField(isPersistant = false)]
		public float radiatorArea;
		[KSPField(isPersistant = false)]
		public string originalName;
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

		//public static double stefan_const = 5.6704e-8;
        protected static float rad_const_h = 1000;
        protected static double alpha = 0.001998001998001998001998001998;

		protected Animation anim;
		protected float radiatedThermalPower;
		protected float convectedThermalPower;
		protected double current_rad_temp;
		protected float myScience = 0;
		protected float directionrotate = 1;
		protected float oldangle = 0;
		protected Vector3 original_eulers;
		protected Transform pivot;
		protected long last_draw_update = 0;
        protected long update_count = 0;
		protected bool hasrequiredupgrade;
		protected int explode_counter = 0;

		protected static List<FNRadiator> list_of_radiators = new List<FNRadiator>();


		public static List<FNRadiator> getRadiatorsForVessel(Vessel vess) {
			List<FNRadiator> list_of_radiators_for_vessel = new List<FNRadiator>();
			list_of_radiators.RemoveAll(item => item == null);
			foreach (FNRadiator radiator in list_of_radiators) {
				if (radiator.vessel == vess) {
					list_of_radiators_for_vessel.Add (radiator);
				}
			}
			return list_of_radiators_for_vessel;
		}

		public static bool hasRadiatorsForVessel(Vessel vess) {
			list_of_radiators.RemoveAll(item => item == null);
			bool has_radiators = false;
			foreach (FNRadiator radiator in list_of_radiators) {
				if (radiator.vessel == vess) {
					has_radiators = true;
				}
			}
			return has_radiators;
		}

		public static double getAverageRadiatorTemperatureForVessel(Vessel vess) {
			list_of_radiators.RemoveAll(item => item == null);
			double average_temp = 0;
			double n_radiators = 0;
			foreach (FNRadiator radiator in list_of_radiators) {
				if (radiator.vessel == vess) {
					average_temp += radiator.getRadiatorTemperature ();
					n_radiators+=1.0f;
				}
			}

			if (n_radiators > 0) {
				average_temp = average_temp / n_radiators;
			} else {
				average_temp = 0;
			}

			return average_temp;
		}

        public static double getAverageMaximumRadiatorTemperatureForVessel(Vessel vess) {
            list_of_radiators.RemoveAll(item => item == null);
            double average_temp = 0;
            double n_radiators = 0;
            foreach (FNRadiator radiator in list_of_radiators) {
                if (radiator.vessel == vess) {
                    average_temp += radiator.radiatorTemp;
                    n_radiators += 1.0f;
                }
            }

            if (n_radiators > 0) {
                average_temp = average_temp / n_radiators;
            } else {
                average_temp = 0;
            }

            return average_temp;
        }


		[KSPEvent(guiActive = true, guiName = "Deploy Radiator", active = true)]
		public void DeployRadiator() {
			if (!isDeployable) {
				return;
			}
			anim [animName].speed = 1f;
			anim [animName].normalizedTime = 0f;
			anim.Blend (animName, 2f);
			radiatorIsEnabled = true;
		}

		[KSPEvent(guiActive = true, guiName = "Retract Radiator", active = false)]
		public void RetractRadiator() {
			if (!isDeployable) {
				return;
			}
			anim [animName].speed = -1f;
			anim [animName].normalizedTime = 1f;
			anim.Blend (animName, 2f);
			radiatorIsEnabled = false;
		}

		[KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
		public void RetrofitRadiator() {
			if (ResearchAndDevelopment.Instance == null) { return;} 
			if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }

			isupgraded = true;
			radiatorType = upgradedName;
			radiatorTemp = upgradedRadiatorTemp;
			radiatorTempStr = radiatorTemp + "K";

            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
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
			if (radiatorIsEnabled) {
				RetractRadiator();
			} else {
				DeployRadiator();
			}
		}

		public override void OnStart(PartModule.StartState state) {
			Actions["DeployRadiatorAction"].guiName = Events["DeployRadiator"].guiName = String.Format("Deploy Radiator");
			Actions["RetractRadiatorAction"].guiName = Events["RetractRadiator"].guiName = String.Format("Retract Radiator");
			Actions["ToggleRadiatorAction"].guiName = String.Format("Toggle Radiator");

            if (state == StartState.Editor) {
                if (hasTechsRequiredToUpgrade()) {
                    isupgraded = true;
                    hasrequiredupgrade = true;
                }
                return;
            }
			

			FNRadiator.list_of_radiators.Add (this);

			anim = part.FindModelAnimators (animName).FirstOrDefault ();
			//orig_emissive_colour = part.renderer.material.GetTexture (emissive_property_name);
			if (anim != null) {
				anim [animName].layer = 1;

				if (radiatorIsEnabled) {
					anim[animName].normalizedTime = 1.0f;
					anim[animName].enabled = true;
					anim.Sample();
				} else {
					//anim.Blend (animName, 0, 0);
				}
				//anim.Play ();
			}

			if (isDeployable) {
				pivot = part.FindModelTransform ("suntransform");
				original_eulers = pivot.transform.localEulerAngles;
			} else {
				radiatorIsEnabled = true;
			}

			if(HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				if(PluginHelper.hasTech(upgradeTechReq)) {
					hasrequiredupgrade = true;
				}
			}else{
				hasrequiredupgrade = true;
			}

			if (radiatorInit == false) {
				radiatorInit = true;
			}

			if (!isupgraded) {
				radiatorType = originalName;
			} else {
				radiatorType = upgradedName;
				radiatorTemp = upgradedRadiatorTemp;
			}


			radiatorTempStr = radiatorTemp + "K";
            this.part.force_activate();
		}

		public override void OnUpdate() {
			Events["DeployRadiator"].active = !radiatorIsEnabled && isDeployable;
			Events["RetractRadiator"].active = radiatorIsEnabled && isDeployable;
			if (ResearchAndDevelopment.Instance != null) {
				Events ["RetrofitRadiator"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;
			} else {
				Events ["RetrofitRadiator"].active = false;
			}
			Fields["upgradeCostStr"].guiActive = !isupgraded && hasrequiredupgrade;

			if (ResearchAndDevelopment.Instance != null) {
				upgradeCostStr = ResearchAndDevelopment.Instance.Science + "/" + upgradeCost.ToString ("0") + " Science";
			}

			
            if (update_count - last_draw_update > 8) {
                thermalPowerDissipStr = radiatedThermalPower.ToString("0.000") + "MW";
                thermalPowerConvStr = convectedThermalPower.ToString("0.000") + "MW";
                radiatorTempStr = current_rad_temp.ToString("0.0") + "K / " + radiatorTemp.ToString("0.0") + "K";

                last_draw_update = update_count;
            }

            colorHeat();

            update_count++;
		}

		public override void OnFixedUpdate() {
			float atmosphere_height = vessel.mainBody.maxAtmosphereAltitude;
			float vessel_height = (float) vessel.mainBody.GetAltitude (vessel.transform.position);
			float conv_power_dissip = 0;
			if (vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
				float pressure = (float) FlightGlobals.getStaticPressure (vessel.transform.position);
				float dynamic_pressure = (float) (0.5*pressure*1.2041*vessel.srf_velocity.sqrMagnitude/101325.0);
				pressure += dynamic_pressure;
				float low_temp = FlightGlobals.getExternalTemperature (vessel.transform.position);

                float delta_temp = Mathf.Max(0, (float)current_rad_temp - low_temp);
				conv_power_dissip = pressure * delta_temp * radiatorArea * rad_const_h/1e6f * TimeWarp.fixedDeltaTime*convectiveBonus;
				if (!radiatorIsEnabled) {
					conv_power_dissip = conv_power_dissip / 2.0f;
				}
				convectedThermalPower = consumeFNResource (conv_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;

				if (radiatorIsEnabled && dynamic_pressure > 1.4854428818159388107574636072046e-3 && isDeployable) {
					part.deactivate();

					//part.breakingForce = 1;
					//part.breakingTorque = 1;
					part.decouple (1);
				}
			}


			if (radiatorIsEnabled) {
				if (getResourceBarRatio (FNResourceManager.FNRESOURCE_WASTEHEAT) >= 1 && current_rad_temp >= radiatorTemp) {
					explode_counter ++;
					if (explode_counter > 25) {
						part.explode ();
					}
				} else {
					explode_counter = 0;
				}

                double radiator_temperature_temp_val = radiatorTemp * Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25);
                if (vessel.HasAnyActiveThermalSources()) radiator_temperature_temp_val = Math.Min(vessel.GetTemperatureofColdestThermalSource() / 1.01, radiator_temperature_temp_val);

                double thermal_power_dissip = (GameConstants.stefan_const * radiatorArea * Math.Pow(radiator_temperature_temp_val, 4) / 1e6) * TimeWarp.fixedDeltaTime;
				radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                double instantaneous_rad_temp = (Math.Min(Math.Pow(radiatedThermalPower * 1e6 / (GameConstants.stefan_const * radiatorArea), 0.25), radiatorTemp));
                instantaneous_rad_temp = Math.Max(instantaneous_rad_temp, Math.Max(FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.16, 2.7));
                if (current_rad_temp <= 0) {
                    current_rad_temp = instantaneous_rad_temp;
                } else {
                    current_rad_temp = instantaneous_rad_temp * alpha + (1.0 - alpha) * instantaneous_rad_temp;
                }

				if (isDeployable) {
					Vector3 pivrot = pivot.rotation.eulerAngles;

					pivot.Rotate (Vector3.up * 5f * TimeWarp.fixedDeltaTime * directionrotate);

					Vector3 sunpos = FlightGlobals.Bodies [0].transform.position;
					Vector3 flatVectorToTarget = sunpos - transform.position;

					flatVectorToTarget = flatVectorToTarget.normalized;
					float dot = Mathf.Asin (Vector3.Dot (pivot.transform.right, flatVectorToTarget)) / Mathf.PI * 180.0f;

					float anglediff = -dot;
					oldangle = dot;
					//print (dot);
					directionrotate = anglediff / 5 / TimeWarp.fixedDeltaTime;
					directionrotate = Mathf.Min (3, directionrotate);
					directionrotate = Mathf.Max (-3, directionrotate);
			
					part.maximum_drag = 0.8f;
					part.minimum_drag = 0.8f;
				}

			} else {
				if (isDeployable) {
					pivot.transform.localEulerAngles = original_eulers;
				}

                double radiator_temperature_temp_val = radiatorTemp * Math.Pow(getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT), 0.25);
				if (vessel.HasAnyActiveThermalSources()) radiator_temperature_temp_val = Math.Min (vessel.GetTemperatureofColdestThermalSource()/1.01, radiator_temperature_temp_val);

                double thermal_power_dissip = (GameConstants.stefan_const * radiatorArea * Math.Pow(radiator_temperature_temp_val, 4) / 1e7) * TimeWarp.fixedDeltaTime;
				radiatedThermalPower = consumeFNResource (thermal_power_dissip, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                double instantaneous_rad_temp = (Math.Min(Math.Pow(radiatedThermalPower * 1e7 / (GameConstants.stefan_const * radiatorArea), 0.25), radiatorTemp));
                instantaneous_rad_temp = Math.Max(instantaneous_rad_temp, Math.Max(FlightGlobals.getExternalTemperature((float)vessel.altitude, vessel.mainBody) + 273.16, 2.7));
                if (current_rad_temp <= 0) {
                    current_rad_temp = instantaneous_rad_temp;
                } else {
                    current_rad_temp = instantaneous_rad_temp * alpha + (1.0 - alpha) * instantaneous_rad_temp;
                }
                
				part.maximum_drag = 0.2f;
				part.minimum_drag = 0.2f;
			}



		}

        public bool hasTechsRequiredToUpgrade() {
            if (HighLogic.CurrentGame != null) {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
                    if (upgradeTechReq != null) {
                        if (PluginHelper.hasTech(upgradeTechReq)) {
                            return true;
                        }
                    }
                } else {
                    return true;
                }
            }
            return false;
        }

		public float getRadiatorTemperature() {
			return (float)current_rad_temp;
		}

		public override string GetInfo() {
            float thermal_power_dissip = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(radiatorTemp, 4) / 1e6);
            float thermal_power_dissip2 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(upgradedRadiatorTemp, 4) / 1e6);
            float thermal_power_dissip3 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(600, 4) / 1e6);
            float thermal_power_dissip4 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(1200, 4) / 1e6);
            float thermal_power_dissip5 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(1800, 4) / 1e6);
            float thermal_power_dissip6 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(2400, 4) / 1e6);
            float thermal_power_dissip7 = (float)(GameConstants.stefan_const * radiatorArea * Math.Pow(3000, 4) / 1e6);
            return String.Format("Maximum Waste Heat Radiated\n Base: {0} MW\n Upgraded: {1} MW\n-----\nRadiator Performance at:\n600K: {2} MW\n1200K: {3} MW\n1800K: {4} MW\n2400K: {5} MW\n3000K: {6} MW\n", thermal_power_dissip, thermal_power_dissip2, thermal_power_dissip3, thermal_power_dissip4, thermal_power_dissip5, thermal_power_dissip6, thermal_power_dissip7);
		}

        public override int getPowerPriority() {
            return 3;
        }

        private void colorHeat()
        {
            const String KSPShader = "KSP/Emissive/Bumped Specular";
            float currentTemperature = getRadiatorTemperature();
            float maxTemperature = part.maxTemp;

            double temperatureRatio = currentTemperature / maxTemperature;
            Color emissiveColor = new Color((float)(Math.Pow(temperatureRatio, 3)), 0.0f, 0.0f, 1.0f);

            Renderer[] array = part.FindModelComponents<Renderer>();
            
            foreach (Renderer renderer in array) 
            {
                if (renderer.material.shader.name != KSPShader)
                    renderer.material.shader = Shader.Find(KSPShader);

                if (part.name.StartsWith("circradiator"))
                {

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_e", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/circradiatorKT/texture1_n", false));

                } else if (part.name.StartsWith("RadialRadiator"))
                {

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/RadialHeatRadiator/d_glow", false));

                    //Debug.Log("rd _Emissive: " + renderer.material.GetTexture("_Emissive"));

                } else if (part.name.StartsWith("LargeFlatRadiator"))
                {

                    if (renderer.material.shader.name != KSPShader)
                        renderer.material.shader = Shader.Find(KSPShader);

                    if (renderer.material.GetTexture("_Emissive") == null)
                        renderer.material.SetTexture("_Emissive", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/glow", false));

                    if (renderer.material.GetTexture("_BumpMap") == null)
                        renderer.material.SetTexture("_BumpMap", GameDatabase.Instance.GetTexture("WarpPlugin/Parts/Electrical/LargeFlatRadiator/radtex_n", false));

                } else if (part.name.StartsWith("radiator"))
                {
                    // radiators have already everything set up
                } else // uknown raidator
                {
                    return;
                }

                renderer.material.SetColor("_EmissiveColor", emissiveColor);
            }
        }

	}
}

