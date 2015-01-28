using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSPlanetaryResourceMapData : MonoBehaviour
    {
        static Dictionary<string, ORSPlanetaryResourceInfo> body_resource_maps = new Dictionary<string, ORSPlanetaryResourceInfo>();
        static Dictionary<string, Vector2d[]> body_abudnance_angles = new Dictionary<string, Vector2d[]>();
        static List<ORSResourceAbundanceMarker> abundance_markers = new List<ORSResourceAbundanceMarker>();
        static int current_body = -1;
        static int map_body = -1;
        //static GameObject resource_prim;
        static Material sphere_material = null;
        static protected string displayed_resource = "";
        static protected string map_resource = "";
        static protected long update_count = 0;
        static GameObject sphere = null;
        static Vector3d sphere_scale = new Vector3d(5000, 5000, 5000);
        static Vector3d sphere_scale_scaled = new Vector3d(2, 2, 2);
        static string sphere_texture;
        static double stored_scale = -1;

        public static IDictionary<string, ORSPlanetaryResourceInfo> PlanetaryResourceMapData { get { return body_resource_maps; } }

        public static void loadPlanetaryResourceData(int body) 
        {
            string celestial_body_name = FlightGlobals.Bodies[body].bodyName;
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("PLANETARY_RESOURCE_DEFINITION");
            Debug.Log("[ORS] Loading Planetary Resource Data. Length: " + configs.Length);
            foreach (ORSResourceAbundanceMarker abundance_marker in abundance_markers) {
                removeAbundanceSphere(abundance_marker.getPlanetarySphere());
                removeAbundanceSphere(abundance_marker.getScaledSphere());
            }
            sphere = null;
            sphere_texture = null;
            body_resource_maps.Clear();
            body_abudnance_angles.Clear();
            map_body = -1;
            current_body = body;
            foreach (UrlDir.UrlConfig config in configs) {
                ConfigNode planetary_resource_config_node = config.config;
                if (planetary_resource_config_node.GetValue("celestialBodyName") == celestial_body_name && planetary_resource_config_node != null) {
                    Debug.Log("[ORS] Loading Planetary Resource Data for " + celestial_body_name);
                    Texture2D map = GameDatabase.Instance.GetTexture(planetary_resource_config_node.GetValue("mapUrl"), false);
                    if (map == null) continue;
                  
                    string resource_gui_name = planetary_resource_config_node.GetValue("name");

                    if (body_resource_maps.ContainsKey(resource_gui_name)) continue; // skip duplicates

                    ORSPlanetaryResourceInfo resource_info = new ORSPlanetaryResourceInfo(resource_gui_name, map, body);
                    if (planetary_resource_config_node.HasValue("resourceName")) {
                        string resource_name = planetary_resource_config_node.GetValue("resourceName");
                        resource_info.setResourceName(resource_name);
                    }
                    if (planetary_resource_config_node.HasValue("resourceScale")) {
                        string resource_scale = planetary_resource_config_node.GetValue("resourceScale");
                        resource_info.setResourceScale(resource_scale);
                    }
                    if (planetary_resource_config_node.HasValue("scaleFactor")) {
                        string scale_factorstr = planetary_resource_config_node.GetValue("scaleFactor");
                        double scale_factor = double.Parse(scale_factorstr);
                        resource_info.setScaleFactor(scale_factor);
                    }
                    if (planetary_resource_config_node.HasValue("scaleMultiplier")) {
                        string scale_multstr = planetary_resource_config_node.GetValue("scaleMultiplier");
                        double scale_mult = double.Parse(scale_multstr);
                        resource_info.setScaleMultiplier(scale_mult);
                    }
                    if (planetary_resource_config_node.HasValue("displayTexture")) {
                        string tex_path = planetary_resource_config_node.GetValue("displayTexture");
                        resource_info.setDisplayTexture(tex_path);
                    } else {
                        string tex_path = planetary_resource_config_node.GetValue("WarpPlugin/ParticleFX/resource_point");
                        resource_info.setDisplayTexture(tex_path);
                    }
                    if (planetary_resource_config_node.HasValue("displayThreshold")) {
                        string display_threshold_str = planetary_resource_config_node.GetValue("displayThreshold");
                        double display_threshold = double.Parse(display_threshold_str);
                        resource_info.setDisplayThreshold(display_threshold);
                    }
                    body_resource_maps.Add(resource_gui_name, resource_info);
                    List<Vector2d> abundance_points_list = new List<Vector2d>();

                    for (int i = 0; i < map.height; ++i) {
                        for (int j = 0; j < map.width; ++j) {
                            if (resource_info.getPixelAbundanceValue(j,i) >= resource_info.getDisplayThreshold()) {
                                //high value location, mark it
                                double theta = (j - map.width / 2)*2.0*180.0/map.width;
                                double phi = (i - map.height / 2)*2.0*90.0/map.height;
                                Vector2d angles = new Vector2d(theta, phi);
                                abundance_points_list.Add(angles);
                            }
                        }
                    }

                    body_abudnance_angles.Add(resource_gui_name, abundance_points_list.ToArray());
                    Debug.Log("[ORS] " + abundance_points_list.Count + " high value " + resource_gui_name + " locations detected");
                }
            }
        }
               
        public static ORSPlanetaryResourcePixel getResourceAvailabilityByRealResourceName(int body, string resourcename, double lat, double lng) 
        {
            if (body != current_body) loadPlanetaryResourceData(body);
            
            try{
                ORSPlanetaryResourceInfo resource_info = body_resource_maps.Where(ri => ri.Value.getResourceName() == resourcename).FirstOrDefault().Value;
                return getResourceAvailability(body, resource_info.getName(),lat,lng);
            }catch(Exception ex) {
                ORSPlanetaryResourcePixel resource_pixel = new ORSPlanetaryResourcePixel(resourcename, 0, body);
                return resource_pixel;
            }
        }

        public static ORSPlanetaryResourcePixel getResourceAvailability(int body, string resourcename, double lat, double lng) 
        {
            if (body != current_body) loadPlanetaryResourceData(body);

            if (body_resource_maps.ContainsKey(resourcename)) 
            {
                ORSPlanetaryResourceInfo resource_info = body_resource_maps[resourcename];
                double resource_val = resource_info.getLatLongAbundanceValue(lat, lng);

                ORSPlanetaryResourcePixel resource_pixel = new ORSPlanetaryResourcePixel(resource_info.getName(), resource_val, resource_info.getBody());
                resource_pixel.setResourceName(resource_info.getResourceName());

                return resource_pixel;
            }else 
            {
                ORSPlanetaryResourcePixel resource_pixel = new ORSPlanetaryResourcePixel(resourcename, 0, body);
                return resource_pixel;
            }
        }


        public static void setDisplayedResource(string displayed_resource) {
            ORSPlanetaryResourceMapData.displayed_resource = displayed_resource;
            foreach (ORSResourceAbundanceMarker abundance_marker in abundance_markers) {
                removeAbundanceSphere(abundance_marker.getPlanetarySphere());
                removeAbundanceSphere(abundance_marker.getScaledSphere());
            }
            map_body = -1;
            sphere = null;
            sphere_texture = null;
            abundance_markers.Clear();
        }

        public static bool resourceIsDisplayed(string resource) 
        {
            return displayed_resource == resource;
        }

        public static void updatePlanetaryResourceMap() 
        {
            if (FlightGlobals.currentMainBody.flightGlobalsIndex != current_body) loadPlanetaryResourceData(FlightGlobals.currentMainBody.flightGlobalsIndex);

            if (body_resource_maps.ContainsKey(displayed_resource) && (FlightGlobals.currentMainBody.flightGlobalsIndex != map_body || displayed_resource != map_resource)) {
                foreach (ORSResourceAbundanceMarker abundance_marker in abundance_markers) {
                    removeAbundanceSphere(abundance_marker.getPlanetarySphere());
                    removeAbundanceSphere(abundance_marker.getScaledSphere());
                }
                abundance_markers.Clear();
                CelestialBody celbody = FlightGlobals.currentMainBody;
                sphere_texture = body_resource_maps[displayed_resource].getDisplayTexturePath();
                Vector2d[] abundance_points_list = body_abudnance_angles[displayed_resource];
                if (abundance_points_list != null && celbody.pqsController != null) {
                    foreach (Vector2d abundance_point in abundance_points_list) {
                        double theta = abundance_point.x;
                        double phi = abundance_point.y;
                        Vector3d up = celbody.GetSurfaceNVector(phi, theta).normalized;
                        double surface_height = celbody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(theta, Vector3d.down) * QuaternionD.AngleAxis(phi, Vector3d.forward) * Vector3d.right);
                        GameObject resource_prim = createAbundanceSphere();
                        GameObject resource_prim_scaled = createAbundanceSphere();

                        Vector3d center = celbody.position + surface_height * up;
                        Vector3d scaledcenter = ScaledSpace.LocalToScaledSpace(celbody.position) + surface_height * up*ScaledSpace.InverseScaleFactor;

                        Transform scaled_trans = ScaledSpace.Instance.scaledSpaceTransforms.Single(t => t.name == celbody.name);
                        resource_prim_scaled.transform.position = scaledcenter;
                        resource_prim_scaled.transform.localScale = sphere_scale_scaled * (FlightGlobals.currentMainBody.Radius / FlightGlobals.Bodies[ORSGameConstants.REF_BODY_KERBIN].Radius);
                        resource_prim_scaled.transform.localRotation = Quaternion.identity;
                        resource_prim_scaled.transform.parent = scaled_trans;
                        resource_prim_scaled.layer = 10;

                        resource_prim.transform.position = center;
                        resource_prim.transform.parent = celbody.transform;
                        resource_prim.transform.localScale = sphere_scale * (FlightGlobals.currentMainBody.Radius / FlightGlobals.Bodies[ORSGameConstants.REF_BODY_KERBIN].Radius);
                        resource_prim.transform.localRotation = Quaternion.identity;

                        ORSResourceAbundanceMarker abundance_marker = new ORSResourceAbundanceMarker(resource_prim_scaled, resource_prim);
                        abundance_markers.Add(abundance_marker);
                    }
                    map_body = current_body;
                    map_resource = displayed_resource;
                    stored_scale = ScaledSpace.ScaleFactor;
                }
                //celbody.renderer.material.mainTexture.
            } else {
                if (body_resource_maps.ContainsKey(displayed_resource) && FlightGlobals.currentMainBody.flightGlobalsIndex == map_body && displayed_resource == map_resource) {
                    CelestialBody celbody = FlightGlobals.currentMainBody;
                    foreach (ORSResourceAbundanceMarker abundance_marker in abundance_markers) {
                        if (lineOfSightToPosition(abundance_marker.getPlanetarySphere().transform.position, celbody)) {
                            if (MapView.MapIsEnabled) {
                                abundance_marker.getScaledSphere().renderer.enabled = true;
                                abundance_marker.getPlanetarySphere().renderer.enabled = false;
                            } else {
                                abundance_marker.getScaledSphere().renderer.enabled = false;
                                abundance_marker.getPlanetarySphere().renderer.enabled = true;
                            }   
                        }else{
                            abundance_marker.getScaledSphere().renderer.enabled = false;
                            abundance_marker.getPlanetarySphere().renderer.enabled = false;
                        }
                    }
                }
            }
        }
                   

        protected static GameObject createAbundanceSphere() 
        {
            if (sphere == null) 
            {
                GameObject resource_prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                resource_prim.collider.enabled = false;
                resource_prim.transform.localScale = sphere_scale * (FlightGlobals.currentMainBody.Radius / FlightGlobals.Bodies[ORSGameConstants.REF_BODY_KERBIN].Radius);
                resource_prim.renderer.material.shader = Shader.Find("Unlit/Texture");
                resource_prim.renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 1.0f);
                if (sphere_texture != null) {
                    resource_prim.renderer.material.mainTexture = GameDatabase.Instance.GetTexture(sphere_texture,false);
                } else {
                    resource_prim.renderer.material.mainTexture = GameDatabase.Instance.GetTexture("OpenResourceSystem/resource_point", false);
                }
                resource_prim.renderer.material.renderQueue = 1000;
                resource_prim.renderer.receiveShadows = false;
                resource_prim.renderer.enabled = false;
                resource_prim.renderer.castShadows = false;
                Destroy(resource_prim.collider);
                sphere = resource_prim;
            }
            return (GameObject) Instantiate(sphere);
        }
        
        protected static void removeAbundanceSphere(GameObject go) 
        {
            Destroy(go);
        }

        protected static bool lineOfSightToPosition(Vector3d a, CelestialBody referenceBody) 
        {
            Vector3d b = FlightGlobals.ActiveVessel.transform.position;
            Vector3d refminusa = referenceBody.position - a;
            Vector3d bminusa = b - a;
            if (Vector3d.Dot(refminusa, bminusa) > 0) {
                if (Vector3d.Dot(refminusa, bminusa.normalized) < bminusa.magnitude) {
                    Vector3d tang = refminusa - Vector3d.Dot(refminusa, bminusa.normalized) * bminusa.normalized;
                    if (tang.magnitude < referenceBody.Radius) {
                        return false;
                    }
                }
            }
            return true;
        }
            

        

    }
}
