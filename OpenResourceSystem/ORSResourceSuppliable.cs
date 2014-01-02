using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem {
    public interface ORSResourceSuppliable {
        void receiveFNResource(double power_supplied,String resourcename);
        float consumeFNResource(double power_to_consume, String resourcename);
        float consumeFNResource(float power_to_consume, String resourcename);
        string getResourceManagerDisplayName();
        int getPowerPriority();
    }
}
