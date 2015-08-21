using UnityEngine;

namespace FNPlugin
{
    public class InterstellarResourceScienceModule : ModuleScienceExperiment
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float resourceAmount;//{ get; set; }

        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;//{get; set;}

        [KSPField(isPersistant = true, guiActive = false)]
        public bool generatorActive;

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
        //[KSPField(isPersistant = true, guiActive = false)]
        //public string experimentID ;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorActivateName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorDeactivateName;


        [KSPField(isPersistant = true, guiActive = false)]
        public string currentBiome = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public bool needSubjects = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimation = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public int crewCount;
        [KSPField(isPersistant = false, guiActive = false)]
        public float loopPoint;
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
            if (startAnimationName != "")
            {
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
        }
        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg


            {

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

            //this.Events["Deploy"].guiActive = false;
            Events["activateGenerator"].guiName = generatorActivateName;
            Events["deActivateGenerator"].guiName = generatorDeactivateName;

            if (generatorActive)
                PlayAnimation("Deploy", false, true, false);
            else
                PlayAnimation("Deploy", true, true, false);

            base.OnStart(state);
        }

        public override void OnUpdate()
        {
            int lcrewCount = part.protoModuleCrew.Count;
            if (generatorActive)
            {
                if (loopPoint != 0) //loop the animation from this point, if 0, dont loop
                    PlayAnimation("Deploy", false, false, true);
                Events["deActivateGenerator"].guiActive = true;
                Events["activateGenerator"].guiActive = false;
                //while the generator is active... update the resource based on how much game time passed
                double dt = TimeWarp.deltaTime;
                // print("part has crews!" + part.protoModuleCrew.Count.ToString());
                if ((part.protoModuleCrew.Count == part.CrewCapacity && needSubjects) || !needSubjects)
                {
                    // double budget = getResourceBudget(generatorResourceInName);
                    // print(budget.ToString());
                    // if (budget > 0)
                    {
                        double spent = part.RequestResource(generatorResourceInName, generatorResourceIn * dt);
                        //  print(spent.ToString());
                        double generatescale = spent / (generatorResourceIn * dt);
                        if (generatorResourceIn == 0)
                            generatescale = 1;
                        double generated = part.RequestResource(generatorResourceOutName, -generatorResourceOut * dt * generatescale);
                        //  print("generated " + generated.ToString());
                        if (generated == 0) //if we didn't generate anything then we're full, refund the spent resource
                            part.RequestResource(generatorResourceInName, -spent);

                    }




                }
            }
            else
            {
                Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = true;
            }
            string biome = BiomeCheck();
            if (biome != currentBiome || lcrewCount != crewCount)
            {
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
                print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    print("vessel has resources!");
                    print(resources[i].info.name);
                    print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        print("Found the resouce!!");
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
            print("Clicked event! check data: " + resourceName + " " + resourceAmount.ToString() + " " + experimentID + " ");
            if (vesselHasEnoughResource(resourceName, resourceAmount))
            {

                print("Has the amount!!");
                double res = part.RequestResource(resourceName, resourceAmount, ResourceFlowMode.ALL_VESSEL);
                print("got " + res.ToString() + "resources");


                base.DeployExperiment();

                //  ReviewDataItem(data);


            }
            else
            {
                ScreenMessage smg = new ScreenMessage("Not Enough Data Stored", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(smg);
                print("not enough data stored");
            }
            print("Deploying Experiment");
            print("resourcename, resource amount " + resourceName + " " + resourceAmount.ToString());


        }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam actParams)
        {
            print("Clicked event! check data: " + resourceName + " " + resourceAmount.ToString() + " " + experimentID + " ");
            if (vesselHasEnoughResource(resourceName, resourceAmount))
            {

                print("Has the amount!!");
                double res = part.RequestResource(resourceName, resourceAmount, ResourceFlowMode.ALL_VESSEL);
                print("got " + res.ToString() + "resources");


                base.DeployAction(actParams);

                //  ReviewDataItem(data);


            }
            else
            {
                ScreenMessage smg = new ScreenMessage("Not Enough Data Stored", 4.0f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(smg);
                print("not enough data stored");
            }
            print("Deploying Experiment");
            print("resourcename, resource amount " + resourceName + " " + resourceAmount.ToString());


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
        [KSPEvent(guiName = "Reset", active = true, guiActive = true)]
        new public void ResetExperiment()
        {
            // refundResource();
            base.ResetExperiment();
        }
        [KSPEvent(guiName = "Reset", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false)]
        new public void ResetExperimentExternal()
        {
            //  refundResource();
            base.ResetExperimentExternal();
        }

        [KSPAction("Reset")]
        new public void ResetAction(KSPActionParam actParams)
        {
            //refundResource();
            base.ResetAction(actParams);
        }

    }

    public class GeneratorEX : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float resourceAmount;//{ get; set; }

        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;//{get; set;}

        [KSPField(isPersistant = true, guiActive = false)]
        public bool generatorActive;

        //consume this resource per game-second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn2;
        //produce this resource per game second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut2;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName;

        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName2;


        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceOutName;

        //[KSPField(isPersistant = true, guiActive = false)]
        //public string experimentID ;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorActivateName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorDeactivateName;

        [KSPField(isPersistant = true, guiActive = false)]
        public string currentBiome = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public bool needSubjects = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimation = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public int crewCount;
        [KSPField(isPersistant = false, guiActive = false)]
        public float loopPoint;
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
            if (startAnimationName != "")
            {
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

        }
        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg


            {

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
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void activateGenerator()
        {
            generatorActive = true;
            PlayAnimation(loopingAnimation, false, false, false);


        }
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void deActivateGenerator()
        {
            generatorActive = false;
            PlayAnimation(loopingAnimation, true, true, false);
        }

        public override void OnStart(PartModule.StartState state)
        {

            //this.Events["Deploy"].guiActive = false;
            Events["activateGenerator"].guiName = generatorActivateName;
            Events["deActivateGenerator"].guiName = generatorDeactivateName;

            if (generatorActive)
                PlayAnimation(loopingAnimation, false, true, false);
            else
                PlayAnimation(loopingAnimation, true, true, false);

            base.OnStart(state);
        }

        public override void OnUpdate()
        {
            int lcrewCount = part.protoModuleCrew.Count;
            if (generatorActive)
            {
                Events["deActivateGenerator"].guiActive = true;
                Events["activateGenerator"].guiActive = false;
                //while the generator is active... update the resource based on how much game time passed
                double dt = TimeWarp.deltaTime;

                //generating a resource!

                double generatescale = 1;
                double spent = 0;
                double spent2 = 0;
                if (generatorResourceIn != 0)
                {
                    spent = part.RequestResource(generatorResourceInName, generatorResourceIn * dt);
                    generatescale = spent / (generatorResourceIn * dt);
                }
                //the smallest of the 2 generated resource requests is what we can make.
                //we'll refund resource in the case where one doesn't spend enough (as indicated by generatescale)
                if (generatorResourceIn2 != 0)
                {
                    spent2 = part.RequestResource(generatorResourceInName2, generatorResourceIn2 * dt);
                    generatescale = System.Math.Min(spent2 / (generatorResourceIn * dt), generatescale);
                }

                //  print(spent.ToString());


                double generated = part.RequestResource(generatorResourceOutName, -generatorResourceOut * dt * generatescale);
                // double refundScale = generatescale; //percentage of refund to generate
                //if the percentage generated is less than the percentage spent then that becomes the refund
                // refundScale = System.Math.Min(refundScale, generated / (-generatorResourceOut * dt * generatescale));
                //  print("generated " + generated.ToString());
                if (generated == 0) //if we didn't generate anything then we're full, refund the spent resource
                {
                    part.RequestResource(generatorResourceInName, -spent);
                    part.RequestResource(generatorResourceInName2, -spent2);
                }







            }
            else
            {
                Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = true;
            }

            base.OnUpdate();

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
                print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    print("vessel has resources!");
                    print(resources[i].info.name);
                    print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        print("Found the resouce!!");
                        if (resources[i].amount >= resourceAmount)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }




    }

}
