using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    [KSPModule("Antimatter Initiated Reactor")]
    class InterstellarCatalysedFissionFusion : InterstellarReactor, IChargedParticleSource
    {
        public double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public override bool IsNeutronRich { get { return current_fuel_mode != null ? !current_fuel_mode.Aneutronic : false; } }
        
    }
}
