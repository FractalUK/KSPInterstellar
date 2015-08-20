extern alias ORSvKSPIE;
using ORSvKSPIE::OpenResourceSystem;

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
        public int fuel_mode;
        [KSPField(isPersistant = false)]
        public float efficency = 0.8f;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 0;
        [KSPField(isPersistant = false)]
        public float maxIsp = 2000;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Max Thrust", guiUnits = " kN")]
        public float baseThrust = 0;

        //Gui
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Efficency")]
        public string efficencyStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant")]
        public string propNameStr = "";
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Maximum Isp")]
        public float maxPropellantIsp;
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Thrust Limiter", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5)]
        public float thrustLimiter = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Max Thrust")]
        public string thrustStr;
        [KSPField(isPersistant = false, guiActiveEditor = false, guiActive = true, guiName = "Current Thrust", guiUnits = " kN")]
        public float currentThrust;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass;

        //Config settings settings
        protected double g0 = PluginHelper.GravityConstant;

        // GUI
        [KSPField(isPersistant = false, guiActive = true, guiName = "Power")]
        public string electricalPowerConsumptionStr = "";
        [KSPField(isPersistant = false, guiActive = true, guiName = "Heat Production")]
        public string heatProductionStr = "";

        // internal
        private float power_ratio = 1;
        private float power_requested_f = 0;
        private float power_recieved_f = 1;
        private float heat_production_f = 0;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;
        private float efficencyModifier;
        private float currentMaxThrust;
        private float oldThrustLimiter;
        private int insufficientPowerTimout = 2;
        private int sufficientPowerTimout = 2;

        public float ElectricPowerModifier { get { return insufficientPowerTimout > 0 ? 1 : 0.0001f; } }

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
            try
            {
                Current_propellant = fuel_mode < _propellants.Count ? _propellants[fuel_mode] : _propellants.FirstOrDefault();
                if ((Current_propellant.SupportedEngines & type) != type)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }
                Propellant new_propellant = Current_propellant.Propellant;
                if (PartResourceLibrary.Instance.GetDefinition(new_propellant.name) != null)
                {
                    var moduleConfig = new ConfigNode("MODULE");
                    moduleConfig.AddValue("name", "ModuleRCSFX");
                    moduleConfig.AddValue("thrusterPower", (ElectricPowerModifier * (thrustLimiter / 100) * Math.Sqrt(Current_propellant.ThrustMultiplier) * baseThrust / Current_propellant.IspMultiplier).ToString("0.0"));
                    moduleConfig.AddValue("resourceName", new_propellant.name);
                    moduleConfig.AddValue("resourceFlowMode", "STAGE_PRIORITY_FLOW");

                    var atmosphereCurve = new ConfigNode("atmosphereCurve");
                    atmosphereCurve.AddValue("key", "0 " + (maxIsp * Current_propellant.IspMultiplier).ToString("0.000"));
                    atmosphereCurve.AddValue("key", "1 " + (maxIsp * Current_propellant.IspMultiplier * 0.5).ToString("0.000"));
                    atmosphereCurve.AddValue("key", "4 " + (maxIsp * Current_propellant.IspMultiplier * 0.00001).ToString("0.000"));
                    moduleConfig.AddNode(atmosphereCurve);

                    attachedRCS.Load(moduleConfig);
                    
                }
                else if (maxSwitching > 0)
                {
                    SwitchPropellant(moveNext, --maxSwitching);
                    return;
                }

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
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[KSPI] - InterstellarRCSModule SetupPropellants " + e.Message);
            }
        }

        public override void OnStart(PartModule.StartState state) 
        {
            if (baseThrust == 0 && maxThrust > 0)
                baseThrust = maxThrust;

            attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();
            String[] resources_to_supply = { FNResourceManager.FNRESOURCE_WASTEHEAT };

            this.resources_to_supply = resources_to_supply;

            // initialize propellant
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            SetupPropellants(true, _propellants.Count);
            efficencyModifier = (float)g0 * 0.5f / 1000.0f / efficency;
            efficencyStr = (efficency * 100).ToString() + "%";
            oldThrustLimiter = thrustLimiter;

            base.OnStart(state);
         }

        public void Update()
        {
            if (Current_propellant == null) return;

            //if (power_ratio > 0.5f )
            //{
            //    sufficientPowerTimout--;
            //    insufficientPowerTimout = 2;
            //}
            //else
            //{
            //    insufficientPowerTimout--;
            //    sufficientPowerTimout = 2;
            //}

            if (oldThrustLimiter != thrustLimiter || insufficientPowerTimout == 0 || sufficientPowerTimout == 0)
            {
                SetupPropellants(true, 0);
                oldThrustLimiter = thrustLimiter;
            }

            propNameStr = Current_propellant.PropellantGUIName;

            maxPropellantIsp = maxIsp * (float)Current_propellant.IspMultiplier;

            currentMaxThrust = baseThrust / (float)Current_propellant.IspMultiplier * (float)Math.Sqrt(Current_propellant.ThrustMultiplier);

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
        }

        public void FixedUpdate()
        {
            currentThrust = 0;

            if (attachedRCS == null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            currentThrust = attachedRCS.thrustForces.Sum(frc => frc);
            float curve_eval_point = (float)Math.Min(FlightGlobals.getStaticPressure(vessel.transform.position) / 100, 1.0);
            float currentIsp = attachedRCS.atmosphereCurve.Evaluate(curve_eval_point);

            power_requested_f = currentThrust * currentIsp * efficencyModifier / (float)Math.Sqrt(Current_propellant.ThrustMultiplier);
            power_recieved_f = consumeFNResource(power_requested_f * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;
            float heat_to_produce = power_recieved_f * (1 - efficency);
            heat_production_f = supplyFNResource(heat_to_produce * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT) / TimeWarp.fixedDeltaTime;
            power_ratio = power_requested_f > 0 ? (float)Math.Min(power_recieved_f / power_requested_f, 1.0) : 1;



            //attachedRCS.thrusterPower = Mathf.Max(currentMaxThrust * power_ratio * (thrustLimiter / 100), 0.0001f);
            //float thrust_ratio = Mathf.Min(Mathf.Min((float)power_ratio, (float)(total_thrust / maxThrust)), 1.0f)*0.125f;
        }

        public override string getResourceManagerDisplayName() 
        {
            return "Electrical Reaction Control System";
        }
    }
}
