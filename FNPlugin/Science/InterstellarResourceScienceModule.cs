using UnityEngine;

namespace FNPlugin
{
    public class InterstellarResourceScienceModule : ModuleScienceExperiment
    {
        [KSPField(isPersistant = true, guiActive = true, guiName = "Active")]
        public bool generatorActive;
        [KSPField(isPersistant = true)]
        public float last_active_time;
        [KSPField(isPersistant = true)]
        public float lastGeneratedPerSecond;

        [KSPField(isPersistant = false, guiActive = false)]
        public float resourceAmount;
        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;
        [KSPField(isPersistant = true, guiActive = true, guiName = "Generated Data", guiFormat = "F3")]
        public float totalGeneratedData;

        //consume this resource per game-second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn;
        //produce this resource per game second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut;

        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceOutName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorActivateName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorDeactivateName;


        [KSPField(isPersistant = true, guiActive = true, guiName = "Biodome" )]
        public string currentBiome = "";
        [KSPField(isPersistant = false, guiActive = false, guiName = "Research")]
        public float research;

        [KSPField(isPersistant = false, guiActive = false)]
        public bool needSubjects = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimation = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public int crewCount;
        [KSPField(isPersistant = false, guiActive = false)]
        public float loopPoint;

        [KSPField(isPersistant = false, guiActiveEditor = true,  guiActive = false, guiName = "Mass", guiUnits = " t")]
        public float partMass;



        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void activateGenerator()
        {
            generatorActive = true;
            PlayAnimation("Deploy", false, false, false);
        }

        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void deActivateGenerator()
        {
            generatorActive = false;
            PlayAnimation("Deploy", true, true, false);
        }

        public override void OnStart(PartModule.StartState state)
        {
            UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - OnStart " + state.ToString());

            //this.Events["Deploy"].guiActive = false;
            Events["activateGenerator"].guiName = generatorActivateName;
            Events["deActivateGenerator"].guiName = generatorDeactivateName;

            if (generatorActive)
                PlayAnimation("Deploy", false, true, false);
            else
                PlayAnimation("Deploy", true, true, false);

            base.OnStart(state);

            // calcualte time past since last frame
            if (generatorActive && last_active_time > 0)
            {
                double time_diff = Planetarium.GetUniversalTime() - last_active_time;
                
                var minutes = time_diff / 60;
                UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - time difference " + minutes + " minutes");
                ScreenMessages.PostScreenMessage("Generated Science Data for " + minutes.ToString("0.00") + " minutes", 5.0f, ScreenMessageStyle.LOWER_CENTER);

                GenerateScience(time_diff, true);
            }
        }

        public override void OnUpdate()
        {
            // store current time in case vesel is unloaded
            last_active_time = (float)Planetarium.GetUniversalTime();

            int lcrewCount = part.protoModuleCrew.Count;
            if (generatorActive)
            {
                if (loopPoint != 0) //loop the animation from this point, if 0, dont loop
                    PlayAnimation("Deploy", false, false, true);

                Events["deActivateGenerator"].guiActive = true;
                Events["activateGenerator"].guiActive = false;
                //while the generator is active... update the resource based on how much game time passed
                // print("part has crews!" + part.protoModuleCrew.Count.ToString());
                if ((part.protoModuleCrew.Count == part.CrewCapacity && needSubjects) || !needSubjects)
                {
                    // double budget = getResourceBudget(generatorResourceInName);
                    // print(budget.ToString());
                    // if (budget > 0)
                    //{
                    GenerateScience(TimeWarp.deltaTime, false);
                    //}
                }
            }
            else
            {
                Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = true;
            }
            string biome = BiomeCheck();
            if (biome != currentBiome || (needSubjects && lcrewCount != crewCount))
            {
                if (biome != currentBiome)
                    UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - reseting research because biome " + biome + " != biome " + currentBiome);
                else
                    UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - reseting research because lcrewCount " + lcrewCount + " !=  crewCount " + crewCount);

                print("biome change " + biome);
                currentBiome = biome;
                crewCount = lcrewCount;
                //reset collected data
                part.RequestResource(resourceName, resourceAmount);

            }
            if (loopingAnimation != "")
                PlayAnimation(loopingAnimation, false, false, true); //plays independently of other anims
            base.OnUpdate();
        }

        private void GenerateScience(double deltaTime, bool offlineCollecting)
        {
            if (deltaTime == 0) return;

            double spent;
            if (!offlineCollecting)
            {
                spent = part.RequestResource(generatorResourceInName, generatorResourceIn * deltaTime);
                lastGeneratedPerSecond = (float)(spent / deltaTime);
            }
            else
            {
                spent = lastGeneratedPerSecond * deltaTime;
                UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - available power: " + spent);
            }

            //  print(spent.ToString());
            double generatescale = spent / (generatorResourceIn * deltaTime);
            if (generatorResourceIn == 0)
                generatescale = 1;

            var generatedScience = generatorResourceOut * deltaTime * generatescale;
            if (offlineCollecting)
            {
                UnityEngine.Debug.Log("[KSPI] - InterstellarResourceScienceModule - generatedScience: " + generatedScience);
            }

            double generated = part.RequestResource(generatorResourceOutName, -generatedScience);

            totalGeneratedData = (float)part.Resources[generatorResourceOutName].amount;

            //  print("generated " + generated.ToString());
            if (generated == 0 && !offlineCollecting) //if we didn't generate anything then we're full, refund the spent resource
                part.RequestResource(generatorResourceInName, -spent);
        }

