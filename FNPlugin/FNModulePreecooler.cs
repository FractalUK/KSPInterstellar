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
                        break;                          //added by attosecond 3/10/14 -- no need to keep the loop going if we found what we wanted
                    }
                }
            }
            
            /*Added by attosecond 3/10/14. Code checks if this is an "all-in-one" intake + precooler part, also checks
             * parts attached to children and parent parts for intakes, on the assumption that the child/parent parts can
             * transfer the intake air to the precooler */

            //first check if this is an all-in-one part
            if (attachedIntake == null)
            {
                List<ModuleResourceIntake> mres = part.FindModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == "IntakeAir").ToList();
                if (mres.Count > 0)
                    attachedIntake = mres.First();
            }

            //now check all parts attached to the parent part
            if (attachedIntake == null)
            {
                foreach (AttachNode attach_node in part.parent.attachNodes)
                {
                    if (attach_node.attachedPart != null && attach_node.attachedPart != part)
                    {
                        List<ModuleResourceIntake> mres = attach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == "IntakeAir").ToList();
                        if (mres.Count > 0)
                        {
                            attachedIntake = mres.First();
                            break;
                        }
                    }
                }
            }

            //and do the same for child parts
            if (attachedIntake == null)
            {
                foreach (Part childpart in part.children)
                {
                    foreach (AttachNode attach_node in childpart.attachNodes)
                    {
                        if (attach_node.attachedPart != null && attach_node.attachedPart != part)
                        {
                            List<ModuleResourceIntake> mres = attach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == "IntakeAir").ToList();
                            if (mres.Count > 0)
                            {
                                attachedIntake = mres.First();
                                break;
                            }
                        }
                    }
                }
            }

            /* 
             * End edits by attosecond
            */
            
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
