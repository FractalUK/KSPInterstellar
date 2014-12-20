using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface IChargedParticleSource : IThermalSource
    {
        float MaximumChargedPower { get; }

        float ChargedParticleRatio { get; }

        double CurrentMeVPerChargedProduct { get; }
    }
}
