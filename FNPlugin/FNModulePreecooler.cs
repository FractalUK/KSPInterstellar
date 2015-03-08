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
                foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null))
                {
                    radialAttachedIntakes = attach_node.attachedPart.children
                        .Where(c => c.attachMode == AttachModes.SRF_ATTACH)
                        .SelectMany(p => p.FindModulesImplementing<ModuleResourceIntake>())
                        .Where(mre => mre.resourceName == "IntakeAir")
                        .ToList();

                    if (radialAttachedIntakes.Count > 0) break;
                }
            }

            part.force_activate();
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
