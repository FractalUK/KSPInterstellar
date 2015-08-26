using System;
using System.Collections.Generic;
using UnityEngine;

namespace FNPlugin
{
    public class FnRcsSounds : PartModule
    {
        [KSPField]
        public string rcsSoundFile = "RcsSounds/Sounds/RcsHeavy";
        [KSPField]
        public string rcsShutoffSoundFile = "RcsSounds/Sounds/RcsHeavyShutoff";
        [KSPField]
        public float rcsVolume = 0.5f;
        [KSPField]
        public bool loopRcsSound = true;
        [KSPField]
        public bool internalRcsSoundsOnly = false;
        [KSPField]
        public bool useLightingEffects = true;

        public FXGroup RcsSound = null;
        public FXGroup RcsShutoffSound = null;
        private List<GameObject> RcsLights = new List<GameObject>();
        private bool Paused = false;

        private ModuleRCS _rcsModule = null;
        public ModuleRCS rcsModule
        {
            get
            {
                if (this._rcsModule == null)
                    //this._rcsModule = (ModuleRCS)this.part.Modules["ModuleRCS"];
                    this._rcsModule = this.part.FindModuleImplementing<ModuleRCS>();
                return this._rcsModule;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                if (state == StartState.Editor || state == StartState.None) return;

                // Works with squad sounds, not with rcsSoundFile.
                if (!GameDatabase.Instance.ExistsAudioClip(rcsSoundFile))
                {
                    Debug.LogError("RcsSounds: Audio file not found: " + rcsSoundFile);
                }

                if (RcsSound != null)
                {
                    RcsSound.audio = this.gameObject.AddComponent<AudioSource>();
                    RcsSound.audio.dopplerLevel = 0f;
                    RcsSound.audio.Stop();
                    RcsSound.audio.clip = GameDatabase.Instance.GetAudioClip(rcsSoundFile);
                    RcsSound.audio.loop = loopRcsSound;
                    // Seek to a random position in the sound file so we don't have 
                    // harmonic effects when burning at multiple RCS nozzles.
                    RcsSound.audio.time = UnityEngine.Random.Range(0, RcsSound.audio.clip.length);
                }
                else
                    Debug.LogError("RcsSounds: Sound FXGroup not found.");

                if (RcsShutoffSound != null)
                {
                    RcsShutoffSound.audio = gameObject.AddComponent<AudioSource>();
                    RcsShutoffSound.audio.dopplerLevel = 0f;
                    RcsShutoffSound.audio.Stop();
                    RcsShutoffSound.audio.clip = GameDatabase.Instance.GetAudioClip(rcsShutoffSoundFile);
                    RcsShutoffSound.audio.loop = false;
                }
                else
                    Debug.LogError("RcsSounds: Sound shuttof FXGroup not found.");

                if (useLightingEffects)
                    AddLights();

                GameEvents.onGamePause.Add(new EventVoid.OnEvent(OnPause));
                GameEvents.onGameUnpause.Add(new EventVoid.OnEvent(OnUnPause));
            }
            catch (Exception ex)
            {
                Debug.LogError("RcsSounds OnStart: " + ex.Message);
            }
        }

        public void OnDestroy()
        {
            GameEvents.onGamePause.Remove(new EventVoid.OnEvent(OnPause));
            GameEvents.onGameUnpause.Remove(new EventVoid.OnEvent(OnUnPause));
        }

        public void OnPause()
        {
            Paused = true;
            RcsSound.audio.Stop();
            RcsShutoffSound.audio.Stop();
        }

        public void OnUnPause()
        {
            Paused = false;
        }

        private float soundPitch = 1;
        private float soundVolume = 0;
        private bool previouslyActive = false;
        public override void OnUpdate()
        {
            try
            {
                if (!Paused && RcsSound != null && RcsSound.audio != null && RcsShutoffSound != null && RcsShutoffSound.audio != null)
                {
                    bool rcsActive = false;
                    float rcsHighestPower = 0f;

                    if (!internalRcsSoundsOnly || CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                    {
                        // Check for the resource as the effects still fire slightly without fuel.
                        var resourceList = new List<PartResource>();
                        ResourceFlowMode m;
                        try
                        {
                            m = (ResourceFlowMode)Enum.Parse(typeof(ResourceFlowMode), rcsModule.resourceFlowMode);
                        }
                        catch (Exception)
                        {
                            m = ResourceFlowMode.ALL_VESSEL;
                        }

                        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(rcsModule.resourceName).id,
                            m, resourceList);
                        double totalAmount = 0;
                        foreach (PartResource r in resourceList)
                            totalAmount += r.amount;

                        if (totalAmount >= 0.01) // 0.01 is the smallest amount shown in the resource menu.
                        {
                            for (int i = 0; i < rcsModule.thrusterFX.Count; i++)
                            {
                                rcsHighestPower = Mathf.Max(rcsHighestPower, rcsModule.thrusterFX[i].Power);
                                if (useLightingEffects)
                                {
                                    RcsLights[i].light.enabled = rcsModule.thrusterFX[i].Active;
                                    RcsLights[i].light.intensity = rcsModule.thrusterFX[i].Power;
                                    RcsLights[i].light.spotAngle = Mathf.Lerp(0, 45, rcsModule.thrusterFX[i].Power);
                                }
                            }
                            if (rcsHighestPower > 0.1f)
                                // Don't respond to SAS idling.
                                rcsActive = true;
                        }
                    }

                    if (rcsActive)
                    {
                        soundVolume = GameSettings.SHIP_VOLUME * rcsVolume * rcsHighestPower;
                        soundPitch = Mathf.Lerp(0.5f, 1f, rcsHighestPower);
                        RcsSound.audio.pitch = soundPitch;
                        RcsSound.audio.volume = soundVolume;
                        if (!RcsSound.audio.isPlaying)
                            RcsSound.audio.Play();
                        previouslyActive = true;
                    }
                    else
                    {
                        RcsSound.audio.Stop();
                        if (useLightingEffects)
                        {
                            for (int i = 0; i < rcsModule.thrusterFX.Count; i++)
                                RcsLights[i].light.enabled = false;
                        }
                        if (previouslyActive)
                        {
                            if (!internalRcsSoundsOnly ||
                                CameraManager.Instance.currentCameraMode == CameraManager.CameraMode.IVA)
                            {
                                RcsShutoffSound.audio.volume = soundVolume / 2;
                                RcsShutoffSound.audio.Play();
                            }
                            previouslyActive = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("RcsSounds Error OnUpdate: " + ex.Message);
            }
        }

        private void AddLights()
        {
            foreach (Transform t in rcsModule.thrusterTransforms)
            {
                GameObject rcsLight = new GameObject();
                rcsLight.AddComponent<Light>();
                rcsLight.light.color = Color.white;

                rcsLight.light.type = LightType.Spot;
                rcsLight.light.intensity = 1f;
                rcsLight.light.range = 2f;
                rcsLight.light.spotAngle = 45f;

                rcsLight.light.transform.parent = t;
                rcsLight.light.transform.position = t.transform.position;
                rcsLight.light.transform.forward = t.transform.up;
                rcsLight.light.enabled = false;
                rcsLight.AddComponent<MeshRenderer>();
                RcsLights.Add(rcsLight);
            }
        }
    }
}
