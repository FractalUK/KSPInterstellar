using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace FNPlugin
{
    [KSPModule("ISRU Refinery")]
    class InterstellarRefinery : FNResourceSuppliableModule
    {
        [KSPField(isPersistant=true)]
        bool refinery_is_enabled;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status_str = "";

        private List<IRefineryActivity> _refinery_activities;
        private IRefineryActivity _current_activity = null;
        private Rect _window_position = new Rect(20, 20, 300, 100);
        private int _window_ID;
        private bool _render_window;
        private GUIStyle _bold_label;

        [KSPEvent(guiActive = true, guiName = "Toggle Refinery Window", active = true)]
        public void ToggleWindow()
        {
            _render_window = !_render_window;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (state == StartState.Editor) return;
            System.Random rnd = new System.Random();
            _window_ID = rnd.Next(int.MaxValue);
            _refinery_activities = new List<IRefineryActivity>();
            _refinery_activities.Add(new NuclearFuelReprocessor(this.part));
            _refinery_activities.Add(new AluminiumElectrolyser(this.part));
            _refinery_activities.Add(new SabatierReactor(this.part));
            _refinery_activities.Add(new WaterElectroliser(this.part));
            _refinery_activities.Add(new AnthraquinoneProcessor(this.part));
            _refinery_activities.Add(new MonopropellantProducer(this.part));
            _refinery_activities.Add(new UF4Ammonolysiser(this.part));
            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        public override void OnUpdate()
        {
            status_str = "Offline";
            if (_current_activity != null) status_str = _current_activity.Status;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && refinery_is_enabled && _current_activity != null)
            {
                double power_ratio = consumeFNResource(_current_activity.PowerRequirements*TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES)/TimeWarp.fixedDeltaTime/_current_activity.PowerRequirements;
                _current_activity.UpdateFrame(power_ratio);
            }
        }

        public override string getResourceManagerDisplayName()
        {
            if (refinery_is_enabled && _current_activity != null)
            {
                return "ISRU Refinery (" + _current_activity.ActivityName + ")";
            }
            return "ISRU Refinery";
        }

        public override string GetInfo()
        {
            return "Refinery Module capable of advanced ISRU processing.";
        }

        private void OnGUI()
        {
            if (this.vessel == FlightGlobals.ActiveVessel && _render_window)
            {
                _window_position = GUILayout.Window(_window_ID, _window_position, Window, "ISRU Refinery Interface");
            }
        }


        private void Window(int window)
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }
            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
            {
                _render_window = false;
            }
            GUILayout.BeginVertical();
            if (_current_activity == null || !refinery_is_enabled)
            {
                _refinery_activities.ForEach(act =>
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(act.ActivityName, GUILayout.ExpandWidth(true)) && act.HasActivityRequirements)
                    {
                        _current_activity = act;
                        refinery_is_enabled = true;
                    }
                    GUILayout.EndHorizontal();
                });
            } else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Activity", _bold_label, GUILayout.Width(150));
                GUILayout.Label(_current_activity.ActivityName, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Status", _bold_label, GUILayout.Width(150));
                GUILayout.Label(_current_activity.Status, GUILayout.Width(150));
                GUILayout.EndHorizontal();
                _current_activity.UpdateGUI();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Deactivate", GUILayout.ExpandWidth(true)))
                {
                    refinery_is_enabled = false;
                    _current_activity = null;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
    }
}
