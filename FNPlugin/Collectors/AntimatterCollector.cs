extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    public class AntimatterCollector : PartModule    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;
        [KSPField(isPersistant = false, guiActive = true, guiName = "Rate")]
        public string collectionRate;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        
        private bool init = false;
        protected int drawCount = 0;
        protected double collection_rate_d = 0;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;
            if (last_active_time != 0 && vessel.orbit.eccentricity < 1) {
                double lat = vessel.mainBody.GetLatitude(vessel.transform.position);
                double vessel_avg_alt = (vessel.orbit.ApR + vessel.orbit.PeR) / 2.0f;
                double vessel_inclination = vessel.orbit.inclination;
                double flux = 0.5 * (vessel.mainBody.GetBeltAntiparticles(vessel_avg_alt, vessel_inclination) + vessel.mainBody.GetBeltAntiparticles(vessel_avg_alt, 0.0));
                double antimatter_to_add = time_diff*flux;
                part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Antimatter, -antimatter_to_add);
            }
        }

        public override void OnUpdate() {
            double lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            double flux = vessel.mainBody.GetBeltAntiparticles(vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
            collectionRate = collection_rate_d.ToString("0.00") + " mg/day";
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                drawCount++;
                double lat = vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
                double flux = vessel.mainBody.GetBeltAntiparticles(vessel.altitude, lat);
                part.ImprovedRequestResource(InterstellarResourcesConfiguration.Instance.Antimatter, -flux * TimeWarp.fixedDeltaTime);
                last_active_time = (float)Planetarium.GetUniversalTime();
                collection_rate_d = flux * GameConstants.EARH_DAY_SECONDS;
            }
        }
    }
}
