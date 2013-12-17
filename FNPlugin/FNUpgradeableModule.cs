using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    interface FNUpgradeableModule {
        void upgradePartModule();
        bool hasTechsRequiredToUpgrade();
    }
}
