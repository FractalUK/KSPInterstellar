extern alias ORSv1_2;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_2::OpenResourceSystem;

namespace FNPlugin {
    class InterstellarReactor : FNResourceSuppliableModule, IThermalSource, FNUpgradeableModule {
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
        public float resourceRate;
        [KSPField(isPersistant = false)]
        public float upgradedResourceRate;
        [KSPField(isPersistant = false)]
        public float minimumThrottle = 0;
        [KSPField(isPersistant = false)]
        public bool canShutdown = true;
        [KSPField(isPersistant = false)]
        public float chargedParticleRatio = 0;
        [KSPField(isPersistant = false)]
        public float upgradedChargedParticleRatio;
        [KSPField(isPersistant = false)]
        public bool consumeGlobal;
        [KSPField(isPersistant = false)]
        public int reactorType;
        [KSPField(isPersistant = false)]
        public float fuelEfficiency;
        [KSPField(isPersistant = false)]
        public float upgradedFuelEfficiency;

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

        protected Rect windowPosition = new Rect(20, 20, 300, 100);
        protected int windowID = 90175467;
        protected bool render_window = false;
        protected GUIStyle bold_label;

        public double FuelEfficiency { get { return isupgraded ? upgradedFuelEfficiency > 0 ? upgradedFuelEfficiency : fuelEfficiency : fuelEfficiency; } }

        public int ReactorType { get { return isupgraded ? reactorType : reactorType; } }

        public virtual string TypeName { get { return isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName; } }

        public double ChargedParticleRatio { get { return isupgraded ? upgradedChargedParticleRatio > 0 ? upgradedChargedParticleRatio : chargedParticleRatio : chargedParticleRatio; } }

        [KSPEvent(guiActive = true, guiName = "Activate Reactor", active = false)]
        public void ActivateReactor() {
            if (getIsNuclear()) { return; }
            IsEnabled = true;
        }

        [KSPEvent(guiActive = true, guiName = "Deactivate Reactor", active = true)]
        public void DeactivateReactor() {
            if (getIsNuclear()) { return; }
            IsEnabled = false;
        }

        [KSPEvent(guiActive = true, guiName = "Enable Tritium Breeding", active = false)]
        public void BreedTritium() {
            if (!isNeutronRich()) { return; }
            breedtritium = true;
        }

        [KSPEvent(guiActive = true, guiName = "Disable Tritium Breeding", active = true)]
        public void StopBreedTritium() {
            if (!isNeutronRich()) { return; }
            breedtritium = false;
        }

        [KSPEvent(guiActive = true, guiName = "Retrofit", active = true)]
        public void RetrofitReactor() {
            if (ResearchAndDevelopment.Instance == null) { return; }
            if (isupgraded || ResearchAndDevelopment.Instance.Science < upgradeCost) { return; }
            upgradePartModule();
            ResearchAndDevelopment.Instance.Science = ResearchAndDevelopment.Instance.Science - upgradeCost;
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Control Window", active = true)]
        public void ToggleWindow() {
            render_window = !render_window;
        }

        [KSPAction("Activate Reactor")]
        public void ActivateReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            ActivateReactor();
        }

        [KSPAction("Deactivate Reactor")]
        public void DeactivateReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            DeactivateReactor();
        }

        [KSPAction("Toggle Reactor")]
        public void ToggleReactorAction(KSPActionParam param) {
            if (getIsNuclear()) { return; }
            IsEnabled = !IsEnabled;
        }

        public override void OnStart(PartModule.StartState state) {
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_THERMALPOWER, FNResourceManager.FNRESOURCE_WASTEHEAT, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES };
            this.resources_to_supply = resources_to_supply;
            print("[KSP Interstellar] Configuring Reactor Fuel Modes");
            fuel_modes = getReactorFuelModes();
            current_fuel_mode = fuel_modes.FirstOrDefault();
            print("[KSP Interstellar] Configuration Complete");
            System.Random rnd = new System.Random();
            windowID = rnd.Next(int.MaxValue);
            base.OnStart(state);

