extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Resource Extractor")]
    class FNModuleResourceExtraction : ORSModuleResourceExtraction{
        public override void OnFixedUpdate() {
            base.OnFixedUpdate();
            //double extractd = ORSHelper.fixedRequestResource(part, "UF4", 1.01666666666666667e-7 * TimeWarp.fixedDeltaTime);
            //print(extractd);
        }
    }
}
