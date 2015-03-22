using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin {
    class ModuleModableScienceGenerator : FNResourceSuppliableModule, IScienceDataContainer {
        [KSPField(isPersistant = true)]
        public bool Deployed;
        [KSPField(isPersistant = true)]
        public string result_string;
        [KSPField(isPersistant = true)]
        public  string result_title;
        [KSPField(isPersistant = true)]
        public float transmit_value;
        [KSPField(isPersistant = true)]
        public float recovery_value;
        [KSPField(isPersistant = true)]
        public float data_size;
        [KSPField(isPersistant = true)]
        public float xmit_scalar;
        [KSPField(isPersistant = true)]
        public float ref_value;
        [KSPField(isPersistant = true)]
        public bool data_gend = false;

        [KSPField(isPersistant = false)]
        public bool rerunnable;
        [KSPField(isPersistant = false)]
        public string deployEventName;
        [KSPField(isPersistant = false)]
        public string reviewEventName;
        [KSPField(isPersistant = false)]
        public string resetEventName;
        [KSPField(isPersistant = false)]
        public string experimentID;

        protected ScienceData science_data;

        protected ModableExperimentResultDialogPage merdp;
        

        [KSPEvent(guiName = "Deploy", active = true, guiActive = true)]
	    public void DeployExperiment() {
            data_gend = generateScienceData();
            ReviewData();
            Deployed = true;
            cleanUpScienceData();
	    }

        [KSPAction("Deploy")]
        public void DeployAction(KSPActionParam actParams) {
            DeployExperiment();
        }

        [KSPEvent(guiName = "Reset", active = true, guiActive = true)]
        public void ResetExperiment() {
            if (science_data != null) {
                DumpData(science_data);
            }
            Deployed = false;
        }

        [KSPAction("Reset")]
        public void ResetAction(KSPActionParam actParams) {
            ResetExperiment();
        }

        [KSPEvent(guiName = "Review Data", active = true, guiActive = true)]
        public void ReviewData() {
            if (science_data != null) {
                if (merdp == null || !data_gend) {
                    ExperimentsResultDialog.DisplayResult(merdp = new ModableExperimentResultDialogPage(base.part, this.science_data, this.science_data.transmitValue, 0, false, "", true, false, new Callback<ScienceData>(this.endExperiment), new Callback<ScienceData>(this.keepData), new Callback<ScienceData>(this.sendDataToComms), new Callback<ScienceData>(this.sendDataToLab)));
                    merdp.setUpScienceData(result_title, result_string, transmit_value, recovery_value, data_size, xmit_scalar, ref_value);
                } else {
                    ExperimentsResultDialog.DisplayResult(merdp);
                }
            } else {
                ResetExperiment();
            }
        }

        public override void OnStart(PartModule.StartState state) {
            if (state == StartState.Editor) { return; }
        }

        public override void OnSave(ConfigNode node) {
            if (science_data != null) {
                ConfigNode science_node = node.AddNode("ScienceData");
                science_data.Save(science_node);
            }
        }

        public override void OnLoad(ConfigNode node) {
            if (node.HasNode("ScienceData")) {
                ConfigNode science_node = node.GetNode("ScienceData");
                science_data = new ScienceData(science_node);
            }
        }

        public override void OnUpdate() {
            Events["DeployExperiment"].guiName = deployEventName;
            Events["ResetExperiment"].guiName = resetEventName;
            Events["ReviewData"].guiName = reviewEventName;
            Events["DeployExperiment"].active = !Deployed;
            Events["ResetExperiment"].active = Deployed;
            Events["ReviewData"].active = Deployed;
            Actions["DeployAction"].guiName = deployEventName;

            if (science_data == null) {
                Deployed = false;
            }
        }

        public bool IsRerunnable() {
            return rerunnable;
        }

        public int GetScienceCount() {
            if (science_data != null) {
                return 1;
            }
            return 0;
        }

        public ScienceData[] GetData() {
            if (science_data != null) {
                return new ScienceData[] { science_data };
            } else {
                return new ScienceData[0];
            }
        }

        

        public void ReviewDataItem(ScienceData science_data) {
            if (science_data == this.science_data) {
                ReviewData();
            }
        }

        public void DumpData(ScienceData science_data) {
            if (science_data == this.science_data) {
                this.science_data = null;
                merdp = null;
                result_string = null;
                result_title = null;
                transmit_value = 0;
                recovery_value = 0;
                Deployed = false;
            }
        }

        protected void endExperiment(ScienceData science_data) {
            
            DumpData(science_data);
            
        }

        protected void sendDataToComms(ScienceData science_data) {
            List<IScienceDataTransmitter> list = base.vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (list.Any<IScienceDataTransmitter>() && science_data != null && data_gend) {
                merdp = null;
                List<ScienceData> list2 = new List<ScienceData>();
                list2.Add(science_data);
                list.OrderBy(new Func<IScienceDataTransmitter, float>(ScienceUtil.GetTransmitterScore)).First<IScienceDataTransmitter>().TransmitData(list2);
                endExperiment(science_data);
                
            }
        }

        protected void sendDataToLab(ScienceData science_data) {

        }

        protected void keepData(ScienceData science_data) {

        }

        protected virtual bool generateScienceData() {
            return false;
        }

        protected virtual void cleanUpScienceData() {

        }
        
    }
}
