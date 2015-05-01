using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    public static class FSGUIwindowID
    {
        public static int housingProgram = 5600;
        public static int baseNumber = 5879;
        public static int infoPopup = 5879;
        public static int animateGeneric = 5880; // 10 reserved
        public static int switchEngineThrustTransform = 5890;
        public static int toggleSurfaceAttach = 5891;
        public static int test = 5892;
        public static int trimAdjustment = 5893; // 4 reserved for pitch/yaw/roll/wheel
        public static int moveCraftAtLaunch = 5898;
        public static int VTOLrotator = 5899;
        public static int textureSwitch = 5900; // 10 reserved
        public static int wheel = 5911;
        public static int partTurner = 5912;
        public static int wing = 5913;
        public static int flightPath = 5916; // 5 reserved

        public static Rect standardRect = new Rect(500f, 300f, 300f, 100f);
        public static Rect tallRect = new Rect(500f, 300f, 300f, 500f);

        public static int lastUsedID = 6050;

        public static int getNextID()
        {
            lastUsedID++;
            return lastUsedID;
        }
    }

    public class InterstellarTextureSwitch : PartModule
    {
        [KSPField]
        public string displayName = "Texture switcher";
        [KSPField]
        public int moduleID = 0;
        [KSPField(isPersistant = true)]
        public int selectedTexture = 0;
        [KSPField(isPersistant = true)]
        public string selectedTextureURL = string.Empty;
        [KSPField(isPersistant = true)]
        public string selectedMapURL = string.Empty;
        [KSPField]
        public bool showListButton = false;
        [KSPField]
        public bool debugMode = false;
        [KSPField]
        public bool switchableInFlight = false;
        [KSPField]
        public string additionalMapType = "_BumpMap";
        [KSPField]
        public bool mapIsNormal = true;
        [KSPField]
        public bool repaintableEVA = true;
        [KSPField]
        public Vector4 GUIposition = new Vector4(FSGUIwindowID.standardRect.x, FSGUIwindowID.standardRect.y, FSGUIwindowID.standardRect.width, FSGUIwindowID.standardRect.height);
        [KSPField]
        public bool showPreviousButton = true;

        List<Transform> targetObjectTransforms = new List<Transform>();
        List<Material> targetMats = new List<Material>();

        private InterstellarNodeLoader textureNode;
        private string textureNodeName = "textures";
        private string textureValueName = "name";
        private List<String> texList = new List<string>();

        private InterstellarNodeLoader mapNode;
        private string mapNodeName = "additionalMap";
        private string mapValueName = "name";
        private List<String> mapList = new List<string>();

        InterstellarNodeLoader objectNode;
        private string objectNodeName = "objects";
        private string objectValueName = "name";
        private List<String> objectList = new List<string>();
        InterstellarDebugMessages debug = new InterstellarDebugMessages(false, InterstellarDebugMessages.OutputMode.both, 2f); //set to true for debug   

        public static Dictionary<String, List<String>> texListDictionary = new Dictionary<String, List<string>>();
        public static Dictionary<String, List<String>> mapListDictionary = new Dictionary<String, List<string>>();
        public static Dictionary<String, List<String>> objectListDictionary = new Dictionary<String, List<string>>();

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "Debug: Log Objects")]
        public void listAllObjects()
        {
            List<Transform> childList = ListChildren(part.transform);
            foreach (Transform t in childList)
            {
                Debug.Log("object: " + t.name);
            }
        }


        List<Transform> ListChildren(Transform a)
        {
            List<Transform> childList = new List<Transform>();
            foreach (Transform b in a)
            {
                childList.Add(b);
                childList.AddRange(ListChildren(b));
            }
            return childList;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Next Texture")]
        public void nextTextureEvent()
        {
            selectedTexture++;
            if (selectedTexture >= texList.Count)
                selectedTexture = 0;
            useTextureAll();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Previous Texture")]
        public void previousTextureEvent()
        {
            selectedTexture--;
            if (selectedTexture < 0)
                selectedTexture = texList.Count - 1;
            useTextureAll();
        }

        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = 5f, guiActive = false, guiActiveEditor = false, guiName = "Repaint")]
        public void nextTextureEVAEvent()
        {
            nextTextureEvent();
        }

        public void useTextureAll()
        {
            foreach (Material mat in targetMats)
            {
                useTexture(mat);
            }
        }

        public void useTexture(Material targetMat)
        {
            if (targetMat != null && texList.Count > 0)
            {
                if (GameDatabase.Instance.ExistsTexture(texList[selectedTexture]))
                {
                    debug.debugMessage("InterstellarTextureSwitch: assigning texture: " + texList[selectedTexture]);
                    targetMat.mainTexture = GameDatabase.Instance.GetTexture(texList[selectedTexture], false);
                    selectedTextureURL = texList[selectedTexture];
                    if (mapList.Count > selectedTexture)
                    {
                        targetMat.SetTexture(additionalMapType, GameDatabase.Instance.GetTexture(mapList[selectedTexture], mapIsNormal));
                        selectedMapURL = mapList[selectedTexture];
                    }
                }
                else
                {
                    debug.debugMessage("InterstellarTextureSwitch: no such texture: " + texList[selectedTexture]);
                }
            }
            else
            {
                debug.debugMessage("InterstellarTextureSwitch: No target material in object.");
            }
        }

        public string uniqueModuleID
        {
            get
            {
                return part.name.Split('(')[0].Split(' ')[0] + moduleID; // parts have the suffix (Clone) in the Editor. In flight the first part has " (vesselName)" added
            }
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine("Alternate textures available:");
            if (texList.Count == 0)
            {
                if (!texListDictionary.TryGetValue(uniqueModuleID, out texList))
                    info.AppendLine("None. Error reading Dictionary");
                else if (texList.Count == 0)
                    info.AppendLine("None");
            }
            for (int i = 0; i < texList.Count; i++)
            {
                string[] splitString = texList[i].Split('/');
                if (splitString.Length > 0)
                    info.AppendLine(splitString[splitString.Length - 1]);
            }
            info.AppendLine("\nUse the Next Texture button on the right click menu.");
            return info.ToString();
        }

        public override void OnLoad(ConfigNode node)
        {
            getNodeValues(node, textureNode, textureNodeName, textureValueName, texListDictionary, texList);
            getNodeValues(node, mapNode, mapNodeName, mapValueName, mapListDictionary, mapList);
            getNodeValues(node, objectNode, objectNodeName, objectValueName, objectListDictionary, objectList);
        }

        private void getNodeValues(ConfigNode node, InterstellarNodeLoader nodeLoader, string nodeName, string valueName, Dictionary<String, List<String>> outputDict, List<String> outputList)
        {
            nodeLoader = new InterstellarNodeLoader(part, moduleName, moduleID.ToString(), nodeName, valueName);
            nodeLoader.debugMode = debugMode;
            outputList = nodeLoader.ProcessNodeAsStringList(node);
            if (!outputDict.ContainsKey(uniqueModuleID))
                outputDict.Add(uniqueModuleID, outputList);
        }

        public override void OnStart(PartModule.StartState state)
        {
            debug.debugMode = debugMode;

            if (!texListDictionary.TryGetValue(uniqueModuleID, out texList))
                debug.debugMessage("InterstellarTextureSwitch: No matching texture list key: " + uniqueModuleID);
            if (!mapListDictionary.TryGetValue(uniqueModuleID, out mapList))
                debug.debugMessage("InterstellarTextureSwitch: No matching map list key: " + uniqueModuleID);
            if (!objectListDictionary.TryGetValue(uniqueModuleID, out objectList))
                debug.debugMessage("InterstellarTextureSwitch: No matching object list key: " + uniqueModuleID);

            debug.debugMessage("InterstellarTextureSwitch found " + texList.Count + " textures, using number " + selectedTexture + ", found " + objectList.Count + " objects, " + mapList.Count + " maps");

            foreach (String targetObjectName in objectList)
            {
                Transform targetObjectTransform = part.FindModelTransform(targetObjectName);
                if (targetObjectTransform != null && targetObjectTransform.gameObject.renderer != null) // check for if the object even has a mesh. otherwise part list loading crashes
                {
                    Material targetMat = targetObjectTransform.gameObject.renderer.material;
                    if (targetMat != null)
                    {
                        if (!targetMats.Contains(targetMat))
                            targetMats.Add(targetMat);
                    }
                    else
                        debug.debugMessage("InterstellarTextureSwitch: No target material in object " + targetObjectName);
                }
                else
                    debug.debugMessage("FStextureSwitch: Object " + targetObjectName + " not found");
            }

            useTextureAll();

            if (switchableInFlight) Events["nextTextureEvent"].guiActive = true;
            if (switchableInFlight && showPreviousButton) Events["previousTextureEvent"].guiActive = true;
            if (showListButton) Events["listAllObjects"].guiActiveEditor = true;
            if (!repaintableEVA) Events["nextTextureEVAEvent"].guiActiveUnfocused = false;
            if (!showPreviousButton)
            {
                Events["previousTextureEvent"].guiActive = false;
                Events["previousTextureEvent"].guiActiveEditor = false;
            }

        }
    }
}
