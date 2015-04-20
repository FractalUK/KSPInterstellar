using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FNPlugin.Extensions;
using System.Reflection;
using UnityEngine;

namespace FNPlugin.Storage
{
    public class FSresource
    {
        //public PartResource resource;
        public string name;
        public int ID;
        public float ratio;
        public double currentSupply = 0f;
        public double amount = 0f;
        public double maxAmount = 0f;

        public FSresource(string _name, float _ratio)
        {
            name = _name;
            ID = _name.GetHashCode();
            ratio = _ratio;
        }

        public FSresource(string _name)
        {
            name = _name;
            ID = _name.GetHashCode();
            ratio = 1f;
        }
    }

    public class FSmodularTank
    {
        public List<FSresource> resources = new List<FSresource>();
    }

    public class InsterstellarFuelSwitch : PartModule, IPartCostModifier
    {
        [KSPField]
        public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField]
        public string resourceAmounts = "100;75,25;200";
        [KSPField]
        public string initialResourceAmounts = "";
        [KSPField]
        public float basePartMass = 0.25f;
        [KSPField]
        public string tankMass = "0;0;0;0";
        [KSPField]
        public string tankCost = "0; 0; 0; 0";
        [KSPField]
        public bool displayCurrentTankCost = false;
        [KSPField]
        public bool hasGUI = true;
        [KSPField]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        [KSPField]
        public bool showInfo = true; // if false, does not feed info to the part list pop up info menu
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Added cost")]
        public float addedCost = 0f;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Dry mass")]
        public float dryMassInfo = 0f;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Multiplier")]
        public float volumeMultiplier = 1f;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Multiplier")]
        public float massMultiplier = 1f;

