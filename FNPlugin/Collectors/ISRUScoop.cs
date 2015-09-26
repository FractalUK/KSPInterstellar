using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    class ISRUScoop : FNResourceSuppliableModule 
    {
        // persistants
        [KSPField(isPersistant = true)]
        public bool scoopIsEnabled = false;
        [KSPField(isPersistant = true)]
        public int currentresource = 0;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float last_power_percentage ;

        // part proterties
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Scooped Air")]
        public float scoopair = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false)]
        public float powerReqMult = 1;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Density")]
        public string atmosphericDensity;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Collected")]
        public string resflow;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Resource")]
        public string currentresourceStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Percentage", guiUnits = "%")]
        public float rescourcePercentage;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Storage")]
        public string resourceStoragename;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string recievedPower;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Trace Atmosphere")]
        public string densityFractionOfUpperAthmosphere;

        
        // internals
        protected float resflowf = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Scoop", active = true)]
        public void ActivateScoop() 
        {
            scoopIsEnabled = true;
            OnUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Disable Scoop", active = true)]
        public void DisableScoop() 
        {
            scoopIsEnabled = false;
            OnUpdate();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Resource", active = true)]
        public void ToggleResource() 
        {
            currentresource++;

            if (ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource) == null
                && ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource) > 0
                && currentresource != 0)
            {
                ToggleResource();
            }
            
            if (currentresource >= ORSAtmosphericResourceHandler.getAtmosphericCompositionForBody(vessel.mainBody.flightGlobalsIndex).Count) 
                currentresource = 0;

            resflow = String.Empty;
            resflowf = 0;
        }

        [KSPAction("Activate Scoop")]
        public void ActivateScoopAction(KSPActionParam param) 
        {
            ActivateScoop();
        }

        [KSPAction("Disable Scoop")]
        public void DisableScoopAction(KSPActionParam param) 
        {
            DisableScoop();
        }

        [KSPAction("Toggle Scoop")]
        public void ToggleScoopAction(KSPActionParam param) 
        {
            if (scoopIsEnabled) 
                DisableScoop();
            else 
                ActivateScoop();
        }

        [KSPAction("Toggle Resource")]
        public void ToggleToggleResourceAction(KSPActionParam param) 
        {
            ToggleResource();
        }

        public override void OnStart(PartModule.StartState state) 
        {
            Actions["ToggleToggleResourceAction"].guiName = Events["ToggleResource"].guiName = String.Format("Toggle Resource");

            if (state == StartState.Editor)  return;

            this.part.force_activate();

            // verify if body has atmosphere at all
            if (!vessel.mainBody.atmosphere) return;

            // verify scoop was enabled 
            if (!scoopIsEnabled) return;

            // verify a timestamp is available
            if (last_active_time == 0) return;

            // verify any power was avaialble in previous save
            if (last_power_percentage < 0.01) return;

            // verify altitude is not too high
            if (vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * PluginHelper.MaxAtmosphericAltitudeMult))
            {
                ScreenMessages.PostScreenMessage("Vessel is too high for resource accumulation", 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // verify altitude is not too low
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)))
            {
                ScreenMessages.PostScreenMessage("Vessel is too low for resource accumulation", 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // verify eccentricity
            if (vessel.orbit.eccentricity > 0.1)
            {
                string message = "Eccentricity of " + vessel.orbit.eccentricity.ToString("0.0000") + " is too High for resource accumulations";
                ScreenMessages.PostScreenMessage(message, 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // verify that an electric or Thermal engine is available with high enough ISP 
            var highIspEngine = part.vessel.parts.Find(p =>
                p.FindModulesImplementing<ElectricEngineControllerFX>().Any(e => e.baseISP > 4200) ||
                p.FindModulesImplementing<ThermalNozzleController>().Any(e => e.AttachedReactor.CoreTemperature > 40000));
            if (highIspEngine == null)
            {
                ScreenMessages.PostScreenMessage("No engine available, with high enough Isp and propelant switch ability to compensate for atmospheric drag", 10.0f, ScreenMessageStyle.LOWER_CENTER);
                return;
            }

            // calcualte time past since last frame
            double time_diff = (Planetarium.GetUniversalTime() - last_active_time) * 55;

            // scoop athmosphere for entire durration
            ScoopAthmosphere(time_diff, true);
        }

        public override void OnUpdate() 
        {
            Events["ActivateScoop"].active = !scoopIsEnabled;
            Events["DisableScoop"].active = scoopIsEnabled;
            Events["ToggleResource"].active = scoopIsEnabled;

            Fields["resflow"].guiActive = scoopIsEnabled;
            Fields["currentresourceStr"].guiActive = scoopIsEnabled;
            Fields["resourceStoragename"].guiActive = scoopIsEnabled;

            double resourcePercentage = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource)*100;
            string resourceDisplayName = ORSAtmosphericResourceHandler.getAtmosphericResourceDisplayName(vessel.mainBody.flightGlobalsIndex, currentresource);
            if (resourceDisplayName != null) 
                currentresourceStr = resourceDisplayName + "(" + resourcePercentage + "%)";
            
            UpdateResourceFlow();
        }

        public override void OnFixedUpdate() 
        {
            if (!scoopIsEnabled) return;

            if (!vessel.mainBody.atmosphere) return;
            
            // scoop athmosphere for a single frame
            ScoopAthmosphere(TimeWarp.fixedDeltaTime, false);

            // store current time in case vesel is unloaded
            last_active_time = (float)Planetarium.GetUniversalTime();
        }

        private void ScoopAthmosphere(double deltaTimeInSeconds, bool offlineCollecting)
        {
            string ors_atmospheric_resource_name = ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource);
            string resourceDisplayName = ORSAtmosphericResourceHandler.getAtmosphericResourceDisplayName(vessel.mainBody.flightGlobalsIndex, currentresource);

            if (ors_atmospheric_resource_name == null)
            {
                resflowf = 0.0f;
                recievedPower = "error";
                densityFractionOfUpperAthmosphere = "error";
                return;
            }

            // map ors resource to kspi resource
            
            if (PluginHelper.OrsResourceMappings == null || !PluginHelper.OrsResourceMappings.TryGetValue(ors_atmospheric_resource_name, out resourceStoragename))
                resourceStoragename = ors_atmospheric_resource_name;
            else if (!PartResourceLibrary.Instance.resourceDefinitions.Contains(resourceStoragename))
                resourceStoragename = ors_atmospheric_resource_name;

            //double resourcedensity = PartResourceLibrary.Instance.GetDefinition(PluginHelper.atomspheric_resources_tocollect[currentresource]).density;
            double resourcedensity = PartResourceLibrary.Instance.GetDefinition(resourceStoragename).density;

            var maxAltitudeAtmosphere = PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody);
            
            double upperAtmospherFraction = Math.Max(0, (vessel.altitude - maxAltitudeAtmosphere) / Math.Max(0.000001, maxAltitudeAtmosphere * PluginHelper.MaxAtmosphericAltitudeMult - maxAltitudeAtmosphere));
            double upperatmosphereDensity = 1 - upperAtmospherFraction;
            
            double airDensity = part.vessel.atmDensity + (PluginHelper.MinAtmosphericAirDensity * upperatmosphereDensity);
            atmosphericDensity = airDensity.ToString("0.00000000");

            var hydrogenTax = 0.4 * Math.Sin(upperAtmospherFraction * Math.PI * 0.5);
            var heliumTax = 0.2 * Math.Sin(upperAtmospherFraction * Math.PI);

            double rescourceFraction = (1.0 - hydrogenTax - heliumTax) * ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);

            // increase density hydrogen
            if (resourceDisplayName == "Hydrogen")
                rescourceFraction += hydrogenTax;
            else if (resourceDisplayName == "Helium")
                rescourceFraction += heliumTax;

            densityFractionOfUpperAthmosphere = (upperatmosphereDensity * 100.0).ToString("0.000") + "%";
            rescourcePercentage = (float)rescourceFraction * 100f;
            if (rescourceFraction <= 0 || vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * PluginHelper.MaxAtmosphericAltitudeMult))
            {
                resflowf = 0.0f;
                recievedPower = "off";
                densityFractionOfUpperAthmosphere = "too high";
                rescourcePercentage = 0;
                return;
            }

            double airspeed = part.vessel.srf_velocity.magnitude + 40.0;
            double air = airspeed * (airDensity / 1000) * scoopair / resourcedensity;
            double scoopedAtm = (float)(air * rescourceFraction);
            double powerrequirementsMW = (scoopair / 0.15f) * 6f * (float)PluginHelper.PowerConsumptionMultiplier * powerReqMult;

            if (scoopedAtm > 0 && part.GetResourceSpareCapacity(resourceStoragename) > 0)
            {
                // calculate available power
                float powerreceivedMW = Math.Max(consumeFNResource(powerrequirementsMW * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);

                float normalisedRevievedPowerMW = powerreceivedMW / TimeWarp.fixedDeltaTime;

                // if power requirement sufficiently low, retreive power from KW source
                if (powerrequirementsMW < 2 && normalisedRevievedPowerMW <= powerrequirementsMW)
                {
                    var requiredKW = (float)(powerrequirementsMW - normalisedRevievedPowerMW) * 1000;
                    var recievedKW = ORSHelper.fixedRequestResource(part, "ElectricCharge", requiredKW * TimeWarp.fixedDeltaTime);
                    powerreceivedMW += (recievedKW / 1000);
                }

                last_power_percentage = offlineCollecting ? last_power_percentage : (float)(powerreceivedMW / powerrequirementsMW / TimeWarp.fixedDeltaTime);
            }
            else
            {
                last_power_percentage = 0;
                powerrequirementsMW = 0;
            }

            recievedPower = powerrequirementsMW < 2 
                ? (last_power_percentage * powerrequirementsMW * 1000).ToString("0.0") + " KW / " + (powerrequirementsMW * 1000).ToString("0.0") + " KW"
                : (last_power_percentage * powerrequirementsMW).ToString("0.0") + " MW / " + powerrequirementsMW.ToString("0.0") + " MW";

            double resourceChange = scoopedAtm * last_power_percentage * deltaTimeInSeconds;

            if (offlineCollecting)
            {
                string numberformat = resourceChange > 100 ? "0" : "0.00";
                ScreenMessages.PostScreenMessage("Atmospheric Scoop collected " + resourceChange.ToString(numberformat) + " " + resourceStoragename, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            //resflowf = (float)part.RequestResource(atmospheric_resource_name, -scoopedAtm * powerpcnt * TimeWarp.fixedDeltaTime);
            resflowf = (float)ORSHelper.fixedRequestResource(part, resourceStoragename, -resourceChange);
            resflowf = -resflowf / TimeWarp.fixedDeltaTime;
            UpdateResourceFlow();
        }

        private void UpdateResourceFlow()
        {
            resflow = resflowf.ToString("0.0000000");
        }

        public override string getResourceManagerDisplayName() 
        {
            return part.partInfo.title;
        }

    }
}
