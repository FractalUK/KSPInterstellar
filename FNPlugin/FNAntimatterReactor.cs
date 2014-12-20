using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    [KSPModule("Antimatter Reactor")]
    class FNAntimatterReactor : InterstellarReactor {

        public override string TypeName { get { return (isupgraded ? upgradedName != "" ? upgradedName : originalName : originalName) + " Antimatter Reactor"; } }

        public override string getResourceManagerDisplayName() {
            return TypeName;
        }

    }
}
