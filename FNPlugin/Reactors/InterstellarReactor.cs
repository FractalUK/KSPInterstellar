using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;

namespace FNPlugin
{
    class InterstellarReactor : FNResourceSuppliableModule, IThermalSource, IUpgradeableModule
    {
        public enum ReactorTypes
        {
            FISSION_MSR = 1,
            FISSION_GFR = 2,
            FUSION_DT = 4,
            FUSION_GEN3 = 8,
            AIM_FISSION_FUSION = 16,
            ANTIMATTER = 32
        }

        // Persistent True
        [KSPField(isPersistant = true)]
        public bool IsEnabled;
        [KSPField(isPersistant = true)]
        public bool isupgraded;
        [KSPField(isPersistant = true)]
        public bool breedtritium;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float ongoing_consumption_rate;
        [KSPField(isPersistant = true)]
        public bool reactorInit;
        [KSPField(isPersistant = true)]
        public bool startDisabled;
        [KSPField(isPersistant = true)]
        public float neutronEmbrittlementDamage;
        [KSPField(isPersistant = true)]
        public float windowPositionX = 20;
        [KSPField(isPersistant = true)]
        public float windowPositionY = 20;

        // Persistent False
        [KSPField(isPersistant = false)]
        public bool canBeCombinedWithLab = false;
        [KSPField(isPersistant = false)]
        public bool canBreedTritium = false;
        [KSPField(isPersistant = false)]
        public bool canDisableTritiumBreeding = true;
        [KSPField(isPersistant = false)]
        public float breedDivider = 100000.0f;
        [KSPField(isPersistant = false)]
        public float bonusBufferFactor = 0.05f;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Heat Transport Efficency")]
        public float heatTransportationEfficiency = 0.8f;
        [KSPField(isPersistant = false)]
        public float ReactorTemp;
        [KSPField(isPersistant = false)]
        public float powerOutputMultiplier = 1;
        [KSPField(isPersistant = false)]
        public float upgradedReactorTemp;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Radius")]
        public float radius;
        [KSPField(isPersistant = false)]
        public float minimumThrottle = 0;
        [KSPField(isPersistant = false)]
        public bool canShutdown = true;
        [KSPField(isPersistant = false)]
        public bool consumeGlobal;
        [KSPField(isPersistant = false)]
        public int reactorType;
        [KSPField(isPersistant = false)]
        public int upgradedReactorType;
        [KSPField(isPersistant = false)]
        public float fuelEfficiency;
        [KSPField(isPersistant = false)]
        public float upgradedFuelEfficiency;
        [KSPField(isPersistant = false)]
        public bool containsPowerGenerator = false;
        [KSPField(isPersistant = false)]
        public float fuelUsePerMJMult = 1f;
        [KSPField(isPersistant = false)]
        public float wasteHeatMultiplier = 1;

        [KSPField(isPersistant = false)]
        public float alternatorPowerKW = 0;
        [KSPField(isPersistant = false)]
        public float thermalPropulsionEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float thermalEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float chargedParticleEnergyEfficiency = 1;
        [KSPField(isPersistant = false)]
        public float maxGeeForceFuelInput = 0;
        [KSPField(isPersistant = false)]
        public float minGeeForceModifier = 0.1f;
        [KSPField(isPersistant = false)]
        public float neutronEmbrittlementLifepointsMax = 100;
        [KSPField(isPersistant = false)]
        public float neutronEmbrittlementDivider = 1e+9f;
        [KSPField(isPersistant = false)]
        public float hotBathModifier = 1;
        [KSPField(isPersistant = false)]
        public float thermalProcessingModifier = 1;
        [KSPField(isPersistant = false, guiActive = false)]
        public int supportedPropellantsTypes = 119;
        [KSPField(isPersistant = false)]
        public bool fullPowerForNonNeutronAbsorbants = true;


        // Visible imput parameters 
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Bimodel upgrade tech")]
        public string bimodelUpgradeTechReq = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade tech")]
        public string powerUpgradeTechReq = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade Power Multiplier")]
        public float powerUpgradeTechMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Extra upgrade Core temp Mult")]
        public float powerUpgradeCoreTempMult = 1;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Raw Power", guiUnits = " MJ")]
        public float currentRawPowerOutput;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Output (Basic)", guiUnits = " MW")]
        public float PowerOutput = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Output (Upgraded)", guiUnits = " MW")]
        public float upgradedPowerOutput = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Upgrade")]
        public string upgradeTechReq = String.Empty;

        // GUI strings
        [KSPField(isPersistant = false, guiActive = false, guiName = "Type")]
        public string reactorTypeStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Core Temp")]
        public string coretempStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Status")]
        public string statusStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Thermal Power")]
        public string currentTPwr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Charged Power")]
        public string currentCPwr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Fuel")]
        public string fuelModeStr = String.Empty;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Connections")]
        public string connectedRecieversStr = String.Empty;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Power to Supply frame")]
        protected float max_power_to_supply = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Thermal Power Recieved")]
        protected float thermal_power_received = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Fixed Max Thermal Power")]
        protected float fixed_maximum_thermal_power;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thermal To Supply")]
        protected float max_thermal_to_supply;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Min throttle")]
        protected float min_throttle;

        // Gui floats
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Empty Mass", guiUnits = " t")]
        public float partMass = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Max Thermal Power", guiUnits = " MW")]
        public float maximumThermalPowerFloat = 0;
        [KSPField(isPersistant = false, guiActive = false, guiName = "Gee Force Mod")]
        public float geeForceModifier;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Power Produced", guiUnits = " MW")]
        public float ongoing_total_power_f;

        // value types
        protected bool hasrequiredupgrade = false;
        protected double tritium_rate;
        protected int deactivate_timer = 0;
        protected List<ReactorFuelMode> fuel_modes;
        protected ReactorFuelMode current_fuel_mode;
        protected double powerPcnt;
        protected double tritium_produced_d;
        protected float helium_produced_f;
        protected long update_count;
        protected long last_draw_update;
        protected float ongoing_thermal_power_f;
        protected float ongoing_charged_power_f;
        
        protected double total_power_per_frame;
        protected bool decay_ongoing = false;
        protected Rect windowPosition;
        protected int windowID = 90175467;
        protected bool render_window = false;
        protected GUIStyle bold_label;
        protected float previousTimeWarp;
        protected bool _hasPowerUpgradeTechnology;
        protected bool? hasBimodelUpgradeTechReq;

