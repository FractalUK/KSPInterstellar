using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    class InterstellarRCSModule : FNResourceSuppliableModule 
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = false)]
        public string AnimationName = "";
        [KSPField(isPersistant = false)]
        public float efficency = 0.8f;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;
        [KSPField(isPersistant = false)]
        public float maxIsp = 544;
        [KSPField(isPersistant = false)]
        public float minIsp = 272;
        [KSPField(isPersistant = false)]
        string displayName = "";

        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Power"), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool powerEnabled = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Max Thrust", guiUnits = " kN")]
        public float baseThrust = 0;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Is Powered")]
        public bool hasSufficientPower = true;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Efficency")]
        public string efficencyStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant Name")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant Maximum Isp")]
        public float maxPropellantIsp;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant Thrust Multiplier")]
        public float currentThrustMultiplier;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Thrust Limiter", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5)]
        public float thrustLimiter = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Max Thrust")]
        public string thrustStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Current Thrust", guiUnits = " kN")]
        public float currentThrust;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass = 0;

        //Config settings settings
        protected double g0 = PluginHelper.GravityConstant;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";

        // internal
        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;

        private float power_ratio = 1;
        private float power_requested_f = 0;
        private float power_recieved_f = 1;
        private float heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;
        private float efficencyModifier;
        private float currentMaxThrust;
        private float oldThrustLimiter;
        private bool oldPowerEnabled;
        private int insufficientPowerTimout = 2;

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

        private void SetupPropellants(bool moveNext, int maxSwitching)
        {
            Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
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
                moduleConfig.AddValue("name", "ModuleRCSFX");
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
                SwitchPropellant(moveNext, --maxSwitching);
                return;
            }
        }

        public override void OnStart(PartModule.StartState state) 
        {
            // old legacy stuff
            if (baseThrust == 0 && maxThrust > 0)
                baseThrust = maxThrust;

            if (partMass == 0)
                partMass = part.mass;

            if (String.IsNullOrEmpty(displayName))
                displayName = part.partInfo.title;

            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };
            this.resources_to_supply = resources_to_supply;

            attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();
            oldThrustLimiter = thrustLimiter;
            oldPowerEnabled = powerEnabled;
            efficencyModifier = (float)g0 * 0.5f / 1000.0f / efficency;
            efficencyStr = (efficency * 100).ToString() + "%";

            if (!String.IsNullOrEmpty(AnimationName))
                rcsStates = SetUpAnimation(AnimationName, this.part);

            // initialize propellant
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            SetupPropellants(true, _propellants.Count);
            currentThrustMultiplier = hasSufficientPower ? Current_propellant.ThrustMultiplier : Current_propellant.ThrustMultiplierCold;

            base.OnStart(state);
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

            propNameStr = Current_propellant.PropellantGUIName;

            currentMaxThrust = baseThrust / (float)Current_propellant.IspMultiplier * currentThrustMultiplier;

            thrustStr = attachedRCS.thrusterPower.ToString("0.000") + " / " + currentMaxThrust.ToString("0.000") + " kN";
        }

        public override void OnUpdate() 
        {
            if (attachedRCS != null && vessel.ActionGroups[KSPActionGroup.RCS]) 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = true;
                Fields["heatProductionStr"].guiActive = true;
                electricalPowerConsumptionStr = power_recieved_f.ToString("0.00") + " MW / " + power_requested_f.ToString("0.00") + " MW";
                heatProductionStr = heat_production_f.ToString("0.00") + " MW";
            } 
            else 
            {
                Fields["electricalPowerConsumptionStr"].guiActive = false;
                Fields["heatProductionStr"].guiActive = false;
            }

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

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            currentThrust = attachedRCS.thrustForces.Sum(frc => frc);

            if (powerEnabled)
            {
                float curve_eval_point = (float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position) / 100, 1.0);
                float currentIsp = attachedRCS.atmosphereCurve.Evaluate(curve_eval_point);

                power_requested_f = currentThrust * currentIsp * efficencyModifier / currentThrustMultiplier;
                power_recieved_f = consumeFNResource(power_requested_f * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
                float heat_to_produce = power_recieved_f * (1 - efficency);
                heat_production_f = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
                power_ratio = power_requested_f > 0 ? (float)Math.Min(power_recieved_f / power_requested_f, 1.0) : 1;
            }
            else
            {
                power_ratio = 0;
                insufficientPowerTimout = 0;
            }

            if (hasSufficientPower && power_ratio < 0.9 && power_recieved_f < 0.01 )
            {
                if (insufficientPowerTimout < 1)
                {
                    hasSufficientPower = false;
                    SetupPropellants(true, 0);
                }
                else
                    insufficientPowerTimout--;
            }
            else if (!hasSufficientPower && power_ratio > 0.9 && power_recieved_f > 0.01)
            {
                insufficientPowerTimout = 2;
                hasSufficientPower = true;
                SetupPropellants(true, 0);
            }
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
