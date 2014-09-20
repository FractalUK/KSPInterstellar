using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSHelper {

        public static double ToLatitude(double lat) 
        {
            int lat_s = ((int)Math.Ceiling(Math.Abs(lat / 90)) % 2);
            lat = lat % 90;
            if (lat_s == 0) {
                lat = (90 * Math.Sign(lat) - lat) * (-1);
            }
            return lat;
        }

        public static double ToLongitude(double lng) 
        {
            int lng_s = ((int)Math.Ceiling(Math.Abs(lng / 180)) % 2);
            lng = lng % 180;
            if (lng_s == 0) {
                lng = (180 * Math.Sign(lng) - lng) * (-1);
            }
            return lng;
        }

        public static float getMaxAtmosphericAltitude(CelestialBody body) {
            if (!body.atmosphere) {
                return 0;
            }
            return (float)-body.atmosphereScaleHeight * 1000.0f * Mathf.Log(1e-6f);
        }

        public static float fixedRequestResource(Part part, string resourcename, float resource_amount) {
            return (float) fixedRequestResource(part, resourcename, (double)resource_amount);
        }

        public static double fixedRequestResource(Part part, string resourcename, double resource_amount) {
            List<PartResource> prl = part.GetConnectedResources(resourcename).ToList();
            List<Part> parts = new List<Part>();
            ResourceFlowMode flow = PartResourceLibrary.Instance.GetDefinition(resourcename).resourceFlowMode;
            prl = prl.Where(p => p.flowState == true).ToList();
            double max_available = 0;
            double spare_capacity = 0;
            foreach (PartResource partresource in prl) {
                parts.Add(partresource.part);
                max_available += partresource.amount;
                spare_capacity += partresource.maxAmount - partresource.amount;
            }

            if (flow == ResourceFlowMode.ALL_VESSEL) { // use our code
                double resource_left_to_draw = 0;
                double total_resource_change = 0;
                double res_ratio = 0;
                
                if (resource_amount > 0) {
                    resource_left_to_draw = Math.Min(resource_amount, max_available);
                    res_ratio = Math.Min(resource_amount / max_available,1);
                } else {
                    resource_left_to_draw = Math.Max(-spare_capacity, resource_amount);
                    res_ratio = Math.Min(-resource_amount / spare_capacity,1);
                }

                if (double.IsNaN(res_ratio) || double.IsInfinity(res_ratio) || res_ratio == 0) {
                    return 0;
                } else {
                    foreach (PartResource local_part_resource in prl) {
                        if (resource_amount > 0) {
                            local_part_resource.amount = local_part_resource.amount - local_part_resource.amount * res_ratio;
                            total_resource_change += local_part_resource.amount * res_ratio;
                        }else{
                            local_part_resource.amount = local_part_resource.amount + (local_part_resource.maxAmount - local_part_resource.amount) * res_ratio;
                            total_resource_change -= (local_part_resource.maxAmount - local_part_resource.amount) * res_ratio;
                        }
                    }
                }
                return total_resource_change;
            } else {
                if (resource_amount > 0) {
                    return part.RequestResource(resourcename, Math.Min(resource_amount, max_available));
                } else {
                    return part.RequestResource(resourcename, Math.Max(-spare_capacity, resource_amount));
                }
            }
        }
    }
}