        protected PartResource thermalPowerResource = null;
        protected PartResource chargedPowerResource = null;
        protected PartResource wasteheatPowerResource = null;

        // reference types
        protected Dictionary<Guid, float> connectedRecievers = new Dictionary<Guid, float>();
        protected Dictionary<Guid, float> connectedRecieversFraction = new Dictionary<Guid, float>();
        protected float connectedRecieversSum;

        protected double tritium_molar_mass_ratio = 3.0160 / 7.0183;
        protected double helium_molar_mass_ratio = 4.0023 / 7.0183;

        protected double partBaseWasteheat;

        protected double storedIsThermalEnergyGenratorActive;
        protected double storedIsChargedEnergyGenratorActive;
        protected double currentIsThermalEnergyGenratorActive;
        protected double currentIsChargedEnergyGenratorActive;

        protected ElectricGeneratorType _firstGeneratorType;

        public List<ReactorProduction> reactorProduction = new List<ReactorProduction>();

        public double UseProductForPropulsion(double ratio, double consumedAmount)
        {
            if (ratio == 0) return 0;

            var hydrogenDefinition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Hydrogen);

            double hydrogenMassSum = 0;

            foreach (var product in reactorProduction)
            {
                if (product.mass == 0) continue;

                var effectiveMass = ratio * product.mass;

                // sum product mass
                hydrogenMassSum += effectiveMass;

                // remove product from store
                var fuelAmount = product.fuelmode.Density > 0 ? (effectiveMass / product.fuelmode.Density) : 0;
                if (fuelAmount == 0) continue;

                part.RequestResource(product.fuelmode.FuelName, fuelAmount);
            }

            var hydrogenAmount = Math.Min(hydrogenMassSum / hydrogenDefinition.density, consumedAmount);

            // at real time we need twise
            if (!this.vessel.packed)
                hydrogenAmount *= 2;

            return part.RequestResource(hydrogenDefinition.name, -hydrogenAmount);
        }

        public int SupportedPropellantsTypes { get { return supportedPropellantsTypes; } }

        public bool FullPowerForNonNeutronAbsorbants { get { return fullPowerForNonNeutronAbsorbants; } }

        public double EfficencyConnectedThermalEnergyGenrator { get { return storedIsThermalEnergyGenratorActive; } }

        public double EfficencyConnectedChargedEnergyGenrator { get { return storedIsChargedEnergyGenratorActive; } }


        public void NotifyActiveThermalEnergyGenrator(double efficency, ElectricGeneratorType generatorType)
        {
            currentIsThermalEnergyGenratorActive = efficency;
            if (_firstGeneratorType == ElectricGeneratorType.unknown)
                _firstGeneratorType = generatorType;
        }

        public void NotifyActiveChargedEnergyGenrator(double efficency, ElectricGeneratorType generatorType)
        {
            currentIsChargedEnergyGenratorActive = efficency;
            if (_firstGeneratorType == ElectricGeneratorType.unknown)
                _firstGeneratorType = generatorType;
        }

        public bool ShouldApplyBalance(ElectricGeneratorType generatorType)
        {
            return generatorType == _firstGeneratorType && storedIsThermalEnergyGenratorActive > 0 && storedIsChargedEnergyGenratorActive > 0; 
        }

        public bool IsThermalSource  {  get { return true; } }

        public float ThermalProcessingModifier { get { return thermalProcessingModifier; } }

        public Part Part { get { return this.part; } }

        public double ProducedWasteHeat { get { return ongoing_total_power_f ; } }

        public float PowerUpgradeTechnologyBonus { get { return _hasPowerUpgradeTechnology ? powerUpgradeTechMult : 1; } }

        public void AttachThermalReciever(Guid key, float radius)
        {
            if (!connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Add(key, radius);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }

            UpdateConnectedRecieversStr();
        }

        public void DetachThermalReciever(Guid key)
        {
            if (connectedRecievers.ContainsKey(key))
            {
                connectedRecievers.Remove(key);
                connectedRecieversSum = connectedRecievers.Sum(r => r.Value);
                connectedRecieversFraction = connectedRecievers.ToDictionary(a => a.Key, a => a.Value / connectedRecieversSum);
            }

            UpdateConnectedRecieversStr();
        }

        public float GetFractionThermalReciever(Guid key)
        {
            float result;
            if (connectedRecieversFraction.TryGetValue(key, out result))
                return result;
            else
                return 0;
        }

        private void UpdateConnectedRecieversStr()
        {
            if (connectedRecievers == null) return;

            connectedRecieversStr = connectedRecievers.Count().ToString() + " (" + connectedRecieversSum.ToString("0.000") + "m)";
        }

        public float ThermalTransportationEfficiency { get { return heatTransportationEfficiency; } }

        public bool HasBimodelUpgradeTechReq
        {
            get
            {
                if (hasBimodelUpgradeTechReq == null)
                    hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirmentOrEmpty(bimodelUpgradeTechReq);
                return (bool)hasBimodelUpgradeTechReq;
            }
        }

        public float ThermalEnergyEfficiency { get { return HasBimodelUpgradeTechReq ? thermalEnergyEfficiency : 0; } }

        public float ChargedParticleEnergyEfficiency {  get { return chargedParticleEnergyEfficiency; } }

        public bool IsSelfContained { get { return containsPowerGenerator; } }

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public float PowerBufferBonus { get { return this.bonusBufferFactor; } }

        public float RawMaximumPower { get { return RawPowerOutput; } }

        public double FuelEfficiency
        {
            get
            {
                var basEfficency = isupgraded
                    ? upgradedFuelEfficiency > 0
                        ? upgradedFuelEfficiency
                        : fuelEfficiency
                    : fuelEfficiency;

                return basEfficency * current_fuel_mode.FuelEfficencyMultiplier;
            }
        }

        public int ReactorType { get { return isupgraded ? upgradedReactorType > 0 ? upgradedReactorType : reactorType : reactorType; } }

        public virtual string TypeName { get { return isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName; } }

        public virtual double ChargedPowerRatio 
        { 
            get 
            { 
                return current_fuel_mode != null
                    ? current_fuel_mode.ChargedPowerRatio * ChargedParticleEnergyEfficiency
                    : 0f; 
            } 
        }

