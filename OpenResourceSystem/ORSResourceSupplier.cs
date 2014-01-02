using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem {
    public interface ORSResourceSupplier {
        double supplyFNResource(double supply, String resourcename);
    }
}
