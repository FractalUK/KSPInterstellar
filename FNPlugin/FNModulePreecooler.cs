using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class FNModulePreecooler : PartModule {
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Precooler status")]
        public string statusStr;

        protected bool functional = false;
        protected ModuleResourceIntake attachedIntake;

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }

            foreach (AttachNode attach_node in part.attachNodes) {
                if (attach_node.attachedPart != null) {
                    List<ModuleResourceIntake> mres = attach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == "IntakeAir").ToList();
                    if(mres.Count > 0) {
                        attachedIntake = mres.First();
                    }
                }
            }
            part.force_activate();
        }

        public override void OnUpdate() {
            if (functional) {
                statusStr = "Active.";
            } else {
                statusStr = "Offline.";
            }
        }

        public override void OnFixedUpdate() {
            functional = false;
            if (attachedIntake != null) {
                if (attachedIntake.intakeEnabled) {
                    functional = true;
                }
            }
        }

        public bool isFunctional() {
            return functional;
        }
    }
}
