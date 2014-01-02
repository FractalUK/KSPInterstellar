using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSOceanicResourceHandler {
        protected static Dictionary<int, List<ORSOceanicResource>> body_oceanic_resource_list = new Dictionary<int, List<ORSOceanicResource>>();

        public static double getOceanicResourceContent(int refBody, string resourcename) {
            List<ORSOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > 0) {
                foreach (ORSOceanicResource bodyAtmosphericResource in bodyOceanicComposition) {
                    if (bodyAtmosphericResource.getResourceName() == resourcename) {
                        return bodyAtmosphericResource.getResourceAbundance();
                    }
                }
            }
            return 0;
        }

        public static double getOceanicResourceContent(int refBody, int resource) {
            List<ORSOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getResourceAbundance();
            }
            return 0;
        }

        public static string getOceanicResourceName(int refBody, int resource) {
            List<ORSOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getResourceName();
            }
            return null;
        }

        public static string getOceanicResourceDisplayName(int refBody, int resource) {
            List<ORSOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getDisplayName();
            }
            return null;
        }

        public static List<ORSOceanicResource> getOceanicCompositionForBody(int refBody) {
            List<ORSOceanicResource> bodyOceanicComposition = new List<ORSOceanicResource>();
            try {
                if (body_oceanic_resource_list.ContainsKey(refBody)) {
                    return body_oceanic_resource_list[refBody];
                } else {
                    ConfigNode[] bodyOceanicResourceList = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_DEFINITION").Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToArray();
                    foreach (ConfigNode bodyOceanicConfig in bodyOceanicResourceList) {
                        string resourcename = null;
                        if (bodyOceanicConfig.HasValue("resourceName")) {
                            resourcename = bodyOceanicConfig.GetValue("resourceName");
                        }
                        double resourceabundance = double.Parse(bodyOceanicConfig.GetValue("abundance"));
                        string displayname = bodyOceanicConfig.GetValue("guiName");
                        ORSOceanicResource bodyOceanicResource = new ORSOceanicResource(resourcename, resourceabundance, displayname);
                        bodyOceanicComposition.Add(bodyOceanicResource);
                    }
                    if (bodyOceanicComposition.Count > 1) {
                        bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.getResourceAbundance()).ToList();
                    }
                }
            } catch (Exception ex) {

            }
            return bodyOceanicComposition;
        }
    }
}
