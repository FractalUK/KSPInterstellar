using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public interface INoozle
    {
        int Fuel_mode { get; }
        bool Static_updating { get; set; }
        bool Static_updating2 { get; set; }
        ConfigNode[] getPropellants();
        double GetNozzleFlowRate();
    }
}
