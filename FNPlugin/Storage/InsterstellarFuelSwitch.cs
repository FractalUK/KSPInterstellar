using FNPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Storage
{
	internal class FSresource
	{
		//public PartResource resource;
		public readonly string Name;
		public int HashCode;
		public float Ratio;
		public double Amount = 0f;
		public double MaxAmount = 0f;

		public FSresource(string name, float ratio)
		{
			Name = name;
			HashCode = name.GetHashCode();
			Ratio = ratio;
		}

		public FSresource(string name)
		{
			Name = name;
			HashCode = name.GetHashCode();
			Ratio = 1f;
		}
	}

	internal class FSmodularTank
	{
		public readonly List<FSresource> Resources = new List<FSresource>();
	}

	public class InsterstellarFuelSwitch : PartModule, IPartCostModifier
	{
		[KSPField] public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
		[KSPField] public string resourceAmounts = "100;75,25;200";
		[KSPField] public string initialResourceAmounts = "";
		[KSPField] public float basePartMass = 0.25f;
		[KSPField] public string tankMass = "0;0;0;0";
		[KSPField] public string tankCost = "0; 0; 0; 0";
		[KSPField] public bool displayCurrentTankCost = false;
		[KSPField] public bool hasGUI = true;
		[KSPField] public bool availableInFlight = false;
		[KSPField] public bool availableInEditor = true;
		[KSPField] public bool showInfo = true; // if false, does not feed info to the part list pop up info menu

		// GUI
		[KSPField(guiActive = false, guiActiveEditor = false, guiName = "Added cost")] public float addedCost = 0f;
		[KSPField(guiActive = false, guiActiveEditor = true, guiName = "Dry mass")] public float dryMassInfo = 0f;
		[KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Multiplier")] public float volumeMultiplier = 1f;
		[KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Multiplier")] public float massMultiplier = 1f;

		// Persistants
		[KSPField(isPersistant = true)] public int selectedTankSetup = -1;
		[KSPField(isPersistant = true)] public bool hasLaunched = false;
		[KSPField(isPersistant = true)] public bool gameLoaded = false;
		[KSPField(isPersistant = true)] public bool configLoaded = false;

		private List<FSmodularTank> _tankList;
		private List<double> _weightList;
		private List<double> _tankCostList;
		private bool _initialized = false;
		private UIPartActionWindow _tweakableUi;

		public override void OnStart(PartModule.StartState state)
		{
			InitializeData();

			if (selectedTankSetup == -1)
				selectedTankSetup = 0;

			if (state != StartState.Editor)
			{
				AssignResourcesToPart(false);
				gameLoaded = true;
			}
			else
				AssignResourcesToPart(false);
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
			if (_initialized) return;

			SetupTankList();
			_weightList = ParseTools.ParseDoubles(tankMass);
			_tankCostList = ParseTools.ParseDoubles(tankCost);

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

			_initialized = true;
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next tank setup")]
		public void NextTankSetupEvent()
		{
			selectedTankSetup++;
			Debug.Log("InsterstellarFuelSwitch nextTankSetupEvent selectedTankSetup++ = " + selectedTankSetup);

			if (selectedTankSetup >= _tankList.Count)
			{
				selectedTankSetup = 0;
				Debug.Log("InsterstellarFuelSwitch nextTankSetupEvent selectedTankSetup = 0 ");
			}

			AssignResourcesToPart(true);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous tank setup")]
		public void PreviousTankSetupEvent()
		{
			selectedTankSetup--;
			Debug.Log("InsterstellarFuelSwitch previousTankSetupEvent selectedTankSetup-- = " + selectedTankSetup);

			if (selectedTankSetup < 0)
			{
				selectedTankSetup = _tankList.Count - 1;
				Debug.Log("InsterstellarFuelSwitch previousTankSetupEvent tankList.Count - 1 = " + selectedTankSetup);
			}

			AssignResourcesToPart(true);
		}

		public void SelectTankSetup(int i, bool calledByPlayer)
		{
			InitializeData();
			if (selectedTankSetup == i) return;

			selectedTankSetup = i;
			Debug.Log("InsterstellarFuelSwitch selectTankSetup selectedTankSetup = i = " + selectedTankSetup);
			AssignResourcesToPart(calledByPlayer);
		}

		private void AssignResourcesToPart(bool calledByPlayer)
		{
			// destroying a resource messes up the gui in editor, but not in flight.
			SetupTankInPart(part, calledByPlayer);
			if (HighLogic.LoadedSceneIsEditor)
			{
				foreach (var conterPart in part.symmetryCounterparts)
				{
					SetupTankInPart(conterPart, calledByPlayer);
					var symSwitch = conterPart.GetComponent<InsterstellarFuelSwitch>();
					if (symSwitch == null) continue;

					symSwitch.selectedTankSetup = selectedTankSetup;
					Debug.Log("InsterstellarFuelSwitch assignResourcesToPart symSwitch.selectedTankSetup = selectedTankSetup = " + selectedTankSetup);
				}
			}

			if (_tweakableUi == null)
				_tweakableUi = part.FindActionWindow();

			if (_tweakableUi != null)
				_tweakableUi.displayDirty = true;
			else
				Debug.Log("InsterstellarFuelSwitch assignResourcesToPart - no UI to refresh");
		}

		private void SetupTankInPart(Part currentPart, bool calledByPlayer)
		{
			// create new ResourceNode
			var newResources = new List<string>();
			var newResourceNodes = new List<ConfigNode>();

			var tankCount = selectedTankSetup;
			if (tankCount >= 0 && tankCount < _tankList.Count)
			{
				Debug.Log("InsterstellarFuelSwitch assignResourcesToPart setupTankInPart = " + selectedTankSetup);
				foreach (var tankSresource in _tankList[tankCount].Resources)
				{
					if (tankSresource.Name == "Structural") continue;

					var resourceName = tankSresource.Name;
					newResources.Add(resourceName);

					var newResourceNode = new ConfigNode("RESOURCE");
					var maxAmount = tankSresource.MaxAmount*volumeMultiplier;

					newResourceNode.AddValue("name", resourceName);
					newResourceNode.AddValue("maxAmount", maxAmount);

					PartResource existingResource = null;
					if (HighLogic.LoadedSceneIsFlight)
					{
						foreach (PartResource pr in part.Resources)
						{
							if (!pr.name.Equals(resourceName)) continue;

							existingResource = pr;
							break;
						}
					}

					if (existingResource != null)
						newResourceNode.AddValue("amount", Math.Min(existingResource.amount, maxAmount));
					else if (calledByPlayer && !HighLogic.LoadedSceneIsEditor)
						newResourceNode.AddValue("amount", 0.0f);
					else
						newResourceNode.AddValue("amount", tankSresource.Amount*volumeMultiplier);

					newResourceNodes.Add(newResourceNode);
				}
			}

			// verify we need to update
			if (newResourceNodes.Count > 0)
			{
				currentPart.Resources.list.Clear();
				var partResources = currentPart.GetComponents<PartResource>();
				foreach (var resource in partResources)
				{
					Debug.Log("InsterstellarFuelSwitch setupTankInPart removing resource: " + resource.resourceName);
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
			return selectedTankSetup >= 0 && selectedTankSetup < _tankCostList.Count
				? (float) _tankCostList[selectedTankSetup]
				: 0f;
		}

		private void UpdateWeight(Part currentPart, int newTankSetup, bool calledByPlayer = false)
		{
			// when changed by player
			if (calledByPlayer && HighLogic.LoadedSceneIsFlight) return;

			if (newTankSetup < _weightList.Count)
				currentPart.mass = (float) ((basePartMass + _weightList[newTankSetup])*massMultiplier);
		}

		public override void OnUpdate()
		{
			//There were some issues with resources slowly trickling in, so I changed this to 0.1% instead of empty.
			var showSwitchButtons = availableInFlight 
				&& !part.GetComponents<PartResource>().Any(r => r.amount > r.maxAmount/1000);

			Events["nextTankSetupEvent"].guiActive = showSwitchButtons;
			Events["previousTankSetupEvent"].guiActive = showSwitchButtons;
		}

		public void Update()
		{
			if (HighLogic.LoadedSceneIsEditor)
				dryMassInfo = part.mass;
		}

		private void SetupTankList()
		{
			_tankList = new List<FSmodularTank>();
			_weightList = new List<double>();
			_tankCostList = new List<double>();

			// First find the amounts each tank type is filled with
			var resourceList = new List<List<double>>();
			var initialResourceList = new List<List<double>>();
			var resourceTankArray = resourceAmounts.Split(';');
			var initialResourceTankArray = initialResourceAmounts.Split(';');

			if (initialResourceAmounts.Equals("") || initialResourceTankArray.Length != resourceTankArray.Length)
				initialResourceTankArray = resourceTankArray;

			for (var tankCount = 0; tankCount < resourceTankArray.Length; tankCount++)
			{
				resourceList.Add(new List<double>());
				initialResourceList.Add(new List<double>());
				var resourceAmountArray = resourceTankArray[tankCount].Trim().Split(',');
				var initialResourceAmountArray = initialResourceTankArray[tankCount].Trim().Split(',');

				if (initialResourceAmounts.Equals("") || initialResourceAmountArray.Length != resourceAmountArray.Length)
					initialResourceAmountArray = resourceAmountArray;

				for (var amountCount = 0; amountCount < resourceAmountArray.Length; amountCount++)
				{
					try
					{
						resourceList[tankCount].Add(double.Parse(resourceAmountArray[amountCount].Trim()));
						initialResourceList[tankCount].Add(double.Parse(initialResourceAmountArray[amountCount].Trim()));
					}
					catch
					{
						Debug.Log("InsterstellarFuelSwitch: error parsing resource amount " + tankCount + "/" + amountCount + ": '" +
						          resourceTankArray[amountCount] + "': '" + resourceAmountArray[amountCount].Trim() + "'");
					}
				}
			}

			// Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
			var tankArray = resourceNames.Split(';');
			for (var tankCount = 0; tankCount < tankArray.Length; tankCount++)
			{
				var newTank = new FSmodularTank();
				_tankList.Add(newTank);
				var resourceNameArray = tankArray[tankCount].Split(',');
				for (var nameCount = 0; nameCount < resourceNameArray.Length; nameCount++)
				{
					var newResource = new FSresource(resourceNameArray[nameCount].Trim(' '));
					if (resourceList[tankCount] != null && nameCount < resourceList[tankCount].Count)
					{
						newResource.MaxAmount = resourceList[tankCount][nameCount];
						newResource.Amount = initialResourceList[tankCount][nameCount];
					}
					newTank.Resources.Add(newResource);
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
			if (!showInfo) return string.Empty;

			var resourceList = ParseTools.ParseNames(resourceNames);
			var info = new StringBuilder();
			info.AppendLine("Fuel tank setups available:");
			foreach (var resource in resourceList)
			{
				info.AppendLine(resource.Replace(",", ", "));
			}
			return info.ToString();
		}
	}
}
