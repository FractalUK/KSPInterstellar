using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    /// <summary>
    /// Storage class required for relay route calculation
    /// </summary>
    class MicrowaveRoute
    {
        public double Efficiency { get; set; }
        public double Distance { get; set; }
        public double FacingFactor { get; set; }
        public VesselRelayPersistence PreviousRelay { get; set; }
        


        public MicrowaveRoute(double efficiency, double distance, double facingFactor, VesselRelayPersistence previousRelay=null)
        {
            Efficiency = efficiency;
            Distance = distance;
            FacingFactor = facingFactor;
            PreviousRelay = previousRelay;
        }
    }
}
