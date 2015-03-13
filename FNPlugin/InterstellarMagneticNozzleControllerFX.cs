extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin
{
	class InterstellarMagneticNozzleControllerFX : FNResourceSuppliableModule
    {
		//Persistent False
		[KSPField(isPersistant = false)]
		public float radius;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplier = 1.0f;
        [KSPField(isPersistant = false)]
        public float powerThrustMultiplier = 1.0f;

        // Visible Non Persistant
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Maximum Power", guiUnits= " MW")]
        private float _max_power;
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Recieved Power", guiUnits = " MW")]
        private float _recievedChargedPower;

		//External
		public bool static_updating = true;
		public bool static_updating2 = true;

		//Internal
		protected ModuleEnginesFX _attached_engine;
		protected IChargedParticleSource _attached_reactor;
        protected int _attached_reactor_distance;

        protected float NozzlePowerThrustMultiplier
        {
            get { return powerTrustMultiplier * powerThrustMultiplier; }
        }

		public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

			_attached_engine = this.part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";
            else
                UnityEngine.Debug.Log("[KSPI] - InterstellarMagneticNozzleControllerFX.OnStart no ModuleEnginesFX found for MagneticNozzle!");

            //List<IChargedParticleSource> source_list = part.attachNodes.Where(atn => atn.attachedPart != null).SelectMany(atn => atn.attachedPart.FindModulesImplementing<IChargedParticleSource>()).ToList();
            //_attached_reactor = source_list.FirstOrDefault();
            _attached_reactor = BreathFirstSearchForChargedParticleSource(10, 1);

            if (_attached_reactor == null)
                UnityEngine.Debug.Log("[KSPI] - InterstellarMagneticNozzleControllerFX.OnStart no IChargedParticleSource found for MagneticNozzle!");
		}

        private IChargedParticleSource BreathFirstSearchForChargedParticleSource(int stackdepth, int parentdepth)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(part, currentDepth, parentdepth);

                if (particleSource != null)
                {
                    _attached_reactor_distance = currentDepth;
                    return particleSource;
                }
            }
            return null;
        }

        private IChargedParticleSource FindChargedParticleSource(Part currentpart, int stackdepth, int parentdepth)
        {
            if (stackdepth == 0)
                return currentpart.FindModulesImplementing<IChargedParticleSource>().FirstOrDefault();

            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null))
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(attachNodes.attachedPart, (stackdepth - 1), parentdepth);

                if (particleSource != null)
                {
                    return particleSource;
                }
            }

            if (parentdepth > 0)
            {
                IChargedParticleSource particleSource = FindChargedParticleSource(currentpart.parent, (stackdepth - 1), (parentdepth - 1));

                if (particleSource != null)
                    return particleSource;
            }

            return null;
        }

		public override void OnUpdate() {

		}


                
		public void FixedUpdate() 
        {
            if (HighLogic.LoadedSceneIsFlight && _attached_engine != null && _attached_reactor != null && _attached_engine.isOperational)
            {
                double exchanger_thrust_divisor = radius > _attached_reactor.getRadius()
                    ? _attached_reactor.getRadius() * _attached_reactor.getRadius() / radius / radius
                    : radius * radius / _attached_reactor.getRadius() / _attached_reactor.getRadius();

                _max_power = _attached_reactor.MaximumChargedPower * (float)exchanger_thrust_divisor;

                if (_attached_reactor is InterstellarFusionReactor)
                    _max_power *= 0.9f;

                double currentMeVPerChargedProduct = _attached_reactor.CurrentMeVPerChargedProduct;
                double joules_per_amu = currentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / GameConstants.dilution_factor;
                double current_isp = Math.Sqrt(joules_per_amu * 2.0 / GameConstants.ATOMIC_MASS_UNIT) / PluginHelper.GravityConstant;
                FloatCurve new_isp = new FloatCurve();
                new_isp.Add(0, (float)current_isp, 0, 0);
                _attached_engine.atmosphereCurve = new_isp;



                double charged_power_received = consumeFNResource(_max_power * TimeWarp.fixedDeltaTime * _attached_engine.currentThrottle * exchanger_thrust_divisor, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
                consumeFNResource(charged_power_received * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);
                _recievedChargedPower = consumeFNResource(charged_power_received * TimeWarp.fixedDeltaTime * 0.01 * Math.Max(_attached_reactor_distance, 1), FNResourceManager.FNRESOURCE_MEGAJOULES) / TimeWarp.fixedDeltaTime;

                double megajoules_ratio = _recievedChargedPower / charged_power_received / 0.01;
                megajoules_ratio = (double.IsNaN(megajoules_ratio) || double.IsInfinity(megajoules_ratio)) ? 0 : megajoules_ratio;

                double atmo_thrust_factor = Math.Min(1.0,Math.Max(1.0 - Math.Pow(vessel.atmDensity,0.2),0));
                


                double engineMaxThrust = 0.000000001;
                if (_max_power > 0)
                {
                    float power_ratio = (float)(charged_power_received / _max_power);
                    double powerTrustModifier = GameConstants.BaseTrustPowerMultiplier * NozzlePowerThrustMultiplier;
                    engineMaxThrust = Math.Max(powerTrustModifier * charged_power_received * megajoules_ratio * atmo_thrust_factor / current_isp / PluginHelper.GravityConstant / _attached_engine.currentThrottle, 0.000000001);
                }

                if (!double.IsInfinity(engineMaxThrust) && !double.IsNaN(engineMaxThrust))
                    _attached_engine.maxThrust = (float)engineMaxThrust;
                else
                    _attached_engine.maxThrust = 0.000000001f;

            } 
            else if (_attached_engine != null)
            {
                _attached_engine.maxThrust = 0.000000001f;
                _recievedChargedPower = 0;
            }
		}

		public override string GetInfo() 
        {
			return "";
		}

        public override string getResourceManagerDisplayName()
        {
            return "Magnetic Nozzle";
        }

	}
}