        public virtual float CoreTemperature 
        { 
            get 
            {
                    var baseCoreTemperature = isupgraded ? upgradedReactorTemp != 0 ? upgradedReactorTemp : ReactorTemp : ReactorTemp;

                    var modifiedBaseCoreTemperature = baseCoreTemperature * (float)Math.Pow(ReactorEmbrittlemenConditionRatio, 2);

                    return _hasPowerUpgradeTechnology ? modifiedBaseCoreTemperature * powerUpgradeCoreTempMult : modifiedBaseCoreTemperature;
            }
        }

        public float HotBathTemperature 
        { 
            get
            {
                return CoreTemperature * hotBathModifier;
            }
        }

        public float ThermalPropulsionEfficiency { get { return thermalPropulsionEfficiency; } }

        public virtual float ReactorEmbrittlemenConditionRatio { get { return (float)Math.Min(Math.Max(1 - (neutronEmbrittlementDamage / neutronEmbrittlementLifepointsMax), 0.01), 1);  } }

        public virtual float NormalisedMaximumPower
        {
            get
            {
                float normalised_fuel_factor = current_fuel_mode == null ? 1.0f : (float)current_fuel_mode.NormalisedReactionRate;
                return RawPowerOutput * normalised_fuel_factor * (float)Math.Pow(ReactorEmbrittlemenConditionRatio, 2);
            }
        }

        public virtual float MaximumThermalPower { get { return NormalisedMaximumPower * (1.0f - (float)ChargedPowerRatio); } }

        public virtual float MinimumPower { get { return 0; } }

        public virtual float MaximumChargedPower { get { return NormalisedMaximumPower * (float)ChargedPowerRatio; } }

        public virtual bool IsNuclear { get { return false; } }

        public virtual bool IsActive { get { return IsEnabled; } }

        public virtual bool IsVolatileSource { get { return false; } }

        public virtual bool IsNeutronRich { get { return false; } }

        public bool IsUpgradeable { get { return upgradedName != ""; } }

        public virtual float MaximumPower { get { return MaximumThermalPower + MaximumChargedPower; } }

        public virtual float StableMaximumReactorPower { get { return IsEnabled ? RawPowerOutput : 0; } }

        public virtual float RawPowerOutput
        {
            get
            {
                var rawPower = (isupgraded && upgradedPowerOutput != 0 ? upgradedPowerOutput : PowerOutput) * PowerUpgradeTechnologyBonus;
                return rawPower * powerOutputMultiplier;
            }
        }

