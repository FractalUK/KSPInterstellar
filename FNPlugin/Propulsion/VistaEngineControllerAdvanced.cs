using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    enum GenerationType { Mk1, Mk2, Mk3, Mk4, Mk5 }

    class VistaEngineControllerAdvanced : VistaEngineControllerBase
    {
        const float maxIsp = 27200f;
        const float minIsp = 15500f;
        const float steps = (maxIsp - minIsp) / 100f;

        // Persistant setting
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = false, guiName = "Selected Isp"), UI_FloatRange(stepIncrement = steps, maxValue = maxIsp, minValue = minIsp)]
        public float localIsp = minIsp;

        // settings
        [KSPField(isPersistant = false)]
        public float neutronAbsorptionFractionAtMinIsp = 0.5f;
        [KSPField(isPersistant = false)]
        public float maxThrustEfficiencyByIspPower = 2f;

        protected override float SelectedIsp { get { return localIsp; } }
        protected override float MaxIsp { get { return maxIsp; } }
        protected override float MaxThrustEfficiencyByIspPower { get { return maxThrustEfficiencyByIspPower; } }
        protected override float NeutronAbsorptionFractionAtMinIsp  { get { return neutronAbsorptionFractionAtMinIsp; } }
    }

    abstract class VistaEngineControllerBase : FNResourceSuppliableModule, IUpgradeableModule 
    {
        // Persistant
		[KSPField(isPersistant = true)]
		bool IsEnabled;
        [KSPField(isPersistant = true)]
        bool rad_safety_features = true;

        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk1 = 0.2f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk2 = 0.1f;
        [KSPField(isPersistant = false)]
        public float minThrottleRatioMk3 = 0.05f;

        // None Persistant
		[KSPField(isPersistant = false, guiActive = true, guiName = "Radiation Hazard To")]
		public string radhazardstr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Temperature")]
        public string temperatureStr = "";

        [KSPField(isPersistant = false)]
        public float powerRequirement = 625;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded = 1250;
        [KSPField(isPersistant = false)]
        public float powerRequirementUpgraded2 = 2500;

        [KSPField(isPersistant = false)]
        public float maxThrust = 75;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded = 300;
        [KSPField(isPersistant = false)]
        public float maxThrustUpgraded2 = 1200;

        [KSPField(isPersistant = false)]
        public float maxAtmosphereDensity = 0.001f;
        [KSPField(isPersistant = false)]
        public float leathalDistance = 2000;
        [KSPField(isPersistant = false)]
        public float killDivider = 50;

        [KSPField(isPersistant = false)]
        public float efficiency = 0.19f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded = 0.38f;
        [KSPField(isPersistant = false)]
        public float efficiencyUpgraded2 = 0.76f;

        [KSPField(isPersistant = false)]
        public float fusionWasteHeat = 625;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded = 2500;
        [KSPField(isPersistant = false)]
        public float fusionWasteHeatUpgraded2 = 10000;

        // Use for SETI Mode
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float powerRequirementMultiplier = 1;

        [KSPField(isPersistant = false)]
        public float maxTemp = 2500;
        [KSPField(isPersistant = false)]
        public float upgradeCost = 100;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName= "upgrade tech 1")]
        public string upgradeTechReq = "advFusionReactions";
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "upgrade tech 2")]
        public string upgradeTechReq2 = "exoticReactions";

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Current Throtle", guiFormat = "F2")]
        public float throttle;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Fusion Ratio", guiFormat = "F2")]
        public float fusionRatio;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Power Requirement", guiFormat = "F2", guiUnits = " MW")]
        public float enginePowerRequirement;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Laser Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public float laserWasteheat;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Absorbed Wasteheat", guiFormat = "F2", guiUnits = " MW")]
        public float absorbedWasteheat;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Radiator Temp")]
        public float coldBathTemp;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Radiator Temp")]
        public float maxTempatureRadiators;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Performance Radiators")]
        public float radiatorPerformance;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Emisiveness")]
        public float partEmissiveConstant;

        // abstracts
        protected abstract float SelectedIsp { get; }
        protected abstract float MaxIsp { get; }
        protected abstract float MaxThrustEfficiencyByIspPower { get; }
        protected abstract float NeutronAbsorptionFractionAtMinIsp { get; }

        // protected
        //protected float selectedIsp = 15500f;
        protected bool hasrequiredupgrade = false;
		protected bool radhazard = false;
		protected float minISP = 0;
		protected double standard_megajoule_rate = 0;
		protected double standard_deuterium_rate = 0;
		protected double standard_tritium_rate = 0;
        protected ModuleEngines curEngineT;

        public GenerationType EngineGenerationType { get; private set; }

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

        #region IUpgradeableModule

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public void upgradePartModule() {}

        public void DetermineTechLevel()
        {
            int numberOfUpgradeTechs = 1;
            if (PluginHelper.upgradeAvailable(upgradeTechReq))
                numberOfUpgradeTechs++;
            if (PluginHelper.upgradeAvailable(upgradeTechReq2))
                numberOfUpgradeTechs++;

            if (numberOfUpgradeTechs == 3)
                EngineGenerationType = GenerationType.Mk3;
            else if (numberOfUpgradeTechs == 2)
                EngineGenerationType = GenerationType.Mk2;
            else
                EngineGenerationType = GenerationType.Mk2;
        }

        #endregion

        public float MaximumThrust { get { return FullTrustMaximum * Mathf.Pow((minISP / SelectedIsp), MaxThrustEfficiencyByIspPower); } }
        
        public float FusionWasteHeat 
        { 
            get 
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return fusionWasteHeat;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return fusionWasteHeatUpgraded;
                else
                    return fusionWasteHeatUpgraded2;
            } 
        }

        public float FullTrustMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return maxThrust;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return maxThrustUpgraded;
                else
                    return maxThrustUpgraded2;
            }
        }

        public float LaserEfficiency
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return efficiency;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return efficiencyUpgraded;
                else
                    return efficiencyUpgraded2;
            }
        }

        public float CurrentPowerRequirement
        {
            get
            {
                return PowerRequirementMaximum * powerRequirementMultiplier * throttle;
            }
        }

        public float PowerRequirementMaximum
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return powerRequirement;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return powerRequirementUpgraded;
                else
                    return powerRequirementUpgraded2;
            }
        }

        public float MinThrottleRatio
        {
            get
            {
                if (EngineGenerationType == GenerationType.Mk1)
                    return minThrottleRatioMk1;
                else if (EngineGenerationType == GenerationType.Mk2)
                    return minThrottleRatioMk2;
                else
                    return minThrottleRatioMk3;
            }
        }
        

        public override void OnStart(PartModule.StartState state) 
        {
            part.maxTemp = maxTemp;
            part.thermalMass = 1;
            part.thermalMassModifier = 1;
            EngineGenerationType = GenerationType.Mk1;

            curEngineT = this.part.FindModuleImplementing<ModuleEngines>();

            if (curEngineT == null) return;

            minISP = curEngineT.atmosphereCurve.Evaluate(0);

            standard_deuterium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio;
            standard_tritium_rate = curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio;

            DetermineTechLevel();

            part.Resources[FNResourceManager.FNRESOURCE_WASTEHEAT].maxAmount = part.mass * 1.0e+5 * wasteHeatMultiplier;

            if (state != StartState.Editor)
                part.emissiveConstant = maxTempatureRadiators > 0 ? 1 - coldBathTemp / maxTempatureRadiators : 0.01;
		}

		public override void OnUpdate() 
        {
            if (curEngineT == null) return;

            Events["DeactivateRadSafety"].active = rad_safety_features;
            Events["ActivateRadSafety"].active = !rad_safety_features;

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

            throttle = curEngineT.currentThrottle > MinThrottleRatio ? curEngineT.currentThrottle : 0;

            if (throttle > 0)
            {
                if (vessel.atmDensity > maxAtmosphereDensity)
                    ShutDown("Inertial Fusion cannot operate in atmosphere!");

                if (radhazard && rad_safety_features)
                    ShutDown("Engines throttled down as they presently pose a radiation hazard");
            }

            KillKerbalsWithRadiation(throttle);

            if (throttle > 0)
            {
                // Calculate Fusion Ratio
                enginePowerRequirement = CurrentPowerRequirement;
                var requestedPowerFixed = enginePowerRequirement * TimeWarp.fixedDeltaTime;
                var recievedPowerFixed = consumeFNResource(requestedPowerFixed, FNResourceManager.FNRESOURCE_MEGAJOULES);
                var plasma_ratio = recievedPowerFixed / requestedPowerFixed;
                fusionRatio = plasma_ratio >= 1 ? 1 : plasma_ratio > 0.75f ? Mathf.Pow((float)plasma_ratio, 6) : 0;

                var laserWasteheatFixed = recievedPowerFixed * (1 - LaserEfficiency);
                laserWasteheat = laserWasteheatFixed / TimeWarp.fixedDeltaTime;

                // Lasers produce Wasteheat
                supplyFNResource(laserWasteheatFixed, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // The Aborbed wasteheat from Fusion
                var rateMultplier = minISP / SelectedIsp;
                var neutronbsorbionBonus = 1 - NeutronAbsorptionFractionAtMinIsp * (1 - ((SelectedIsp - minISP) / (MaxIsp - minISP)));
                absorbedWasteheat = FusionWasteHeat * wasteHeatMultiplier * fusionRatio * throttle * neutronbsorbionBonus;
                supplyFNResource(absorbedWasteheat * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

                // change ratio propellants Hydrogen/Fusion
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio = (float)standard_deuterium_rate / rateMultplier;  
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio = (float)standard_tritium_rate / rateMultplier; 

                // Update ISP
                var currentIsp = SelectedIsp; 
                FloatCurve newISP = new FloatCurve();
                newISP.Add(0, currentIsp);
                curEngineT.atmosphereCurve = newISP;

                // Update FuelFlow
                var maxFuelFlow = fusionRatio * MaximumThrust / currentIsp / PluginHelper.GravityConstant;
                curEngineT.maxFuelFlow = maxFuelFlow;

                if (!curEngineT.getFlameoutState && plasma_ratio < 0.75 && recievedPowerFixed > 0)
                    curEngineT.status = "Insufficient Electricity";
            }
            else
            {
                enginePowerRequirement = 0;
                absorbedWasteheat = 0;
                laserWasteheat = 0;
                fusionRatio = 0;

                var currentIsp = SelectedIsp; 
                FloatCurve newISP = new FloatCurve();
                newISP.Add(0, (float)currentIsp);
                curEngineT.atmosphereCurve = newISP;
                var rateMultplier = minISP / SelectedIsp;

                curEngineT.maxFuelFlow = 0;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Deuterium).ratio = (float)(standard_deuterium_rate) / rateMultplier;
                curEngineT.propellants.FirstOrDefault(pr => pr.name == InterstellarResourcesConfiguration.Instance.Tritium).ratio = (float)(standard_tritium_rate) / rateMultplier;
            }

            coldBathTemp = (float)FNRadiator.getAverageRadiatorTemperatureForVessel(vessel);
            maxTempatureRadiators = (float)FNRadiator.getAverageMaximumRadiatorTemperatureForVessel(vessel);
            radiatorPerformance = Mathf.Max(1 - (coldBathTemp / maxTempatureRadiators), 0.000001f);
            partEmissiveConstant = (float)part.emissiveConstant;
        }

        private void KillKerbalsWithRadiation(float throttle)
        {
            if (!radhazard || throttle <= 0.00 || rad_safety_features) return;

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
            return part.name;
        }

        public override int getPowerPriority() 
        {
            return 1;
        }
	}
}