        public string BiomeCheck()
        {
            // bool flying = vessel.altitude < vessel.mainBody.maxAtmosphereAltitude;
            //bool orbiting = 

            //return "InspaceOver" + vessel.mainBody.name;

            string situation = vessel.RevealSituationString();
            if (situation.Contains("Landed") || situation.Contains("flight"))
                return FlightGlobals.currentMainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name + situation;
            return situation;
        }

        float getResourceBudget(string name)
        {
            //   
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // print("vessel has resources!");
                    print(resources[i].info.name);
                    // print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        // print("Found the resouce!!");
                        return (float)resources[i].amount;
                    }
                }
            }
            return 0;
        }
        bool vesselHasEnoughResource(string name, float rc)
        {
            //   
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    //print("vessel has resources!");
                    print(resources[i].info.name);
                    //print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        //print("Found the resouce!!");
                        if (resources[i].amount >= resourceAmount)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        new public void DumpData(ScienceData data)
        {
            // refundResource();
            base.DumpData(data);
        }

        [KSPEvent(guiName = "Deploy", active = true, guiActive = true)]
        new public void DeployExperiment()
        {
            //print("Clicked event! check data: " + resourceName + " " + resourceAmount.ToString() + " " + experimentID + " ");
            if (vesselHasEnoughResource(resourceName, resourceAmount))
            {
                //print("Has the possibleAmount!!");
                double res = part.RequestResource(resourceName, resourceAmount, ResourceFlowMode.ALL_VESSEL);
                //print("got " + res.ToString() + "resources");
                base.DeployExperiment();
                //  ReviewDataItem(data);
            }
            else
            {
                ScreenMessage smg = new ScreenMessage("Not Enough Data Stored", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(smg);
                print("not enough data stored");
            }
            //print("Deploying Experiment");
            //print("resourcename, resource possibleAmount " + resourceName + " " + resourceAmount.ToString());
        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam actParams)
        {
            //print("Clicked event! check data: " + resourceName + " " + resourceAmount.ToString() + " " + experimentID + " ");
            if (vesselHasEnoughResource(resourceName, resourceAmount))
            {

                //print("Has the possibleAmount!!");
                double res = part.RequestResource(resourceName, resourceAmount, ResourceFlowMode.ALL_VESSEL);
                //print("got " + res.ToString() + "resources");

                base.DeployAction(actParams);
                //  ReviewDataItem(data);
            }
            else
            {
                ScreenMessage smg = new ScreenMessage("Not Enough Data Stored", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(smg);
                print("not enough data stored");
            }
            //print("Deploying Experiment");
            //print("resourcename, resource possibleAmount " + resourceName + " " + resourceAmount.ToString());
        }

        //[KSPEvent(active = true, guiActive = true, guiName = "Review Data")]
        //new public void ReviewDataEvent()
        //{
        //    print("Reviewing Data");
        //    base.ReviewDataEvent();
        //}
        void refundResource()
        {
            print("refund resource!");
            double res = part.RequestResource(resourceName, -resourceAmount, ResourceFlowMode.ALL_VESSEL);
            print("refunded " + res.ToString() + " resource");
        }

        //[KSPEvent(guiName = "Reset", active = true, guiActive = true)]
        //new public void ResetExperiment()
        //{
        //    // refundResource();
        //    base.ResetExperiment();
        //}

        //[KSPEvent(guiName = "Reset", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false)]
        //new public void ResetExperimentExternal()
        //{
        //    //  refundResource();
        //    base.ResetExperimentExternal();
        //}

        //[KSPAction("Reset")]
        //new public void ResetAction(KSPActionParam actParams)
        //{
        //    //refundResource();
        //    base.ResetAction(actParams);
        //}

        private void PlayStartAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                if (speed < 0)
                {
                    StartAnimation[startAnimationName].time = StartAnimation[startAnimationName].length;
                    if (loopPoint != 0)
                        StartAnimation[startAnimationName].time = loopPoint;
                }
                if (instant)
                    StartAnimation[startAnimationName].speed = 999999 * speed;

                StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                StartAnimation[startAnimationName].speed = speed;
                StartAnimation.Play(startAnimationName);
            }
        }

        private void PlayLoopAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName == "") return;

            // print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
            if (StartAnimation[startAnimationName].time >= StartAnimation[startAnimationName].length || StartAnimation.isPlaying == false)
            {
                StartAnimation[startAnimationName].time = loopPoint;
                //print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
                if (instant)
                    StartAnimation[startAnimationName].speed = 999999 * speed;

                StartAnimation[startAnimationName].speed = speed;
                StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                StartAnimation.Play(startAnimationName);
            }
        }

        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg

            var anim = part.FindModelAnimators();

            foreach (Animation a in anim)
            {
                // print("animation found " + a.name + " " + a.clip.name);
                if (a.clip.name == name)
                {
                    // print("animation playingxx " + a.name + " " + a.clip.name);
                    var xanim = a;
                    if (loop)
                        PlayLoopAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                    else
                        PlayStartAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                }
            }
        }

    }



}
