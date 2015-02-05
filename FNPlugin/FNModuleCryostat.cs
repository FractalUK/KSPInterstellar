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
        [KSPField(isPersistant = false)]
        public int maxStoreAmount = 0;
        
        protected PartResource cryostat_resource;
        protected double power_d;

        //private List<PartResource> resourceCollection = new List<PartResource>();
        private PartResourceList _partResources;
        private PartResource _selectedResource;
        private bool isInEditorMode;


        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Type")]
        private int _selectedResourceId = 0;

        //GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string powerStatusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Resource")]
        public string resourceType = String.Empty;


        [KSPEvent(guiName = "Swap Type", guiActiveEditor = true, guiActiveUnfocused = false, guiActive = true)]
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
            _partResources = part.Resources;

            isInEditorMode = state == StartState.Editor;

            if (maxStoreAmount > 0)
            {
                resourceType = SetSelectedResourceId(_selectedResourceId, true);
                print("[KSPI] resourceType " + resourceType);
            }

            if (state == StartState.Editor) { return; }
            
            this.part.force_activate();
            cryostat_resource = part.Resources[resourceName];
        }

        public override void OnUpdate() 
        {
            powerStatusStr = power_d.ToString("0.0") + " KW / " + powerReqKW.ToString("0.0") + " KW";

            if (_selectedResource != null)
                resourceType = _selectedResource.resourceName;
        }

        public override void OnFixedUpdate() 
        {
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
