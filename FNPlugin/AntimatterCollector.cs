using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    public class AntimatterCollector : PartModule    {
        [KSPField(isPersistant = false, guiActive = true, guiName = "Antimatter Flux")]
        public string ParticleFlux;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        
        private bool init = false;
        protected int drawCount = 0;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
            this.part.force_activate();

            double now = Planetarium.GetUniversalTime();
            double time_diff = now - last_active_time;
            if (last_active_time != 0 && vessel.orbit.eccentricity < 1) {
                float lat = (float) vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
                float vessel_avg_alt = (float) (vessel.orbit.ApR + vessel.orbit.PeR) / 2.0f;
                float vessel_inclination = (float)vessel.orbit.inclination;
				float flux = (VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, vessel_avg_alt, vessel_inclination) + VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, vessel_avg_alt, 90))/2.0f;
                //vessel.orbit.
                double antimatter_to_add = time_diff*flux;
                part.RequestResource("Antimatter", -antimatter_to_add);
            }
        }

        public override void OnUpdate() {
            float lat = (float)vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            float flux = VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude, lat);
            ParticleFlux = flux.ToString("E");
        }

        public override void OnFixedUpdate() {
            drawCount++;
            float lat = (float) vessel.mainBody.GetLatitude(this.vessel.GetWorldPos3D());
            float flux = VanAllen.getBeltAntiparticles(vessel.mainBody.flightGlobalsIndex, (float)vessel.altitude,lat);
            if (drawCount % 2 == 0) {
                part.RequestResource("Antimatter", -flux * TimeWarp.fixedDeltaTime*2);
            }

            //float antimatter_pcnt = antimatter_provided / AntimatterRate / TimeWarp.fixedDeltaTime;
            //part.RequestResource("ThermalPower", -ThermalPower*TimeWarp.fixedDeltaTime*antimatter_pcnt);
            last_active_time = (float)Planetarium.GetUniversalTime();
        }

        //public override string GetInfo() {
            //return String.Format("Core Temperature: {0}K\n Thermal Power: {1}MW", ReactorTemp,ThermalPower);
        //}
    }
}