            if (state == StartState.Editor) {
                if (hasTechsRequiredToUpgrade()) {
                    isupgraded = true;
                    upgradePartModule();
                }
                return;
            }

            if (hasTechsRequiredToUpgrade()) hasrequiredupgrade = true;

            if (!reactorInit && startDisabled) {
                last_active_time = (float)(Planetarium.GetUniversalTime() - 4.0 * 86400.0);
                IsEnabled = false;
                startDisabled = false;
                reactorInit = true;
            } else if (!reactorInit) {
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

        public override void OnUpdate() {
            //Update Events
            Events["ActivateReactor"].active = !IsEnabled && !getIsNuclear();
            Events["DeactivateReactor"].active = IsEnabled && !getIsNuclear();
            Events["BreedTritium"].active = !breedtritium && isNeutronRich() && IsEnabled;
            Events["StopBreedTritium"].active = breedtritium && isNeutronRich() && IsEnabled;
            Events["RetrofitReactor"].active = ResearchAndDevelopment.Instance != null ? !isupgraded && ResearchAndDevelopment.Instance.Science >= upgradeCost && hasrequiredupgrade : false;
            //Update Fields
            Fields["currentTPwr"].guiActive = IsEnabled && (ongoing_thermal_power_f > 0);
            Fields["currentCPwr"].guiActive = IsEnabled && (ongoing_charged_power_f > 0);
            //
            reactorTypeStr = isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName;
            coretempStr = getCoreTemp().ToString("0") + " K";
            if (update_count - last_draw_update > 10) {
                if (IsEnabled) {
                    if (ongoing_thermal_power_f > 0) currentTPwr = PluginHelper.getFormattedPowerString(ongoing_thermal_power_f) + "_th";
                    if (ongoing_charged_power_f > 0) currentCPwr = PluginHelper.getFormattedPowerString(ongoing_charged_power_f) + "_cp";
                    statusStr = "Active (" + powerPcnt.ToString("0.00") + "%)";
                } else {
                    if (powerPcnt > 0) statusStr = "Decay Heating (" + powerPcnt.ToString("0.00") + "%)";
                    else statusStr = "Offline";
                }

                last_draw_update = update_count;
            }
            if (!vessel.isActiveVessel || part == null) RenderingManager.RemoveFromPostDrawQueue(0, OnGUI);
            update_count++;
        }

        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            if (IsEnabled && getThermalPower() > 0) {
                if (reactorIsOverheating()) {
                    if(FlightGlobals.ActiveVessel == vessel) ScreenMessages.PostScreenMessage("Warning Dangerous Overheating Detected: Emergency reactor shutdown occuring NOW!", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    IsEnabled = false;
                    return;
                }
                // Max Power
                double max_power_to_supply = Math.Max(getThermalPower() * TimeWarp.fixedDeltaTime, 0);
                double fuel_ratio = Math.Min(current_fuel_mode.ReactorFuels.Min(fuel => fuel.GetAvailabilityToReactor(this, consumeGlobal) / fuel.GetFuelUseForPower(this,max_power_to_supply)), 1.0);
                double min_throttle = fuel_ratio > 0 ? minimumThrottle / fuel_ratio : 1;
                max_power_to_supply = max_power_to_supply * fuel_ratio;
                // Charged Power
                double charged_particles_to_supply = max_power_to_supply * chargedParticleRatio;
                double charged_power_received = supplyManagedFNResourceWithMinimum(charged_particles_to_supply, minimumThrottle, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES);
                double charged_power_ratio = chargedParticleRatio > 0 ? charged_power_received / getThermalPower() / chargedParticleRatio / TimeWarp.fixedDeltaTime : 0;
                ongoing_charged_power_f = (float)(charged_power_received / TimeWarp.fixedDeltaTime);
                // Thermal Power
                double thermal_power_to_supply = max_power_to_supply * (1.0 - chargedParticleRatio);
                double thermal_power_received = supplyManagedFNResourceWithMinimum(thermal_power_to_supply, min_throttle, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double thermal_power_ratio = (1 - chargedParticleRatio) > 0 ? thermal_power_received / getThermalPower() / (1 - chargedParticleRatio) / TimeWarp.fixedDeltaTime : 0;
                ongoing_thermal_power_f = (float) (thermal_power_received / TimeWarp.fixedDeltaTime);
                // Total
                double total_power_received = thermal_power_received + charged_power_received;
                total_power_per_frame = total_power_received;
                double total_power_ratio = total_power_received / getThermalPower() / TimeWarp.fixedDeltaTime;
                current_fuel_mode.ReactorFuels.ForEach(fuel => fuel.ConsumeReactorResource(this,consumeGlobal,total_power_received*fuel.FuelUsePerMJ)); // consume fuel
                ongoing_total_power_f = ongoing_charged_power_f + ongoing_thermal_power_f;
                // Waste Heat
                supplyFNResource(total_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                //
                powerPcnt = 100.0 * total_power_ratio;

                if (min_throttle > 1.05) IsEnabled = false;

                if (breedtritium) {
                    double tritium_rate = thermal_power_received / TimeWarp.fixedDeltaTime / 1000.0f / GameConstants.tritiumBreedRate;
                    double lith_rate = tritium_rate * TimeWarp.fixedDeltaTime;
                    double lith_used = ORSHelper.fixedRequestResource(part, "Lithium", lith_rate);
                    tritium_produced_f = (float)(-ORSHelper.fixedRequestResource(part, "Tritium", -lith_used) / TimeWarp.fixedDeltaTime);
                    if (tritium_produced_f <= 0) breedtritium = false;
                }

            } else if (getThermalPower() > 0 && Planetarium.GetUniversalTime() - last_active_time <= 3 * 86400 && getIsNuclear()) {
                double daughter_half_life = 86400.0 / 24.0 * 9.0;
                double time_t = Planetarium.GetUniversalTime() - last_active_time;
                double power_fraction = 0.1 * Math.Exp(-time_t / daughter_half_life);
                double power_to_supply = Math.Max(getThermalPower() * TimeWarp.fixedDeltaTime * power_fraction, 0);
                double thermal_power_received = supplyManagedFNResourceWithMinimum(power_to_supply, 1.0, FNResourceManager.FNRESOURCE_THERMALPOWER);
                double total_power_ratio = thermal_power_received / getThermalPower() / TimeWarp.fixedDeltaTime;
                supplyFNResource(thermal_power_received, FNResourceManager.FNRESOURCE_WASTEHEAT); // generate heat that must be dissipated
                powerPcnt = 100.0 * total_power_ratio;
            } else {
                powerPcnt = 0;
            }
        }

        public virtual float getCoreTemp() {
            return isupgraded ? upgradedReactorTemp != 0 ? upgradedReactorTemp : ReactorTemp : ReactorTemp;
        }

        public virtual float getCoreTempAtRadiatorTemp(float rad_temp) {
            return isupgraded ? upgradedReactorTemp != 0 ? upgradedReactorTemp : ReactorTemp : ReactorTemp;
        }

        public virtual float getThermalPowerAtTemp(float temp) {
            return isupgraded ? upgradedPowerOutput != 0 ? upgradedPowerOutput : PowerOutput : PowerOutput;
        }

        public virtual float getThermalPower() {
            return isupgraded ? upgradedPowerOutput != 0 ? upgradedPowerOutput : PowerOutput : PowerOutput;
        }

        public virtual float getChargedPower() {
            return isupgraded ? upgradedPowerOutput != 0 ? upgradedPowerOutput * chargedParticleRatio : PowerOutput * chargedParticleRatio : PowerOutput * chargedParticleRatio;
        }

        public virtual bool getIsNuclear() {
            return false;
        }

        public virtual bool isNeutronRich() {
            return false;
        }

        public virtual float getMinimumThermalPower() {
            return 0;
        }

        public float getRadius() {
            return radius;
        }

        public bool isActive() {
            return IsEnabled;
        }

        public virtual bool shouldScaleDownJetISP() {
            return false;
        }

        public void enableIfPossible() {
            if (!getIsNuclear() && !IsEnabled) IsEnabled = true;
        }

        public bool isVolatileSource() {
            return false;
        }

        public bool hasTechsRequiredToUpgrade() {
            return PluginHelper.upgradeAvailable(upgradeTechReq);
        }

        public void upgradePartModule() {
            fuel_modes = getReactorFuelModes();
        }

        protected void doPersistentResourceUpdate() {
            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;
            current_fuel_mode.ReactorFuels.ForEach(fuel => fuel.ConsumeReactorResource(this, consumeGlobal, time_diff * ongoing_consumption_rate * fuel.FuelUsePerMJ));
            if (breedtritium) {
                tritium_rate = getThermalPower() / 1000.0 / GameConstants.tritiumBreedRate;
                List<PartResource> lithium_resources = part.GetConnectedResources("Lithium").ToList();
                List<PartResource> tritium_resources = part.GetConnectedResources("LqdTritium").ToList();
                double lithium_current_amount = lithium_resources.Sum(rs => rs.amount);
                double tritium_missing_amount = tritium_resources.Sum(rs => rs.maxAmount - rs.amount);
                double lithium_to_take = Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, lithium_current_amount);
                double tritium_to_add = Math.Min(tritium_rate * time_diff * ongoing_consumption_rate, tritium_missing_amount);
                ORSHelper.fixedRequestResource(part, "Lithium", Math.Min(tritium_to_add, lithium_to_take));
                ORSHelper.fixedRequestResource(part, "LqdTritium", -Math.Min(tritium_to_add, lithium_to_take));
            }
        }

        protected bool reactorIsOverheating() {
            if (getResourceBarRatio(FNResourceManager.FNRESOURCE_WASTEHEAT) >= 0.95 && canShutdown) {
                deactivate_timer++;
                if (deactivate_timer > 3) {
                    return true;
                }
            } else {
                deactivate_timer = 0;
            }
            return false;
        }

        protected List<ReactorFuelMode> getReactorFuelModes() {
            ConfigNode[] fuelmodes = GameDatabase.Instance.GetConfigNodes("REACTOR_FUEL_MODE");
            return fuelmodes.Select(node => new ReactorFuelMode(node)).Where(fm => (fm.SupportedReactorTypes & ReactorType) == ReactorType).ToList();
        }

        private void OnGUI() {
            if (this.vessel == FlightGlobals.ActiveVessel && render_window) {
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Reactor System Interface");
            }
        }

        private void Window(int windowID) {
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
            GUILayout.Label("Current Thermal Power", bold_label, GUILayout.Width(150));
            GUILayout.Label(currentTPwr,  GUILayout.Width(150));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Thermal Power", bold_label, GUILayout.Width(150));
            GUILayout.Label(PluginHelper.getFormattedPowerString(getThermalPower()*(1.0 - ChargedParticleRatio)) + "_th", GUILayout.Width(150));
            GUILayout.EndHorizontal();
            if (chargedParticleRatio > 0) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Charged Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(currentTPwr, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Max Charged Power", bold_label, GUILayout.Width(150));
                GUILayout.Label(PluginHelper.getFormattedPowerString(getChargedPower()) + "_cp", GUILayout.Width(150));
                GUILayout.EndHorizontal();
            }
            if (current_fuel_mode != null & current_fuel_mode.ReactorFuels != null) {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Fuel", bold_label, GUILayout.Width(150));
                GUILayout.Label("Usage", GUILayout.Width(150));
                GUILayout.EndHorizontal();
                current_fuel_mode.ReactorFuels.ForEach(fuel => {
                    double fuel_use = total_power_per_frame * fuel.FuelUsePerMJ/TimeWarp.fixedDeltaTime/FuelEfficiency;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(fuel.FuelName, bold_label, GUILayout.Width(150));
                    GUILayout.Label(fuel_use.ToString("0.00000") + " " + fuel.Unit + "/s", GUILayout.Width(150));
                    GUILayout.EndHorizontal();
                });
            }
            if (!getIsNuclear()) {
                GUILayout.BeginHorizontal();
                if (IsEnabled) if (GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true))) DeactivateReactor();
                if (!IsEnabled) if (GUILayout.Button("Activate", GUILayout.ExpandWidth(true))) ActivateReactor();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
            
        }


    }
}
