extern alias ORSv1_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_1::OpenResourceSystem;

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
            this.part.force_activate();

            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;
            if (last_active_time != 0 && vessel.orbit.eccentricity < 1) {
                double lat = vessel.mainBody.GetLatitude(vessel.transform.position);
                double vessel_avg_alt = (vessel.orbit.ApR + vessel.orbit.PeR) / 2.0f;
                double vessel_inclination = vessel.orbit.inclination;
				float flux = (VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel_avg_alt, (float)vessel_inclination) + VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel_avg_alt, 0.0f))/2.0f;
                //vessel.orbit.
                double antimatter_to_add = time_diff*flux;
                //part.RequestResource("Antimatter", -antimatter_to_add);
                ORSHelper.fixedRequestResource(part, "Antimatter", -antimatter_to_add);
            }
        }

        public override void OnUpdate() {
            float lat = (float)vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            float flux = VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
            collectionRate = collection_rate_d.ToString("0.00") + " mg/day";
        }

        public override void OnFixedUpdate() {
            drawCount++;
            float lat = (float) vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            float flux = VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude,lat);
            //part.RequestResource("Antimatter", -flux * TimeWarp.fixedDeltaTime);
            ORSHelper.fixedRequestResource(part, "Antimatter", -flux * TimeWarp.fixedDeltaTime);
            last_active_time = (float)Planetarium.GetUniversalTime();
            collection_rate_d = flux*86400;
        }

        //public override string GetInfo() {
            //return String.Format("Core Temperature: {0}K\n Thermal Power: {1}MW", ReactorTemp,ThermalPower);
        //}
    }
}
