using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin {
    public class TechUpdateWindow {
        protected bool render_window = false;
        protected Rect windowPosition = new Rect(20, 20, 350, 100);
        protected int windowID = 983479;
        protected GUIStyle bold_label;

        public TechUpdateWindow() {
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        public void Show() {
            render_window = true;
        }

        private void OnGUI() {
            if (render_window) {
                windowPosition = GUILayout.Window(windowID, windowPosition, Window, "Interstellar Tech Tree Update");
            }
        }

        private void Window(int windowID) {
            bold_label = new GUIStyle(GUI.skin.label);
            bold_label.fontStyle = FontStyle.Bold;
            if (GUI.Button(new Rect(windowPosition.width - 20, 2, 18, 18), "x")) {
                render_window = false;
            }
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("An Update to the Interstellar Tech Tree is Available",bold_label,GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Install Update",GUILayout.Width(160))) {
                UpdateTechTree();
                render_window = false;
            }
            if (GUILayout.Button("Dismiss", GUILayout.Width(160))) {
                render_window = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        protected void UpdateTechTree() {
            ConfigNode new_tech_nodes = PluginHelper.getNewTechTreeFile();
            if (new_tech_nodes != null) {
                new_tech_nodes.Save(PluginHelper.getTechTreeFilePath());
                PopupDialog.SpawnPopupDialog("Restart KSP", "Changes to the tech tree have been applied, please restart KSP before continuing.", "OK", false, GUI.skin);
            }
        }
    }
}
