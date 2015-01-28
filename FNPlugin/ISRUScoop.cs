extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

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
        [KSPField(isPersistant = false)]
        public float scoopair = 0;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Flow")]
        public string resflow;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Resource")]
        public string currentresourceStr;
        
        // internals
        protected float resflowf = 0;

        [KSPEvent(guiActive = true, guiName = "Activate Scoop", active = true)]
        public void ActivateScoop() {
            scoopIsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Scoop", active = true)]
        public void DisableScoop() {
            scoopIsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Resource", active = true)]
        public void ToggleResource() 
        {
            currentresource++;

            if (ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource) == null && ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource) > 0 && currentresource != 0) 
                ToggleResource();
            
            if (currentresource >= ORSAtmosphericResourceHandler.getAtmosphericCompositionForBody(vessel.mainBody.flightGlobalsIndex).Count) 
                currentresource = 0;
        }

        [KSPAction("Activate Scoop")]
        public void ActivateScoopAction(KSPActionParam param) {
            ActivateScoop();
        }

        [KSPAction("Disable Scoop")]
        public void DisableScoopAction(KSPActionParam param) {
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
            if (vessel.altitude < (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * (1.0 + (1.0 - PluginHelper.MaxAtmosphericAltitudeMult))))
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

            // verify that an electric engine is present
            var plasmaEngine = part.vessel.parts.Find(p => p.FindModulesImplementing<ElectricEngineControllerFX>().Any());
            if (plasmaEngine == null)
            {
                ScreenMessages.PostScreenMessage("No electric engine available to balance atmospheric drag", 10.0f, ScreenMessageStyle.LOWER_CENTER);
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

            double resourcePercentage = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource)*100;
            string resourceDisplayName = ORSAtmosphericResourceHandler.getAtmosphericResourceDisplayName(vessel.mainBody.flightGlobalsIndex, currentresource);
            if (resourceDisplayName != null) 
                currentresourceStr = resourceDisplayName + "(" + resourcePercentage + "%)";
            
            resflow = resflowf.ToString("0.000000");
        }

        public override void OnFixedUpdate() 
        {
            if (!scoopIsEnabled) return;
            
            // scoop athmosphere for a single frame
            ScoopAthmosphere(TimeWarp.fixedDeltaTime, false);

            // store current time in case vesel is unloaded
            last_active_time = (float)Planetarium.GetUniversalTime();
        }

        private void ScoopAthmosphere(double deltaTimeInSeconds, bool offlineCollecting)
        {
            string atmospheric_resource_name = ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource);

            if (atmospheric_resource_name == null)
            {
                resflowf = 0.0f;
                return;
            }

            double resourcedensity = PartResourceLibrary.Instance.GetDefinition(atmospheric_resource_name).density;
            double respcent = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);
            //double resourcedensity = PartResourceLibrary.Instance.GetDefinition(PluginHelper.atomspheric_resources_tocollect[currentresource]).density;
            //double respcent = PluginHelper.getAtmosphereResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);

            double airdensity = (part.vessel.atmDensity + PluginHelper.MinAtmosphericAirDensity) / 1000.0;
            double powerrequirements = (scoopair / 0.15f) * 6f * (float)PluginHelper.PowerConsumptionMultiplier;
                
            double airspeed = part.vessel.srf_velocity.magnitude + 40.0;
            double air = airspeed * airdensity * scoopair / resourcedensity;

            if (respcent == 0 || vessel.altitude > (PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody) * PluginHelper.MaxAtmosphericAltitudeMult))
            {
                resflowf = 0.0f;
                return;
            }

            double scoopedAtm = air * respcent;
            float powerreceived = Math.Max(consumeFNResource(powerrequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
            last_power_percentage = offlineCollecting ? last_power_percentage : (float)(powerreceived / powerrequirements / TimeWarp.fixedDeltaTime);
            double resourceChange = scoopedAtm * last_power_percentage * deltaTimeInSeconds;

            if (offlineCollecting)
            {
                string numberformat = resourceChange > 100 ? "0" : "0.00";
                ScreenMessages.PostScreenMessage("Atmospheric Scoop collected " + resourceChange.ToString(numberformat) + " " + atmospheric_resource_name, 10.0f, ScreenMessageStyle.LOWER_CENTER);
            }

            //resflowf = (float)part.RequestResource(atmospheric_resource_name, -scoopedAtm * powerpcnt * TimeWarp.fixedDeltaTime);
            resflowf = (float)ORSHelper.fixedRequestResource(part, atmospheric_resource_name, -resourceChange);
            resflowf = -resflowf / TimeWarp.fixedDeltaTime;
        }

        public override string getResourceManagerDisplayName() 
        {
            return "Atmospheric Scoop";
        }

    }
}
