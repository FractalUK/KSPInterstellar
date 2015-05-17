using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin 
{
    class FNModulePreecooler : PartModule 
    {
        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Precooler status")]
        public string statusStr;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Intake")]
        public string attachedIntakeName;


        protected bool functional = false;
        
        public ModuleResourceIntake attachedIntake;

        public List<ModuleResourceIntake> radialAttachedIntakes;

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

            // first look for stack attacke air intake
            foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null)) 
            {
                attachedIntake = attach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(mre => mre.resourceName == "IntakeAir");

                if (attachedIntake != null) break;
            }

            if (attachedIntake == null)
            {
                // look for stack attacked parts one part further
                foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null))
                {
                    foreach (AttachNode subAttach_node in attach_node.attachedPart.attachNodes.Where(a => a.attachedPart != null))
                    {
                        attachedIntake = subAttach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(mre => mre.resourceName == "IntakeAir");

                        if (attachedIntake != null) break;
                    }
                    if (attachedIntake != null) break;
                }
            }

            // if not did found and stack connected airintakes, find an radial connected air intake
            if (attachedIntake == null)
            {
                //if (radialAttachedIntakes.Count > 0) break;
                radialAttachedIntakes = part.children.SelectMany(p => p.FindModulesImplementing<ModuleResourceIntake>()).Where(mre => mre.resourceName == "IntakeAir").ToList();
            }



            part.force_activate();

            if (attachedIntake != null)
                attachedIntakeName = attachedIntake.name;
            else
            {
                if (radialAttachedIntakes == null )
                    attachedIntakeName = "Null found";
                else if (radialAttachedIntakes.Count > 1)
                    attachedIntakeName = "Multiple intakes found";
                else if (radialAttachedIntakes.Count > 0)
                    attachedIntakeName = radialAttachedIntakes.First().name;
                else
                    attachedIntakeName = "Not found";
            }
        }

        public override void OnUpdate() 
        {
            if (functional) 
                statusStr = "Active.";
            else 
                statusStr = "Offline.";
        }

        public int ValidAttachedIntakes
        {
            get
            {
                return attachedIntake != null ? 1 : Math.Min(radialAttachedIntakes.Count(), 2);
            }
        }

        public override void OnFixedUpdate() 
        {
            functional = ((attachedIntake != null && attachedIntake.intakeEnabled) || radialAttachedIntakes.Any(i => i.intakeEnabled) );
        }

        public bool isFunctional() 
        {
            return functional;
        }
    }
}
