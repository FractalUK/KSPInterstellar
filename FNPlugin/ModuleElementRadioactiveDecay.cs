using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin {
    [KSPModule("Radioactive Decay")]
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

        [KSPField(isPersistant = true)]
        public float lastActiveTime = 1;

        protected double density_rat = 1;

        PartResource decay_resource;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) {
                return;
            }
            decay_resource = part.Resources[resourceName];
            double time_diff = lastActiveTime - Planetarium.GetUniversalTime();
            if (PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct)) {
                density_rat = decay_resource.info.density / PartResourceLibrary.Instance.GetDefinition(decayProduct).density;
            }
            if(time_diff > 0) {
                double n_0 = decay_resource.amount;
                decay_resource.amount = n_0 * Math.Exp(-decayConstant * time_diff);
                double n_change = n_0 - decay_resource.amount;
                if (PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct)) {
                    ORSHelper.fixedRequestResource(part, decayProduct, -n_change * density_rat);
                }
            }
        }

        public void FixedUpdate() {
            if (HighLogic.LoadedSceneIsFlight)
            {
                double decay_amount = decayConstant * decay_resource.amount * TimeWarp.fixedDeltaTime;
                decay_resource.amount -= decay_amount;
                if (PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct))
                {
                    ORSHelper.fixedRequestResource(part, decayProduct, -decay_amount * density_rat);
                }

                lastActiveTime = (float)Planetarium.GetUniversalTime();
            }
        }

        public override string GetInfo()
        {
            return "Radioactive Decay";
        }

    }
}