        public virtual void StartReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = false;
            else
            {
                if (IsNuclear) return;

                IsEnabled = true;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Reactor Control Window", active = true, guiActiveUnfocused = true, unfocusedRange = 5f)]
        public void ToggleReactorControlWindow()
        {
            render_window = !render_window;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor()
        {
            StartReactor();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor()
        {
            if (HighLogic.LoadedSceneIsEditor)
                startDisabled = true;
            else
            {
                if (IsNuclear) return;

                IsEnabled = false;
            }
        }

        [KSPEvent(guiActive = true, guiName = "Enable Tritium Breeding", active = false)]
        public void StartBreedTritiumEvent()
        {
            if (!IsNeutronRich) return;

            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Tritium Breeding", active = true)]
        public void StopBreedTritiumEvent()
        {
            if (!IsNeutronRich) return;

            breedtritium = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor()
        {
            if (ResearchAndDevelopment.Instance == null) return;
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) return;

            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }



        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            StartReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            if (IsNuclear) return;

            IsEnabled = !IsEnabled;
        }

        private bool CanPartUpgradeAlternative()
        {
            if (PluginHelper.PartTechUpgrades == null)
            {
                print("[KSP Interstellar] PartTechUpgrades is not initialized");
                return false;
            }

            string upgradetechName;
            if (!PluginHelper.PartTechUpgrades.TryGetValue(part.name, out upgradetechName))
            {
                print("[KSP Interstellar] PartTechUpgrade entry is not found for part '" + part.name + "'");
                return false;
            }

            print("[KSP Interstellar] Found matching Interstellar upgradetech for part '" + part.name + "' with technode " + upgradetechName);

            return PluginHelper.upgradeAvailable(upgradetechName);
        }

        public override void OnStart(PartModule.StartState state)
        {
            windowPosition = new Rect(windowPositionX, windowPositionY, 300, 100);

            _firstGeneratorType = ElectricGeneratorType.unknown;
            _hasPowerUpgradeTechnology = PluginHelper.upgradeAvailable(powerUpgradeTechReq);
            previousTimeWarp = TimeWarp.fixedDeltaTime - 1.0e-6f;
            hasBimodelUpgradeTechReq = PluginHelper.HasTechRequirmentOrEmpty(bimodelUpgradeTechReq);

            if (!part.Resources.Contains(FNResourceManager.FNRESOURCE_THERMALPOWER))
            {
                ConfigNode node = new ConfigNode("RESOURCE");
                node.AddValue("name", FNResourceManager.FNRESOURCE_THERMALPOWER);
                node.AddValue("maxAmount", PowerOutput);
                node.AddValue("possibleAmount", 0);
                part.AddResource(node);
                part.Resources.UpdateList();
            }

            // while in edit mode, listen to on attach event
            if (state == StartState.Editor)
                part.OnEditorAttach += OnEditorAttach;

            // initialise resource defenitions
            thermalPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_THERMALPOWER);
            chargedPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
            wasteheatPowerResource = part.Resources.list.FirstOrDefault(r => r.resourceName == FNResourceManager.FNRESOURCE_WASTEHEAT);

            // calculate WasteHeat Capacity
            partBaseWasteheat = part.mass * 1.0e+5 * wasteHeatMultiplier + (StableMaximumReactorPower * 100);

            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_THERMALPOWER, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;

            var rnd = new System.Random();
            windowID = rnd.Next(int.MaxValue);
            base.OnStart(state);

            // check if we need to upgrade
            if (state == StartState.Editor)
            {
                print("[KSPI] Checking for upgrade tech: " + UpgradeTechnology);
                if (this.HasTechsRequiredToUpgrade() || CanPartUpgradeAlternative())
                {
                    print("[KSPI] Found required upgradeTech, Upgrading Reactor");
                    upgradePartModule();
                }
            }

            // configure reactor modes
            print("[KSP Interstellar] Configuring Reactor Fuel Modes");
            fuel_modes = GetReactorFuelModes();
            setDefaultFuelMode();
            UpdateFuelMode();
            print("[KSP Interstellar] Configuration Reactor Fuels Complete");

            if (state == StartState.Editor)
            {
                maximumThermalPowerFloat = MaximumThermalPower;
                reactorTypeStr = isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName;
                coretempStr = CoreTemperature.ToString("0") + " K";
                return;
            }

            if (this.HasTechsRequiredToUpgrade() || CanPartUpgradeAlternative())
                hasrequiredupgrade = true;

            if (!reactorInit)
            {
                IsEnabled = true;
                reactorInit = true;
                breedtritium = true;

                if (startDisabled)
                {
                    last_active_time = (float)(Planetarium.GetUniversalTime() - 4.0 * GameConstants.EARH_DAY_SECONDS);
                    IsEnabled = false;
                    startDisabled = false;
                    breedtritium = false;
                }
            }

            print("[KSP Interstellar] Reactor Persistent Resource Update");
            if (IsEnabled && last_active_time > 0)
                DoPersistentResourceUpdate();

            // only force activate if not with a engine model
            var myAttachedEngine = this.part.FindModuleImplementing<ModuleEngines>();
            if (myAttachedEngine == null)
            {
                this.part.force_activate();
                Fields["partMass"].guiActiveEditor = true;
                Fields["radius"].guiActiveEditor = true;
                Fields["connectedRecieversStr"].guiActiveEditor = true;
                Fields["heatTransportationEfficiency"].guiActiveEditor = true;
            }

            //RenderingManager.AddToPostDrawQueue(0, OnGUI);
            print("[KSP Interstellar] Succesfully Completed Configuring Reactor");
        }

        /// <summary>
        /// Event handler called when part is attached to another part
        /// </summary>
        private void OnEditorAttach()
        {
            foreach (var node in part.attachNodes)
            {
                if (node.attachedPart == null) continue;

                var generator = node.attachedPart.FindModuleImplementing<FNGenerator>();
                if (generator != null)
                    generator.FindAndAttachToThermalSource();
            }
        }

        public void Update()
        {
            //Update Events
            //Events["ActivateReactor"].active = (HighLogic.LoadedSceneIsEditor && startDisabled) || (!HighLogic.LoadedSceneIsEditor && !IsEnabled && !IsNuclear);
            //Events["DeactivateReactor"].active = (HighLogic.LoadedSceneIsEditor && !startDisabled) || (!HighLogic.LoadedSceneIsEditor && IsEnabled && !IsNuclear);
            Events["ActivateReactor"].active = (HighLogic.LoadedSceneIsEditor && startDisabled);
            Events["DeactivateReactor"].active = (HighLogic.LoadedSceneIsEditor && !startDisabled);
        }

        protected void UpdateFuelMode()
        {
            fuelModeStr = current_fuel_mode != null ? current_fuel_mode.ModeGUIName : "null";
        }

        public override void OnUpdate()
        {
            maximumThermalPowerFloat = MaximumThermalPower;

            Events["StartBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && !breedtritium && IsNeutronRich && IsEnabled;
            Events["StopBreedTritiumEvent"].active = canDisableTritiumBreeding && canBreedTritium && breedtritium && IsNeutronRich && IsEnabled;
            Events["RetrofitReactor"].active = ResearchAndDevelopment.Instance != null ? !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade : false;
            UpdateFuelMode();

            reactorTypeStr = isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName;
            coretempStr = CoreTemperature.ToString("0") + " K";
            if (update_count - last_draw_update > 10)
            {
                if (IsEnabled)
                {
                    if (current_fuel_mode != null && !current_fuel_mode.ReactorFuels.Any(fuel => GetFuelAvailability(fuel) <= 0))
                    {
                        if (ongoing_thermal_power_f > 0) 
                            currentTPwr = PluginHelper.getFormattedPowerString(ongoing_thermal_power_f) + "_th";
                        if (ongoing_charged_power_f > 0) 
                            currentCPwr = PluginHelper.getFormattedPowerString(ongoing_charged_power_f) + "_cp";
                        statusStr = "Active (" + powerPcnt.ToString("0.00") + "%)";
                    }
                    else if (current_fuel_mode != null)
                        statusStr = current_fuel_mode.ReactorFuels.FirstOrDefault(fuel => GetFuelAvailability(fuel) <= 0).FuelName + " Deprived";
                }
                else
                {
                    if (powerPcnt > 0)
                        statusStr = "Decay Heating (" + powerPcnt.ToString("0.00") + "%)";
                    else
                        statusStr = "Offline";
                }

                last_draw_update = update_count;
            }
            //if (!vessel.isActiveVessel || part == null) RenderingManager.RemoveFromPostDrawQueue(0, OnGUI);
            update_count++;
        }

        /// <summary>
        /// FixedUpdate is also called when not activated
        /// </summary>
        public void FixedUpdate() 
        {
            if (!HighLogic.LoadedSceneIsFlight) return;

            base.OnFixedUpdate();

            // add alternator power
            if (alternatorPowerKW != 0)
            {
                part.RequestResource("ElectricCharge", -alternatorPowerKW * TimeWarp.fixedDeltaTime);
                part.temperature = part.temperature + (TimeWarp.fixedDeltaTime * alternatorPowerKW / 1000.0 / part.mass);
            }
        }

        public override void OnFixedUpdate() // OnFixedUpdate is only called when (force) activated
        {
            storedIsThermalEnergyGenratorActive = currentIsThermalEnergyGenratorActive;
            storedIsChargedEnergyGenratorActive = currentIsChargedEnergyGenratorActive;
            currentIsThermalEnergyGenratorActive = 0;
            currentIsChargedEnergyGenratorActive = 0;

            currentRawPowerOutput = RawPowerOutput;

            decay_ongoing = false;
            //base.OnFixedUpdate();
            if (IsEnabled && MaximumPower > 0)
            {
                if (ReactorIsOverheating())
                {
                    if (FlightGlobals.ActiveVessel == vessel)
                        ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency reactor shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);

                    IsEnabled = false;
                    return;
                }

                // calculate thermalpower capacity
                if (TimeWarp.fixedDeltaTime != previousTimeWarp)
                {
                    if (thermalPowerResource != null)
                    {
                        var requiredThermalCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumThermalPower);
                        var previousThermalCapacity = Math.Max(0.0001, 10 * previousTimeWarp * MaximumThermalPower);

                        thermalPowerResource.maxAmount = requiredThermalCapacity;
                        thermalPowerResource.amount = requiredThermalCapacity > previousThermalCapacity
                            ? Math.Max(0, Math.Min(requiredThermalCapacity, thermalPowerResource.amount + requiredThermalCapacity - previousThermalCapacity))
                            : Math.Max(0, Math.Min(requiredThermalCapacity, (thermalPowerResource.amount / thermalPowerResource.maxAmount) * requiredThermalCapacity));
                    }

                    if (chargedPowerResource != null)
                    {
                        var requiredChargedCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumChargedPower);
                        var previousChargedCapacity = Math.Max(0.0001, 10 * previousTimeWarp * MaximumChargedPower);

                        chargedPowerResource.maxAmount = requiredChargedCapacity;
                        chargedPowerResource.amount = requiredChargedCapacity > previousChargedCapacity
                            ? Math.Max(0, Math.Min(requiredChargedCapacity, chargedPowerResource.amount + requiredChargedCapacity - previousChargedCapacity))
                            : Math.Max(0, Math.Min(requiredChargedCapacity, (chargedPowerResource.amount / chargedPowerResource.maxAmount) * requiredChargedCapacity));
                    }

                    if (wasteheatPowerResource)
                    {
                        var requiredWasteheatCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * partBaseWasteheat);
                        var previousWasteheatCapacity = Math.Max(0.0001, 10 * previousTimeWarp * partBaseWasteheat);

                        var wasteHeatRatio = Math.Max(0, Math.Min(1, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                        wasteheatPowerResource.maxAmount = requiredWasteheatCapacity;
                        wasteheatPowerResource.amount = requiredWasteheatCapacity * wasteHeatRatio;

                        //wasteheatPowerResource.maxAmount = requiredWasteheatCapacity;
                        //wasteheatPowerResource.amount = requiredWasteheatCapacity > previousWasteheatCapacity
                        //    ? Math.Max(0, Math.Min(requiredWasteheatCapacity, wasteheatPowerResource.amount + requiredWasteheatCapacity - previousWasteheatCapacity))
                        //    : Math.Max(0, Math.Min(requiredWasteheatCapacity, (wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount) * requiredWasteheatCapacity));
                    }
                }
                else
                {
                    if (thermalPowerResource != null)
                    {
                        thermalPowerResource.maxAmount = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumThermalPower);
                        thermalPowerResource.amount = Math.Min(thermalPowerResource.amount, thermalPowerResource.maxAmount);
                    }

                    if (chargedPowerResource != null)
                    {
                        chargedPowerResource.maxAmount = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * MaximumChargedPower);
                        chargedPowerResource.amount = Math.Min(chargedPowerResource.amount, chargedPowerResource.maxAmount);
                    }

                    if (wasteheatPowerResource != null)
                    {
                        var wasteHeatRatio = Math.Max(0, Math.Min(1, wasteheatPowerResource.amount / wasteheatPowerResource.maxAmount));
                        var requiredWasteheatCapacity = Math.Max(0.0001, 10 * TimeWarp.fixedDeltaTime * partBaseWasteheat);
                        wasteheatPowerResource.maxAmount = requiredWasteheatCapacity;
                        wasteheatPowerResource.amount = requiredWasteheatCapacity * wasteHeatRatio;
                    }
                }

                previousTimeWarp = TimeWarp.fixedDeltaTime;

                // Max Power
                max_power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime, 0);
                geeForceModifier = maxGeeForceFuelInput > 0 ? (float)Math.Min(Math.Max(maxGeeForceFuelInput > 0 ? 1.1 - (part.vessel.geeForce / maxGeeForceFuelInput) : 0.1, 0.1), 1) : 1;
                float fuel_ratio = (float)Math.Min(current_fuel_mode.ReactorFuels.Min(fuel => GetFuelRatio(fuel, FuelEfficiency, max_power_to_supply * geeForceModifier)), 1.0);
                min_throttle = fuel_ratio > 0 ? minimumThrottle / fuel_ratio : 1;

