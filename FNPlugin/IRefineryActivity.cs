using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    interface IRefineryActivity
    {
        String ActivityName { get; }

        double CurrentPower { get; }

        bool HasActivityRequirements { get; }

        double PowerRequirements { get; }

        String Status { get; }

        void UpdateFrame(double power_ratio);

        void UpdateGUI();
    }
}
