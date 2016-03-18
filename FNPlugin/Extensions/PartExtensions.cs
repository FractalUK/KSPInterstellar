using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

namespace FNPlugin
{
    public static class PartExtensions
    {
        private static FieldInfo windowListField;

        /// <summary>
        /// Find the UIPartActionWindow for a part. Usually this is useful just to mark it as dirty.
        /// </summary>
        public static UIPartActionWindow FindActionWindow(this Part part)
        {
            if (part == null)
                return null;

            // We need to do quite a bit of piss-farting about with reflection to 
            // dig the thing out. We could just use Object.Find, but that requires hitting a heap more objects.
            UIPartActionController controller = UIPartActionController.Instance;
            if (controller == null)
                return null;

            if (windowListField == null)
            {
                Type cntrType = typeof(UIPartActionController);
                foreach (FieldInfo info in cntrType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (info.FieldType == typeof(List<UIPartActionWindow>))
                    {
                        windowListField = info;
                        goto foundField;
                    }
                }
                Debug.LogWarning("*PartUtils* Unable to find UIPartActionWindow list");
                return null;
            }
            foundField:

            List<UIPartActionWindow> uiPartActionWindows = (List<UIPartActionWindow>)windowListField.GetValue(controller);
            if (uiPartActionWindows == null)
                return null;

            return uiPartActionWindows.FirstOrDefault(window => window != null && window.part == part);
        }

        public static bool IsConnectedToModule(this Part currentPart, String partmodule, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.Modules.Contains(partmodule);
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToModule(currentPart.parent, partmodule, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToModule(child, partmodule, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static bool IsConnectedToPart(this Part currentPart, String partname, int maxChildDepth, Part previousPart = null)
        {
            bool found = currentPart.name == partname;
            if (found)
                return true;

            if (currentPart.parent != null && currentPart.parent != previousPart)
            {
                bool foundPart = IsConnectedToPart(currentPart.parent, partname, maxChildDepth, currentPart);
                if (foundPart)
                    return true;
            }

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    bool foundPart = IsConnectedToPart(child, partname, (maxChildDepth - 1), currentPart);
                    if (foundPart)
                        return true;
                }
            }

            return false;
        }

        public static double FindAmountOfAvailableFuel(this Part currentPart, String resourcename, int maxChildDepth, Part previousPart = null)
        {
            double amount = 0;

            if (currentPart.Resources.Contains(resourcename))
            {
                var partResourceAmount = currentPart.Resources[resourcename].amount;
                //UnityEngine.Debug.Log("[KSPI] - found " + partResourceAmount.ToString("0.0000") + " " + resourcename + " resource in " + currentPart.name);
                amount += partResourceAmount;
            }

            if (currentPart.parent != null && currentPart.parent != previousPart)
                amount += FindAmountOfAvailableFuel(currentPart.parent, resourcename, maxChildDepth, currentPart);

            if (maxChildDepth > 0)
            {
                foreach (var child in currentPart.children.Where(c => c != null && c != previousPart))
                {
                    amount += FindAmountOfAvailableFuel(child, resourcename, (maxChildDepth - 1), currentPart);
                }
            }

            return amount;
        }
    }
}
