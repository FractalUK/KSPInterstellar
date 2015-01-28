extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin{
	class InterstellarMagneticNozzleControllerFX : FNResourceSuppliableModule
    {
		// Persistent True

		//Persistent False
		[KSPField(isPersistant = false)]
		public float radius;
        [KSPField(isPersistant = false)]
        public float powerTrustMultiplier = 1.0f;

        //Config settings
        protected double g0 = PluginHelper.GravityConstant;

		//External
		public bool static_updating = true;
		public bool static_updating2 = true;

		//Internal
		protected ModuleEnginesFX _attached_engine;
		protected IChargedParticleSource _attached_reactor;

		public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

			_attached_engine = this.part.Modules["ModuleEnginesFX"] as ModuleEnginesFX;

            if (_attached_engine != null)
                _attached_engine.Fields["finalThrust"].guiFormat = "F5";

            List<IChargedParticleSource> source_list = part.attachNodes.Where(atn => atn.attachedPart != null).SelectMany(atn => atn.attachedPart.FindModulesImplementing<IChargedParticleSource>()).ToList();
            _attached_reactor = source_list.FirstOrDefault();
		}

		public override void OnUpdate() {

		}
                
		public void FixedUpdate() {
            if (HighLogic.LoadedSceneIsFlight && _attached_engine != null && _attached_reactor != null && _attached_engine.isOperational)
            {
                double max_power = _attached_reactor.MaximumChargedPower;
                if (_attached_reactor is InterstellarFusionReactor) max_power *= 0.9;
                double dilution_factor = 15000.0;
                double joules_per_amu = _attached_reactor.CurrentMeVPerChargedProduct * 1e6 * GameConstants.ELECTRON_CHARGE / dilution_factor;
                double isp = Math.Sqrt(joules_per_amu * 2.0 / GameConstants.ATOMIC_MASS_UNIT) / g0;
                FloatCurve new_isp = new FloatCurve();
                new_isp.Add(0, (float)isp, 0, 0);
                _attached_engine.atmosphereCurve = new_isp;

                double charged_power_received = consumeFNResource(max_power * TimeWarp.fixedDeltaTime * _attached_engine.currentThrottle, FNResourceManager.FNRESOURCE_CHARGED_PARTICLES) / TimeWarp.fixedDeltaTime;
                consumeFNResource(charged_power_received * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_WASTEHEAT);

                double megajoules_received = consumeFNResource(charged_power_received * TimeWarp.fixedDeltaTime * 0.01, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.fixedDeltaTime;
                double megajoules_ratio = megajoules_received / charged_power_received / 0.01;
                megajoules_ratio = (double.IsNaN(megajoules_ratio) || double.IsInfinity(megajoules_ratio)) ? 0 : megajoules_ratio;

                double atmo_thrust_factor = Math.Min(1.0,Math.Max(1.0 - Math.Pow(vessel.atmDensity,0.2),0));

                double exchanger_thrust_divisor = 1;
                if (radius > _attached_reactor.getRadius())
                {
                    exchanger_thrust_divisor = _attached_reactor.getRadius() * _attached_reactor.getRadius() / radius / radius;
                } else
                {
                    exchanger_thrust_divisor = radius * radius / _attached_reactor.getRadius() / _attached_reactor.getRadius();
                }

                double engineMaxThrust = 0.000000001;
                float power_ratio;
                if (max_power > 0)
                {
                    power_ratio = (float)(charged_power_received / max_power);
                    double powerTrustModifier = GameConstants.BaseTrustPowerMultiplier * powerTrustMultiplier; 
                    engineMaxThrust = Math.Max(powerTrustModifier * charged_power_received * megajoules_ratio * atmo_thrust_factor * exchanger_thrust_divisor / isp / g0 / _attached_engine.currentThrottle, 0.000000001);
                }

                if (!double.IsInfinity(engineMaxThrust) && !double.IsNaN(engineMaxThrust))
                {
                    _attached_engine.maxThrust = (float)engineMaxThrust;
                } else
                {
                    _attached_engine.maxThrust = 0.000000001f;
                }
            } else if (_attached_engine != null)
            {
                _attached_engine.maxThrust = 0.000000001f;
            }
		}

		public override string GetInfo() {
			return "";
		}

        public override string getResourceManagerDisplayName()
        {
            return "Magnetic Nozzle";
        }

	}
}