using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenResourceSystem 
{
    public class ORSHelper 
    {
        public static double ToLatitude(double lat) 
        {
            int lat_s = ((int)Math.Ceiling(Math.Abs(lat / 90)) % 2);
            lat = lat % 90;
            if (lat_s == 0) 
                lat = (90 * Math.Sign(lat) - lat) * (-1);

            return lat;
        }

        public static double ToLongitude(double lng) 
        {
            int lng_s = ((int)Math.Ceiling(Math.Abs(lng / 180)) % 2);
            lng = lng % 180;
            if (lng_s == 0) 
                lng = (180 * Math.Sign(lng) - lng) * (-1);

            return lng;
        }

        public static float getMaxAtmosphericAltitude(CelestialBody body) 
        {
            if (!body.atmosphere) return 0;
            
            return (float)-body.atmosphereDepth * 1000.0f * Mathf.Log(1e-6f);
        }

        public static double fixedRequestResourceSpareCapacity(Part part, string resourcename)
        {
            return part.GetConnectedResources(resourcename).Sum(r => r.maxAmount - r.amount);
        }

        public static float fixedRequestResource(Part part, string resourcename, float resource_amount) 
        {
            return (float) fixedRequestResource(part, resourcename, (double)resource_amount);
        }

        public static double fixedRequestResource(Part part, string resourcename, double resource_amount)
        {
            if (resource_amount == 0)
                return 0;

            ResourceFlowMode flow = PartResourceLibrary.Instance.GetDefinition(resourcename).resourceFlowMode;

            return fixedRequestResource(part, resourcename, resource_amount, flow);
        }

        public static void removeVesselFromCache(Vessel vessel)
        {
            if (orsPropellantDictionary.ContainsKey(vessel))
                orsPropellantDictionary.Remove(vessel);
        }

        private static Dictionary<Vessel, Dictionary<Part, ORSPropellantControl>> orsPropellantDictionary = new Dictionary<Vessel,Dictionary<Part,ORSPropellantControl>>();

        public static double fixedRequestResource(Part part, string resourcename, double resource_amount, ResourceFlowMode flow)
        {
            if (flow == ResourceFlowMode.NULL)
                flow = PartResourceLibrary.Instance.GetDefinition(resourcename).resourceFlowMode;

            if (flow != ResourceFlowMode.ALL_VESSEL)
                return part.RequestResource(resourcename, resource_amount);

            var partsWithResource = part.vessel.parts.Where(p => p.Resources.Contains(resourcename));

            Dictionary<Part, ORSPropellantControl> partLookup;
            if (orsPropellantDictionary.ContainsKey(part.vessel))
                partLookup = orsPropellantDictionary[part.vessel];
            else
            {
                partLookup = part.vessel.FindPartModulesImplementing<ORSPropellantControl>().ToDictionary(p => p.part);
                orsPropellantDictionary.Add(part.vessel, partLookup);
            }

            var partResources = partsWithResource.Where(p => !partLookup.ContainsKey(p) || partLookup[p].isPropellant).Select(p => p.Resources[resourcename]);
            IList<PartResource> relevant_part_resources = new List<PartResource>
                (
                resource_amount > 0
                    ? partResources.Where(p => p.flowState && p.amount > 0)
                    : partResources.Where(p => p.flowState && p.maxAmount > p.amount)
                );

            if (!relevant_part_resources.Any())
                return 0;

            double total_resource_change = 0;
            double res_ratio = resource_amount > 0
                ? Math.Min(resource_amount/relevant_part_resources.Sum(p => p.amount), 1)
                : Math.Min(-resource_amount/relevant_part_resources.Sum(p => p.maxAmount - p.amount), 1);

            if (res_ratio == 0 || double.IsNaN(res_ratio) || double.IsInfinity(res_ratio))
                return 0;
            
            foreach (PartResource local_part_resource in relevant_part_resources)
            {
                if (resource_amount > 0)
                {
                    var part_resource_change = local_part_resource.amount*res_ratio;
                    local_part_resource.amount -= part_resource_change;
                    total_resource_change += part_resource_change;
                }
                else
                {
                    var part_resource_change = (local_part_resource.maxAmount - local_part_resource.amount)*res_ratio;
                    local_part_resource.amount += part_resource_change;
                    total_resource_change -= part_resource_change;
                }
            }
            return total_resource_change;
        }
    }
}
