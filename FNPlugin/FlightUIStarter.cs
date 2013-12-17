using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightUIStarter :  MonoBehaviour{
        protected Rect button_position;
        protected Texture2D guibuttontexture;

        public void Start() {
            guibuttontexture = GameDatabase.Instance.GetTexture("WarpPlugin/megajoule_click", false);
            button_position = new Rect(Screen.width - guibuttontexture.width, Screen.height - guibuttontexture.height - 150, guibuttontexture.width, guibuttontexture.height);
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        protected void OnGUI() {
            string resourcename = FNResourceManager.FNRESOURCE_MEGAJOULES;
            Vessel vessel = FlightGlobals.ActiveVessel;
            FNResourceManager mega_manager = null;
            if (FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).hasManagerForVessel(vessel)) {
                mega_manager = FNResourceOvermanager.getResourceOvermanagerForResource(resourcename).getManagerForVessel(vessel);
                if (mega_manager.getPartModule() != null) {
                    mega_manager.OnGUI();
                }
            }
            GUILayout.BeginArea(button_position);
            if (GUILayout.Button(guibuttontexture) && mega_manager != null) {
                mega_manager.showWindow();
            }
            GUILayout.EndArea();
        }
    }
}
