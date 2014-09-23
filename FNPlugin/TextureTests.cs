using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
/*
namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class TextureTests : MonoBehaviour {
        Vector3d sphere_scale = new Vector3d(5000, 5000, 5000);
        GameObject sphere;
        string sphere_texture = null;

        public void Start() {
            //CelestialBody body = FlightGlobals.currentMainBody;
            //Debug.Log("Planet Texture Width: " + body.renderer.material.mainTexture.width);
            //Debug.Log("Planet Texture Height: " + body.renderer.material.mainTexture.width);
            //Texture2D p_texture = body.pqsController.parentSphere. as Texture2D;
            //Color32[] colours = p_texture.GetPixels32();
            //colours.ToList().ForEach(cl => { cl.r = 255; cl.b = 255; cl.g = 255;});
            //p_texture.SetPixels32(colours);
            CelestialBody celbody = FlightGlobals.currentMainBody;
            Transform scaled_trans = ScaledSpace.Instance.scaledSpaceTransforms.Single(t => t.name == celbody.name);
            
            Vector3d scaledcenter = ScaledSpace.LocalToScaledSpace(celbody.position);
            print("SF " + ScaledSpace.InverseScaleFactor);
            sphere = createAbundanceSphere();
            Vector3d scaledsize = sphere.transform.localScale * ScaledSpace.InverseScaleFactor;
            sphere.transform.position = scaledcenter;
            sphere.transform.rotation = celbody.transform.rotation;
            sphere.transform.parent = scaled_trans;
            sphere.layer = 10;
            sphere.transform.localPosition = new Vector3(0, 0, 0);
            sphere.transform.localRotation = Quaternion.Euler(0, 18.25f, 0);
            sphere.transform.localScale = scaledsize;
            sphere.renderer.enabled = true;
        }

        public void Update() {
            if (sphere != null) {
                if (MapView.MapIsEnabled) {
                    sphere.renderer.enabled = true;
                } else {
                    sphere.renderer.enabled = false;
                }
            }
        }

        protected GameObject createAbundanceSphere() {
            if (sphere == null) {
                string path = "WarpPlugin/resourcesphere";
                print(path);
                GameObject model = GameDatabase.Instance.GetModel(path);
                GameObject resource_prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //resource_prim.GetComponent<MeshFilter>().mesh = model.GetComponent<MeshFilter>().mesh;
                //DatabaseLoaderModel_MU dlm = new DatabaseLoaderModel_MU();
                //mu
                resource_prim.collider.enabled = false;
                //Vector3d sphere_scale = new Vector3d(FlightGlobals.currentMainBody.Radius, FlightGlobals.currentMainBody.Radius, FlightGlobals.currentMainBody.Radius);
                 	
                Vector3d sphere_scale = new Vector3d(600000, 600000, 600000);
                resource_prim.transform.localScale = sphere_scale * 21;
                //resource_prim.transform.localScale = Vector3.one * 1050;
                //print("here");
                //print(resource_prim.renderer);
                //print(resource_prim.renderer.material);
                resource_prim.renderer.material = new Material(Shader.Find("Unlit/Texture"));
                //resource_prim.renderer.material.shader = Shader.Find("Unlit/Texture");
                resource_prim.renderer.material.color = new Color(Color.red.r, Color.red.g, Color.red.b, 1.0f);
                print("not renderer");
                if (sphere_texture != null) {
                    //resource_prim.renderer.material.mainTexture = GameDatabase.Instance.GetTexture(sphere_texture, false);
                } else {
                    //resource_prim.renderer.material.mainTexture = GameDatabase.Instance.GetTexture("WarpPlugin/PlanetResourceData/kerbin_uranium", false);
                }
                // remap colours
                Texture2D tex = GameDatabase.Instance.GetTexture("WarpPlugin/PlanetResourceData/kerbin_thorium", false) as Texture2D;
                Color32[] pixels = tex.GetPixels32();
                print(tex.width);
                print(tex.height);
                //Color32[] new_pixels = pixels.Select(px => (px.r + px.g + px.b) / 3).Select(intensity => new Color32((byte)Mathf.Max(255 - 2 * intensity, 0.0f), 20, (byte)Mathf.Max(2 * intensity - 255, 0.0f), 150)).ToArray();
                //2*Mathf.Abs(128 - intensity);
                int max_pixel = pixels.Max(px => (px.r + px.g + px.b) / 3);
                Color32[] new_pixels = pixels.Select(px => (px.r + px.g + px.b) / 3).Select(intensity => new Color32((byte)Mathf.Max((float)intensity / (float)max_pixel * 255f, 0.0f), 0, (byte)Mathf.Max((1.0f - (float)intensity / (float)max_pixel) * 255f, 0.0f), (byte)Mathf.Max((float)intensity / (float)max_pixel * 255f, 0.0f))).ToArray();
                //Color32[] new_pixels = pixels.Select(px => (px.r + px.g + px.b) / 3).Select(intensity => new Color32(255,0,0,255)).ToArray();
                print("New Pixels " + new_pixels.Length);
                print(tex.format);
                Texture2D tex2 = new Texture2D(tex.width, tex.height);
                tex2.SetPixels32(new_pixels);
                tex2.wrapMode = TextureWrapMode.Clamp;
                //tex.SetPixels32(new_pixels);
                tex2.Apply();
                resource_prim.renderer.material.mainTexture = tex2;
                resource_prim.renderer.material.renderQueue = 1000;
                resource_prim.renderer.receiveShadows = false;
                resource_prim.renderer.enabled = false;
                resource_prim.renderer.castShadows = false;
                Destroy(resource_prim.collider);
                sphere = resource_prim;
            }
            return (GameObject)Instantiate(sphere);
        }
    }


}*/
