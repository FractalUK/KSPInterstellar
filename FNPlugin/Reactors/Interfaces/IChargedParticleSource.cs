using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface IChargedParticleSource : IThermalSource
    {
        float MaximumChargedPower { get; } 

        double ChargedPowerRatio { get; }

        double CurrentMeVPerChargedProduct { get; }

        double  UseProductForPropulsion(double ratio);
    }
}
