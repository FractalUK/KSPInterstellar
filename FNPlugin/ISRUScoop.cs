using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            float resourcedensity = PartResourceLibrary.Instance.GetDefinition(PluginHelper.atomspheric_resources_tocollect[currentresource]).density;
            float respcent = PluginHelper.getAtmosphereResourceContent(vessel.mainBody.flightGlobalsIndex, currentresource);
            float airdensity = (float)part.vessel.atmDensity;
            float airspeed = (float)part.vessel.srf_velocity.magnitude + 10;
            float powerrequirements = scoopair / 0.01f * 6f;
            if (respcent > 0) {
                float air = airspeed * airdensity * scoopair / resourcedensity * TimeWarp.fixedDeltaTime;
                float scoopedAtm = part.RequestResource("IntakeAtm", air);
                //float powerreceived = part.RequestResource("Megajoules", powerrequirements * TimeWarp.fixedDeltaTime);
                float powerreceived = consumePower(powerrequirements * TimeWarp.fixedDeltaTime,FNResourceManager.FNRESOURCE_MEGAJOULES);
                float powerpcnt = powerreceived / powerrequirements / TimeWarp.fixedDeltaTime;
                if (drawCount % 2 == 0) {
                    resflowf = part.RequestResource(PluginHelper.atomspheric_resources_tocollect[currentresource], -scoopedAtm * powerpcnt * respcent * 0.2f);
                    resflowf = -resflowf / TimeWarp.fixedDeltaTime/2;
                }
                
            }
        }

    }
}
