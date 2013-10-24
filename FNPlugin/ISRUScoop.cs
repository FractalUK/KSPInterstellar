using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class ISRUScoop : FNResourceSuppliableModule {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Flow")]
        public string resflow;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Resource")]
        public string currentresourceStr;
        [KSPField(isPersistant = false)]
        public float scoopair;
        
        protected float resflowf;
        protected int currentresource;

        protected int drawCount = 0;

        [KSPEvent(guiActive = true, guiName = "Toggle Resource", active = true)]
        public void ToggleResource() {

            currentresource++;
            if (currentresource >= PluginHelper.atomspheric_resources.Length) {
                currentresource = 0;
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
            
            float respcent = PluginHelper.getAtmosphereResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource)*100;
            currentresourceStr = PluginHelper.atomspheric_resources[currentresource] + "(" + respcent + "%)";
            resflow = resflowf.ToString("0.0000");
        }

        public override void OnFixedUpdate() {
            drawCount++;
            double resourcedensity = PartResourceLibrary.Instance.GetDefinition(PluginHelper.atomspheric_resources_tocollect[currentresource]).density;
            double respcent = PluginHelper.getAtmosphereResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);
            double airdensity = part.vessel.atmDensity/1000;
            //float airspeed = (float)part.vessel.srf_velocity.magnitude + 10;
            double powerrequirements = scoopair / 0.01f * 6f;

			double airspeed = part.vessel.srf_velocity.magnitude+40.0;
			double air = airspeed * airdensity * scoopair / resourcedensity;


			if (respcent > 0  && vessel.altitude <= PluginHelper.getMaxAtmosphericAltitude(vessel.mainBody)) {

				double scoopedAtm = air*respcent;

				float powerreceived = Math.Max (consumeFNResource (powerrequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES), 0);
				float powerpcnt = (float) (powerreceived / powerrequirements / TimeWarp.fixedDeltaTime);

				resflowf = (float)part.RequestResource (PluginHelper.atomspheric_resources_tocollect [currentresource], -scoopedAtm * powerpcnt*TimeWarp.fixedDeltaTime);
				resflowf = -resflowf / TimeWarp.fixedDeltaTime;

                
			} 
        }

    }
}
