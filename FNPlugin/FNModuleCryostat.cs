extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Cyrostat Tank")]
    class FNModuleCryostat : FNResourceSuppliableModule {
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public string resourceGUIName;
        [KSPField(isPersistant = false)]
        public float boilOffRate;
        [KSPField(isPersistant = false)]
        public float powerReqKW;
        [KSPField(isPersistant = false)]
        public float boilOffMultiplier;
        [KSPField(isPersistant = false)]
        public float boilOffAddition;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStatusStr;

        protected PartResource cryostat_resource;
        protected double power_d;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();
            cryostat_resource = part.Resources[resourceName];
        }

        public override void OnUpdate() {
            powerStatusStr = power_d.ToString("0.0") + " KW / " + powerReqKW.ToString("0.0") + " KW";
        }

        public override void OnFixedUpdate() {
            if (cryostat_resource != null && cryostat_resource.amount > 0.0) 
            {
                double charge = consumeFNResource(powerReqKW / 1000.0 * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000.0;
                if (charge <= powerReqKW * TimeWarp.fixedDeltaTime) 
                {
                    double rem_charge = powerReqKW * TimeWarp.fixedDeltaTime - charge;
                    charge += ORSHelper.fixedRequestResource(part, "ElectricCharge", rem_charge);
                }
                power_d = charge / TimeWarp.fixedDeltaTime;

                if (charge >= powerReqKW) 
                {
                    cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boilOffRate * TimeWarp.fixedDeltaTime * cryostat_resource.maxAmount);
                } 
                else 
                {
                    cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - (boilOffRate + boilOffAddition) * TimeWarp.fixedDeltaTime * cryostat_resource.maxAmount * boilOffMultiplier);
                }
            }
        }

        public override string getResourceManagerDisplayName() {
            return resourceGUIName + " Cryostat";
        }

        public override int getPowerPriority() {
            return 2;
        }

        public override string GetInfo() {
            return "Power Requirements: " + powerReqKW.ToString("0.0") + " KW\n Powered Boil Off Fraction: " + boilOffRate * GameConstants.EARH_DAY_SECONDS + " /day\n Unpowered Boil Off Fraction: " + (boilOffRate + boilOffAddition) * boilOffMultiplier * GameConstants.EARH_DAY_SECONDS + " /day";
        }
    }
}
