using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;

namespace InterstellarFuelSwitch
{
    public class IFSresource
    {
        public int ID;
        public string name;
        public double currentSupply = 0;
        public double amount = 0;
        public double maxAmount = 0;
        public double boiloffTemp = 0;
        public double latendHeatVaporation = 0;

        public IFSresource(string _name)
        {
            name = _name;
            ID = _name.GetHashCode();
        }
    }

    public class IFSmodularTank
    {
        public string GuiName = String.Empty;
        //public string Contents = String.Empty;

        public List<IFSresource> Resources = new List<IFSresource>();
    }

    public class InterstellarFuelSwitch : PartModule, IRescalable<InterstellarFuelSwitch> , IPartMassModifier, IPartCostModifier
    {
        // Persistants
        [KSPField(isPersistant = true)]
        public string configuredAmounts = "";

        // Config properties
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
        public string boilOffTemp = "";
        [KSPField]
        public string latendHeatVaporation = "";
        [KSPField]
        public bool displayCurrentBoilOffTemp = false;
        [KSPField]
        public bool displayCurrentTankCost = false;
        [KSPField]
        public bool hasGUI = true;
        [KSPField]
        public bool boiloffActive = false;
        [KSPField]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        [KSPField]
        public bool showTemperature = false;
        [KSPField]
        public bool showTankName = true;
        [KSPField]
        public bool showInfo = true; // if false, does not feed info to the part list pop up info menu
        [KSPField]
        public string resourcesToIgnore = ""; // obsolete
        [KSPField]
        public string resourcesFormat = "0.000000";
        [KSPField]
        public float heatConvectiveConstant = 0.01f;
        [KSPField]
        public float heatConductivity = 0.01f;
        [KSPField]
        public string nextTankSetupText = "Next tank setup";
        [KSPField]
        public string previousTankSetupText = "Previous tank setup";

        // Gui

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Temp")]
        public string partTemperatureStr = String.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Specific Heat")]
        public string specificHeatStr = String.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Heat Absorbed")]
        public string boiloffEnergy = String.Empty; 

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Tank")]
        public string tankGuiName = String.Empty; 
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Added cost")]
        public float addedCost = 0;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Dry mass", guiUnits = " t")]
        public float dryMassInfo = 0;
        [KSPField(guiActive = true, guiActiveEditor = false, guiName = "Part mass", guiUnits = " t")]
        public float currentPartMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Boiloff Temp")]
        public string currentBoiloffTempStr = "";

        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr0 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr1 = "";
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr2 = "";

        // Obsolte
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Multiplier")]
        public float volumeMultiplier = 1;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Multiplier")]
        public float massMultiplier = 1;

        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Exponent")]
        public float volumeExponent = 3;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Exponent")]
        public float massExponent = 3;
        [KSPField(isPersistant = true)]
        public bool traceBoiloff;

