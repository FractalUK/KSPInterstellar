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

            foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null)) 
            {
                List<ModuleResourceIntake> mres = attach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().Where(mre => mre.resourceName == "IntakeAir").ToList();
                if(mres.Count > 0) 
                {
                    attachedIntake = mres.First();
                    break;
                }
            }

            // if not did found and stack connected airintakes, find an radial connected air intake
            if (attachedIntake == null)
            {
                radialAttachedIntakes = this.part.children
                    .Where(c => c.attachMode == AttachModes.SRF_ATTACH)  
                    .SelectMany(p => p.FindModulesImplementing<ModuleResourceIntake>())
                    .Where(mre => mre.resourceName == "IntakeAir")
                    .ToList();
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
