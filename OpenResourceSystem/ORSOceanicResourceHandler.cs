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
            ORSOceanicResource resource = bodyOceanicComposition.FirstOrDefault(oor => oor.getResourceName() == resourcename);
            return resource != null ? resource.getResourceAbundance() : 0;
        }

        public static double getOceanicResourceContent(int refBody, int resource) {
            List<ORSOceanicResource> bodyOceanicComposition = getOceanicCompositionForBody(refBody);
            if (bodyOceanicComposition.Count > resource) return bodyOceanicComposition[resource].getResourceAbundance();
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
                    ConfigNode oceanic_resource_pack = GameDatabase.Instance.GetConfigNodes("OCEANIC_RESOURCE_PACK_DEFINITION").FirstOrDefault();
                    Debug.Log("[ORS] Loading oceanic data from pack: " + (oceanic_resource_pack.HasValue("name") ? oceanic_resource_pack.GetValue("name") : "unknown pack"));
                    if (oceanic_resource_pack != null) {
                        List<ConfigNode> oceanic_resource_list = oceanic_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
                        if (oceanic_resource_list.Any())
                        {
                            bodyOceanicComposition = oceanic_resource_list.Select(orsc => new ORSOceanicResource(orsc.HasValue("resourceName") ? orsc.GetValue("resourceName") : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();
                            if (bodyOceanicComposition.Any())
                            {
                                bodyOceanicComposition = bodyOceanicComposition.OrderByDescending(bacd => bacd.getResourceAbundance()).ToList();
                                body_oceanic_resource_list.Add(refBody, bodyOceanicComposition);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Debug.Log("[ORS] Exception while loading oceanic resources : " + ex.ToString());
            }
            return bodyOceanicComposition;
        }
    }
}
