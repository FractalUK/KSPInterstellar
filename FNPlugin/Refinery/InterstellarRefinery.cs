using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin.Refinery
{
    [KSPModule("ISRU Refinery")]
    class InterstellarRefinery : FNResourceSuppliableModule
    {
        [KSPField(isPersistant=true)]
        bool refinery_is_enabled;

        [KSPField(isPersistant = false, guiActive = true, guiName = "Status")]
        public string status_str = "";

        [KSPField(isPersistant = false)]
        public float powerReqMult = 1f;

        const int labelWidth = 200;
        const int valueWidth = 200;

        private List<IRefineryActivity> _refinery_activities;
        private IRefineryActivity _current_activity = null;
        private Rect _window_position = new Rect(50, 50, labelWidth + valueWidth, 100);
        private int _window_ID;
        private bool _render_window;

        private GUIStyle _bold_label;
        private GUIStyle _enabled_button;
        private GUIStyle _disabled_button;

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

            var unsortedList =  new List<IRefineryActivity>();

            try
            {
                unsortedList.Add(new AnthraquinoneProcessor(this.part));
                unsortedList.Add(new NuclearFuelReprocessor(this.part));
                unsortedList.Add(new AluminiumElectrolyser(this.part));
                unsortedList.Add(new SabatierReactor(this.part));
                unsortedList.Add(new WaterElectroliser(this.part));
                unsortedList.Add(new MonopropellantProducer(this.part));
                unsortedList.Add(new UF4Ammonolysiser(this.part));
                unsortedList.Add(new HaberProcess(this.part));
                unsortedList.Add(new AmmoniaElectrolyzer(this.part));
                unsortedList.Add(new CarbonDioxideElectroliser(this.part));
            }
            catch (Exception e)
            {
                Debug.LogException(e, new UnityEngine.Object() { name = "ISRU Refinery" });
                Debug.LogWarning("ISRU Refinery Exception " + e.Message);
            }

            _refinery_activities = unsortedList.OrderBy(a => a.ActivityName).ToList();

            RenderingManager.AddToPostDrawQueue(0, OnGUI);
        }

        public override void OnUpdate()
        {
            status_str = "Offline";
            if (_current_activity != null)
            {
                status_str = _current_activity.Status;
            }
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !refinery_is_enabled || _current_activity == null) return;

            var fixedConsumedPowerMW = consumeFNResource(powerReqMult * _current_activity.PowerRequirements * TimeWarp.fixedDeltaTime, FNResourceManager.FNRESOURCE_MEGAJOULES);
            var power_ratio = fixedConsumedPowerMW / TimeWarp.fixedDeltaTime / _current_activity.PowerRequirements / powerReqMult;
            _current_activity.UpdateFrame(power_ratio, overflowAllowed);
        }

        public override string getResourceManagerDisplayName()
        {
            if (refinery_is_enabled && _current_activity != null) return "ISRU Refinery (" + _current_activity.ActivityName + ")";

            return "ISRU Refinery";
        }

        public override string GetInfo()
        {
            return "Refinery Module capable of advanced ISRU processing.";
        }

        private void OnGUI()
        {
            if (this.vessel != FlightGlobals.ActiveVessel || !_render_window) return;

            _window_position = GUILayout.Window(_window_ID, _window_position, Window, "ISRU Refinery Interface");
        }

        private bool overflowAllowed;

        private void Window(int window)
        {
            if (_bold_label == null)
            {
                _bold_label = new GUIStyle(GUI.skin.label);
                _bold_label.fontStyle = FontStyle.Bold;
            }

            if (_enabled_button == null)
            {
                _enabled_button = new GUIStyle(GUI.skin.button);
                _enabled_button.fontStyle = FontStyle.Bold;
            }

            if (_disabled_button == null)
            {
                _disabled_button = new GUIStyle(GUI.skin.button);
                _disabled_button.fontStyle = FontStyle.Normal;
            }

            

            if (GUI.Button(new Rect(_window_position.width - 20, 2, 18, 18), "x"))
                _render_window = false;

            GUILayout.BeginVertical();

            if (_current_activity == null || !refinery_is_enabled)
            {
                _refinery_activities.ForEach(act =>
                {

                    GUILayout.BeginHorizontal();
                    bool hasRequirement = act.HasActivityRequirements;
                    GUIStyle guistyle = hasRequirement ? _enabled_button : _disabled_button;

                    if (GUILayout.Button(act.ActivityName, guistyle, GUILayout.ExpandWidth(true)) && hasRequirement)
                    {
                        _current_activity = act;
                        refinery_is_enabled = true;
                    }
                    GUILayout.EndHorizontal();
                });
            }
            else
            {
                // show button to enable/disable resource overflow
                GUILayout.BeginHorizontal();
                if (overflowAllowed)
                {
                    if (GUILayout.Button("Disable Overflow", GUILayout.ExpandWidth(true)))
                        overflowAllowed = false;
                }
                else
                {
                    if (GUILayout.Button("Enable Overflow", GUILayout.ExpandWidth(true)))
                        overflowAllowed = true;
                }
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Activity", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_current_activity.ActivityName, GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Status", _bold_label, GUILayout.Width(labelWidth));
                GUILayout.Label(_current_activity.Status, GUILayout.Width(valueWidth));
                GUILayout.EndHorizontal();

                // allow current activity to show feedback
                _current_activity.UpdateGUI();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Deactivate Proces", GUILayout.ExpandWidth(true)))
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
