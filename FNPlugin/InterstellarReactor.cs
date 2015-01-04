using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin {
    class InterstellarReactor : FNResourceSuppliableModule, IThermalSource, IUpgradeableModule {
        public enum ReactorTypes {
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

        // Persistent False
        [KSPField(isPersistant = false)]
        public float ReactorTemp;
        [KSPField(isPersistant = false)]
        public float PowerOutput;
        [KSPField(isPersistant = false)]
        public float upgradedReactorTemp;
        [KSPField(isPersistant = false)]
        public float upgradedPowerOutput;
        [KSPField(isPersistant = false)]
        public string animName;
        [KSPField(isPersistant = false)]
        public string upgradedName;
        [KSPField(isPersistant = false)]
        public string originalName;
        [KSPField(isPersistant = false)]
        public float upgradeCost;
        [KSPField(isPersistant = false)]
        public float radius;
        [KSPField(isPersistant = false)]
        public string upgradeTechReq = null;
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

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Type")]
        public string reactorTypeStr;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Core Temp")]
        public string coretempStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string statusStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Thermal Power")]
        public string currentTPwr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Charged Power")]
        public string currentCPwr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Fuel")]
        public string fuelModeStr = "";

        protected bool hasrequiredupgrade = false;
        protected double tritium_rate;
        protected int deactivate_timer = 0;
        protected List<ReactorFuelMode> fuel_modes;
        protected ReactorFuelMode current_fuel_mode;
        protected double powerPcnt;
        protected float tritium_produced_f;
        protected long update_count;
        protected long last_draw_update;
        protected float ongoing_thermal_power_f;
        protected float ongoing_charged_power_f;
        protected float ongoing_total_power_f;
        protected double total_power_per_frame;
        protected bool decay_ongoing = false;

        protected Rect windowPosition = new Rect(20, 20, 300, 100);
        protected int windowID = 90175467;
        protected bool render_window = false;
        protected GUIStyle bold_label;

        public bool IsSelfContained { get { return containsPowerGenerator; } }

        public String UpgradeTechnology { get { return upgradeTechReq; } }

        public double FuelEfficiency { get { return isupgraded ? upgradedFuelEfficiency > 0 ? upgradedFuelEfficiency : fuelEfficiency : fuelEfficiency; } }

        public int ReactorType { get { return isupgraded ? upgradedReactorType > 0 ? upgradedReactorType : reactorType : reactorType; } }

        public virtual string TypeName { get { return isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName; } }

        public float ChargedParticleRatio { get { return current_fuel_mode != null ? (float) current_fuel_mode.ChargedPowerRatio : 0f; } }

        public virtual float CoreTemperature { get {return isupgraded ? upgradedReactorTemp != 0 ? upgradedReactorTemp : ReactorTemp : ReactorTemp; } }

        public virtual float MaximumThermalPower 
        { 
            get 
            {
                float thermal_fuel_factor = current_fuel_mode == null ? 1.0f : (float)current_fuel_mode.NormalisedReactionRate;
                return isupgraded ? upgradedPowerOutput != 0 ? upgradedPowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor : PowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor : PowerOutput * (1.0f - ChargedParticleRatio) * thermal_fuel_factor; 
            } 
        }

        public virtual float MinimumPower { get { return 0; } }

        public virtual float MaximumChargedPower
        {
            get
            {
                float charged_fuel_factor = current_fuel_mode == null ? 1.0f : (float)current_fuel_mode.NormalisedReactionRate;
                return isupgraded ? upgradedPowerOutput != 0 ? charged_fuel_factor * upgradedPowerOutput * ChargedParticleRatio : charged_fuel_factor * PowerOutput * ChargedParticleRatio :  charged_fuel_factor * PowerOutput * ChargedParticleRatio;
            }
        }

        public virtual bool IsNuclear { get { return false; } }

        public virtual bool IsActive { get { return IsEnabled; } }

        public virtual bool IsVolatileSource { get { return false; } }

        public virtual bool IsNeutronRich { get { return false; } }

        public bool IsUpgradeable { get {return upgradedName != ""; } }

        public virtual float MaximumPower { get { return MaximumThermalPower + MaximumChargedPower; } }

        [KSPEvent(guiActive = true, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor() 
        {
            if (IsNuclear) { return; }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor()
        {
            if (IsNuclear) { return; }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Enable Tritium Breeding", active = false)]
        public void BreedTritium() 
        {
            if (!IsNeutronRich) { return; }
            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Tritium Breeding", active = true)]
        public void StopBreedTritium() 
        {
            if (!IsNeutronRich) { return; }
            breedtritium = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor() 
        {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            upgradePartModule();
            ResearchAndDevelopment.Instance.AddScience(-upgradeCost, TransactionReasons.RnDPartPurchase);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Control Window", active = true)]
        public void ToggleWindow() 
        {
            render_window = !render_window;
        }

        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param) 
        {
            if (IsNuclear) { return; }
            ActivateReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param)
        {
            if (IsNuclear) { return; }
            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param)
        {
            if (IsNuclear) { return; }
            IsEnabled = !IsEnabled;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_THERMALPOWER, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;
            print("[KSP Interstellar] Configuring Reactor Fuel Modes");
            fuel_modes = getReactorFuelModes();
            setDefaultFuelMode();
            print("[KSP Interstellar] Configuration Complete");
            System.Random rnd = new System.Random();
            windowID = rnd.Next(int.MaxValue);
            base.OnStart(state);

            if (state == StartState.Editor) 
            {
                if (this.HasTechsRequiredToUpgrade()) 
                {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            if (this.HasTechsRequiredToUpgrade()) hasrequiredupgrade = true;

            if (!reactorInit && startDisabled) 
            {
                last_active_time = (float)(Planetarium.GetUniversalTime() - 4.0 * 86400.0);
                IsEnabled = false;
                startDisabled = false;
                reactorInit = true;
            } else if (!reactorInit) 
            {
                IsEnabled = true;
                reactorInit = true;
            }
            print("[KSP Interstellar] Reactor Persistent Resource Update");
            if (IsEnabled && last_active_time > 0) {
                doPersistentResourceUpdate();
            }

            this.part.force_activate();
            RenderingManager.AddToPostDrawQueue(0, OnGUI);

            print("[KSP Interstellar] Configuring Reactor");
        }

        public override void OnUpdate() 
        {
            //Update Events
            Events["ActivateReactor"].active = !IsEnabled && !IsNuclear;
            Events["DeactivateReactor"].active = IsEnabled && !IsNuclear;
            Events["BreedTritium"].active = !breedtritium && IsNeutronRich && IsEnabled;
            Events["StopBreedTritium"].active = breedtritium && IsNeutronRich && IsEnabled;
            Events["RetrofitReactor"].active = ResearchAndDevelopment.Instance != null ? !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade : false;
            //Update Fields
            Fields["currentTPwr"].guiActive = IsEnabled && (ongoing_thermal_power_f > 0);
            Fields["currentCPwr"].guiActive = IsEnabled && (ongoing_charged_power_f > 0);
            fuelModeStr = current_fuel_mode != null ? current_fuel_mode.ModeGUIName : "";
            //
            reactorTypeStr = isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName;
            coretempStr = CoreTemperature.ToString("0") + " K";
            if (update_count - last_draw_update > 10) 
            {
                if (IsEnabled) 
                {
                    if (current_fuel_mode != null && !current_fuel_mode.ReactorFuels.Any(fuel => getFuelAvailability(fuel) <= 0))
                    {
                        if (ongoing_thermal_power_f > 0) currentTPwr = PluginHelper.getFormattedPowerString(ongoing_thermal_power_f) + "_th";
                        if (ongoing_charged_power_f > 0) currentCPwr = PluginHelper.getFormattedPowerString(ongoing_charged_power_f) + "_cp";
                        statusStr = "Active (" + powerPcnt.ToString("0.00") + "%)";
                    } else if (current_fuel_mode != null)
                    {
                        statusStr = current_fuel_mode.ReactorFuels.FirstOrDefault(fuel => getFuelAvailability(fuel) <= 0).FuelName + " Deprived";
                    }
                } else {
                    if (powerPcnt > 0) statusStr = "Decay Heating (" + powerPcnt.ToString("0.00") + "%)";
                    else statusStr = "Offline";
                }

                last_draw_update = update_count;
            }
            if (!vessel.isActiveVessel || part == null) RenderingManager.RemoveFromPostDrawQueue(0, OnGUI);
            update_count++;
        }

        public override void OnFixedUpdate() 
        {
            decay_ongoing = false;
            base.OnFixedUpdate();
            if (IsEnabled && MaximumPower > 0) {
                if (reactorIsOverheating()) {
                    if(FlightGlobals.ActiveVessel == vessel) ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency reactor shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    IsEnabled = false;
                    return;
                }

                // Max Power
                double max_power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime, 0);
                double fuel_ratio = Math.Min(current_fuel_mode.ReactorFuels.Min(fuel => getFuelAvailability(fuel) / fuel.GetFuelUseForPower(FuelEfficiency,max_power_to_supply)), 1.0);
                double min_throttle = fuel_ratio > 0 ? minimumThrottle / fuel_ratio : 1;
                max_power_to_supply = max_power_to_supply * fuel_ratio;
                // Charged Power
                double max_charged_to_supply = Math.Max(MaximumChargedPower * TimeWarp.fixedDeltaTime, 0) * fuel_ratio;
                double charged_particles_to_supply = max_charged_to_supply;
                double charged_power_received = supplyManagedFNResourceWithMinimum(charged_particles_to_supply, minimumThrottle, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                double charged_power_ratio = ChargedParticleRatio > 0 ? charged_power_received / max_charged_to_supply : 0;
                ongoing_charged_power_f = (float)(charged_power_received / TimeWarp.fixedDeltaTime);
                // Thermal Power
                double max_thermal_to_supply = Math.Max(MaximumThermalPower * TimeWarp.fixedDeltaTime, 0) * fuel_ratio;
                double thermal_power_to_supply = max_thermal_to_supply;
                double thermal_power_received = supplyManagedFNResourceWithMinimum(thermal_power_to_supply, min_throttle, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double thermal_power_ratio = (1 - ChargedParticleRatio) > 0 ? thermal_power_received / max_thermal_to_supply : 0;
                ongoing_thermal_power_f = (float) (thermal_power_received / TimeWarp.fixedDeltaTime);
                // Total
                double total_power_received = thermal_power_received + charged_power_received;
                total_power_per_frame = total_power_received;
                double total_power_ratio = total_power_received / MaximumPower / TimeWarp.fixedDeltaTime;
                ongoing_consumption_rate = (float)total_power_ratio;

                foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels) consumeReactorFuel(fuel, total_power_received * fuel.FuelUsePerMJ); // consume fuel
                 
                ongoing_total_power_f = ongoing_charged_power_f + ongoing_thermal_power_f;
                // Waste Heat
                supplyFNResource(total_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                //
                powerPcnt = 100.0 * total_power_ratio;

                if (min_throttle > 1.05) IsEnabled = false;
                if (breedtritium) 
                {
                    PartResourceDefinition lithium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Lithium);
                    PartResourceDefinition tritium_def = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Tritium);
                    double breed_rate = thermal_power_received / TimeWarp.fixedDeltaTime / 100000.0 / GameConstants.tritiumBreedRate;
                    double lith_rate = breed_rate * TimeWarp.fixedDeltaTime / lithium_def.density;
                    double lith_used = ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Lithium, lith_rate);
                    double lt_density_ratio = lithium_def.density / tritium_def.density;
                    tritium_produced_f = (float)(-ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Tritium, -lith_used*3.0/7.0*lt_density_ratio) / TimeWarp.fixedDeltaTime);
                    if (tritium_produced_f <= 0) breedtritium = false;
                }

                if(Planetarium.GetUniversalTime() != 0)
                {
                    last_active_time = (float)(Planetarium.GetUniversalTime());
                }

            } else if (MaximumPower > 0 && Planetarium.GetUniversalTime() - last_active_time <= 3 * 86400 && IsNuclear)
            {
                double daughter_half_life = 86400.0 / 24.0 * 9.0;
                double time_t = Planetarium.GetUniversalTime() - last_active_time;
                double power_fraction = 0.1 * Math.Exp(-time_t / daughter_half_life);
                double power_to_supply = Math.Max(MaximumPower * TimeWarp.fixedDeltaTime * power_fraction, 0);
                double thermal_power_received = supplyManagedFNResourceWithMinimum(power_to_supply, 1.0, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double total_power_ratio = thermal_power_received / MaximumPower / TimeWarp.fixedDeltaTime;
                ongoing_consumption_rate = (float)total_power_ratio;
                supplyFNResource(thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                powerPcnt = 100.0 * total_power_ratio;
                decay_ongoing = true;
            } else {
                powerPcnt = 0;
            }
        }

        public virtual float GetCoreTempAtRadiatorTemp(float rad_temp)
        {
            return CoreTemperature;
        }

        public virtual float GetThermalPowerAtTemp(float temp) 
        {
            return MaximumPower;
        }
                

        public float getRadius()
        {
            return radius;
        }

        public virtual bool shouldScaleDownJetISP()
        {
            return false;
        }

        public void enableIfPossible()
        {
            if (!IsNuclear && !IsEnabled) IsEnabled = true;
        }

        public bool isVolatileSource() 
        {
            return false;
        }

        public void upgradePartModule() 
        {
            fuel_modes = getReactorFuelModes();
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
            basic_fuelmodes.ForEach(fm => {
                sb.AppendLine("---");
                sb.AppendLine(fm.ModeGUIName);
                sb.AppendLine("Power Multiplier: " + fm.NormalisedReactionRate.ToString("0.00"));
                sb.AppendLine("Charged Particle Ratio: " + fm.ChargedPowerRatio.ToString("0.00"));
                sb.AppendLine("Total Energy Density: " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                foreach (ReactorFuel fuel in fm.ReactorFuels)
                {
                    sb.AppendLine(fuel.FuelName + " " + fuel.FuelUsePerMJ * PowerOutput * fm.NormalisedReactionRate * 86400.0 / fuelEfficiency + fuel.Unit + "/day");
                }
                sb.AppendLine("---");
            });
            if (IsUpgradeable) {
                sb.AppendLine("-----");
                sb.AppendLine("UPGRADED REACTOR INFO");
                sb.AppendLine(upgradedName);
                sb.AppendLine("Thermal Power: " + PluginHelper.getFormattedPowerString(upgradedPowerOutput));
                sb.AppendLine("Heat Exchanger Temperature: " + upgradedReactorTemp.ToString("0") + "K");
                sb.AppendLine("Fuel Burnup: " + (upgradedFuelEfficiency * 100.0).ToString("0.00") + "%");
                sb.AppendLine("UPGRADED FUEL MODES");
                advanced_fuelmodes.ForEach(fm => {
                    sb.AppendLine("---");
                    sb.AppendLine(fm.ModeGUIName);
                    sb.AppendLine("Power Multiplier: " + fm.NormalisedReactionRate.ToString("0.00"));
                    sb.AppendLine("Charged Particle Ratio: " + fm.ChargedPowerRatio.ToString("0.00"));
                    sb.AppendLine("Total Energy Density: " + fm.ReactorFuels.Sum(fuel => fuel.EnergyDensity).ToString("0.00") + " MJ/kg");
                    foreach (ReactorFuel fuel in fm.ReactorFuels) {
                        sb.AppendLine(fuel.FuelName + " " + fuel.FuelUsePerMJ * upgradedPowerOutput * fm.NormalisedReactionRate * 86400.0/upgradedFuelEfficiency + fuel.Unit + "/day");
                    }
                    sb.AppendLine("---");
                });
            }
            return sb.ToString();
        }

        protected void doPersistentResourceUpdate() 
        {
            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;
            foreach (ReactorFuel fuel in current_fuel_mode.ReactorFuels) consumeReactorFuel(fuel, time_diff * ongoing_consumption_rate * fuel.FuelUsePerMJ); // consume fuel
            if (breedtritium) 
            {
                tritium_rate = MaximumPower / 1000.0 / GameConstants.tritiumBreedRate;
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

        protected bool reactorIsOverheating() 
        {
            if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && canShutdown) 
            {
                deactivate_timer++;
                if (deactivate_timer > 3) return true;
            } else 
            {
                deactivate_timer = 0;
            }
            return false;
        }

        protected List<ReactorFuelMode> getReactorFuelModes()
        {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
            return fuelmodes.Select(node => new ReactorFuelMode(node)).Where(fm => (fm.SupportedReactorTypes & ReactorType) == ReactorType).ToList();
        }

        protected virtual void setDefaultFuelMode()
        {
            current_fuel_mode = fuel_modes.FirstOrDefault();
        }

        protected virtual double consumeReactorFuel(ReactorFuel fuel, double consume_amount)
        {
            if (!consumeGlobal) 
            {
                if (part.Resources.Contains(fuel.FuelName)) 
                {
                    double amount = Math.Min(consume_amount, part.Resources[fuel.FuelName].amount / FuelEfficiency);
                    part.Resources[fuel.FuelName].amount -= amount;
                    return amount;
                } else return 0;
            } 
            return part.ImprovedRequestResource(fuel.FuelName, consume_amount / FuelEfficiency);
        }

        protected double getFuelAvailability(ReactorFuel fuel) 
        {
            if (!consumeGlobal) 
            {
                if (part.Resources.Contains(fuel.FuelName)) return part.Resources[fuel.FuelName].amount;
                else return 0;
            } 
            return part.GetConnectedResources(fuel.FuelName).Sum(rs => rs.amount);
        }

        private void OnGUI() 
        {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window) 
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Reactor System Interface");
        }

        private void Window(int windowID) 
        {
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x")) {
                render_window = false;
            }
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(TypeName, bold_label, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (ChargedParticleRatio < 1.0)
            {
                GUILayout.Label("Current Thermal Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(currentTPwr, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Thermal Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(PluginHelper.getFormattedPowerString(MaximumThermalPower) + "_th", GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }
            if (ChargedParticleRatio > 0) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Charged Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(currentCPwr, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Charged Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(PluginHelper.getFormattedPowerString(MaximumChargedPower) + "_cp", GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }
            if (current_fuel_mode != null & current_fuel_mode.ReactorFuels != null) {
                if (!current_fuel_mode.Aneutronic && breedtritium)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tritium Breed Rate", bold_label, GUILayout.Width(150));
                    GUILayout.Label((tritium_produced_f*86400.0).ToString("0.000") + " l/day" , GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel", bold_label, GUILayout.Width(150));
                GUILayout.Label("Usage", bold_label, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                double fuel_lifetime_d = double.MaxValue;
                foreach(ReactorFuel fuel in current_fuel_mode.ReactorFuels) 
                {
                    double availability = getFuelAvailability(fuel);
                    double fuel_use = total_power_per_frame * fuel.FuelUsePerMJ/TimeWarp.fixedDeltaTime/FuelEfficiency*current_fuel_mode.NormalisedReactionRate*86400;
                    fuel_lifetime_d = Math.Min(fuel_lifetime_d, availability / fuel_use);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(fuel.FuelName, bold_label, GUILayout.Width(150));
                    GUILayout.Label(fuel_use.ToString("0.0000") + " " + fuel.Unit + "/day", GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Lifetime");
                GUILayout.Label( (double.IsNaN(fuel_lifetime_d) ? "-" : (fuel_lifetime_d).ToString("0.00")) + " days", GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }
            if (!IsNuclear)
            {
                GUILayout.BeginHorizontal();
                if (IsEnabled) if (GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true))) DeactivateReactor();
                if (!IsEnabled) if (GUILayout.Button("Activate", GUILayout.ExpandWidth(true))) ActivateReactor();
                GUILayout.EndHorizontal();
            } else
            {
                if (IsEnabled)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Shutdown", GUILayout.ExpandWidth(true))) IsEnabled = false;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
            
        }


    }
}
