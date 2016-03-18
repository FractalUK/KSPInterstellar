using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public class ThermalPowerTransport : PartModule
    {
        [KSPField(isPersistant = true,  guiActive = false, guiActiveEditor = true, guiName = "Thermal Cost")]
        public float thermalCost = 0.5f;
    }


}
