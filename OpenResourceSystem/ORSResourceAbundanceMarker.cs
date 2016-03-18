using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem 
{
    public class ORSResourceAbundanceMarker 
    {
        GameObject non_scale_sphere;
        GameObject scaled_sphere;

        public ORSResourceAbundanceMarker(GameObject scaled_sphere, GameObject non_scale_sphere) 
        {
            this.scaled_sphere = scaled_sphere;
            this.non_scale_sphere = non_scale_sphere;
        }

        public GameObject getScaledSphere() 
        {
            return scaled_sphere;
        }

        public GameObject getPlanetarySphere() 
        {
            return non_scale_sphere;
        }
    }
}
