using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface IChargedParticleSource : IThermalSource
    {
        double CurrentMeVPerChargedProduct { get; }

        double  UseProductForPropulsion(double ratio, double consumedAmount);

        float MaximumChargedIspMult { get; }

        float MinimumChargdIspMult { get; }
    }
}
