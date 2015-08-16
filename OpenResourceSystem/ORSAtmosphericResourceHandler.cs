using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem 
{
    public class ORSAtmosphericResourceHandler 
    {
        protected static Dictionary<int, List<ORSAtmosphericResource>> body_atmospheric_resource_list = new Dictionary<int, List<ORSAtmosphericResource>>();

        public static double getAtmosphericResourceContent(int refBody, string resourcename) {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            ORSAtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.getResourceName() == resourcename);
            return resource != null ? resource.getResourceAbundance() : 0;
        }

        public static double getAtmosphericResourceContentByDisplayName(int refBody, string resourcename) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            ORSAtmosphericResource resource = bodyAtmosphericComposition.FirstOrDefault(oor => oor.getDisplayName() == resourcename);
            return resource != null ? resource.getResourceAbundance() : 0;
        }

        public static double getAtmosphericResourceContent(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getResourceAbundance();

            return 0;
        }

        public static string getAtmosphericResourceName(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getResourceName();

            return null;
        }

        public static string getAtmosphericResourceDisplayName(int refBody, int resource) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = getAtmosphericCompositionForBody(refBody);
            if (bodyAtmosphericComposition.Count > resource) 
                return bodyAtmosphericComposition[resource].getDisplayName();

            return null;
        }

        public static List<ORSAtmosphericResource> getAtmosphericCompositionForBody(int refBody) 
        {
            List<ORSAtmosphericResource> bodyAtmosphericComposition = new List<ORSAtmosphericResource>();
            try 
            {
                if (body_atmospheric_resource_list.ContainsKey(refBody)) 
                    return body_atmospheric_resource_list[refBody];
                else 
                {
                    ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION_KSPI").FirstOrDefault();
                    //ConfigNode atmospheric_resource_pack = GameDatabase.Instance.GetConfigNodes("ATMOSPHERIC_RESOURCE_PACK_DEFINITION").FirstOrDefault(c => c.name == "KSPI_AtmosphericPack");

                    Debug.Log("[ORS] Loading atmospheric data from pack: " + (atmospheric_resource_pack.HasValue("name") ? atmospheric_resource_pack.GetValue("name") : "unknown pack"));
                    if (atmospheric_resource_pack != null) 
                    {
                        List<ConfigNode> atmospheric_resource_list = atmospheric_resource_pack.nodes.Cast<ConfigNode>().Where(res => res.GetValue("celestialBodyName") == FlightGlobals.Bodies[refBody].name).ToList();
                        if (atmospheric_resource_list.Any())
                        {
                            bodyAtmosphericComposition = atmospheric_resource_list.Select(orsc => new ORSAtmosphericResource(orsc.HasValue("resourceName") 
                                ? orsc.GetValue("resourceName") 
                                : null, double.Parse(orsc.GetValue("abundance")), orsc.GetValue("guiName"))).ToList();

                            if (bodyAtmosphericComposition.Any())
                            {
                                bodyAtmosphericComposition = bodyAtmosphericComposition.OrderByDescending(bacd => bacd.getResourceAbundance()).ToList();
                                body_atmospheric_resource_list.Add(refBody, bodyAtmosphericComposition);
                            }
                        }
                    }
                }
            } 
            catch (Exception ex) 
            {
                Debug.Log("[ORS] Exception while loading atmospheric resources : " + ex.ToString());
            }
            return bodyAtmosphericComposition;
        }
    }
}
