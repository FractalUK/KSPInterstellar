using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    [KSPModule("Antimatter Reactor")]
	class FNAntimatterReactor : InterstellarReactor, IChargedParticleSource  
	{
        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public override string getResourceManagerDisplayName() { return TypeName; } 

		public double CurrentMeVPerChargedProduct { get { return current_fuel_mode != null ? current_fuel_mode.MeVPerChargedProduct : 0; } }

        public float MaximumChargedIspMult { get { return 100f; } }

        public float MinimumChargdIspMult { get { return 1; } }
    }
}
