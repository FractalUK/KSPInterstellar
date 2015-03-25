extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin 
{
    [KSPModule("Cyrostat Tank")]
    class FNModuleCryostat : FNResourceSuppliableModule 
    {
        // Confuration
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public string resourceGUIName;
        [KSPField(isPersistant = false)]
        public float boilOffRate;
        [KSPField(isPersistant = false)]
        public float powerReqKW;
        [KSPField(isPersistant = false)]
        public float fullPowerReqKW = 0;
        [KSPField(isPersistant = false)]
        public float boilOffMultiplier;
        [KSPField(isPersistant = false)]
        public float boilOffAddition;
        [KSPField(isPersistant = false)]
        public int maxStoreAmount = 0;

        [KSPField(isPersistant = false)]
        public string StartActionName = "Activate Cooling";
        [KSPField(isPersistant = false)]
        public string StopActionName = "Deactivate Cooling";

        // Persistant
        [KSPField(isPersistant = true)]
        bool isDisabled;
        
        protected PartResource cryostat_resource;
        protected double recievedPowerKW;
        protected double currentPowerReq;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStatusStr = String.Empty;

        [KSPEvent(guiName = "Deactivate Cooling", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void Deactivate()
        {
            isDisabled = true;
        }

        [KSPEvent(guiName = "Activate Cooling", guiActive = true, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void Activate()
        {
            isDisabled = false;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            if (fullPowerReqKW == 0)
                fullPowerReqKW = powerReqKW;

            Events["Activate"].guiName = StartActionName;
            Events["Deactivate"].guiName = StopActionName;
           
            this.part.force_activate();
        }

        public override void OnUpdate() 
        {
            if (part.Resources.Contains(resourceName))
                cryostat_resource = part.Resources[resourceName];
            else
                cryostat_resource = null;


            if (cryostat_resource != null)
            {
                Events["Activate"].active = isDisabled;
                Events["Deactivate"].active = !isDisabled;
                Fields["powerStatusStr"].guiActive = true;

                var resourceRatio = cryostat_resource.amount / cryostat_resource.maxAmount;

                currentPowerReq = fullPowerReqKW > powerReqKW
                    ? powerReqKW + (fullPowerReqKW - powerReqKW) * resourceRatio
                    : fullPowerReqKW + (powerReqKW - fullPowerReqKW) * (1 - resourceRatio);

                powerStatusStr = powerReqKW < 1.0e+3
                    ? recievedPowerKW.ToString("0.000") + " KW / " + currentPowerReq.ToString("0.000") + " KW"
                    : powerReqKW < 1.0e+6
                        ? (recievedPowerKW / 1.0e+3).ToString("0.000") + " MW / " + (currentPowerReq / 1.0e+3).ToString("0.000") + " MW"
                        : (recievedPowerKW / 1.0e+6).ToString("0.000") + " GW / " + (currentPowerReq / 1.0e+6).ToString("0.000") + " GW";
            }
            else
            {
                Events["Activate"].active = false;
                Events["Deactivate"].active = false;
                Fields["powerStatusStr"].guiActive = false;
            }
        }

        public override void OnFixedUpdate() 
        {
            if (cryostat_resource != null && cryostat_resource.amount > 0.0 && currentPowerReq > 0) 
            {
                if (!isDisabled)
                {
                    var fixedPowerReqKW = currentPowerReq * TimeWarp.fixedDeltaTime;

                    double fixedRecievedChargeKW = consumeFNResource(fixedPowerReqKW / 1000.0, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000.0;

                    if (powerReqKW < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                        fixedRecievedChargeKW += ORSHelper.fixedRequestResource(part, "ElectricCharge", fixedPowerReqKW - fixedRecievedChargeKW);

                    recievedPowerKW = fixedRecievedChargeKW / TimeWarp.fixedDeltaTime;
                }
                else
                    recievedPowerKW = 0;

                var reducuction = (recievedPowerKW >= currentPowerReq) ? boilOffRate : (boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq))) * boilOffMultiplier;

                cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - (reducuction * cryostat_resource.maxAmount * TimeWarp.fixedDeltaTime));
            }
        }

        public override string getResourceManagerDisplayName() 
        {
            return resourceGUIName + " Cryostat";
        }

        public override int getPowerPriority() 
        {
            return 2;
        }

        public override string GetInfo() 
        {
            return "Power Requirements: " + powerReqKW.ToString("0.0") + " KW\n Powered Boil Off Fraction: " + boilOffRate * GameConstants.EARH_DAY_SECONDS + " /day\n Unpowered Boil Off Fraction: " + (boilOffRate + boilOffAddition) * boilOffMultiplier * GameConstants.EARH_DAY_SECONDS + " /day";
        }
    }
}
