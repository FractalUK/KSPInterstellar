using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenResourceSystem {
    public static class ORSPartExtensions {

        public static IEnumerable<PartResource> GetConnectedResources(this Part part, PartResourceDefinition definition) {
            List<PartResource> resources = new List<PartResource>();
            part.GetConnectedResources(definition.id, definition.resourceFlowMode, resources);
            return resources;
        }

        public static IEnumerable<PartResource> GetConnectedResources(this Part part, String resourcename) {
            PartResourceDefinition definition = PartResourceLibrary.Instance.GetDefinition(resourcename);
            return GetConnectedResources(part, definition);
        }

        public static double ImprovedRequestResource(this Part part, String resourcename, double resource_amount) {
            return ORSHelper.fixedRequestResource(part, resourcename, resource_amount);
        }

    }
}