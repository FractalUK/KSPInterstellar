using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FNPlugin {
    interface FNResourceSuppliable {
        void supplyPower(float power_supplied,String resourcename);
        float consumePower(double power_to_consume, String resourcename);
        float consumePower(float power_to_consume, String resourcename);
    }
}
