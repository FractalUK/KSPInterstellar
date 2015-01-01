using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using OpenResourceSystem;

namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightUIStarter :  MonoBehaviour{
        protected Rect button_position;
        protected Texture2D guibuttontexture;
        protected bool hide_button = false;
        public static bool show_window = false;

        public void Start() {
            guibuttontexture = GameDatabase.Instance.GetTexture("WarpPlugin/megajoule_click", false);
            if (!PluginHelper.using_toolbar) {
                button_position = new Rect(Screen.width - guibuttontexture.width, Screen.height - guibuttontexture.height - 150, guibuttontexture.width, guibuttontexture.height);
            }
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        public void Update() {
            if (Input.GetKeyDown(KeyCode.F2)) {
                hide_button = !hide_button;
            }
        }

        protected void OnGUI() {
            string resourcename = FNResourceManager.FNRESOURCE_MEGAJOULES;
            Vessel vessel = FlightGlobals.ActiveVessel;
            ORSResourceManager mega_manager = null;
            if (vessel != null) {
                if (FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel) && !hide_button) {
                    mega_manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
                    if (mega_manager.getPartModule() != null) {
                        mega_manager.OnGUI();

                        if (!PluginHelper.using_toolbar) {
                            GUILayout.BeginArea(button_position);
                            if (GUILayout.Button(guibuttontexture)) {
                                mega_manager.showWindow();
                            }
                            GUILayout.EndArea();
                        } else {
                            if (show_window) {
                                mega_manager.showWindow();
                                show_window = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