        // Persistants
        [KSPField(isPersistant = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public string selectedTankSetupTxt;
        [KSPField(isPersistant = true)]
        public bool gameLoaded = false;
        [KSPField(isPersistant = true)]
        public bool configLoaded = false;

        private List<IFSmodularTank> tankList;
        private HashSet<string> activeResourceList;
        private List<double> weightList;
        private List<double> tankCostList;
        private bool initialized = false;

        private double initializePartTemperature = -1;

        public float currentVolumeMultiplier = 1;
        public float currentMassMultiplier = 1;

        private PartResource _partResource0;
        private PartResource _partResource1;
        private PartResource _partResource2;

        private PartResourceDefinition _partRresourceDefinition0;
        private PartResourceDefinition _partRresourceDefinition1;
        private PartResourceDefinition _partRresourceDefinition2;

        List<string> currentResources;

        UIPartActionWindow tweakableUI;

	    public virtual void OnRescale(TweakScale.ScalingFactor factor)
	    {
		    try
		    {
			    currentVolumeMultiplier = (float) Math.Pow(factor.absolute.linear, volumeExponent);
			    currentMassMultiplier = (float) Math.Pow(factor.absolute.linear, massExponent);

                //part.heatConvectiveConstant = this.heatConvectiveConstant * (float)Math.Pow(factor.absolute.linear, 1);
                //part.heatConductivity = this.heatConductivity * (float)Math.Pow(factor.absolute.linear, 1);

			    UpdateMass(part, selectedTankSetup, false);
		    }
		    catch (Exception e)
		    {
				Debug.LogError("OnRescale");
			    Debug.LogException(e);
			    throw;
		    }
	    }

	    public override void OnStart(PartModule.StartState state)
	    {
            try
            {
                InitializeData();

                if (selectedTankSetup == -1)
                    selectedTankSetup = 0;

                this.enabled = true;

                if (state != StartState.Editor)
                {
                    AssignResourcesToPart(false);
                    gameLoaded = true;
                }
                else
                {
                    AssignResourcesToPart(false);
                }

                Fields["partTemperatureStr"].guiActive = showTemperature;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnStart Error");
                throw;
            }
	    }

	    public void SelectTankSetup(int i, bool calledByPlayer)
	    {
            try
            {
                InitializeData();
                if (selectedTankSetup == i) return;

                selectedTankSetup = i;
                AssignResourcesToPart(calledByPlayer);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SelectTankSetup Error");
                throw;
            }
	    }

	    public override void OnAwake()
	    {
            try
            {
                if (configLoaded)
                    InitializeData();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnAwake Error");
                throw;
            }
	    }

	    public override void OnLoad(ConfigNode node)
	    {
            try
            {

                base.OnLoad(node);
                if (!configLoaded)
                    InitializeData();

                configLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnLoad Error");
                throw;
            }
	    }

	    private void InitializeData()
	    {
            try
            {
                if (initialized) return;

                SetupTankList(false);

                weightList = ParseTools.ParseDoubles(tankMass, () => weightList);
                tankCostList = ParseTools.ParseDoubles(tankCost, () => tankCost);

                if (hasGUI)
                {
                    var nextEvent = Events["nextTankSetupEvent"];
                    nextEvent.guiActive = availableInFlight;
                    nextEvent.guiActiveEditor = availableInEditor;
                    nextEvent.guiName = nextTankSetupText;

                    var previousEvent = Events["previousTankSetupEvent"];
                    previousEvent.guiActive = availableInFlight;
                    previousEvent.guiActiveEditor = availableInEditor;
                    previousEvent.guiName = previousTankSetupText;
                }
                else
                {
                    Events["nextTankSetupEvent"].guiActive = false;
                    Events["nextTankSetupEvent"].guiActiveEditor = false;
                    Events["previousTankSetupEvent"].guiActive = false;
                    Events["previousTankSetupEvent"].guiActiveEditor = false;
                }

                Fields["addedCost"].guiActiveEditor = displayCurrentTankCost && HighLogic.LoadedSceneIsEditor;

                initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch InitializeData Error");
                throw;
            }
	    }

	    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next tank setup")]
	    public void nextTankSetupEvent()
	    {
            try
            {
                selectedTankSetup++;

                if (selectedTankSetup >= tankList.Count)
                    selectedTankSetup = 0;

                AssignResourcesToPart(true);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch nextTankSetupEvent Error");
                throw;
            }
	    }

	    [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous tank setup")]
	    public void previousTankSetupEvent()
	    {
            try
            {
                selectedTankSetup--;
                if (selectedTankSetup < 0)
                    selectedTankSetup = tankList.Count - 1;

                AssignResourcesToPart(true);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch previousTankSetupEvent Error");
                throw;
            }
	    }

	    private void AssignResourcesToPart(bool calledByPlayer = false)
	    {
            try
            {
                // destroying a resource messes up the gui in editor, but not in flight.
                currentResources = SetupTankInPart(part, calledByPlayer);

                // update GUI part
                ConfigureResourceMassGui(currentResources);
                UpdateTankName();

                if (HighLogic.LoadedSceneIsEditor)
                {
                    foreach (var symPart in part.symmetryCounterparts)
                    {
                        var symNewResources = SetupTankInPart(symPart, calledByPlayer);

                        InterstellarFuelSwitch symSwitch = symPart.GetComponent<InterstellarFuelSwitch>();
                        if (symSwitch != null)
                        {
                            symSwitch.selectedTankSetup = selectedTankSetup;
                            symSwitch.selectedTankSetupTxt = selectedTankSetupTxt;
                            symSwitch.ConfigureResourceMassGui(symNewResources);
                            symSwitch.UpdateTankName();
                        }
                    }
                }

                if (tweakableUI == null)
                    tweakableUI = part.FindActionWindow();

                if (tweakableUI != null)
                    tweakableUI.displayDirty = true;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch AssignResourcesToPart Error " + e.Message);
                throw;
            }
	    }

        public void UpdateTankName()
        {
            var selectedTank = tankList[selectedTankSetup];
            tankGuiName = selectedTank.GuiName;
            Fields["tankGuiName"].guiActive = showTankName && !String.IsNullOrEmpty(tankGuiName);
            Fields["tankGuiName"].guiActiveEditor = showTankName && !String.IsNullOrEmpty(tankGuiName);

            if (displayCurrentBoilOffTemp)
            {
                Fields["currentBoiloffTempStr"].guiActive = true;
                Fields["currentBoiloffTempStr"].guiActiveEditor = true;
                currentBoiloffTempStr = selectedTank.Resources[0].boiloffTemp.ToString("0.00");
            }
            else
            {
                Fields["currentBoiloffTempStr"].guiActive = false;
                Fields["currentBoiloffTempStr"].guiActiveEditor = false;
            }
        }

	    private List<string> SetupTankInPart(Part currentPart, bool calledByPlayer)
	    {
            try
            {
                // find selected tank
                var selectedTank = calledByPlayer || String.IsNullOrEmpty(selectedTankSetupTxt)
                    ? selectedTankSetup < tankList.Count ? tankList[selectedTankSetup] : tankList[0]
                    : tankList.FirstOrDefault(t => t.GuiName == selectedTankSetupTxt) ?? (selectedTankSetup < tankList.Count ? tankList[selectedTankSetup] : tankList[0]);

                // update txt and index for future
                selectedTankSetupTxt = selectedTank.GuiName;
                selectedTankSetup = tankList.IndexOf(selectedTank);

                // check if we need to change anything
                //if (!HighLogic.LoadedSceneIsEditor && gameLoaded && !calledByPlayer)
                //{
                //    Debug.Log("InsterstellarFuelSwitch assignResourcesToPart, no change required");
                //    return part.Resources.list.Select(r => r.resourceName).ToList();
                //}

                // create new ResourceNode
                var newResources = new List<string>();
                var newResourceNodes = new List<ConfigNode>();
                var parsedConfigAmount = new List<float>();

                // parse configured amounts
                if (configuredAmounts.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                    {
                        configuredAmounts = String.Empty;
                        //Debug.Log("InsterstellarFuelSwitch assignResourcesToPart calledByPlayer reset configuredAmounts");
                    }

                    //Debug.Log("InsterstellarFuelSwitch assignResourcesToPart parsing configuredAmounts = " + configuredAmounts);
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
                        //Debug.Log("InsterstellarFuelSwitch assignResourcesToPart not HighLogic.LoadedSceneIsEditor reset configuredAmounts");
                    }
                }

                // imitialise minimum boiloff temperature at current part temperature
                double minimumBoiloffTemerature = -1;

                for (int resourceId = 0; resourceId < selectedTank.Resources.Count; resourceId++)
                {
                    var selectedTankResource = selectedTank.Resources[resourceId];

                    if (minimumBoiloffTemerature == -1 || (selectedTankResource.boiloffTemp > 0 && selectedTankResource.boiloffTemp < minimumBoiloffTemerature))
                        minimumBoiloffTemerature = selectedTankResource.boiloffTemp;

                    if (selectedTankResource.name == "Structural")
                        continue;

                    newResources.Add(selectedTankResource.name);

                    ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                    double maxAmount = selectedTankResource.maxAmount * currentVolumeMultiplier;

                    newResourceNode.AddValue("name", selectedTankResource.name);
                    newResourceNode.AddValue("maxAmount", maxAmount);

                    PartResource existingResource = null;
                    if (!HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsEditor && !calledByPlayer))
                    {
                        foreach (PartResource partResource in currentPart.Resources)
                        {
                            if (partResource.resourceName.Equals(selectedTankResource.name))
                            {
                                existingResource = partResource;
                                break;
                            }
                        }
                    }

                    double resourceNodeAmount;
                    if (existingResource != null)
                        resourceNodeAmount = Math.Min(existingResource.amount, maxAmount);
                    else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                        resourceNodeAmount = parsedConfigAmount[resourceId];
                    else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                        resourceNodeAmount = 0.0;
                    else
                        resourceNodeAmount = selectedTank.Resources[resourceId].amount * currentVolumeMultiplier;

                    newResourceNode.AddValue("amount", resourceNodeAmount);
                    newResourceNodes.Add(newResourceNode);
                }


                //// prepare part to initialise temerature 
                //if (minimumBoiloffTemerature != -1)
                //{
                //    var currentFuelswitch = part.FindModuleImplementing<InterstellarFuelSwitch>();
                //    if (currentFuelswitch != null)
                //    {
                //        Debug.Log("InsterstellarFuelSwitch SetupTankInPart prepared to initialise part temperature at " + minimumBoiloffTemerature);
                //        currentFuelswitch.initializePartTemperature = minimumBoiloffTemerature;
                //    }
                //}

                var finalResourceNodes = new List<ConfigNode>();

                // remove all resources except those we ignore
                PartResource[] partResources = currentPart.GetComponents<PartResource>();
                foreach (PartResource resource in partResources)
                {
                    if (activeResourceList.Contains(resource.resourceName))
                    {
                        if (newResourceNodes.Count > 0)
                        {
                            finalResourceNodes.AddRange(newResourceNodes);
                            newResourceNodes.Clear();
                        }

                        currentPart.Resources.list.Remove(resource);
                        DestroyImmediate(resource);
                    }
                    else
                    {
                        ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                        newResourceNode.AddValue("name", resource.resourceName);
                        newResourceNode.AddValue("maxAmount", resource.maxAmount);
                        newResourceNode.AddValue("amount", resource.amount);

                        finalResourceNodes.Add(newResourceNode);
                        Debug.Log("InsterstellarFuelSwitch SetupTankInPart created confignode for: " + resource.resourceName);

                        // remove all
                        currentPart.Resources.list.Remove(resource);
                        DestroyImmediate(resource);
                        Debug.Log("InsterstellarFuelSwitch SetupTankInPart remove resource " + resource.resourceName);
                    }
                }

                // add any remaining bew nodes
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                // add new resources
                //if (newResourceNodes.Count > 0)
                if (finalResourceNodes.Count > 0)
                {
                    Debug.Log("InsterstellarFuelSwitch SetupTankInPart adding resources: " + ParseTools.Print(newResources));
                    //foreach (var resourceNode in newResourceNodes)
                    foreach (var resourceNode in finalResourceNodes)
                    {
                        currentPart.AddResource(resourceNode);
                    }
                }

                // This also needs to be done when going from a setup with resources to a setup with no resources.
                currentPart.Resources.UpdateList();
                UpdateMass(currentPart, selectedTankSetup, calledByPlayer);
                UpdateCost();

                return newResources;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SetupTankInPart Error");
                throw;
            }
	    }

        public void ConfigureResourceMassGui(List<string> newResources)
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

        public float GetModuleMass(float defaultMass)
        {
            var newMass = CalculateDryMass(selectedTankSetup);
            return newMass;
        }

	    private float UpdateCost()
	    {
            try
            {
                if (selectedTankSetup >= 0 && selectedTankSetup < tankCostList.Count)
                {
                    addedCost = (float)tankCostList[selectedTankSetup];
                    return addedCost;
                }

                addedCost = 0;
                if (_partRresourceDefinition0 == null || _partResource0 == null) return addedCost;
                addedCost += _partRresourceDefinition0.unitCost * (float)_partResource0.maxAmount;

                if (_partRresourceDefinition1 == null || _partResource1 == null) return addedCost;
                addedCost += _partRresourceDefinition1.unitCost * (float)_partResource1.maxAmount;

                if (_partRresourceDefinition2 != null && _partResource2 != null)
                    addedCost += _partRresourceDefinition2.unitCost * (float)_partResource2.maxAmount;

                return addedCost;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch UpdateCost Error");
                throw;
            }
	    }

	    private void UpdateMass(Part currentPart, int tankSetupIndex, bool calledByPlayer = false)
	    {
            try
            {
                // when changed by player
                if (calledByPlayer && HighLogic.LoadedSceneIsFlight) return;

                if (tankSetupIndex < 0 || tankSetupIndex >= weightList.Count) return;

                var newMass = CalculateDryMass(tankSetupIndex);
                currentPart.mass = newMass;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch UpdateMass Error");
                throw;
            }
	    }

        private float CalculateDryMass(int tankSetupIndex)
        {
            if (weightList == null || tankSetupIndex < 0 || tankSetupIndex >= weightList.Count)
                return basePartMass * currentMassMultiplier;

            return (float)((basePartMass + weightList[tankSetupIndex]) * currentMassMultiplier);
        }

	    private string formatMassStr(double amount)
        {
            try
            {

                if (amount >= 1)
                    return (amount).ToString(resourcesFormat) + " t";
                if (amount >= 1e-3)
                    return (amount * 1e3).ToString(resourcesFormat) + " kg";
                if (amount >= 1e-6)
                    return (amount * 1e6).ToString(resourcesFormat) + " g";

                return (amount * 1e9).ToString(resourcesFormat) + " mg";
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch formatMassStr Error");
                throw;
            }
        }

        private void UpdateGuiResourceMass()
        {
            resourceAmountStr0 = _partRresourceDefinition0 == null || _partResource0 == null ? String.Empty : formatMassStr(_partRresourceDefinition0.density * _partResource0.amount);
            resourceAmountStr1 = _partRresourceDefinition1 == null || _partResource1 == null ? String.Empty : formatMassStr(_partRresourceDefinition1.density * _partResource1.amount);
            resourceAmountStr2 = _partRresourceDefinition2 == null || _partResource2 == null ? String.Empty : formatMassStr(_partRresourceDefinition2.density * _partResource2.amount);
        }

        public override void OnUpdate()
        {
            if (initializePartTemperature != -1 && initializePartTemperature > 0)
            {
                //Debug.Log("InsterstellarFuelSwitch OnUpdate initialise part temperature at " + initializePartTemperature);
                part.temperature = initializePartTemperature;
                initializePartTemperature = -1;
                traceBoiloff = true;
            }

            //There were some issues with resources slowly trickling in, so I changed this to 0.1% instead of empty.
            var showSwitchButtons = availableInFlight && !part.GetComponents<PartResource>().Any(r => r.amount > r.maxAmount / 1000);

            Events["nextTankSetupEvent"].guiActive = showSwitchButtons;
            Events["previousTankSetupEvent"].guiActive = showSwitchButtons;
        }

        public void ProcessBoiloff()
        {
            try
            {

                if (!boiloffActive) return;

                if (!traceBoiloff) return;

                if (tankList == null) return;

                var currentTemperature = part.temperature;
                var selectedTank = tankList[selectedTankSetup];
                foreach (var resource in selectedTank.Resources)
                {
                    if (currentTemperature <= resource.boiloffTemp) continue;

                    var deltaTemperatureDifferenceInKelvin = currentTemperature - resource.boiloffTemp;

                    PartResource partResource = part.Resources.list.FirstOrDefault(r => r.resourceName == resource.name);
                    PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resource.name);

                    if (resourceDefinition == null || partResource == null) continue;

                    specificHeatStr = resourceDefinition.specificHeatCapacity.ToString("0.0000");

                    var specificHeat = resourceDefinition.specificHeatCapacity > 0 ? resourceDefinition.specificHeatCapacity : 1000;

                    //var standardSpecificHeatCapacity = 800;
                    // calcualte boiloff
                    //var wetMass = partResource.amount * resourceDefinition.density;
                    //var drymass = CalculateDryMass(selectedTankSetup);
                    //var ThermalMass =  (drymass * standardSpecificHeatCapacity * part.thermalMassModifier) + (wetMass * specificHeat);

                    var ThermalMass = part.thermalMass;
                    var heatAbsorbed = ThermalMass * deltaTemperatureDifferenceInKelvin;

                    this.boiloffEnergy = (heatAbsorbed / TimeWarp.fixedDeltaTime).ToString("0.0000") + " kJ/s";

                    var latendHeatVaporation = resource.latendHeatVaporation != 0 ? resource.latendHeatVaporation * 1000 : specificHeat * 34;

                    var resourceBoilOff = heatAbsorbed / latendHeatVaporation;
                    var boiloffAmount = resourceBoilOff / resourceDefinition.density;

                    // reduce boiloff from part  
                    partResource.amount = Math.Max(0, partResource.amount - boiloffAmount);
                    part.temperature = resource.boiloffTemp;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch UpdateMass Error");
                throw;
            }
        }

        public override void OnFixedUpdate()
        {
            currentPartMass = part.mass;
            
            ProcessBoiloff();

            partTemperatureStr = part.temperature + " K";

            base.OnFixedUpdate();
        }

	    // Note: do note remove, it is called by KSP
        public void Update()
        {
            if (currentResources != null)
                ConfigureResourceMassGui(currentResources);

            UpdateGuiResourceMass();

            if (!HighLogic.LoadedSceneIsEditor) return;

            // update Dry Mass
            var newMass = CalculateDryMass(selectedTankSetup);
            part.mass = newMass;
            dryMassInfo = newMass;

            configuredAmounts = "";
            foreach (var resoure in part.Resources.list)
            {
                configuredAmounts += resoure.amount + ",";
            }
        }

	    private void SetupTankList(bool calledByPlayer)
	    {
            try
            {
                tankList = new List<IFSmodularTank>();

                // toDo: move to tankList
                weightList = new List<double>();
                tankCostList = new List<double>();
                activeResourceList = new HashSet<string>();

                // First find the amounts each tank type is filled with
                List<List<double>> resourceList = new List<List<double>>();
                List<List<double>> initialResourceList = new List<List<double>>();
                List<List<double>> boilOffTempList = new List<List<double>>();
                List<List<double>> latendHeatVaporationList = new List<List<double>>();

                string[] resourceTankAmountArray = resourceAmounts.Split(';');
                string[] initialResourceTankArray = initialResourceAmounts.Split(';');
                string[] tankGuiNameTankArray = resourceGui.Split(';');
                string[] boilOffTempTankArray = boilOffTemp.Split(';');
                string[] latendHeatVaporationArray = latendHeatVaporation.Split(';');
                string[] tankNameArray = resourceNames.Split(';');

                int tankGuiNameArrayCount = tankGuiNameTankArray.Count();

                // if missing or not complete, use full amount
                if (initialResourceAmounts.Equals(String.Empty) ||
                    initialResourceTankArray.Length != resourceTankAmountArray.Length)
                    initialResourceTankArray = resourceTankAmountArray;

                for (int tankCounter = 0; tankCounter < resourceTankAmountArray.Length; tankCounter++)
                {
                    resourceList.Add(new List<double>());
                    initialResourceList.Add(new List<double>());
                    boilOffTempList.Add(new List<double>());
                    latendHeatVaporationList.Add(new List<double>());

                    string[] resourceAmountArray = resourceTankAmountArray[tankCounter].Trim().Split(',');
                    string[] initialResourceAmountArray = initialResourceTankArray[tankCounter].Trim().Split(',');
                    string[] boilOffTempAmountArray = boilOffTempTankArray.Count() > tankCounter ? boilOffTempTankArray[tankCounter].Trim().Split(',') : new string[0];
                    string[] latendHeatVaporationAmountArray = latendHeatVaporationArray.Count() > tankCounter ? latendHeatVaporationArray[tankCounter].Trim().Split(',') : new string[0];

                    // if missing or not complete, use full amount
                    if (initialResourceAmounts.Equals(String.Empty) ||
                        initialResourceAmountArray.Length != resourceAmountArray.Length)
                        initialResourceAmountArray = resourceAmountArray;

                    for (var amountCounter = 0; amountCounter < resourceAmountArray.Length; amountCounter++)
                    {
                        try
                        {
                            if (tankCounter >= resourceList.Count || amountCounter >= resourceAmountArray.Count()) continue;

                            resourceList[tankCounter].Add(double.Parse(resourceAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing resourceTankAmountArray amount " + tankCounter + "/" + amountCounter +
                                      ": '" + resourceTankAmountArray[tankCounter] + "': '" + resourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < initialResourceList.Count && amountCounter < initialResourceAmountArray.Count())
                                initialResourceList[tankCounter].Add(ParseTools.ParseDouble(initialResourceAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing initialResourceList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + initialResourceList[tankCounter] + "': '" + initialResourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < boilOffTempList.Count && amountCounter < boilOffTempAmountArray.Length)
                                boilOffTempList[tankCounter].Add(ParseTools.ParseDouble(boilOffTempAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing boilOffTempList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + boilOffTempList[tankCounter] + "': '" + boilOffTempAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < latendHeatVaporationList.Count && amountCounter < latendHeatVaporationAmountArray.Length)
                                latendHeatVaporationList[tankCounter].Add(ParseTools.ParseDouble(latendHeatVaporationAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing latendHeatVaporation amount " + tankCounter + "/" + amountCounter +
                                      ": '" + latendHeatVaporationList[tankCounter] + "': '" + latendHeatVaporationAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }
                    }
                }

                // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
                for (int currentResourceCounter = 0; currentResourceCounter < tankNameArray.Length; currentResourceCounter++)
                {
                    var newTank = new IFSmodularTank();

                    if (currentResourceCounter < tankGuiNameArrayCount)
                        newTank.GuiName = tankGuiNameTankArray[currentResourceCounter];

                    tankList.Add(newTank);
                    string[] resourceNameArray = tankNameArray[currentResourceCounter].Split(',');
                    for (var nameCounter = 0; nameCounter < resourceNameArray.Length; nameCounter++)
                    {
                        var resourceName = resourceNameArray[nameCounter].Trim(' ');

                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);

                        var newResource = new IFSresource(resourceName);
                        if (resourceList[currentResourceCounter] != null && nameCounter < resourceList[currentResourceCounter].Count)
                        {
                            newResource.maxAmount = resourceList[currentResourceCounter][nameCounter];
                            newResource.amount = initialResourceList[currentResourceCounter][nameCounter];
                        }

                        // add boiloff data
                        if (currentResourceCounter < boilOffTempList.Count && boilOffTempList[currentResourceCounter] != null
                            && boilOffTempList[currentResourceCounter].Count > nameCounter)
                        {
                            newResource.boiloffTemp = boilOffTempList[currentResourceCounter][nameCounter];
                        }

                        if (currentResourceCounter < latendHeatVaporationList.Count && latendHeatVaporationList[currentResourceCounter] != null
                            && latendHeatVaporationList[currentResourceCounter].Count > nameCounter)
                        {
                            newResource.latendHeatVaporation = latendHeatVaporationList[currentResourceCounter][nameCounter];
                        }

                        //newTank.Contents += newResource.name + ",";
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
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SetupTankList Error");
                throw;
            }
	    }

	    public float GetModuleCost()
        {
            try
            {
                return UpdateCost();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetModuleCost Error");
                throw;
            }
        }

        public float GetModuleCost(float modifier)
        {
            try
            {
                return UpdateCost();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetModuleCost(float modifier) Error");
                throw;
            }
        }

	    public override string GetInfo()
	    {
            try
            {
                if (!showInfo) return string.Empty;

                var resourceList = ParseTools.ParseNames(resourceNames);
                var info = new StringBuilder();

                info.AppendLine("Fuel tank setups available:");

                foreach (string t in resourceList)
                {
                    info.AppendLine(t.Replace(",", ", "));
                }
                return info.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetInfo Error " + e.Message);
                throw;
            }
	    }
    } 
}
