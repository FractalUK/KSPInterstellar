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
        public ModuleResourceIntake attachedIntake = null;
        public List<ModuleResourceIntake> radialAttachedIntakes;

        public override void OnStart(PartModule.StartState state) 
        {
            if (state == StartState.Editor) return;

            UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - Onstart start search for Air Intake module to cool");

            // first check if part itself has an air intake
            attachedIntake = part.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(mre => mre.resourceName == "IntakeAir");

            if (attachedIntake != null)
                UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - Found Airintake on self");

            if (attachedIntake == null)
            {
                // then look to connect radial attached children
                radialAttachedIntakes = part.children
                    .Where(p => p.attachMode == AttachModes.SRF_ATTACH)
                    .SelectMany(p => p.FindModulesImplementing<ModuleResourceIntake>()).Where(mre => mre.resourceName == "IntakeAir").ToList();

                if (radialAttachedIntakes.Count > 0)
                    UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - Found Airintake in children");
            }

            // third look for stack attachable air intake
            if (attachedIntake == null && (radialAttachedIntakes == null || radialAttachedIntakes.Count == 0))
            {
                UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - looking at attached nodes");

                foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null))
                {
                    var attachedPart = attach_node.attachedPart;

                    // skip any parts that contain a precooler
                    if (attachedPart.FindModulesImplementing<FNModulePreecooler>().Any())
                    {
                        UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - skipping Module Implementing FNModulePreecooler");
                        continue;
                    }

                    attachedIntake = attachedPart.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(mre => mre.resourceName == "IntakeAir");

                    if (attachedIntake != null)
                    {
                        UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - found Airintake in attached part with name " + attachedIntake.name);
                        break;
                    }
                }

                if (attachedIntake == null)
                {
                    UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - looking at deeper attached nodes");

                    // look for stack attacked parts one part further
                    foreach (AttachNode attach_node in part.attachNodes.Where(a => a.attachedPart != null))
                    {
                        if (attach_node.attachedPart.FindModulesImplementing<FNModulePreecooler>().Any()) continue;
                        
                        foreach (AttachNode subAttach_node in attach_node.attachedPart.attachNodes.Where(a => a.attachedPart != null))
                        {
                            attachedIntake = subAttach_node.attachedPart.FindModulesImplementing<ModuleResourceIntake>().FirstOrDefault(mre => mre.resourceName == "IntakeAir");

                            if (attachedIntake != null)
                            {
                                UnityEngine.Debug.Log("[KSPI] - FNModulePreecooler - found Airintake in deeper attached part with name " + attachedIntake.name);
                                break;
                            }
                        }
                        if (attachedIntake != null) break;
                    }
                }
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
