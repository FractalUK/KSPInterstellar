using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class ModuleElementRadioactiveDecay : PartModule {
        // Persistent False
        [KSPField(isPersistant = false)]
        public float decayConstant;
        [KSPField(isPersistant = false)]
        public string resourceName;
        [KSPField(isPersistant = false)]
        public string decayProduct;
        [KSPField(isPersistant = false)]
        public float convFactor = 1;

        PartResource decay_resource;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) {
                return;
            }
            decay_resource = part.Resources[resourceName];
            part.force_activate();   
        }

        public override void OnFixedUpdate() {
            double decay_amount = decayConstant * decay_resource.amount * TimeWarp.fixedDeltaTime;
            decay_resource.amount -= decay_amount;
            if (PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct)) {
                part.RequestResource(decayProduct, -decay_amount);
            }
        }

    }
}
