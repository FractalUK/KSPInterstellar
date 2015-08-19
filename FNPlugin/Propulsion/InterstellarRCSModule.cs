extern alias ORSvKSPIE;
using ORSvKSPIE::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
    public class InterstellarRCSModule : PartModule
    {
        [KSPField(isPersistant = true)]
        public int fuel_mode;
        [KSPField(isPersistant = false)]
        public string AnimationName;
        [KSPField(isPersistant = false)]
        public int type = 16;
        [KSPField(isPersistant = false)]
        public float maxThrust = 1;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass;

        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Propellant")]
        public string propNameStr = "";
        [KSPField(isPersistant = true, guiActiveEditor = true, guiActive = true, guiName = "Thrust Limiter", guiUnits = "%"), UI_FloatRange(stepIncrement = 0.05f, maxValue = 100, minValue = 5)]
        public float thrustLimiter = 100;
        [KSPField(isPersistant = false, guiActiveEditor = true, guiActive = true, guiName = "Thrust")]
        public string thrustStr;

        private AnimationState[] rcsStates;
        private bool rcsIsOn;
        private bool rcsPartActive;
        private List<ElectricEnginePropellant> _propellants;
        private ModuleRCS attachedRCS;

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

        public override void OnStart(PartModule.StartState state)
        {
            rcsStates = SetUpAnimation(AnimationName, this.part);
            attachedRCS = this.part.FindModuleImplementing<ModuleRCS>();

            // initialize propellant
            _propellants = ElectricEnginePropellant.GetPropellantsEngineForType(type);
            SetupPropellants(true, _propellants.Count);

            base.OnStart(state);
        }

        public void Update()
        {
            propNameStr = Current_propellant != null ? Current_propellant.PropellantGUIName : "";

            attachedRCS.thrusterPower = maxThrust * (thrustLimiter / 100);

            thrustStr = attachedRCS.thrusterPower.ToString("0.000") + " / " + maxThrust.ToString("0.000") + " kN";
        }

        public override void OnUpdate()
        {
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
                    attachedRCS.SetResource(new_propellant.name);
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

		public static float RoundToMultiple(float f, float multiple)
		{
			float factor = 1/multiple;
			f *= factor;
			f = Mathf.Round(f);
			f /= factor;
			return f;
		}

		public static float SignedAngle(Vector3 fromDirection, Vector3 toDirection, Vector3 referenceRight)
		{
			float angle = Vector3.Angle(fromDirection, toDirection);
			float sign = Mathf.Sign(Vector3.Dot(toDirection, referenceRight));
			float finalAngle = sign * angle;
			return finalAngle;
		}
	}
}


/*using System;

namespace FNPlugin
{
	class ElectricalRCSSystem : FNResourceSuppliableRCSModule	{
		protected float maxThrust = 0.5f;

		[KSPField(isPersistant = false, guiActive = true, guiName = "Thrust")]
		public string thrustStr;

		public ElectricalRCSSystem () : base()	{
			maxThrust = thrusterPower;
		}


		public override void OnUpdate() {
			//base.OnFixedUpdate();
			float rcs_total_thrust = 0;
			foreach (float thrust_val in thrustForces) {
				rcs_total_thrust += thrust_val*maxThrust;
			}

			float power_required_megajoules = rcs_total_thrust*1000* 9.81f * realISP/1E6f;

			float power_received = consumeFNResource (power_required_megajoules*TimeWarp.deltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.deltaTime;
			if (power_required_megajoules > 0) {
				float power_received_pcnt = power_received / power_required_megajoules;
				thrusterPower = maxThrust * power_received_pcnt;
			} else {
				thrusterPower = 0;
			}

			thrustStr = thrusterPower.ToString ("0.000") + "kN";
		}
	}
}
 * */
