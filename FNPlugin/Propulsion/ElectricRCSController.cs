using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    class ElectricRCSController : FNResourceSuppliableModule 
    {
        [KSPField(isPersistant = true)]
        bool isInitialised = false;
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = true)]
        public string fuel_mode_name;
        [KSPField(isPersistant = false)]
        public string AnimationName = "";
        [KSPField(isPersistant = false)]
        public float efficency = 0.8f;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;
        [KSPField(isPersistant = false)]
        public float maxIsp = 2000;
        [KSPField(isPersistant = false)]
        public float minIsp = 250;
        [KSPField(isPersistant = false)]
        public string displayName = "";

        [KSPField(isPersistant = false)]
        public bool showConsumption = true;

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Full Thrust"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool fullThrustEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "FT Threshold", guiUnits = "%"), UI_FloatRange(stepIncrement = 1f, maxValue = 100, minValue = 0)]
        public float fullThrustMinLimiter;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Use Throttle"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool useThrotleEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Use Lever"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool useLeverEnabled;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = false, guiName = "Precision"), UI_FloatRange(stepIncrement = 1f, maxValue = 100, minValue = 5)]
        public float precisionFactorLimiter;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool powerEnabled = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant Name")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Propellant Maximum Isp")]
        public float maxPropellantIsp;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Propellant Thrust Multiplier")]
        public float currentThrustMultiplier = 1;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Thrust / ISP Mult")]
        public string thrustIspMultiplier = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Thrust Limiter"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5)]
        public float thrustLimiter = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Base Thrust", guiUnits = " kN")]
        public float baseThrust = 0;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Max Thrust")]
        public string thrustStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Forces")]
        public string thrustForcesStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Current Total Thrust", guiUnits = " kN")]
        public float currentThrust;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;

        //Config settings settings
        protected double g0 = PluginHelper.GravityConstant;

        // GUI
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = false, guiName = "Is Powered")]
        public bool hasSufficientPower = true;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Consumption")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Efficency")]
        public string efficencyStr = "";

        // internal
        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;

        private float power_ratio = 1;
        private float power_requested_f = 0;
        private float power_requested_raw = 0;
        private float power_recieved_f = 1;
        private float power_recieved_raw = 0;
        private float power_remainer_raw;

        private float heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;
        private FNModuleRCSFX attachedModuleRCSFX;
        private float efficencyModifier;
        private float currentMaxThrust;
        private float oldThrustLimiter;
        private bool oldPowerEnabled;
        private int insufficientPowerTimout = 2;
        private bool delayedVerificationPropellant; 

        public ElectricEnginePropellant Current_propellant { get; set; }

        [KSPAction("Next Propellant")]
        public void ToggleNextPropellantAction(KSPActionParam param)
        {
            ToggleNextPropellantEvent();
        }

        [KSPAction("Previous Propellant")]
        public void TogglePreviousPropellantAction(KSPActionParam param)
        {
            TogglePreviousPropellantEvent();
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Next Propellant", active = true)]
        public void ToggleNextPropellantEvent()
        {
            SwitchToNextPropellant(_propellants.Count);
        }

        [KSPEvent(guiActiveEditor = true, guiActive = true, guiName = "Previous Propellant", active = true)]
        public void TogglePreviousPropellantEvent()
        {
            SwitchToPreviousPropellant(_propellants.Count);
        }

        protected void SwitchPropellant(bool next, int maxSwitching)
        {
            if (next)
                SwitchToNextPropellant(maxSwitching);
            else
                SwitchToPreviousPropellant(maxSwitching);
        }

        protected void SwitchToNextPropellant(int maxSwitching)
        {
            fuel_mode++;
            if (fuel_mode >= _propellants.Count)
                fuel_mode = 0;

            SetupPropellants(true, maxSwitching);
        }

        protected void SwitchToPreviousPropellant(int maxSwitching)
        {
            fuel_mode--;
            if (fuel_mode < 0)
                fuel_mode = _propellants.Count - 1;

            SetupPropellants(false, maxSwitching);
        }

        private void SetupPropellants(bool moveNext = true, int maxSwitching = 0)
        {
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
            fuel_mode_name = Current_propellant.PropellantName;

            if ((Current_propellant.SupportedEngines & type) != type)
            {
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }

            Propellant new_propellant = Current_propellant.Propellant;
            if (HighLogic.LoadedSceneIsFlight)
            {
                // you can have any fuel you want in the editor but not in flight
                List<PartResource> totalpartresources = part.GetConnectedResources(new_propellant.name).ToList();

                if (!totalpartresources.Any() && maxSwitching > 0)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }
            }

            if (PartResourceLibrary.Instance.GetDefinition(new_propellant.name) != null)
            {
                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

                var moduleConfig = new ConfigNode("MODULE");
                moduleConfig.AddValue("name", "FNModuleRCSFX");
                moduleConfig.AddValue("thrusterPower", ((thrustLimiter / 100) * currentThrustMultiplier * baseThrust / Current_propellant.IspMultiplier).ToString("0.000"));
                moduleConfig.AddValue("resourceName", new_propellant.name);
                moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

                maxPropellantIsp = (hasSufficientPower ? maxIsp : minIsp) * Current_propellant.IspMultiplier * currentThrustMultiplier;

                var atmosphereCurve = new ConfigNode("atmosphereCurve");
                atmosphereCurve.AddValue("key", "0 " + (maxPropellantIsp).ToString("0.000"));
                atmosphereCurve.AddValue("key", "1 " + (maxPropellantIsp * 0.5).ToString("0.000"));
                atmosphereCurve.AddValue("key", "4 " + (maxPropellantIsp * 0.00001).ToString("0.000"));
                moduleConfig.AddNode(atmosphereCurve);

                attachedRCS.Load(moduleConfig);
            }
            else if (maxSwitching > 0)
            {
                Debug.Log("ElectricRCSController SetupPropellants switching mode because no definition found for " + new_propellant.name);
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
        }

        [KSPAction("Toggle Yaw")]
        public void ToggleYawAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableYaw = !attachedModuleRCSFX.enableYaw;
        }

        [KSPAction("Toggle Pitch")]
        public void TogglePitchAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enablePitch = !attachedModuleRCSFX.enablePitch;
        }

        [KSPAction("Toggle Roll")]
        public void ToggleRollAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableRoll = !attachedModuleRCSFX.enableRoll;
        }

        [KSPAction("Toggle Enable X")]
        public void ToggleEnableXAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableX = !attachedModuleRCSFX.enableX;
        }

        [KSPAction("Toggle Enable Y")]
        public void ToggleEnableYAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableY = !attachedModuleRCSFX.enableY;
        }

        [KSPAction("Toggle Enable Z")]
        public void ToggleEnableZAction(KSPActionParam param)
        {
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.enableZ = !attachedModuleRCSFX.enableZ;
        }

        [KSPAction("Toggle Full Thrust")]
        public void ToggleFullThrustAction(KSPActionParam param)
        {
            fullThrustEnabled = !fullThrustEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.fullThrust = fullThrustEnabled;
        }

        [KSPAction("Toggle Use Throtle")]
        public void ToggleUseThrotleEnabledAction(KSPActionParam param)
        {
            useThrotleEnabled = !useThrotleEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.useThrottle = useThrotleEnabled;
        }

        [KSPAction("Toggle Use Lever")]
        public void ToggleUseLeverAction(KSPActionParam param)
        {
            useLeverEnabled = !useLeverEnabled;
            if (attachedModuleRCSFX != null)
                attachedModuleRCSFX.useLever = useLeverEnabled;
        }

        [KSPAction("Toggle Power")]
        public void TogglePowerAction(KSPActionParam param)
        {
            powerEnabled = !powerEnabled;

            power_recieved_f = powerEnabled ? consumeFNResource(0.1, FNResourceManager.FNRESOURCE_MEGAJOULES) : 0;
            hasSufficientPower = power_recieved_f > 0.01;
            SetupPropellants();
            currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
        }

        public override void OnStart(PartModule.StartState state) 
        {
            try
            {
                attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();
                attachedModuleRCSFX = attachedRCS as FNModuleRCSFX;

                if (!isInitialised)
                {
                    if (attachedModuleRCSFX != null)
                    {
                        useLeverEnabled = attachedModuleRCSFX.useLever;
                        precisionFactorLimiter = attachedModuleRCSFX.precisionFactor * 100;
                        fullThrustMinLimiter = attachedModuleRCSFX.fullThrustMin * 100;
                        fullThrustEnabled = attachedModuleRCSFX.fullThrust;
                        useThrotleEnabled = attachedModuleRCSFX.useThrottle;
                    }
                }

                if (attachedModuleRCSFX != null)
                {
                    attachedModuleRCSFX.Fields["RCS"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableYaw"].guiActive = true;
                    attachedModuleRCSFX.Fields["enablePitch"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableRoll"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableX"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableY"].guiActive = true;
                    attachedModuleRCSFX.Fields["enableZ"].guiActive = true;
                    attachedModuleRCSFX.fullThrust = fullThrustEnabled;
                    attachedModuleRCSFX.fullThrustMin = fullThrustMinLimiter / 100;
                    attachedModuleRCSFX.useLever = useLeverEnabled;
                    attachedModuleRCSFX.precisionFactor = precisionFactorLimiter / 100;
                }

                // old legacy stuff
                if (baseThrust == 0 && maxThrust > 0)
                    baseThrust = maxThrust;

                if (partMass == 0)
                    partMass = part.mass;

                if (String.IsNullOrEmpty(displayName))
                    displayName = part.partInfo.title;

                String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
                this.resources_to_supply = resources_to_supply;

                oldThrustLimiter = thrustLimiter;
                oldPowerEnabled = powerEnabled;
                efficencyModifier = (float)g0 * 0.5f / 1000.0f / efficency;
                efficencyStr = (efficency * 100).ToString() + "%";

                if (!String.IsNullOrEmpty(AnimationName))
                    rcsStates = SetUpAnimation(AnimationName, this.part);

                // initialize propellant
                _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);

                delayedVerificationPropellant = true;
                // find correct fuel mode index
                if (!String.IsNullOrEmpty(fuel_mode_name))
                {
                    Debug.Log("ElectricRCSController OnStart loaded fuelmode " + fuel_mode_name);
                    Current_propellant = _propellants.FirstOrDefault(p => p.PropellantName == fuel_mode_name);
                }
                if (Current_propellant != null && _propellants.Contains(Current_propellant))
                {
                    fuel_mode = _propellants.IndexOf(Current_propellant);
                    Debug.Log("ElectricRCSController OnStart index of fuelmode " + Current_propellant.PropellantGUIName + " = " + fuel_mode);
                }

                base.OnStart(state);

                Fields["electricalPowerConsumptionStr"].guiActive = showConsumption;
            }
            catch (Exception e)
            {
                Debug.LogError("ElectricRCSController OnStart Error: " + e.Message);
                throw;
            }
         }

        public void Update()
        {
            if (Current_propellant == null) return;

            if (oldThrustLimiter != thrustLimiter)
            {
                SetupPropellants(true, 0);
                oldThrustLimiter = thrustLimiter;
            }

            if (oldPowerEnabled != powerEnabled)
            {
                hasSufficientPower = powerEnabled;
                SetupPropellants(true, 0);
                oldPowerEnabled = powerEnabled;
            }

            if (attachedModuleRCSFX != null)
            {
                attachedModuleRCSFX.fullThrustMin = fullThrustMinLimiter;
                attachedModuleRCSFX.fullThrust = fullThrustEnabled;
                attachedModuleRCSFX.useThrottle = useThrotleEnabled;
            }

            propNameStr = Current_propellant.PropellantGUIName;

            currentMaxThrust = baseThrust / (float)Current_propellant.IspMultiplier * currentThrustMultiplier;

            thrustStr = attachedRCS.thrusterPower.ToString("0.000") + " / " + currentMaxThrust.ToString("0.000") + " kN";

            thrustIspMultiplier = maxPropellantIsp + " / " + currentThrustMultiplier;
        }

        public override void OnUpdate() 
        {
            if (delayedVerificationPropellant)
            {
                // test is we got any megajoules
                power_recieved_f = consumeFNResource(0.1, FNResourceManager.FNRESOURCE_MEGAJOULES);
                hasSufficientPower = power_recieved_f > 0.01;

                delayedVerificationPropellant = false;
                SetupPropellants(true, _propellants.Count);
                currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;
            }

            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                electricalPowerConsumptionStr = power_recieved_f.ToString("0.00") + " MW / " + power_requested_f.ToString("0.00") + " MW";
            } 
            else 
                Fields["electricalPowerConsumptionStr"].guiActive = false;

            if (rcsStates == null) return;

            rcsIsOn = this.vessel.ActionGroups.groups[3];
            foreach (ModuleRCS rcs in part.FindModulesImplementing<ModuleRCS>())
            {
                rcsPartActive = rcs.isEnabled;
            }

            foreach (AnimationState anim in rcsStates)
            {
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime < 1) { anim.speed = 1; }
                if (attachedRCS.rcsEnabled && rcsIsOn && rcsPartActive && anim.normalizedTime >= 1)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 1;
                }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime > 0) { anim.speed = -1; }
                if ((!attachedRCS.rcsEnabled || !rcsIsOn || !rcsPartActive) && anim.normalizedTime <= 0)
                {
                    anim.speed = 0;
                    anim.normalizedTime = 0;
                }
            }
        }

        

        public void FixedUpdate()
        {
            currentThrust = 0;

            if (attachedRCS == null) return;

            thrustForcesStr = String.Empty;

            if (attachedModuleRCSFX != null)
                currentThrust = attachedModuleRCSFX.curThrust;
            else
                currentThrust = attachedRCS.thrustForces.Sum(frc => frc);

            foreach (var force in attachedRCS.thrustForces)
            {
                thrustForcesStr += force.ToString("0.00") + "kN ";
            }

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            if (powerEnabled)
            {
                float curve_eval_point = (float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position) / 100, 1.0);
                power_requested_f = currentThrust * maxIsp * efficencyModifier / currentThrustMultiplier;
                power_requested_raw = power_requested_f * TimeWarp.fixedDeltaTime;
                power_recieved_raw = consumeFNResource(power_requested_raw, FNResourceManager.FNRESOURCE_MEGAJOULES) + power_remainer_raw;
                power_remainer_raw = 0;
                power_recieved_f = power_recieved_raw / TimeWarp.fixedDeltaTime;

                float heat_to_produce = power_recieved_f * (1 - efficency);
                heat_production_f = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                power_ratio = power_requested_f > 0 ? (float)Math.Min(power_recieved_f / power_requested_f, 1.0) : 1;
            }
            else
            {
                power_recieved_raw = 0;
                power_ratio = 0;
                insufficientPowerTimout = 0;
            }

            if (hasSufficientPower && power_ratio < 0.9 && power_recieved_f < 0.01 )
            {
                if (insufficientPowerTimout < 1)
                {
                    hasSufficientPower = false;
                    SetupPropellants();
                }
                else
                    insufficientPowerTimout--;
            }
            else if (!hasSufficientPower && power_ratio > 0.9 && power_recieved_f > 0.01)
            {
                insufficientPowerTimout = 2;
                hasSufficientPower = true;
                SetupPropellants();
            }

            // process remainder
            if (hasSufficientPower)
                power_recieved_raw -= power_requested_raw;

            power_remainer_raw += power_recieved_raw;
            power_recieved_raw = 0;
        }

        public static AnimationState[] SetUpAnimation(string animationName, Part part)  //Thanks Majiir!
        {
            var states = new List<AnimationState>();
            foreach (var animation in part.FindModelAnimators(animationName))
            {
                var animationState = animation[animationName];
                animationState.speed = 0;
                animationState.enabled = true;
                animationState.wrapMode = WrapMode.ClampForever;
                animation.Blend(animationName);
                states.Add(animationState);
            }
            return states.ToArray();
        }

        public override string getResourceManagerDisplayName() 
        {
            return displayName;
        }
    }
}
