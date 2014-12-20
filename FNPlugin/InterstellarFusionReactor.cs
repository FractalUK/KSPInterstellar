using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    abstract class InterstellarFusionReactor : InterstellarReactor, IChargedParticleSource
    {
        public abstract double CurrentMeVPerChargedProduct { get; }
    }
}
