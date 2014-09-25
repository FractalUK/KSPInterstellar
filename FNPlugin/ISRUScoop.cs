extern alias ORSv1_4_2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_2::OpenResourceSystem;

namespace FNPlugin {
    class ISRUScoop : FNResourceSuppliableModule {
        [KSPField(isPersistant = true)]
        public bool scoopIsEnabled = false;
        [KSPField(isPersistant = true)]
        public int currentresource = 0;

        [KSPField(isPersistant = false)]
        public float scoopair = 0;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Flow")]
        public string resflow;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Resource")]
        public string currentresourceStr;
        
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
        public void ToggleResource() {
            currentresource++;

            if (ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource) == null && ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource) > 0 && currentresource != 0) {
                ToggleResource();
            }

            if (currentresource >= ORSAtmosphericResourceHandler.getAtmosphericCompositionForBody(vessel.mainBody.flightGlobalsIndex).Count) {
                currentresource = 0;
            }
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
        public void ToggleScoopAction(KSPActionParam param) {
            if (scoopIsEnabled) {
                DisableScoop();
            } else {
                ActivateScoop();
            }
        }

        [KSPAction("Toggle Resource")]
        public void ToggleToggleResourceAction(KSPActionParam param) {
            ToggleResource();
        }

        public override void OnStart(PartModule.StartState state) {
            Actions["ToggleToggleResourceAction"].guiName = Events["ToggleResource"].guiName = String.Format("Toggle Resource");

            if (state == StartState.Editor) { return; }
            this.part.force_activate();
        }

        public override void OnUpdate() {
            Events["ActivateScoop"].active = !scoopIsEnabled;
            Events["DisableScoop"].active = scoopIsEnabled;
            Events["ToggleResource"].active = scoopIsEnabled;
            Fields["resflow"].guiActive = scoopIsEnabled;
            Fields["currentresourceStr"].guiActive = scoopIsEnabled;
            double respcent = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource)*100;
            string resname = ORSAtmosphericResourceHandler.getAtmosphericResourceDisplayName(vessel.mainBody.flightGlobalsIndex, currentresource);
            if (resname != null) {
                currentresourceStr = resname + "(" + respcent + "%)";
            }
            resflow = resflowf.ToString("0.0000");
        }

        public override void OnFixedUpdate() {
            if (scoopIsEnabled) {
                string atmospheric_resource_name = ORSAtmosphericResourceHandler.getAtmosphericResourceName(vessel.mainBody.flightGlobalsIndex, currentresource);
                if (atmospheric_resource_name != null) {
                    double resourcedensity = PartResourceLibrary.Instance.GetDefinition(atmospheric_resource_name).density;
                    double respcent = ORSAtmosphericResourceHandler.getAtmosphericResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);
                    //double resourcedensity = PartResourceLibrary.Instance.GetDefinition(PluginHelper.atomspheric_resources_tocollect[currentresource]).density;
                    //double respcent = PluginHelper.getAtmosphereResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);

                    double airdensity = part.vessel.atmDensity / 1000;
                    double powerrequirements = scoopair / 0.15f * 6f;

                    double airspeed = part.vessel.srf_velocity.magnitude + 40.0;
                    double air = airspeed * airdensity * scoopair / resourcedensity;

                    if (respcent > 0 && vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {
                        double scoopedAtm = air * respcent;

                        float powerreceived = Math.Max(consumeFNResource(powerrequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
                        float powerpcnt = (float)(powerreceived / powerrequirements / TimeWarp.fixedDeltaTime);

                        //resflowf = (float)part.RequestResource(atmospheric_resource_name, -scoopedAtm * powerpcnt * TimeWarp.fixedDeltaTime);
                        resflowf = (float)ORSHelper.fixedRequestResource(part,atmospheric_resource_name, -scoopedAtm * powerpcnt * TimeWarp.fixedDeltaTime);
                        resflowf = -resflowf / TimeWarp.fixedDeltaTime;
                    }
                } else {

                }
            }
        }

        public override string getResourceManagerDisplayName() {
            return "Atmospheric Scoop";
        }

    }
}
