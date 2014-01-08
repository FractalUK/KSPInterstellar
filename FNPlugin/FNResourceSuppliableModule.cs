using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenResourceSystem;

namespace FNPlugin {
    abstract class FNResourceSuppliableModule : ORSResourceSuppliableModule{
        protected override ORSResourceManager createResourceManagerForResource(string resourcename) {
            return getOvermanagerForResource(resourcename).createManagerForVessel(this);
        }

        protected override ORSResourceOvermanager getOvermanagerForResource(string resourcename) {
            return FNResourceOvermanager.getResourceOvermanagerForResource(resourcename);
        }
    }
}
