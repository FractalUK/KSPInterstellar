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
        // 
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

        //private List<PartResource> resourceCollection = new List<PartResource>();
        private PartResourceList _partResources;
        private PartResource _selectedResource;
        private bool isInEditorMode;
        private double currentPowerReq;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStatusStr = String.Empty;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Type")]
        private int _selectedResourceId = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Resource")]
        public string resourceType = String.Empty;

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


        [KSPEvent(guiName = "Swap Type", guiActive = false, guiActiveEditor = false, guiActiveUnfocused = false)]
        public void SwapType()
        {
            if (SelectedResourceId < (_partResources.Count - 1))
            {
                print("[KSPI] SelectedResourceId = " + SelectedResourceId + " swapping to next resource");
                SelectedResourceId++;
            }
            else
            {
                print("[KSPI]  swapping to first resource");
                SelectedResourceId = 0;
            }
        }
        public int SelectedResourceId
        {
            get { return _selectedResourceId; }
            set { SetSelectedResourceId(value, false); }
        }

        private string SetSelectedResourceId(int index, bool startup)
        {
            int counter = 0;
            foreach (PartResource resource in _partResources)
            {
                if (counter == index)
                {
                    _selectedResource = resource;
                    _selectedResourceId = counter;
                    resourceType = _selectedResource.resourceName;

                    resource.maxAmount = maxStoreAmount;
                    resource.useGUILayout = true;
                    resource.enabled = true;
                    resource.flowMode = PartResource.FlowMode.Both;
                    resource.flowState = true;

                    if (isInEditorMode || startup)
                        resource.amount = maxStoreAmount;
                    else
                        resource.amount = 0;
                }
                else
                {
                    resource.enabled = false;
                    resource.useGUILayout = false;
                    resource.flowMode = PartResource.FlowMode.None;
                    resource.flowState = false;
                    resource.amount = 0;
                    resource.maxAmount = 1;
                }

                counter++;

            }

            if (_selectedResource != null)
                return _selectedResource.resourceName;
            else
                return "missing";
        }



        public override void OnStart(PartModule.StartState state) 
        {
            if (fullPowerReqKW == 0)
                fullPowerReqKW = powerReqKW;

            Events["Activate"].guiName = StartActionName;
            Events["Deactivate"].guiName = StopActionName;

            //_partResources = part.Resources;
            //isInEditorMode = state == StartState.Editor;
            //if (maxStoreAmount > 0)
            //{
            //    var partResource = _partResources.list.Last();
            //    part.Resources.list.Remove(partResource);
            //    PartModule.DestroyImmediate(partResource);

            //    resourceType = SetSelectedResourceId(_selectedResourceId, true);
            //    print("[KSPI] resourceType " + resourceType);
            //}

            //if (state == StartState.Editor) { return; }

            //if (maxStoreAmount > 0)
            //{
            //    ConfigNode node = new ConfigNode("RESOURCE");
            //    node.AddValue("name", "LqdMethane");
            //    node.AddValue("amount", 0);
            //    node.AddValue("maxAmount", maxStoreAmount);
            //    var partResource = part.AddResource(node);
            //    partResource.enabled = true;
            //}
            
            this.part.force_activate();
            //cryostat_resource = part.Resources[resourceName];
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

                if (_selectedResource != null)
                    resourceType = _selectedResource.resourceName;
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
