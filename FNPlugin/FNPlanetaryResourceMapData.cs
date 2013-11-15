using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    class FNPlanetaryResourceMapData : MonoBehaviour {
        static Dictionary<string, FNPlanetaryResourceInfo> body_resource_maps = new Dictionary<string, FNPlanetaryResourceInfo>();
        static Dictionary<string, Vector2d[]> body_abudnance_angles = new Dictionary<string, Vector2d[]>();
        static List<GameObject> abundance_spheres = new List<GameObject>();
        static List<double> surface_height_list = new List<double>();
        static int current_body = -1;
        //static GameObject resource_prim;
        static Material sphere_material = null;
        static protected string displayed_resource = "";
        static protected long update_count = 0;
        static GameObject sphere = null;
        static int sphere_ticker = 0;
        

        public static void loadPlanetaryResourceData(int body) {
            string celestial_body_name = FlightGlobals.Bodies[body].bodyName;
            UrlDir.UrlConfig[] configs = GameDatabase.Instance.GetConfigs("PLANETARY_RESOURCE_DEFINITION");
            Debug.Log("[WarpPlugin] Loading Planetary Resource Data. Length: " + configs.Length);
            body_resource_maps.Clear();
            body_abudnance_angles.Clear();
            foreach (UrlDir.UrlConfig config in configs) {
                ConfigNode planetary_resource_config_node = config.config;
                if (planetary_resource_config_node.GetValue("celestialBodyName") == celestial_body_name) {
                    Debug.Log("[WarpPlugin] Loading Planetary Resource Data for " + celestial_body_name);
                    Texture2D map = GameDatabase.Instance.GetTexture(planetary_resource_config_node.GetValue("mapUrl"), false);
                    string resource_gui_name = planetary_resource_config_node.GetValue("name");
                    FNPlanetaryResourceInfo resource_info = new FNPlanetaryResourceInfo(resource_gui_name, map, body);
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
                    body_resource_maps.Add(resource_gui_name, resource_info);
                    List<Vector2d> abundance_points_list = new List<Vector2d>();

                    for (int i = 0; i < map.height; ++i) {
                        for (int j = 0; j < map.width; ++j) {
                            if (getPixelAbundanceValue(j,i, resource_info) >= 0.001) {
                                //high value location, mark it
                                double theta = (j - map.width / 2)*2.0*180.0/map.width;
                                double phi = (i - map.height / 2)*2.0*90.0/map.height;
                                Vector2d angles = new Vector2d(theta, phi);
                                //body_abudnance_angles.Add(resource_gui_name, angles);
                                abundance_points_list.Add(angles);
                            }
                        }
                    }

                    body_abudnance_angles.Add(resource_gui_name, abundance_points_list.ToArray());
                    Debug.Log("[WarpPlugin] " + abundance_points_list.Count + " high value " + resource_gui_name + " locations detected");
                }
            }
            current_body = body;
        }

        protected static double getPixelAbundanceValue(int pix_x, int pix_y, FNPlanetaryResourceInfo resource_info) {
            Texture2D map = resource_info.getResourceMap();
            Color pix_color = map.GetPixel(pix_x, pix_y);
            double resource_val = 0;
            double scale_factor = resource_info.getScaleFactor();
            double scale_multiplier = resource_info.getScaleMultiplier();
            if (resource_info.getResourceScale() == FNPlanetaryResourceInfo.LOG_SCALE) {
                resource_val = Math.Pow(scale_factor, pix_color.grayscale * 255.0) / 1000000 * scale_multiplier;
            }else if (resource_info.getResourceScale() == FNPlanetaryResourceInfo.LINEAR_SCALE) {
                resource_val = pix_color.grayscale * scale_multiplier;
            }
            return resource_val;
        }

        public static FNPlanetaryResourcePixel getResourceAvailability(int body, string resourcename, double lat, double lng) {
            if (body != current_body) {
                loadPlanetaryResourceData(body);
            }
            int lng_s = ((int)Math.Ceiling(Math.Abs(lng / 180)) % 2);
            lng = lng % 180;
            if (lng_s == 0) {
                lng = (180*Math.Sign(lng) - lng)*(-1);
            }
            int lat_s = ((int)Math.Ceiling(Math.Abs(lat / 90)) % 2);
            lat = lat % 90;
            if (lat_s == 0) {
                lat = (90 * Math.Sign(lat) - lat)*(-1);
            }
            if (body_resource_maps.ContainsKey(resourcename)) {
                FNPlanetaryResourceInfo resource_info = body_resource_maps[resourcename];
                Texture2D map = resource_info.getResourceMap();
                double len_x = map.width;
                double len_y = map.height;
                double origin_x = map.width / 2.0;
                double origin_y = map.height / 2.0;

                double map_x = (lng * len_x/2/180 + origin_x);
                double map_y = (lat * len_y/2/90 + origin_y);

                int pix_x = (int)Math.Round(map_x);
                int pix_y = (int)Math.Round(map_y);
                                
                double resource_val = getPixelAbundanceValue(pix_x, pix_y, resource_info);

                FNPlanetaryResourcePixel resource_pixel = new FNPlanetaryResourcePixel(resource_info.getName(), resource_val, resource_info.getBody());
                resource_pixel.setResourceName(resource_info.getResourceName());

                return resource_pixel;
            }else {
                FNPlanetaryResourcePixel resource_pixel = new FNPlanetaryResourcePixel(resourcename, 0, body);
                return resource_pixel;
            }
        }

        public static void setDisplayedResource(string displayed_resource) {
            FNPlanetaryResourceMapData.displayed_resource = displayed_resource;
            foreach (GameObject abundance_sphere in abundance_spheres) {
                removeAbundanceSphere(abundance_sphere);
            }
            abundance_spheres.Clear();
            surface_height_list.Clear();
        }

        public static void showPlanetaryResourceMapTexture() {
            
            if (body_resource_maps.ContainsKey(displayed_resource) && current_body >= 0) {
                /*
                if (resource_prim == null) {
                    FNPlanetaryResourceInfo resource_info = body_resource_maps[displayed_resource];
                    Texture2D map = resource_info.getResourceMap();
                    CelestialBody celbody = FlightGlobals.Bodies[current_body];
                    resource_prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    resource_prim.collider.enabled = false;
                    resource_prim.transform.localScale = new Vector3((float)celbody.Radius * 2.03f, (float)celbody.Radius * 2.03f, (float)celbody.Radius * 2.03f);
                    resource_prim.transform.position = celbody.transform.position;
                    resource_prim.transform.rotation = celbody.transform.rotation;
                    resource_prim.renderer.material.shader = Shader.Find("Unlit/Texture");
                    resource_prim.renderer.material.color = new Color(Color.white.r, Color.white.g, Color.white.b, 1.0f);
                    resource_prim.renderer.material.mainTexture = map;
                    resource_prim.renderer.receiveShadows = false;
                    resource_prim.renderer.material.renderQueue = 1000;
                    resource_prim.renderer.enabled = true;
                    
                }else {
                    CelestialBody celbody = FlightGlobals.Bodies[current_body];
                    resource_prim.transform.position = celbody.transform.position;
                    resource_prim.transform.rotation = celbody.transform.rotation;
                }
                 */
                
                CelestialBody celbody = FlightGlobals.Bodies[current_body];
                double theta;
                double phi;
                Vector3d up;
                Vector3d center;
                GameObject resource_prim;
                Vector3d translation_vec = new Vector3d(0,0,0);
                
                //if (MapView.MapIsEnabled) {
                    
                    Vector2d[] abundance_points_list = body_abudnance_angles[displayed_resource];
                    int hundreds_of_spheres = abundance_points_list.Length / 100;
                    int i = 0;
                    double surface_height = 0;
                    foreach (Vector2d abundance_point in abundance_points_list) {
                        theta = abundance_point.x;
                        phi = abundance_point.y;
                        
                        if (abundance_spheres.Count <= i) {
                            up = celbody.GetSurfaceNVector(phi, theta).normalized;
                            surface_height = celbody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(theta, Vector3d.down) * QuaternionD.AngleAxis(phi, Vector3d.forward) * Vector3d.right);
                            surface_height_list.Add(surface_height);
                            center = celbody.position + surface_height*up;
                            resource_prim = createAbundanceSphere();
                            abundance_spheres.Add(resource_prim);
                            resource_prim.transform.position = center;
                            if(lineOfSightToPosition(resource_prim.transform.position,celbody)) {
                                resource_prim.renderer.enabled = true;
                            }else{
                                resource_prim.renderer.enabled = false;
                            }
                        } else {
                            resource_prim = abundance_spheres[i];
                            up = celbody.GetSurfaceNVector(phi, theta).normalized;
                            if (surface_height_list.Count <= i) {
                                surface_height = celbody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(theta, Vector3d.down) * QuaternionD.AngleAxis(phi, Vector3d.forward) * Vector3d.right);
                                surface_height_list.Add(surface_height);
                            } else {
                                surface_height = surface_height_list[i];
                            }
                            center = celbody.position + surface_height * up;
                            if (lineOfSightToPosition(center, celbody)) {
                                resource_prim.transform.position = center;
                                if (!resource_prim.renderer.enabled) {
                                    resource_prim.renderer.enabled = true;
                                }
                            } else {
                                if (resource_prim.renderer.enabled) {
                                    resource_prim.renderer.enabled = false;
                                }
                            }
                        }
                        i++;
                    }

                
                //}
            }
            update_count++;
        }

        protected static GameObject createAbundanceSphere() {
            if (sphere == null) {
                GameObject resource_prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                resource_prim.collider.enabled = false;
                resource_prim.transform.localScale = new Vector3(5000, 5000, 5000);
                if (sphere_material == null) {
                    resource_prim.renderer.material.shader = Shader.Find("Unlit/Texture");
                    resource_prim.renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 1.0f);
                    resource_prim.renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/resource_point", false);
                    resource_prim.renderer.material.renderQueue = 1000;
                }else {
                    resource_prim.renderer.sharedMaterial = sphere_material;
                }
                resource_prim.renderer.receiveShadows = false;
                resource_prim.renderer.enabled = false;
                resource_prim.renderer.castShadows = false;
                Destroy(resource_prim.collider);
                sphere = resource_prim;
            }
            return (GameObject) Instantiate(sphere);
        }

        protected static void removeAbundanceSphere(GameObject go) {
            Destroy(go);
        }

        protected static bool lineOfSightToPosition(Vector3d a, CelestialBody referenceBody) {
            Vector3d b = FlightGlobals.ActiveVessel.transform.position;
            //foreach (CelestialBody referenceBody in FlightGlobals.Bodies) {
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
            //}
            return true;
        }
            

        

    }
}
