extern alias ORSv1_4_3;
using ORSv1_4_3::OpenResourceSystem;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FNPlugin  {
    class AtmosphericIntake : PartModule
    {
        protected Vector3 _intake_direction;
        protected PartResourceDefinition _resource;
        protected float _intake_speed;

        [KSPField(guiName = "Atm Flow", guiUnits = "U", guiFormat = "F2", isPersistant = false, guiActive = false)]
        public float airFlow;
        [KSPField(guiName = "Atm Speed", guiUnits = "M/s", guiFormat = "F2", isPersistant = false, guiActive = false)]
        public float airSpeed;
        [KSPField(isPersistant = false)]
        public float aoaThreshold = 0.1f;
        [KSPField(isPersistant = false)]
        public float area;
        [KSPField(isPersistant = false)]
        public string intakeTransformName;
        [KSPField(isPersistant = false)]
        public float maxIntakeSpeed = 100;
        [KSPField(isPersistant = false)]
        public float unitScalar = 0.2f;
        [KSPField(isPersistant = false)]
        public bool useIntakeCompensation = true;
        [KSPField(isPersistant = false)]
        public bool storesResource = false;

        public override void OnStart(PartModule.StartState state)
        {
            Transform intakeTransform = part.FindModelTransform(intakeTransformName);
            if (intakeTransform == null)
                Debug.Log("[KSPI] AtmosphericIntake unable to get intake transform for " + part.name);
            _intake_direction = intakeTransform != null ? intakeTransform.forward.normalized : Vector3.forward;
            _resource = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            airSpeed = (float)vessel.srf_velocity.magnitude + _intake_speed;

            float intakeAngle = Mathf.Clamp(Vector3.Dot((Vector3)vessel.srf_velocity.normalized, _intake_direction), 0, 1);
            float intakeExposure = (intakeAngle > aoaThreshold ? intakeAngle * (airSpeed * unitScalar + _intake_speed) : _intake_speed) * area * unitScalar;
            airFlow = ((float)vessel.atmDensity) * intakeExposure / _resource.density;
            double airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;
            if (!storesResource)
            {
                foreach (PartResource resource in part.Resources)
                {
                    if (resource.resourceName != _resource.name)
                        continue;

                    airThisUpdate = airThisUpdate >= 0 ? (airThisUpdate <= resource.maxAmount ? airThisUpdate : resource.maxAmount) : 0;
                    resource.amount = airThisUpdate;
                    break;
                }
            }
            else
                part.ImprovedRequestResource(_resource.name, -airThisUpdate);

            if (!useIntakeCompensation)
                return;

            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName != _resource.name)
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
