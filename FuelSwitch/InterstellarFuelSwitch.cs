using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    public class FSresource
    {
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
        public string GuiName = String.Empty;
        public string Contents = String.Empty;

        public List<FSresource> Resources = new List<FSresource>();
    }

    public class InterstellarFuelSwitch : PartModule, IPartCostModifier
    {
        [KSPField]
        public string resourceGui = "";
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
        public string tankCost = "";
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

        // Persistants
        [KSPField(isPersistant = true)]
        public string configuredAmounts = "";


        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Tank")]
        public string tankGuiName = String.Empty; 
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Added cost")]
        public float addedCost = 0f;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Dry mass", guiUnits = " t")]
        public float dryMassInfo = 0f;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Multiplier")]
        public float volumeMultiplier = 1f;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Multiplier")]
        public float massMultiplier = 1f;

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr2 = "";

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

        private PartResource _partResource0;
        private PartResource _partResource1;
        private PartResource _partResource2;

        private PartResourceDefinition _partRresourceDefinition0;
        private PartResourceDefinition _partRresourceDefinition1;
        private PartResourceDefinition _partRresourceDefinition2;

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

        public void SelectTankSetup(int i, bool calledByPlayer)
        {
            InitializeData();
            if (selectedTankSetup != i)
            {
                selectedTankSetup = i;
                AssignResourcesToPart(calledByPlayer);
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
            if (initialized) return;

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

            if (HighLogic.LoadedSceneIsEditor ||  HighLogic.CurrentGame == null || HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                Fields["addedCost"].guiActiveEditor = displayCurrentTankCost;

            initialized = true;
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

        private void AssignResourcesToPart(bool calledByPlayer, bool calledAtStartup = false)
        {
            // destroying a resource messes up the gui in editor, but not in flight.
            SetupTankInPart(part, calledByPlayer, calledAtStartup);
            if (HighLogic.LoadedSceneIsEditor)
            {
                for (int s = 0; s < part.symmetryCounterparts.Count; s++)
                {
                    SetupTankInPart(part.symmetryCounterparts[s], calledByPlayer);
                    InterstellarFuelSwitch symSwitch = part.symmetryCounterparts[s].GetComponent<InterstellarFuelSwitch>();
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
            List<float> parsedConfigAmount = new List<float>();

            // parse configured amounts
            if (configuredAmounts.Length > 0)
            {
                // empty configuration if switched by user
                if (calledByPlayer)
                {
                    configuredAmounts = String.Empty;
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart calledByPlayer reset configuredAmounts");
                }

                Debug.Log("InsterstellarFuelSwitch assignResourcesToPart parsing configuredAmounts = " + configuredAmounts);
                string[] configAmount = configuredAmounts.Split(',');
                foreach (string item in configAmount)
                {
                    float value;
                    if (float.TryParse(item, out value))
                        parsedConfigAmount.Add(value);
                }

                // empty configuration if in flight
                if (!HighLogic.LoadedSceneIsEditor)
                {
                    configuredAmounts = String.Empty;
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart not HighLogic.LoadedSceneIsEditor reset configuredAmounts");
                }
            }

            var selectedTank = tankList[selectedTankSetup];
            tankGuiName = selectedTank.GuiName;
            Fields["tankGuiName"].guiActive = !String.IsNullOrEmpty(tankGuiName);
            Fields["tankGuiName"].guiActiveEditor = !String.IsNullOrEmpty(tankGuiName);

            Debug.Log("InsterstellarFuelSwitch assignResourcesToPart setupTankInPart = " + selectedTankSetup);
            for (int resourceId = 0; resourceId < selectedTank.Resources.Count; resourceId++)
            {
                if (tankList[selectedTankSetup].Resources[resourceId].name == "Structural")
                {
                    Debug.Log("InsterstellarFuelSwitch found Structural");
                    continue;
                }

                var resourceName = selectedTank.Resources[resourceId].name;
                newResources.Add(resourceName);

                ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                double maxAmount = selectedTank.Resources[resourceId].maxAmount * volumeMultiplier;

                newResourceNode.AddValue("name", resourceName);
                newResourceNode.AddValue("maxAmount", maxAmount);

                PartResource existingResource = null;

                if (!HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsEditor && !calledByPlayer))
                {
                    foreach (PartResource partResource in part.Resources)
                    {
                        if (partResource.resourceName.Equals(resourceName))
                        {
                            Debug.Log("InsterstellarFuelSwitch assignResourcesToPart existing resource found");
                            existingResource = partResource;
                            break;
                        }
                    }
                }

                double resourceNodeAmount;
                if (existingResource != null)
                {
                    resourceNodeAmount = Math.Min(existingResource.amount, maxAmount);
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart existingResource = " + resourceNodeAmount);
                }
                else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                {
                    resourceNodeAmount = parsedConfigAmount[resourceId];
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart parsedConfigAmount[" + resourceId + "] = " + resourceNodeAmount);
                }
                else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                {
                    resourceNodeAmount = 0.0;
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart reset");
                }
                else
                {
                    resourceNodeAmount = tankList[selectedTankSetup].Resources[resourceId].amount * volumeMultiplier;
                    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart tankList[" + selectedTankSetup + "].Resources[" + resourceId + "].amount * " + volumeMultiplier + " = " + resourceNodeAmount);
                }

                newResourceNode.AddValue("amount", resourceNodeAmount);
                newResourceNodes.Add(newResourceNode);
            }

            currentPart.Resources.list.Clear();
            PartResource[] partResources = currentPart.GetComponents<PartResource>();
            for (int i = 0; i < partResources.Length; i++)
            {
                var resource = partResources[i];
                Debug.Log("InsterstellarFuelSwitch setupTankInPart removing resource: " + resource.resourceName);
                DestroyImmediate(resource);
            }

            if (newResourceNodes.Count > 0)
            {
                Debug.Log("InsterstellarFuelSwitch setupTankInPart adding new resources: " + ParseTools.Print(newResources));
                foreach (var resourceNode in newResourceNodes)
                {
                    currentPart.AddResource(resourceNode);
                }
            }

            // This also needs to be done when going from a setup with resources to a setup with no resources.
            currentPart.Resources.UpdateList();
            UpdateWeight(currentPart, selectedTankSetup, calledByPlayer);
            UpdateCost();

            // update GUI
            ConfigureResourceMassGui(newResources);
        }

        private void ConfigureResourceMassGui(List<string> newResources)
        {
            _partRresourceDefinition0 = newResources.Count > 0 ? PartResourceLibrary.Instance.GetDefinition(newResources[0]) : null;
            _partRresourceDefinition1 = newResources.Count > 1 ? PartResourceLibrary.Instance.GetDefinition(newResources[1]) : null;
            _partRresourceDefinition2 = newResources.Count > 2 ? PartResourceLibrary.Instance.GetDefinition(newResources[2]) : null;

            Fields["resourceAmountStr0"].guiName = _partRresourceDefinition0 != null ? _partRresourceDefinition0.name : ":";
            Fields["resourceAmountStr1"].guiName = _partRresourceDefinition1 != null ? _partRresourceDefinition1.name : ":";
            Fields["resourceAmountStr2"].guiName = _partRresourceDefinition2 != null ? _partRresourceDefinition2.name : ":";

            _partResource0 = _partRresourceDefinition0 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition0.name);
            _partResource1 = _partRresourceDefinition1 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition1.name);
            _partResource2 = _partRresourceDefinition2 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition2.name);
        }

        private float UpdateCost() 
        {
            if (selectedTankSetup >= 0 && selectedTankSetup < tankCostList.Count)
            {
                addedCost = (float)tankCostList[selectedTankSetup];
                //part.partInfo.cost = newCost
                //Debug.Log("InsterstellarFuelSwitch: changed addedCost to " + addedCost);
                return addedCost;
            }
            else
            {
                addedCost = 0;
                if (_partRresourceDefinition0 != null && _partResource0 != null)
                {
                    addedCost += _partRresourceDefinition0.unitCost * (float)_partResource0.maxAmount;
                    if (_partRresourceDefinition1 != null && _partResource1 != null)
                    {
                        addedCost += _partRresourceDefinition1.unitCost * (float)_partResource1.maxAmount;
                        if (_partRresourceDefinition2 != null && _partResource2 != null)
                            addedCost += _partRresourceDefinition2.unitCost * (float)_partResource2.maxAmount;
                    }
                }

                return addedCost;
            }
        }
        private void UpdateWeight(Part currentPart, int newTankSetup, bool calledByPlayer = false)
        {
            // when changed by player
            if (calledByPlayer && HighLogic.LoadedSceneIsFlight) return;

            if (newTankSetup < weightList.Count)
            {
                var newMass = (float)((basePartMass + weightList[newTankSetup]) * massMultiplier);
                Debug.Log("InsterstellarFuelSwitch: UpdateWeight to " + basePartMass + " + " + weightList[newTankSetup].ToString() + " * " + massMultiplier);
                currentPart.mass = Math.Max(basePartMass, newMass);
            }
        }

        protected string formatMassStr(double amount)
        {
            if (amount >= 1)
                return (amount).ToString("0.0000000") + " t";
            else if (amount >= 1e-3)
                return (amount * 1e3).ToString("0.0000000") + " kg";
            else if (amount >= 1e-6)
                return (amount * 1e6).ToString("0.0000000") + " g";
            else 
                return (amount * 1e9).ToString("0.0000000") + " mg";
        }

        private void UpdateGuiResourceMass()
        {
            resourceAmountStr0 = _partRresourceDefinition0 == null || _partResource0 == null ? String.Empty : formatMassStr(_partRresourceDefinition0.density * _partResource0.amount);
            resourceAmountStr1 = _partRresourceDefinition1 == null || _partResource1 == null ? String.Empty : formatMassStr(_partRresourceDefinition1.density * _partResource1.amount);
            resourceAmountStr2 = _partRresourceDefinition2 == null || _partResource2 == null ? String.Empty : formatMassStr(_partRresourceDefinition2.density * _partResource2.amount);
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
            UpdateGuiResourceMass();

            if (!HighLogic.LoadedSceneIsEditor) return;

            dryMassInfo = part.mass;
            configuredAmounts = "";
            foreach ( var resoure in part.Resources.list)
            {
                configuredAmounts += resoure.amount + ",";
            }

        }
        private void SetupTankList(bool calledByPlayer)
        {
            tankList = new List<FSmodularTank>();
            weightList = new List<double>();
            tankCostList = new List<double>();

            // First find the amounts each tank type is filled with
            List<List<double>> resourceList = new List<List<double>>();
            List<List<double>> initialResourceList = new List<List<double>>();
            string[] resourceTankAmountArray = resourceAmounts.Split(';');
            string[] initialResourceTankArray = initialResourceAmounts.Split(';');
            string[] tankGuiNameArray = resourceGui.Split(';');
            int tankGuiNameArrayCount = tankGuiNameArray.Count();

            if (initialResourceAmounts.Equals("") || initialResourceTankArray.Length != resourceTankAmountArray.Length)
                initialResourceTankArray = resourceTankAmountArray;

            //Debug.Log("FSDEBUGRES: " + resourceTankArray.Length+" "+resourceAmounts);
            for (int tankCount = 0; tankCount < resourceTankAmountArray.Length; tankCount++)
            {
                resourceList.Add(new List<double>());
                initialResourceList.Add(new List<double>());
                string[] resourceAmountArray = resourceTankAmountArray[tankCount].Trim().Split(',');
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
                        Debug.Log("InsterstellarFuelSwitch: error parsing resource amount " + tankCount + "/" + amountCount + ": '" + resourceTankAmountArray[amountCount] + "': '" + resourceAmountArray[amountCount].Trim() + "'");
                    }
                }
            }

            // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
            string[] tankNameArray = resourceNames.Split(';');
            for (int currentResourceCounter = 0; currentResourceCounter < tankNameArray.Length; currentResourceCounter++)
            {
                FSmodularTank newTank = new FSmodularTank();

                if (currentResourceCounter < tankGuiNameArrayCount)
                    newTank.GuiName = tankGuiNameArray[currentResourceCounter];

                tankList.Add(newTank);
                string[] resourceNameArray = tankNameArray[currentResourceCounter].Split(',');
                for (int nameCount = 0; nameCount < resourceNameArray.Length; nameCount++)
                {
                    FSresource newResource = new FSresource(resourceNameArray[nameCount].Trim(' '));
                    if (resourceList[currentResourceCounter] != null)
                    {
                        if (nameCount < resourceList[currentResourceCounter].Count)
                        {
                            newResource.maxAmount = resourceList[currentResourceCounter][nameCount];
                            newResource.amount = initialResourceList[currentResourceCounter][nameCount];
                        }
                    }
                    newTank.Contents += newResource.name + ",";
                    newTank.Resources.Add(newResource);
                }
            }

            var maxNrTanks = tankList.Max(t => t.Resources.Count);

            Fields["resourceAmountStr0"].guiActive = maxNrTanks > 0;
            Fields["resourceAmountStr1"].guiActive = maxNrTanks > 1;
            Fields["resourceAmountStr2"].guiActive = maxNrTanks > 2;

            Fields["resourceAmountStr0"].guiActiveEditor = maxNrTanks > 0;
            Fields["resourceAmountStr1"].guiActiveEditor = maxNrTanks > 1;
            Fields["resourceAmountStr2"].guiActiveEditor = maxNrTanks > 2;
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