                // Charged Power
                var fixed_maximum_charged_power = MaximumChargedPower * TimeWarp.fixedDeltaTime;
                double max_charged_to_supply = Math.Max(fixed_maximum_charged_power, 0) * fuel_ratio * geeForceModifier;
                double charged_power_received = supplyManagedFNResourceWithMinimum(max_charged_to_supply, min_throttle, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                double charged_power_ratio = ChargedPowerRatio > 0 ? charged_power_received / max_charged_to_supply : 0;

                // Thermal Power
                fixed_maximum_thermal_power = MaximumThermalPower * TimeWarp.fixedDeltaTime;
                max_thermal_to_supply = Math.Max(fixed_maximum_thermal_power, 0) * fuel_ratio * geeForceModifier;
                thermal_power_received = supplyManagedFNResourceWithMinimum(max_thermal_to_supply, min_throttle, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double thermal_power_ratio = (1 - ChargedPowerRatio) > 0 ? thermal_power_received / max_thermal_to_supply : 0;

                // add additional power
                var thermal_shortage_ratio = charged_power_ratio > thermal_power_ratio ? charged_power_ratio - thermal_power_ratio : 0;
                var chargedpower_shortagage_ratio = thermal_power_ratio > charged_power_ratio ? thermal_power_ratio - charged_power_ratio : 0;

                thermal_power_received = thermal_power_received + (float)(thermal_shortage_ratio * fixed_maximum_thermal_power * fuel_ratio * geeForceModifier);
                charged_power_received = charged_power_received + (chargedpower_shortagage_ratio * fixed_maximum_charged_power * fuel_ratio * geeForceModifier);

                // update GUI
                ongoing_thermal_power_f = (float)(thermal_power_received / TimeWarp.fixedDeltaTime);
                ongoing_charged_power_f = (float)(charged_power_received / TimeWarp.fixedDeltaTime);

                // Total
                double total_power_received = thermal_power_received + charged_power_received;

                neutronEmbrittlementDamage += (float)(total_power_received * current_fuel_mode.NeutronsRatio / neutronEmbrittlementDivider);
                ongoing_total_power_f = (float)total_power_received / TimeWarp.fixedDeltaTime;

                total_power_per_frame = total_power_received;
                double total_power_ratio = total_power_received / MaximumPower / TimeWarp.fixedDeltaTime;
                ongoing_consumption_rate = (float)total_power_ratio;

                // consume fuel
                foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
                {
                    var fuel_request = total_power_received * fuel.FuelUsePerMJ * fuelUsePerMJMult;
                    var fuel_recieved = ConsumeReactorFuel(fuel, fuel_request);
                }

                // refresh production list
                reactorProduction.Clear();

                // produce reactor products
                foreach (ReactorProduct product in current_fuel_mode.ReactorProducts)
                {
                    var product_supply = total_power_received * product.ProductUsePerMJ * fuelUsePerMJMult;
                    var massProduced = ProduceReactorProduct(product, product_supply);

                    reactorProduction.Add(new ReactorProduction() { fuelmode = product, mass = massProduced });
                }

                // Waste Heat
                supplyFNResource(total_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated

                powerPcnt = 100.0 * total_power_ratio;

                if (min_throttle > 1.05) IsEnabled = false;

                BreedTritium(total_power_received, TimeWarp.fixedDeltaTime);

                if (Planetarium.GetUniversalTime() != 0)
                    last_active_time = (float)(Planetarium.GetUniversalTime());
            }
            else if (IsEnabled && IsNuclear && MaximumPower > 0 && (Planetarium.GetUniversalTime() - last_active_time <= 3 * GameConstants.EARH_DAY_SECONDS))
            {
                double power_fraction = 0.1 * Math.Exp(-(Planetarium.GetUniversalTime() - last_active_time) / GameConstants.EARH_DAY_SECONDS / 24.0 * 9.0);
                double power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime * power_fraction, 0);
                double thermal_power_received = supplyManagedFNResourceWithMinimum(power_to_supply, 1.0, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double total_power_ratio = thermal_power_received / MaximumPower / TimeWarp.fixedDeltaTime;
                ongoing_consumption_rate = (float)total_power_ratio;
                supplyFNResource(thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                powerPcnt = 100.0 * total_power_ratio;
                decay_ongoing = true;
            }
            else
                powerPcnt = 0;

            if (!IsEnabled)
            {
                if (thermalPowerResource != null)
                {
                    thermalPowerResource.maxAmount = 0.0001;
                    thermalPowerResource.amount = 0;
                }

                if (chargedPowerResource != null)
                {
                    chargedPowerResource.maxAmount = 0.0001;
                    chargedPowerResource.amount = 0;
                }
            }

        }

        protected double GetFuelRatio(ReactorFuel reactorFuel, double fuelEfficency, double megajoules)
        {
            var fuelUseForPower = reactorFuel.GetFuelUseForPower(fuelEfficency, megajoules, fuelUsePerMJMult);

            return GetFuelAvailability(reactorFuel) / fuelUseForPower;
        }

        private void BreedTritium(double thermal_power_received, float fixedDeltaTime)
        {
            if (!breedtritium || thermal_power_received <= 0)
            {
                tritium_produced_d = 0;
                helium_produced_f = 0;
                return;
            }

            PartResourceDefinition lithium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium);
            PartResourceDefinition tritium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Tritium);
            PartResourceDefinition helium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Helium);

