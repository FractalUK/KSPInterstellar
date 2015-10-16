using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using UnityEngine;

namespace OpenResourceSystem 
{
    public class ORSPropellantControl  : PartModule
    {
        [KSPField(isPersistant = true,  guiActive = false, guiActiveEditor = true, guiName = "Is Propellant")]
        public bool isPropellant = true;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false, guiName = "Controlled")]
        public string resources = String.Empty;

        List<string> controlledResources;

        public override void OnStart(PartModule.StartState state)
        {
            // nothing
            controlledResources = resources.Split(',').ToList();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Switch Propellant Mode")]
        public void SwitchIsPropellant()
        {
            isPropellant = !isPropellant;
        }

        public void Update()
        {
            Events["SwitchIsPropellant"].guiName = isPropellant ? "Lock as Propellant Source" : "Unlock as Propellant Source";
            Events["SwitchIsPropellant"].active = controlledResources.Any(r => part.Resources.Contains(r));
        }

        public static string GetDescription<T>(T enumerationValue)  where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            //Tries to find a DescriptionAttribute for a potential friendly name for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    //Pull out the description value
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            //If we have no description attribute, just return the ToString of the enum
            return enumerationValue.ToString();

        }

        public static int GetLengthEnum<T>(T enumerationValue) where T : struct
        {
            Type type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
            }

            //Tries to find a DescriptionAttribute for a potential friendly name for the enum
            MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                return attrs.Length;
            }
            //If we have no description attribute, just return the ToString of the enum
            return 0;

        }


    }
}
