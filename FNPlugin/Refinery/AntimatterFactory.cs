using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    class AntimatterFactory 
    {
        protected Part part;
        protected Vessel vessel;
        protected double current_rate = 0;
        protected double efficiency = 0.01149;

        public AntimatterFactory(Part part)
        {
            this.part = part;
            vessel = part.vessel;
            if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {

                if (PluginHelper.hasTech("ultraHighEnergyPhysics"))
                    efficiency = efficiency / 100;
                else if (PluginHelper.hasTech("highEnergyScience"))
                    efficiency = efficiency / 1000;
                else
                    efficiency = efficiency / 10000;
            }
        }

        public void produceAntimatterFrame(double rate_multiplier) 
        {
            double energy_provided = rate_multiplier * PluginHelper.BaseAMFPowerConsumption * 1E6f;
            double antimatter_density = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.Antimatter).density;
            double antimatter_mass = energy_provided / GameConstants.warpspeed / GameConstants.warpspeed / 200000.0f / antimatter_density*efficiency;
            current_rate = -ORSHelper.fixedRequestResource(part, InterstellarResourcesConfiguration.Instance.Antimatter, -antimatter_mass * TimeWarp.fixedDeltaTime) / TimeWarp.fixedDeltaTime;
        }

        public double getAntimatterProductionRate() 
        {
            return current_rate;
        }

        public double getAntimatterProductionEfficiency() 
        {
            return efficiency;
        }
    }
}
