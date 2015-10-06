using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    [KSPModule("Radioactive Decay")]
    class ModuleElementRadioactiveDecay : PartModule 
    {
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

        private PartResource decay_resource;
        private bool resourceDefinitionsContainDecayProduct;

        public override void OnStart(PartModule.StartState state)
        {
            double time_diff = lastActiveTime - Planetarium.GetUniversalTime();

            if (state == StartState.Editor)
                return;

            if (part.Resources.Contains(resourceName))
                decay_resource = part.Resources[resourceName];
            else
            {
                decay_resource = null;
                return;
            }

            resourceDefinitionsContainDecayProduct = PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct);
            if (resourceDefinitionsContainDecayProduct)
                density_rat = decay_resource.info.density / PartResourceLibrary.Instance.GetDefinition(decayProduct).density;

            if (decay_resource != null && time_diff > 0)
            {
                double n_0 = decay_resource.amount;
                decay_resource.amount = n_0 * Math.Exp(-decayConstant * time_diff);
                double n_change = n_0 - decay_resource.amount;

                if (resourceDefinitionsContainDecayProduct)
                    ORSHelper.fixedRequestResource(part, decayProduct, -n_change * density_rat);
            }
        }

        public void FixedUpdate()
        {
            if (decay_resource == null) return;

            if (!HighLogic.LoadedSceneIsFlight) return;

            lastActiveTime = (float)Planetarium.GetUniversalTime();

            double decay_amount = decayConstant * decay_resource.amount * TimeWarp.fixedDeltaTime;
            decay_resource.amount -= decay_amount;

            if (resourceDefinitionsContainDecayProduct)
                ORSHelper.fixedRequestResource(part, decayProduct, -decay_amount * density_rat);
        }

        public override string GetInfo()
        {
            return "Radioactive Decay";
        }

    }
}