        // Persistants
        [KSPField(isPersistant = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public bool hasLaunched = false;
        [KSPField(isPersistant = true)]
        public bool gameLoaded = false;
        [KSPField(isPersistant = true)]
        public bool configLoaded = false;

        private List<FSmodularTank> tankList;
        private List<double> weightList;
        private List<double> tankCostList;
        private bool initialized = false;

        UIPartActionWindow tweakableUI;

        public override void OnStart(PartModule.StartState state)
        {
            Debug.Log("InsterstellarFuelSwitch OnStart loaded persistant selectedTankSetup = " + selectedTankSetup);
            InitializeData();

            if (selectedTankSetup == -1)
                selectedTankSetup = 0;

            if (state != StartState.Editor)
            {
                Debug.Log("InsterstellarFuelSwitch OnStart started outside editor");

                AssignResourcesToPart(false, gameLoaded);
                gameLoaded = true;
            }
            else
            {
                Debug.Log("InsterstellarFuelSwitch OnStart started inside editor");
                AssignResourcesToPart(false, false);
            }
        }
        public override void OnAwake()
        {
            if (configLoaded)
                InitializeData();
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (!configLoaded)
                InitializeData();

            configLoaded = true;
        }

        private void InitializeData()
        {
            if (!initialized)
            {
                SetupTankList(false);
                weightList = ParseTools.ParseDoubles(tankMass);
                tankCostList = ParseTools.ParseDoubles(tankCost);

                if (HighLogic.LoadedSceneIsFlight) 
                    hasLaunched = true;

                if (hasGUI)
                {
                    Events["nextTankSetupEvent"].guiActive = availableInFlight;
                    Events["nextTankSetupEvent"].guiActiveEditor = availableInEditor;
                    Events["previousTankSetupEvent"].guiActive = availableInFlight;
                    Events["previousTankSetupEvent"].guiActiveEditor = availableInEditor;
                }
                else
                {
                    Events["nextTankSetupEvent"].guiActive = false;
                    Events["nextTankSetupEvent"].guiActiveEditor = false;
                    Events["previousTankSetupEvent"].guiActive = false;
                    Events["previousTankSetupEvent"].guiActiveEditor = false;
                }

                if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    Fields["addedCost"].guiActiveEditor = displayCurrentTankCost;

                initialized = true;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next tank setup")]
        public void nextTankSetupEvent()
        {
            selectedTankSetup++;
            Debug.Log("InsterstellarFuelSwitch nextTankSetupEvent selectedTankSetup++ = " + selectedTankSetup);

            if (selectedTankSetup >= tankList.Count)
            {
                selectedTankSetup = 0;
                Debug.Log("InsterstellarFuelSwitch nextTankSetupEvent selectedTankSetup = 0 ");
            }

            AssignResourcesToPart(true);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous tank setup")]
        public void previousTankSetupEvent()
        {
            selectedTankSetup--;
            Debug.Log("InsterstellarFuelSwitch previousTankSetupEvent selectedTankSetup-- = " + selectedTankSetup);

            if (selectedTankSetup < 0)
            {
                selectedTankSetup = tankList.Count - 1;
                Debug.Log("InsterstellarFuelSwitch previousTankSetupEvent tankList.Count - 1 = " + selectedTankSetup);
            }

            AssignResourcesToPart(true);
        }
        public void SelectTankSetup(int i, bool calledByPlayer)
        {
            InitializeData();
            if (selectedTankSetup != i)
            {
                selectedTankSetup = i;
                Debug.Log("InsterstellarFuelSwitch selectTankSetup selectedTankSetup = i = " + selectedTankSetup);
                AssignResourcesToPart(calledByPlayer);
            }
        }

        private void AssignResourcesToPart(bool calledByPlayer, bool calledAtStartup = false)
        {
            // destroying a resource messes up the gui in editor, but not in flight.
            SetupTankInPart(part, calledByPlayer, calledAtStartup);
            if (HighLogic.LoadedSceneIsEditor)
            {
                for (int s = 0; s < part.symmetryCounterparts.Count; s++)
                {
                    SetupTankInPart(part.symmetryCounterparts[s], calledByPlayer);
                    InsterstellarFuelSwitch symSwitch = part.symmetryCounterparts[s].GetComponent<InsterstellarFuelSwitch>();
                    if (symSwitch != null)
                    {
                        symSwitch.selectedTankSetup = selectedTankSetup;
                        Debug.Log("InsterstellarFuelSwitch assignResourcesToPart symSwitch.selectedTankSetup = selectedTankSetup = " + selectedTankSetup);
                    }
                }
            }
            //Debug.Log("refreshing UI");
            if (tweakableUI == null)
                tweakableUI = part.FindActionWindow();

            if (tweakableUI != null)
                tweakableUI.displayDirty = true;
            else
                Debug.Log("InsterstellarFuelSwitch assignResourcesToPart - no UI to refresh");
        }
        private void SetupTankInPart(Part currentPart, bool calledByPlayer, bool calledAtStartup = false)
        {
            // create new ResourceNode
            List<string> newResources = new List<string>();
            List<ConfigNode> newResourceNodes = new List<ConfigNode>();

            int tankCount = selectedTankSetup;
            if(tankCount >= 0 && tankCount < tankList.Count) // Why was this ever a for loop?
            {
                Debug.Log("InsterstellarFuelSwitch assignResourcesToPart setupTankInPart = " + selectedTankSetup);
                for (int resourceCount = 0; resourceCount < tankList[tankCount].resources.Count; resourceCount++)
                {
                    if (tankList[tankCount].resources[resourceCount].name == "Structural") continue;

                    var resourceName = tankList[tankCount].resources[resourceCount].name;
                    newResources.Add(resourceName);

                    ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                    double maxAmount = tankList[tankCount].resources[resourceCount].maxAmount * volumeMultiplier;

                    newResourceNode.AddValue("name", resourceName);
                    newResourceNode.AddValue("maxAmount", maxAmount);

                    PartResource existingResource = null;
                    if (HighLogic.LoadedSceneIsFlight)
                        foreach(PartResource pr in part.Resources)
                            if(pr.name.Equals(resourceName))
                            {
                                existingResource = pr;
                                break;
                            }

                    if(existingResource != null)
                        newResourceNode.AddValue("amount", Math.Min(existingResource.amount, maxAmount));
                    else if (calledByPlayer && !HighLogic.LoadedSceneIsEditor)
                        newResourceNode.AddValue("amount", 0.0f);
                    else
                        newResourceNode.AddValue("amount", tankList[tankCount].resources[resourceCount].amount * volumeMultiplier);

                    newResourceNodes.Add(newResourceNode);
                }
            }

            // verify we need to update
            if (newResourceNodes.Count > 0) 
            {
                currentPart.Resources.list.Clear();
                PartResource[] partResources = currentPart.GetComponents<PartResource>();
                for (int i = 0; i < partResources.Length; i++)
                {
                    var resource = partResources[i];
                    var resourcename = resource.resourceName;
                    Debug.Log("InsterstellarFuelSwitch setupTankInPart removing resource: " + resourcename);
                    DestroyImmediate(resource);
                }

                Debug.Log("InsterstellarFuelSwitch setupTankInPart adding new resources: " + ParseTools.Print(newResources));
                foreach (var resourceNode in newResourceNodes)
                {
                    currentPart.AddResource(resourceNode);
                }
            }
            else
                Debug.Log("InsterstellarFuelSwitch setupTankInPart keeps existing resources unchanged");

            // This also needs to be done when going from a setup with resources to a setup with no resources.
            currentPart.Resources.UpdateList();
            UpdateWeight(currentPart, selectedTankSetup, calledByPlayer);
            UpdateCost();
        }

        private float UpdateCost() // Does this even do anything?
        {
            //GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship); //crashes game
            return selectedTankSetup >= 0 && selectedTankSetup < tankCostList.Count ? (float)tankCostList[selectedTankSetup] : 0f;
        }
        private void UpdateWeight(Part currentPart, int newTankSetup, bool calledByPlayer = false)
        {
            // when changed by player
            if (calledByPlayer && HighLogic.LoadedSceneIsFlight) return;

            if (newTankSetup < weightList.Count)
                currentPart.mass = (float)((basePartMass + weightList[newTankSetup]) * massMultiplier);
        }
        public override void OnUpdate()
        {
            //There were some issues with resources slowly trickling in, so I changed this to 0.1% instead of empty.
            bool showSwitchButtons = availableInFlight && !part.GetComponents<PartResource>().Any(r => r.amount > r.maxAmount / 1000);

            Events["nextTankSetupEvent"].guiActive = showSwitchButtons;
            Events["previousTankSetupEvent"].guiActive = showSwitchButtons;
        }
        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                dryMassInfo = part.mass;
        }
        private void SetupTankList(bool calledByPlayer)
        {
            tankList = new List<FSmodularTank>();
            weightList = new List<double>();
            tankCostList = new List<double>();

            // First find the amounts each tank type is filled with
            List<List<double>> resourceList = new List<List<double>>();
            List<List<double>> initialResourceList = new List<List<double>>();
            string[] resourceTankArray = resourceAmounts.Split(';');
            string[] initialResourceTankArray = initialResourceAmounts.Split(';');

            if (initialResourceAmounts.Equals("") || initialResourceTankArray.Length != resourceTankArray.Length)
                initialResourceTankArray = resourceTankArray;

            //Debug.Log("FSDEBUGRES: " + resourceTankArray.Length+" "+resourceAmounts);
            for (int tankCount = 0; tankCount < resourceTankArray.Length; tankCount++)
            {
                resourceList.Add(new List<double>());
                initialResourceList.Add(new List<double>());
                string[] resourceAmountArray = resourceTankArray[tankCount].Trim().Split(',');
                string[] initialResourceAmountArray = initialResourceTankArray[tankCount].Trim().Split(',');

                if (initialResourceAmounts.Equals("") || initialResourceAmountArray.Length != resourceAmountArray.Length)
                    initialResourceAmountArray = resourceAmountArray;

                for (int amountCount = 0; amountCount < resourceAmountArray.Length; amountCount++)
                {
                    try
                    {
                        resourceList[tankCount].Add(double.Parse(resourceAmountArray[amountCount].Trim()));
                        initialResourceList[tankCount].Add(double.Parse(initialResourceAmountArray[amountCount].Trim()));
                    }
                    catch
                    {
                        Debug.Log("InsterstellarFuelSwitch: error parsing resource amount " + tankCount + "/" + amountCount + ": '" + resourceTankArray[amountCount] + "': '" + resourceAmountArray[amountCount].Trim() + "'");
                    }
                }
            }

            // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
            string[] tankArray = resourceNames.Split(';');
            for (int tankCount = 0; tankCount < tankArray.Length; tankCount++)
            {
                FSmodularTank newTank = new FSmodularTank();
                tankList.Add(newTank);
                string[] resourceNameArray = tankArray[tankCount].Split(',');
                for (int nameCount = 0; nameCount < resourceNameArray.Length; nameCount++)
                {
                    FSresource newResource = new FSresource(resourceNameArray[nameCount].Trim(' '));
                    if (resourceList[tankCount] != null)
                    {
                        if (nameCount < resourceList[tankCount].Count)
                        {
                            newResource.maxAmount = resourceList[tankCount][nameCount];
                            newResource.amount = initialResourceList[tankCount][nameCount];
                        }
                    }
                    newTank.resources.Add(newResource);
                }
            }
        }

        public float GetModuleCost()
        {
            return UpdateCost();
        }

        public float GetModuleCost(float modifier)
        {
            return UpdateCost();
        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                var resourceList = ParseTools.ParseNames(resourceNames);
                var info = new StringBuilder();
                info.AppendLine("Fuel tank setups available:");
                for (int i = 0; i < resourceList.Count; i++)
                {
                    info.AppendLine(resourceList[i].Replace(",", ", "));
                }
                return info.ToString();
            }
            else
                return string.Empty;
        }
    } 
}
