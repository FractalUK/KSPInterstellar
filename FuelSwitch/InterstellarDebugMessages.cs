using System.Collections.Generic;
using UnityEngine;

namespace InterstellarFuelSwitch
{
    public class InterstellarDebugMessages : MonoBehaviour
    {
        public bool debugMode = true;
        private List<debugLine> outputLines = new List<debugLine>();
        //private float nextPostDuration = 5f;
        public Rect screenPosition = new Rect(500f, 500f, 300f, 100f);
        public float lineSpacing = 25f;
        public string moduleName = string.Empty;
        //public string debugMessageOutput = "both";
        public enum OutputMode
        {
            screen,
            log,
            both,
            none,
        }
        OutputMode outputMode = OutputMode.log;
        public float postToScreenDuration = 5f;

        public InterstellarDebugMessages()
        {
        }

        public InterstellarDebugMessages(bool _debugMode, string _moduleName)
        {
            debugMode = _debugMode;
            outputMode = OutputMode.log;
            moduleName = _moduleName + ": ";
        }

        public InterstellarDebugMessages(bool _debugMode, OutputMode _outputMode, float _postToScreenDuration)
        {
            debugMode = _debugMode;
            outputMode = _outputMode;
            postToScreenDuration = _postToScreenDuration;
        }

        public void Log(string input)
        {
            debugMessage(input);
        }

        public void debugMessage(object input) // fully automatic mode, posts to screen or or log depending on general setting
        {
            if (!debugMode) return;

            switch (outputMode)
            {
                case OutputMode.both:
                    debugMessage(input, true, postToScreenDuration);
                    break;
                case OutputMode.log:
                    debugMessage(input, true, 0f);
                    break;
                case OutputMode.screen:
                    debugMessage(input, false, postToScreenDuration);
                    break;
            }
        }

        public void debugMessage(object input, bool postToLog, float postToScreenDuration) // fully manual mode, post to screen or log depending on parameters
        {
            if (!debugMode) return;
            
            PostMessage(input, postToLog, postToScreenDuration);
        }

        public void debugMessage(object input, float postToScreenDuration) // semi-manual mode: posts to screen regardless of general setting, and to log, depending on general setting
        {
            if (!debugMode) return;

            switch (outputMode)
            {
                case OutputMode.both:
                    debugMessage(input, true, postToScreenDuration);
                    break;
                case OutputMode.log:
                    debugMessage(input, true, postToScreenDuration);
                    break;
                case OutputMode.screen:
                    debugMessage(input, false, postToScreenDuration);
                    break;
            }
        }

        public void PostMessage(object input, bool postToLog, float postToScreenDuration) // Posts uninstantiated, so it doesn't care about debugMode.
        {
            if (postToLog)
                Debug.Log(moduleName + input);
            if (postToScreenDuration > 0f) // will only work in the flight scene, gives an error in other places.
            {
                outputLines.Add(new debugLine(input.ToString(), postToScreenDuration));
                //nextPostDuration = postToScreenDuration;
            }
        }

        public void OnGUI()
        {
            if (outputLines.Count <= 0) return;
            
            for (int i = 0; i < outputLines.Count; i++)
            {
                GUI.Label(new Rect(screenPosition.x, screenPosition.y + lineSpacing * i, screenPosition.width, screenPosition.height), outputLines[i].text);
                outputLines[i].delay -= Time.deltaTime;
                if (outputLines[i].delay <= 0f)
                {
                    outputLines.RemoveAt(i);
                    i--;
                }
            }
        }


        public static void Post(string input, bool postToLog, float postToScreenDuration) // Posts uninstantiated, so it doesn't care about debugMode.
        {
            if (postToLog)
                Debug.Log(input);
            if (postToScreenDuration > 0f && HighLogic.LoadedSceneIsFlight) // will only work in the flight scene, gives an error in other places.
                ScreenMessages.PostScreenMessage(new ScreenMessage(input, postToScreenDuration, ScreenMessageStyle.UPPER_RIGHT));
        }
    }

    public class debugLine
    {
        public string text;
        public float delay;

        public debugLine(string _text, float _delay)
        {
            text = _text;
            delay = _delay;
        }
    }
}