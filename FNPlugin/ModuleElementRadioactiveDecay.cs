extern alias ORSv1_4_1;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ORSv1_4_1::OpenResourceSystem;

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

        private double decayWithProductionRate( double time_diff )
        {
            // get generation creation rate
            double generation_rate = 0;
            int other_containers = 1;
            if (resourceName == "Tritium") {
                List<FNReactor> reactors = part.vessel.FindPartModulesImplementing<FNReactor> ();
                foreach (FNReactor reactor in reactors) {
                    generation_rate += Convert.ToDouble(reactor.tritiumBreedRate);
                }

                List<PartResource> tritium_resources = new List<PartResource> ();
                part.GetConnectedResources (PartResourceLibrary.Instance.GetDefinition (resourceName).id, PartResourceLibrary.Instance.GetDefinition (resourceName).resourceFlowMode, tritium_resources);

                foreach (PartResource res in tritium_resources) {
                    if (res.maxAmount > res.amount)
                        other_containers++;
                }
            }
            generation_rate /= other_containers; // 0 by default

            // calculate decay with resource generation
            double amount = decay_resource.amount;
            double quantity = decay_resource.amount - generation_rate * time_diff;

            double decayed_total = 0;
            double decay_amount = 0;
            double interval = 86400; // 1 day

            while( time_diff >= interval )
            {
                time_diff     -= interval;
                decay_amount   = quantity * decayConstant * interval;
                quantity      += generation_rate * interval - decay_amount;
                quantity       = Math.Min(decay_resource.maxAmount,quantity);
                decayed_total += decay_amount;
            }

            time_diff      -= time_diff;
            decay_amount    = amount * decayConstant * time_diff;
            quantity       += generation_rate * time_diff - decay_amount;
            quantity        = Math.Min(decay_resource.maxAmount,quantity);
            decayed_total  += decay_amount;

            return decayed_total;
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) {
                return;
            }
            decay_resource = part.Resources[resourceName];
            part.force_activate();

            double time_diff = lastActiveTime - Planetarium.GetUniversalTime();
            if (PartResourceLibrary.Instance.resourceDefinitions.Contains(decayProduct)) {
                density_rat = decay_resource.info.density / PartResourceLibrary.Instance.GetDefinition(decayProduct).density;
            }

            double time_diff = Planetarium.GetUniversalTime() - lastActiveTime;
            if (time_diff > 0) {
                double decay_amount = decayWithProductionRate(time_diff);
                ORSHelper.fixedRequestResource(part, decayProduct, -decay_amount*density_rat);
                decay_resource.amount -= decay_amount;
            }

            lastActiveTime = (float) Planetarium.GetUniversalTime();
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
