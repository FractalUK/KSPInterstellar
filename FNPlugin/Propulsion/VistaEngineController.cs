using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
	class VistaEngineController : FNResourceSuppliableModule, IUpgradeableModule 
    {
        // Persistant
		[KSPField(isPersistant = true)]
		bool IsEnabled;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Upgraded")]
        public bool isupgraded = false;
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        // None Persistant
		[KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Hazard To")]
		public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
        public string temperatureStr = "";

        [KSPField(isPersistant = false)]
        public float powerRequirement = 2500;
        [KSPField(isPersistant = false)]
        public float maxThrust = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 1200;
        [KSPField(isPersistant = false)]
        public float maxAtmosphereDensity = 0.001f;

        [KSPField(isPersistant = false)]
        public float efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public float leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public float killDivider = 50;

        [KSPField(isPersistant = false)]
        public float fusionWasteHeat = 2500;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded = 10000;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float maxTemp = 3200;

        

        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;
        [KSPField(isPersistant = false)]
        public string originalName = "Prototype DT Vista Engine";
        [KSPField(isPersistant = false)]
        public string upgradedName = "DT Vista Engine";

        // Gui
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string engineType = "";
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName= "upgrade tech")]
        public string upgradeTechReq = null;

        //[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Heat Prduction")]
        //public float currentHeatProduction;
        //[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Base Heat Prduction")]
        //public float baseHeatProduction;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Radiator Temp")]
        public float coldBathTemp;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Radiator Temp")]
        public float maxTempatureRadiators;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Performance Radiators")]
        public float radiatorPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Emisiveness")]
        public float partEmissiveConstant;

        protected bool hasrequiredupgrade = false;
		protected bool radhazard = false;
		protected double minISP = 0;
		protected double standard_megajoule_rate = 0;
		protected double standard_deuterium_rate = 0;
		protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;

        

		[KSPEvent(guiActive = true, guiName = "Disable Radiation Safety", active = true)]
		public void DeactivateRadSafety() 
        {
			rad_safety_features = false;
		}

		[KSPEvent(guiActive = true, guiName = "Activate Radiation Safety", active = false)]
		public void ActivateRadSafety() 
        {
			rad_safety_features = true;
		}

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitEngine()
        {
            if (ResearchAndDevelopment.Instance == null || isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public float MaximumThrust { get { return isupgraded ? maxThrustUpgraded : maxThrust; } }
        public float FusionWasteHeat { get { return isupgraded ? fusionWasteHeatUpgraded : fusionWasteHeat; } }

        public void upgradePartModule()
        {
            engineType = upgradedName;
            isupgraded = true;
        }

        #endregion

        public override void OnStart(PartModule.StartState state) 
        {
            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;

            engineType = originalName;
            //curEngineT = (ModuleEnginesFX)this.part.Modules["ModuleEnginesFX"];
            curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

            if (curEngineT == null) return;

            minISP = curEngineT.atmosphereCurve.Evaluate(0);
            //currentHeatProduction = curEngineT.heatProduction;

            standard_deuterium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio;
            standard_tritium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio;

            // if we can upgrade, let's do so
            if (isupgraded)
                upgradePartModule();
            else if (this.HasTechsRequiredToUpgrade())
                hasrequiredupgrade = true;

            // calculate WasteHeat Capacity
            part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT].maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;

            if (state == StartState.Editor && this.HasTechsRequiredToUpgrade())
            {
                isupgraded = true;
                upgradePartModule();
            }
            
            if (state != StartState.Editor)
                part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;
		}

		public override void OnUpdate() 
        {
            if (curEngineT == null) return;

            Events["DeactivateRadSafety"].active = rad_safety_features;
            Events["ActivateRadSafety"].active = !rad_safety_features;
            Events["RetrofitEngine"].active = !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade;

			if (curEngineT.isOperational && !IsEnabled) 
            {
				IsEnabled = true;
				part.force_activate ();
			}

			int kerbal_hazard_count = 0;
			foreach (Vessel vess in FlightGlobals.Vessels) 
            {
				float distance = (float)Vector3d.Distance (vessel.transform.position, vess.transform.position);
                if (distance < leathalDistance && vess != this.vessel)
					kerbal_hazard_count += vess.GetCrewCount ();
			}

			if (kerbal_hazard_count > 0) 
            {
				radhazard = true;
				if (kerbal_hazard_count > 1) 
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbals.";
                else 
					radhazardstr = kerbal_hazard_count.ToString () + " Kerbal.";
				
				Fields["radhazardstr"].guiActive = true;
			} 
            else 
            {
				Fields["radhazardstr"].guiActive = false;
				radhazard = false;
				radhazardstr = "None.";
			}
		}

        private void ShutDown(string reason)
        {
            curEngineT.Events["Shutdown"].Invoke();
            curEngineT.currentThrottle = 0;
            curEngineT.requestedThrottle = 0;

            ScreenMessages.PostScreenMessage(reason, 5.0f, ScreenMessageStyle.UPPER_CENTER);
            foreach (FXGroup fx_group in part.fxGroups)
            {
                fx_group.setActive(false);
            }
        }

		public override void OnFixedUpdate()
        {
            temperatureStr = part.temperature.ToString("0.00") + "K / " + part.maxTemp.ToString("0.00") + "K";

            if (curEngineT == null) return;

            float throttle = curEngineT.currentThrottle > 0 ? Mathf.Max(curEngineT.currentThrottle, 0.01f) : 0;

            //double atmo_thrust_factor = Math.Min(1.0, Math.Max(1.0 - Math.Pow(vessel.atmDensity, 0.2), 0));

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown("Inertial Fusion cannot operate in atmosphere!");

                if (radhazard && rad_safety_features)
                    ShutDown("Engines throttled down as they presently pose a radiation hazard");
            }

            KillKerbalsWithRadiation(throttle);

            coldBathTemp = (float)FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = (float)FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);

            if (throttle > 0)
            {
                // Calculate Fusion Ratio
                var recievedPower = consumeFNResource(powerRequirement * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
                var plasma_ratio = recievedPower / (powerRequirement * TimeWarp.fixedDeltaTime);
                var fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.75 ? Mathf.Pow((float)plasma_ratio, 6.0f) : 0;

                // Lasers produce Wasteheat
                supplyFNResource(recievedPower * (1 - efficiency), FNResourceManager.FNRESOURCE_WASTEHEAT);

                // The Aborbed wasteheat from Fusion
                supplyFNResource(FusionWasteHeat * wasteHeatMultiplier * fusionRatio * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // change ratio propellants Hydrogen/Fusion
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio = (float)(standard_deuterium_rate / throttle / throttle);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio = (float)(standard_tritium_rate / throttle / throttle);

                // Update ISP
                FloatCurve newISP = new FloatCurve();
                var currentIsp = Math.Max(minISP * fusionRatio / throttle, minISP / 10);
                newISP.Add(0, (float)currentIsp);
                curEngineT.atmosphereCurve = newISP;

                // Update FuelFlow
                var maxFuelFlow = fusionRatio * MaximumThrust / currentIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;

                if (!curEngineT.getFlameoutState)
                {
                    if (plasma_ratio < 0.75 && recievedPower > 0)
                        curEngineT.status = "Insufficient Electricity";
                }
            }
            else
            {
                var currentIsp = minISP * 100;

                FloatCurve newISP = new FloatCurve();
                newISP.Add(0, (float)currentIsp);
                curEngineT.atmosphereCurve = newISP;

                var maxFuelFlow = MaximumThrust / currentIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = (float)maxFuelFlow;

                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio = (float)(standard_deuterium_rate);
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio = (float)(standard_tritium_rate);
            }

            radiatorPerformance = (float)Math.Max(1 - (float)(coldBathTemp / maxTempatureRadiators), 0.000001);
            partEmissiveConstant = (float)part.emissiveConstant;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0 || rad_safety_features) return;

            System.Random rand = new System.Random(new System.DateTime().Millisecond);
            List<Vessel> vessels_to_remove = new List<Vessel>();
            List<ProtoCrewMember> crew_to_remove = new List<ProtoCrewMember>();
            double death_prob = TimeWarp.fixedDeltaTime;

            foreach (Vessel vess in FlightGlobals.Vessels)
            {
                float distance = (float)Vector3d.Distance(vessel.transform.position, vess.transform.position);

                if (distance >= leathalDistance || vess == this.vessel || vess.GetCrewCount() <= 0) continue;

                float inv_sq_dist = distance / killDivider;
                float inv_sq_mult = 1.0f / inv_sq_dist / inv_sq_dist;
                foreach (ProtoCrewMember crew_member in vess.GetVesselCrew())
                {
                    if (UnityEngine.Random.value < (1.0 - death_prob * inv_sq_mult)) continue;

                    if (!vess.isEVA)
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        crew_to_remove.Add(crew_member);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(crew_member.name + " was killed by Neutron Radiation!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                        vessels_to_remove.Add(vess);
                    }
                }
            }

            foreach (Vessel vess in vessels_to_remove)
            {
                vess.rootPart.Die();
            }

            foreach (ProtoCrewMember crew_member in crew_to_remove)
            {
                Vessel vess = FlightGlobals.Vessels.Find(p => p.GetVesselCrew().Contains(crew_member));
                Part part = vess.Parts.Find(p => p.protoModuleCrew.Contains(crew_member));
                part.RemoveCrewmember(crew_member);
                crew_member.Die();
            }
        }

        public override string getResourceManagerDisplayName() 
        {
            return engineType;
        }

        public override int getPowerPriority() 
        {
            return 1;
        }
	}
}

