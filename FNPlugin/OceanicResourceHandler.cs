using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class OceanicResourceHandler {
        protected static Dictionary<int, List<FNOceanicResource>> body_oceanic_resource_list = new Dictionary<int, List<FNOceanicResource>>();

        public static double getOceanicResourceContent(int refBody, string resourcename) {
            List<FNOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > 0) {
                foreach (FNOceanicResource bodyAtmosphericResource in bodyOceanicComposition) {
                    if (bodyAtmosphericResource.getResourceName() == resourcename) {
                        return bodyAtmosphericResource.getResourceAbundance();
                    }
                }
            }
            return 0;
        }

        public static double getOceanicResourceContent(int refBody, int resource) {
            List<FNOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getResourceAbundance();
            }
            return 0;
        }

        public static string getOceanicResourceName(int refBody, int resource) {
            List<FNOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getResourceName();
            }
            return null;
        }

        public static string getOceanicResourceDisplayName(int refBody, int resource) {
            List<FNOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) {
                return bodyOceanicComposition[resource].getDisplayName();
            }
            return null;
        }

        public static List<FNOceanicResource> getOceanicCompositionForBody(int refBody) {
            List<FNOceanicResource> bodyOceanicComposition = new List<FNOceanicResource>();
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
                        FNOceanicResource bodyOceanicResource = new FNOceanicResource(resourcename, resourceabundance, displayname);
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