            // calculate current maximum litlium consumption
            var breed_rate = current_fuel_mode.NeutronsRatio * thermal_power_received / breedDivider / GameConstants.tritiumBreedRate;
            var lith_rate = breed_rate / lithium_def.density;

            // get spare room tritium
            var partsThatStoreTritium = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Tritium);
            var spareRoomTritiumAmount = partsThatStoreTritium.Sum(r => r.maxAmount - r.amount);

            // limit lithium consumption to maximum tritium storage
            var maximumTritiumProduction = lith_rate * tritium_molar_mass_ratio * lithium_def.density / tritium_def.density;
            var maximumLitiumConsumtionRatio = Math.Min(maximumTritiumProduction, spareRoomTritiumAmount) / maximumTritiumProduction;
            var lithium_request = lith_rate * maximumLitiumConsumtionRatio;

            // consume the lithium
            var lith_used = part.RequestResource(InterstellarResourcesConfiguration.Instance.Lithium, lithium_request);

            // caculate products
            var tritium_production = lith_used * tritium_molar_mass_ratio * lithium_def.density / tritium_def.density;
            var helium_production = lith_used * helium_molar_mass_ratio * lithium_def.density / helium_def.density;

            // produce tritium and helium
            tritium_produced_d = (float)(-part.RequestResource(InterstellarResourcesConfiguration.Instance.Tritium, -tritium_production) / fixedDeltaTime);
            helium_produced_f = (float)(-part.RequestResource(InterstellarResourcesConfiguration.Instance.Helium, -helium_production) / fixedDeltaTime);
        }

        public virtual float GetCoreTempAtRadiatorTemp(float rad_temp)
        {
            return CoreTemperature;
        }

        public virtual float GetThermalPowerAtTemp(float temp)
        {
            return MaximumPower;
        }

        public float GetRadius()
        {
            return radius;
        }

        public virtual bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void EnableIfPossible()
        {
            if (!IsNuclear && !IsEnabled)
                IsEnabled = true;
        }

        public bool isVolatileSource()
        {
            return false;
        }

        public void upgradePartModule()
        {
            isupgraded = true;
            fuel_modes = GetReactorFuelModes();
        }

        public override string GetInfo()
        {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
            List<ReactorFuelMode> basic_fuelmodes = fuelmodes.Select(node => new ReactorFuelMode(node)).Where(fm => (fm.SupportedReactorTypes & reactorType) == reactorType).ToList();
            List<ReactorFuelMode> advanced_fuelmodes = fuelmodes.Select(node => new ReactorFuelMode(node)).Where(fm => (fm.SupportedReactorTypes & (upgradedReactorType > 0 ? upgradedReactorType : reactorType)) == (upgradedReactorType > 0 ? upgradedReactorType : reactorType)).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("BASIC REACTOR INFO");
            sb.AppendLine(originalName);
            sb.AppendLine("Thermal Power: " + PluginHelper.getFormattedPowerString(PowerOutput));
            sb.AppendLine("Heat Exchanger Temperature: " + ReactorTemp.ToString("0") + "K");
            sb.AppendLine("Fuel Burnup: " + (fuelEfficiency * 100.0).ToString("0.00") + "%");
            sb.AppendLine("BASIC FUEL MODES");
            basic_fuelmodes.ForEach(fm =>
            {
                sb.AppendLine("---");
                sb.AppendLine(fm.ModeGUIName);
                sb.AppendLine("Power Multiplier: " + fm.NormalisedReactionRate.ToString("0.00"));
                sb.AppendLine("Charged Particle Ratio: " + fm.ChargedPowerRatio.ToString("0.00"));
                sb.AppendLine("Total Energy Density: " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                foreach (ReactorFuel fuel in fm.ReactorFuels)
                {
                    sb.AppendLine(fuel.FuelName + " " + fuel.FuelUsePerMJ * fuelUsePerMJMult * PowerOutput * fm.NormalisedReactionRate * GameConstants.EARH_DAY_SECONDS / fuelEfficiency + fuel.Unit + "/day");
                }
                sb.AppendLine("---");
            });
            if (IsUpgradeable)
            {
                sb.AppendLine("-----");
                sb.AppendLine("UPGRADED REACTOR INFO");
                sb.AppendLine(upgradedName);
                sb.AppendLine("Thermal Power: " + PluginHelper.getFormattedPowerString(upgradedPowerOutput));
                sb.AppendLine("Heat Exchanger Temperature: " + upgradedReactorTemp.ToString("0") + "K");
                sb.AppendLine("Fuel Burnup: " + (upgradedFuelEfficiency * 100.0).ToString("0.00") + "%");
                sb.AppendLine("UPGRADED FUEL MODES");
                advanced_fuelmodes.ForEach(fm =>
                {
                    sb.AppendLine("---");
                    sb.AppendLine(fm.ModeGUIName);
                    sb.AppendLine("Power Multiplier: " + fm.NormalisedReactionRate.ToString("0.00"));
                    sb.AppendLine("Charged Particle Ratio: " + fm.ChargedPowerRatio.ToString("0.00"));
                    sb.AppendLine("Total Energy Density: " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                    foreach (ReactorFuel fuel in fm.ReactorFuels)
                    {
                        sb.AppendLine(fuel.FuelName + " " + fuel.FuelUsePerMJ * fuelUsePerMJMult * upgradedPowerOutput * fm.NormalisedReactionRate * GameConstants.EARH_DAY_SECONDS / upgradedFuelEfficiency + fuel.Unit + "/day");
                    }
                    sb.AppendLine("---");
                });
            }
            return sb.ToString();
        }

        protected void DoPersistentResourceUpdate()
        {
            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;

            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels)
            {
                ConsumeReactorFuel(fuel, time_diff * ongoing_consumption_rate * fuel.FuelUsePerMJ * fuelUsePerMJMult);
            }

            if (breedtritium)
            {
                tritium_rate = MaximumPower * current_fuel_mode.NeutronsRatio / breedDivider / GameConstants.tritiumBreedRate;
                PartResourceDefinition lithium_definition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium);
                PartResourceDefinition tritium_definition = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Tritium);
                List<PartResource> lithium_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Lithium).ToList();
                List<PartResource> tritium_resources = part.GetConnectedResources(InterstellarResourcesConfiguration.Instance.Tritium).ToList();
                double lithium_current_amount = lithium_resources.Sum(rs => rs.amount);
                double tritium_missing_amount = tritium_resources.Sum(rs => rs.maxAmount - rs.amount);
                double lithium_to_take = Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, lithium_current_amount);
                double tritium_to_add = Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, tritium_missing_amount) * lithium_definition.density / tritium_definition.density; ;
                ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Lithium, Math.Min(tritium_to_add, lithium_to_take));
                ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Tritium, -Math.Min(tritium_to_add, lithium_to_take));
            }
        }

        protected bool ReactorIsOverheating()
        {
            if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && canShutdown)
            {
                deactivate_timer++;
                if (deactivate_timer > 3)
                    return true;
            }
            else
                deactivate_timer = 0;

            return false;
        }

        protected List<ReactorFuelMode> GetReactorFuelModes()
        {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
            return fuelmodes.Select(node => new ReactorFuelMode(node))
                .Where(fm =>
                    (fm.SupportedReactorTypes & ReactorType) == ReactorType
                    && PluginHelper.HasTechRequirmentOrEmpty(fm.TechRequirement)
                    && VerifyUpgradedWhenNeeded(fm.RequiresUpgrade)
                    ).ToList();
        }

        private bool VerifyUpgradedWhenNeeded(bool requiresUpgrade)
        {
            return !requiresUpgrade || isupgraded;
        }

        protected bool FuelRequiresLab(bool requiresLab)
        {
            bool isConnectedToLab = part.IsConnectedToModule("ScienceModule", 10);

            return !requiresLab || isConnectedToLab && canBeCombinedWithLab;
        }

        protected virtual void setDefaultFuelMode()
        {
            current_fuel_mode = fuel_modes.FirstOrDefault();

            if (current_fuel_mode == null)
                print("[KSP Interstellar] Warning : current_fuel_mode is null");
            else
                print("[KSP Interstellar] current_fuel_mode = " + current_fuel_mode.ModeGUIName);
        }

        protected virtual double ConsumeReactorFuel(ReactorFuel fuel, double consume_amount)
        {
            if (!fuel.ConsumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName))
                {
                    double amount = Math.Min(consume_amount / FuelEfficiency, part.Resources[fuel.FuelName].amount);
                    part.Resources[fuel.FuelName].amount -= amount;
                    return amount;
                }
                else
                    return 0;
            }
            return part.RequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
        }

        protected virtual double ProduceReactorProduct(ReactorProduct product, double produce_amount)
        {
            var effectiveAmount = produce_amount / FuelEfficiency;
            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.FuelName))
                {
                    double availableStorage = part.Resources[product.FuelName].maxAmount - part.Resources[product.FuelName].amount;
                    double possibleAmount = Math.Min(effectiveAmount, availableStorage);
                    part.Resources[product.FuelName].amount += possibleAmount;
                    return effectiveAmount * product.Density;
                }
                else
                    return 0;
            }

            part.RequestResource(product.FuelName, -effectiveAmount);
            return effectiveAmount * product.Density;
        }

        protected double GetFuelAvailability(ReactorFuel fuel)
        {
            if (fuel == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability fuel null");

            if (!fuel.ConsumeGlobal)
            {
                if (part.Resources.Contains(fuel.FuelName))
                    return part.Resources[fuel.FuelName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetConnectedResources(fuel.FuelName).Sum(rs => rs.amount);
            else
                return part.FindAmountOfAvailableFuel(fuel.FuelName, 4);
        }

        protected double GetFuelAvailability(ReactorProduct product)
        {
            if (product == null)
                UnityEngine.Debug.LogError("[KSPI] - GetFuelAvailability product null");

            if (!product.ProduceGlobal)
            {
                if (part.Resources.Contains(product.FuelName))
                    return part.Resources[product.FuelName].amount;
                else
                    return 0;
            }

            if (HighLogic.LoadedSceneIsFlight)
                return part.GetConnectedResources(product.FuelName).Sum(rs => rs.amount);
            else
                return part.FindAmountOfAvailableFuel(product.FuelName, 4);
        }

        public void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window)
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Reactor System Interface");
        }

        protected void PrintToGUILayout(string label, string value, GUIStyle style, int witdhLabel = 150, int witdhValue = 200)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, style, GUILayout.Width(witdhLabel));
            GUILayout.Label(value, GUILayout.Width(witdhValue));
            GUILayout.EndHorizontal();
        }

        protected virtual void WindowReactorSpecificOverride()  {}

        private void Window(int windowID)
        {
            try
            {
                windowPositionX = windowPosition.x;
                windowPositionY = windowPosition.y;

                bold_label = new GUIStyle(GUI.skin.label);
                bold_label.fontStyle = FontStyle.Bold;

                if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x"))
                    render_window = false;

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label(TypeName, bold_label, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                PrintToGUILayout("Reactor Embrittlement", (100 * (1 - ReactorEmbrittlemenConditionRatio)).ToString("0.000") + "%", bold_label);
                PrintToGUILayout("Radius", radius.ToString() + "m", bold_label);
                PrintToGUILayout("Core Temperature", coretempStr, bold_label);
                PrintToGUILayout("Status", statusStr, bold_label);

                PrintToGUILayout("Fuel Mode", fuelModeStr, bold_label);

                WindowReactorSpecificOverride();

                //if (ChargedPowerRatio > 0)
                PrintToGUILayout("Max Power Output", PluginHelper.getFormattedPowerString(NormalisedMaximumPower, "0.0", "0.00") + " / " + PluginHelper.getFormattedPowerString(RawPowerOutput, "0.0", "0.00"), bold_label);

                if (ChargedPowerRatio < 1.0)
                    PrintToGUILayout("Thermal Power", currentTPwr + " / " + PluginHelper.getFormattedPowerString(MaximumThermalPower) + "_th", bold_label);
                if (ChargedPowerRatio > 0)
                    PrintToGUILayout("Charged Power", currentCPwr + " / " + PluginHelper.getFormattedPowerString(MaximumChargedPower) + "_cp", bold_label);

                if (current_fuel_mode != null & current_fuel_mode.ReactorFuels != null)
                {
                    if (IsNeutronRich && breedtritium)
                        PrintToGUILayout("Tritium Breed Rate", 100 * current_fuel_mode.NeutronsRatio + "% " + (tritium_produced_d * GameConstants.EARH_DAY_SECONDS).ToString("0.000000") + " l/day ", bold_label);
                    else
                        PrintToGUILayout("Is Neutron rich", IsNeutronRich.ToString(), bold_label);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Fuel", bold_label, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    double fuel_lifetime_d = double.MaxValue;
                    foreach (var fuel in current_fuel_mode.ReactorFuels)
                    {
                        double availability = GetFuelAvailability(fuel);
                        PrintToGUILayout(fuel.FuelName, (availability * fuel.Density * 1000).ToString("0.000000") + " kg", bold_label);

                        double fuel_use = total_power_per_frame * fuel.FuelUsePerMJ * fuelUsePerMJMult / TimeWarp.fixedDeltaTime / FuelEfficiency * current_fuel_mode.NormalisedReactionRate * GameConstants.EARH_DAY_SECONDS;
                        fuel_lifetime_d = Math.Min(fuel_lifetime_d, availability / fuel_use);
                        PrintToGUILayout(fuel.FuelName, fuel_use.ToString("0.000000") + " " + fuel.Unit + "/day", bold_label);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Products", bold_label, GUILayout.Width(150));
                    GUILayout.EndHorizontal();

                    foreach (var product in current_fuel_mode.ReactorProducts)
                    {
                        double availability = GetFuelAvailability(product);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName, bold_label, GUILayout.Width(150));
                        GUILayout.Label((availability * product.Density * 1000).ToString("0.000000") + " kg", GUILayout.Width(150));
                        GUILayout.EndHorizontal();

                        double fuel_use = total_power_per_frame * product.ProductUsePerMJ * fuelUsePerMJMult / TimeWarp.fixedDeltaTime / FuelEfficiency * current_fuel_mode.NormalisedReactionRate * GameConstants.EARH_DAY_SECONDS;
                        fuel_lifetime_d = Math.Min(fuel_lifetime_d, availability / fuel_use);
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(product.FuelName, bold_label, GUILayout.Width(150));
                        GUILayout.Label(fuel_use.ToString("0.000000") + " " + product.Unit + "/day", GUILayout.Width(150));
                        GUILayout.EndHorizontal();
                    }

                    PrintToGUILayout("Current Lifetime", (double.IsNaN(fuel_lifetime_d) ? "-" : (fuel_lifetime_d).ToString("0.00")) + " days", bold_label);
                }

                if (!IsNuclear)
                {
                    GUILayout.BeginHorizontal();

                    if (IsEnabled && GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true)))
                        DeactivateReactor();
                    if (!IsEnabled && GUILayout.Button("Activate", GUILayout.ExpandWidth(true)))
                        ActivateReactor();

                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (IsEnabled)
                    {
                        GUILayout.BeginHorizontal();

                        if (GUILayout.Button("Shutdown", GUILayout.ExpandWidth(true)))
                            IsEnabled = false;

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
                GUI.DragWindow();
            }

            catch (Exception e)
            {
                Debug.LogError("ElectricRCSController Window(" + windowID + "): " + e.Message);
                throw;
            }
        }
    }
}
