using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin  
{
    class AtmosphericIntake : PartModule
    {
        protected Vector3 _intake_direction;
        protected PartResourceDefinition _resourceAtmosphere;

        [KSPField(guiName = "Intake Speed", isPersistant = false, guiActive = false)]
        protected float _intake_speed;
        [KSPField(guiName = "Atm Flow", guiUnits = "U", guiFormat = "F2", isPersistant = false, guiActive = false)]
        public float airFlow;
        [KSPField(guiName = "Atm Speed", guiUnits = "M/s", guiFormat = "F2", isPersistant = false, guiActive = false)]
        public float airSpeed;
        [KSPField(guiName = "Air This Update", isPersistant = false, guiActive = false)]
        public float airThisUpdate;
        [KSPField(guiName = "intake Angle", isPersistant = false, guiActive = false)]
        public float intakeAngle = 0;

        [KSPField(guiName = "AoA TreshHold", isPersistant = false, guiActive = false)]
        public float aoaThreshold = 0.1f;
        [KSPField(isPersistant = false, guiName = "Area", guiActiveEditor = true, guiActive = false)]
        public float area;
        [KSPField(isPersistant = false)]
        public string intakeTransformName;
        [KSPField(isPersistant = false, guiName = "max Intake Speed", guiActive = false, guiActiveEditor = false)]
        public float maxIntakeSpeed = 100;
        [KSPField(isPersistant = false, guiName = "Unit Scalar", guiActive = false, guiActiveEditor = false)]
        public float unitScalar = 0.2f;
        [KSPField(isPersistant = false)]
        public bool useIntakeCompensation = true;
        [KSPField(isPersistant = false)]
        public bool storesResource = false;
        [KSPField(isPersistant = false, guiName = "Intake Exposure", guiActiveEditor = true, guiActive = false)]
        public float intakeExposure = 0;

        public override void OnStart(PartModule.StartState state)
        {
            Transform intakeTransform = part.FindModelTransform(intakeTransformName);
            if (intakeTransform == null)
                Debug.Log("[KSPI] AtmosphericIntake unable to get intake transform for " + part.name);
            _intake_direction = intakeTransform != null ? intakeTransform.forward.normalized : Vector3.forward;
            _resourceAtmosphere = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);

            // ToDo: connect with atmospheric intake to readout updated area
            // ToDo: change density of air to 
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            airSpeed = (float)vessel.srf_velocity.magnitude + _intake_speed;

            intakeAngle = Mathf.Clamp(Vector3.Dot((Vector3)vessel.srf_velocity.normalized, _intake_direction), 0, 1);

            //intakeExposure = (intakeAngle < aoaThreshold 
            //    ? (1 - intakeAngle) * (airSpeed * unitScalar) + _intake_speed
            //    : (1 - intakeAngle) * (airSpeed * unitScalar) * (float)Math.Pow(aoaThreshold / intakeAngle, 2) + _intake_speed);
            
            intakeExposure = (airSpeed * unitScalar) + _intake_speed;
            intakeExposure *= area * unitScalar;
            airFlow = (float)vessel.atmDensity * intakeExposure / _resourceAtmosphere.density ;
            airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;

            if (!storesResource)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName != _resourceAtmosphere.name)
                        continue;

                    airThisUpdate = airThisUpdate >= 0 ? (airThisUpdate <= resource.maxAmount ? airThisUpdate : (float)resource.maxAmount) : 0;
                    resource.amount = airThisUpdate;
                    break;
                }
            }
            else
                part.ImprovedRequestResource(_resourceAtmosphere.name, -airThisUpdate);

            if (!useIntakeCompensation)
                return;

            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName != _resourceAtmosphere.name)
                    continue;

                if (airThisUpdate > resource.amount && resource.amount != 0.0)
                    _intake_speed = Mathf.Lerp(_intake_speed, 0f, TimeWarp.fixedDeltaTime);
                else
                    _intake_speed = Mathf.Lerp(_intake_speed, maxIntakeSpeed, TimeWarp.fixedDeltaTime);

                break;
            }
        }

    }
}
