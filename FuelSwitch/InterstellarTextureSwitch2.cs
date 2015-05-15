using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    public class InterstellarTextureSwitch2 : PartModule
    {
        [KSPField]
        public int moduleID = 0;
        [KSPField]
        public string textureRootFolder = string.Empty;
        [KSPField]
        public string objectNames = string.Empty;
        [KSPField]
        public string textureNames = string.Empty;
        [KSPField]
        public string mapNames = string.Empty;
        [KSPField]
        public string textureDisplayNames = "Default";

        [KSPField]
        public string nextButtonText = "Next Texture";
        [KSPField]
        public string prevButtonText = "Previous Texture";
        [KSPField]
        public string statusText = "Current Texture";

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
        //[KSPField]
        //public Vector4 GUIposition = new Vector4(FSGUIwindowID.standardRect.x, FSGUIwindowID.standardRect.y, FSGUIwindowID.standardRect.width, FSGUIwindowID.standardRect.height);
        [KSPField]
        public bool showPreviousButton = true;
        [KSPField]
        public bool useFuelSwitchModule = false;
        [KSPField]
        public string fuelTankSetups = "0";
        [KSPField]
        public bool showInfo = true;
        [KSPField]
        public bool updateSymmetry = true;

        private List<Transform> targetObjectTransforms = new List<Transform>();
        private List<List<Material>> targetMats = new List<List<Material>>();
        private List<String> texList = new List<string>();
        private List<String> mapList = new List<string>();
        private List<String> objectList = new List<string>();
        private List<String> textureDisplayList = new List<string>();
        private List<int> fuelTankSetupList = new List<int>();
        private InterstellarFuelSwitch fuelSwitch;

        private bool initialized = false;

        InterstellarDebugMessages debug;

        [KSPField(guiActiveEditor = true, guiName = "Current Texture")]
        public string currentTextureName = string.Empty;

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
            if (selectedTexture >= texList.Count && selectedTexture >= mapList.Count)
                selectedTexture = 0;
            useTextureAll(true);
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Previous Texture")]
        public void previousTextureEvent()
        {
            selectedTexture--;
            if (selectedTexture < 0)
                selectedTexture = Mathf.Max(texList.Count - 1, mapList.Count - 1);
            useTextureAll(true);
        }

        [KSPEvent(guiActiveUnfocused = true, unfocusedRange = 5f, guiActive = false, guiActiveEditor = false, guiName = "Repaint")]
        public void nextTextureEVAEvent()
        {
            nextTextureEvent();
        }

        public void useTextureAll(bool calledByPlayer)
        {
            applyTexToPart(calledByPlayer);

            if (!updateSymmetry) return;

            for (int i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                // check that the moduleID matches to make sure we don't target the wrong tex switcher
                InterstellarTextureSwitch2[] symSwitch = part.symmetryCounterparts[i].GetComponents<InterstellarTextureSwitch2>();
                for (int j = 0; j < symSwitch.Length; j++)
                {
                    if (symSwitch[j].moduleID == moduleID)
                    {
                        symSwitch[j].selectedTexture = selectedTexture;
                        symSwitch[j].applyTexToPart(calledByPlayer);
                    }
                }
            }

        }

        private void applyTexToPart(bool calledByPlayer)
        {
            initializeData();
            foreach (List<Material> matList in targetMats)
            {
                foreach (Material mat in matList)
                {
                    useTextureOrMap(mat);
                }
            }
            if (useFuelSwitchModule)
            {
                debug.debugMessage("calling on InterstellarFuelSwitch tank setup " + selectedTexture);
                if (selectedTexture < fuelTankSetupList.Count)
                    fuelSwitch.SelectTankSetup(fuelTankSetupList[selectedTexture], calledByPlayer);
                else
                    debug.debugMessage("no such fuel tank setup");
            }
        }

        public void useTextureOrMap(Material targetMat)
        {
            if (targetMat != null)
            {
                useTexture(targetMat);

                useMap(targetMat);
            }
            else
                debug.debugMessage("No target material in object.");
        }

        private void useMap(Material targetMat)
        {
            debug.debugMessage("maplist count: " + mapList.Count + ", selectedTexture: " + selectedTexture + ", texlist Count: " + texList.Count);
            if (mapList.Count > selectedTexture)
            {
                if (GameDatabase.Instance.ExistsTexture(mapList[selectedTexture]))
                {
                    debug.debugMessage("map " + mapList[selectedTexture] + " exists in db");
                    targetMat.SetTexture(additionalMapType, GameDatabase.Instance.GetTexture(mapList[selectedTexture], mapIsNormal));
                    selectedMapURL = mapList[selectedTexture];

                    if (selectedTexture < textureDisplayList.Count && texList.Count == 0)
                    {
                        currentTextureName = textureDisplayList[selectedTexture];
                        debug.debugMessage("setting currentTextureName to " + textureDisplayList[selectedTexture]);
                    }
                    else
                        debug.debugMessage("not setting currentTextureName. selectedTexture is " + selectedTexture + ", texDispList count is" + textureDisplayList.Count + ", texList count is " + texList.Count);
                }
                else
                {
                    debug.debugMessage("map " + mapList[selectedTexture] + " does not exist in db");
                }
            }
            else
            {
                if (mapList.Count > selectedTexture) // why is this check here? will never happen.
                    debug.debugMessage("no such map: " + mapList[selectedTexture]);
                else
                {
                    debug.debugMessage("useMap, index out of range error, maplist count: " + mapList.Count + ", selectedTexture: " + selectedTexture);
                    for (int i = 0; i < mapList.Count; i++)
                    {
                        debug.debugMessage("map " + i + ": " + mapList[i]);
                    }
                }
            }
        }

        private void useTexture(Material targetMat)
        {
            if (texList.Count <= selectedTexture) return;

            if (GameDatabase.Instance.ExistsTexture(texList[selectedTexture]))
            {
                debug.debugMessage("assigning texture: " + texList[selectedTexture]);
                targetMat.mainTexture = GameDatabase.Instance.GetTexture(texList[selectedTexture], false);
                selectedTextureURL = texList[selectedTexture];

                if (selectedTexture > textureDisplayList.Count - 1)
                    currentTextureName = getTextureDisplayName(texList[selectedTexture]);
                else
                    currentTextureName = textureDisplayList[selectedTexture];
            }
            else
                debug.debugMessage("no such texture: " + texList[selectedTexture]);

        }

        public override string GetInfo()
        {
            if (showInfo)
            {
                List<string> variantList;
                if (textureNames.Length > 0)
                    variantList = ParseTools.ParseNames(textureNames);
                else
                    variantList = ParseTools.ParseNames(mapNames);

                textureDisplayList = ParseTools.ParseNames(textureDisplayNames);
                StringBuilder info = new StringBuilder();
                info.AppendLine("Alternate textures available:");
                if (variantList.Count == 0)
                {
                    if (variantList.Count == 0)
                        info.AppendLine("None");
                }
                for (int i = 0; i < variantList.Count; i++)
                {
                    if (i > textureDisplayList.Count - 1)
                        info.AppendLine(getTextureDisplayName(variantList[i]));
                    else
                        info.AppendLine(textureDisplayList[i]);
                }
                info.AppendLine("\nUse the Next Texture button on the right click menu.");
                return info.ToString();
            }
            else
                return string.Empty;
        }

        private string getTextureDisplayName(string longName)
        {
            string[] splitString = longName.Split('/');
            return splitString[splitString.Length - 1];
        }

        public override void OnStart(PartModule.StartState state)
        {
            initializeData();

            useTextureAll(false);

            if (switchableInFlight) Events["nextTextureEvent"].guiActive = true;
            if (switchableInFlight && showPreviousButton) Events["previousTextureEvent"].guiActive = true;
            if (showListButton) Events["listAllObjects"].guiActiveEditor = true;
            if (!repaintableEVA) Events["nextTextureEVAEvent"].guiActiveUnfocused = false;
            if (!showPreviousButton)
            {
                Events["previousTextureEvent"].guiActive = false;
                Events["previousTextureEvent"].guiActiveEditor = false;
            }

            Events["nextTextureEvent"].guiName = nextButtonText;
            Events["previousTextureEvent"].guiName = prevButtonText;
            Fields["currentTextureName"].guiName = statusText;
        }

        // runs the kind of commands that would normally be in OnStart, if they have not already been run. In case a method is called upon externally, but values have not been set up yet
        private void initializeData()
        {
            if (initialized) return;

            debug = new InterstellarDebugMessages(debugMode, "InterstellarTextureSwitch2");
            // you can't have fuel switching without symmetry, it breaks the editor GUI.
            if (useFuelSwitchModule) updateSymmetry = true;

            objectList = ParseTools.ParseNames(objectNames, true);
            texList = ParseTools.ParseNames(textureNames, true, true, textureRootFolder);
            mapList = ParseTools.ParseNames(mapNames, true, true, textureRootFolder);
            textureDisplayList = ParseTools.ParseNames(textureDisplayNames);
            fuelTankSetupList = ParseTools.ParseIntegers(fuelTankSetups);

            debug.debugMessage("found " + texList.Count + " textures, using number " + selectedTexture + ", found " + objectList.Count + " objects, " + mapList.Count + " maps");

            foreach (String targetObjectName in objectList)
            {
                Transform[] targetObjectTransformArray = part.FindModelTransforms(targetObjectName);
                List<Material> matList = new List<Material>();
                foreach (Transform t in targetObjectTransformArray)
                {
                    if (t != null && t.gameObject.renderer != null) // check for if the object even has a mesh. otherwise part list loading crashes
                    {
                        Material targetMat = t.gameObject.renderer.material;
                        if (targetMat != null && !matList.Contains(targetMat))
                            matList.Add(targetMat);
                    }
                }
                targetMats.Add(matList);
            }

            if (useFuelSwitchModule)
            {
                fuelSwitch = part.GetComponent<InterstellarFuelSwitch>(); // only looking for first, not supporting multiple fuel switchers
                if (fuelSwitch == null)
                {
                    useFuelSwitchModule = false;
                    debug.debugMessage("no InterstellarFuelSwitch module found, despite useFuelSwitchModule being true");
                }
            }
            initialized = true;

        }
    }
}
