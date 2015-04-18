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
        public float resourceRatioExp = 0.5f;
        [KSPField(isPersistant = false)]
        public float boilOffRate;
        [KSPField(isPersistant = false)]
        public float powerReqKW;
        [KSPField(isPersistant = false)]
        public float fullPowerReqKW = 0;
        [KSPField(isPersistant = false)]
        public float boilOffMultiplier;
        [KSPField(isPersistant = false)]
        public float boilOffBase = 10000;
        [KSPField(isPersistant = false)]
        public float boilOffAddition;
        [KSPField(isPersistant = false)]
        public float boilOffTemp = 20.271f;
        [KSPField(isPersistant = false)]
        public float convectionMod = 1;

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
        [KSPField(isPersistant = false, guiActive = true, guiName = "Boiloff")]
        public string boiloffStr;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Environment Factor")]
        public float environmentFactor;

        public float boiloff;

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
                bool coolingIsRelevant = powerReqKW > 0 && cryostat_resource.amount > 0;

                Events["Activate"].active = isDisabled && coolingIsRelevant;
                Events["Deactivate"].active = !isDisabled && coolingIsRelevant;
                Fields["powerStatusStr"].guiActive = coolingIsRelevant;
                Fields["boiloffStr"].guiActive = boiloff > 0;

                var atmosphereModifier = convectionMod == -1 ?  0 : convectionMod + FlightGlobals.getStaticPressure(vessel.transform.position) / (convectionMod + 1);
                var temperatureModifier = Math.Max(0, part.temperature + 273.15 - boilOffTemp) / 273.15;
                environmentFactor = (float)(atmosphereModifier * temperatureModifier);

                if (powerReqKW > 0)
                {
                    var resourceRatio = Math.Pow(cryostat_resource.amount / cryostat_resource.maxAmount, resourceRatioExp);

                    currentPowerReq = fullPowerReqKW > powerReqKW
                        ? powerReqKW + (fullPowerReqKW - powerReqKW) * resourceRatio
                        : fullPowerReqKW + (powerReqKW - fullPowerReqKW) * (1 - resourceRatio);

                    currentPowerReq *= environmentFactor;

                    powerStatusStr = powerReqKW < 1.0e+3
                        ? recievedPowerKW.ToString("0.00") + " KW / " + currentPowerReq.ToString("0.00") + " KW"
                        : powerReqKW < 1.0e+6
                            ? (recievedPowerKW / 1.0e+3).ToString("0.000") + " MW / " + (currentPowerReq / 1.0e+3).ToString("0.000") + " MW"
                            : (recievedPowerKW / 1.0e+6).ToString("0.000") + " GW / " + (currentPowerReq / 1.0e+6).ToString("0.000") + " GW";
                }
                else
                    currentPowerReq = 0;
            }
            else
            {
                Events["Activate"].active = false;
                Events["Deactivate"].active = false;
                Fields["powerStatusStr"].guiActive = false;
                Fields["boiloffStr"].guiActive = false;
            }
        }

        public override void OnFixedUpdate()
        {
            if (cryostat_resource == null || cryostat_resource.amount <= 0.0)
            {
                boiloff = 0;
                return;
            }

            if (!isDisabled && currentPowerReq > 0)
            {
                var fixedPowerReqKW = currentPowerReq * TimeWarp.fixedDeltaTime;

                double fixedRecievedChargeKW = consumeFNResource(fixedPowerReqKW / 1000.0, FNResourceManager.FNRESOURCE_MEGAJOULES) * 1000.0;

                if (powerReqKW < 1000 && fixedRecievedChargeKW <= fixedPowerReqKW)
                    fixedRecievedChargeKW += ORSHelper.fixedRequestResource(part, "ElectricCharge", fixedPowerReqKW - fixedRecievedChargeKW);

                recievedPowerKW = fixedRecievedChargeKW / TimeWarp.fixedDeltaTime;
            }
            else
                recievedPowerKW = 0;

            var boiloffReducuction = recievedPowerKW >= currentPowerReq
                    ? boilOffRate
                    : (boilOffRate + (boilOffAddition * (1 - recievedPowerKW / currentPowerReq)));

            boiloff = boiloffReducuction <= 0 ? 0 :
                (float)(environmentFactor * boiloffReducuction * boilOffMultiplier * boilOffBase);

            boiloffStr = boiloff.ToString("0.00") + " L/s " + cryostat_resource.resourceName;

            cryostat_resource.amount = Math.Max(0, cryostat_resource.amount - boiloff * TimeWarp.fixedDeltaTime);
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
