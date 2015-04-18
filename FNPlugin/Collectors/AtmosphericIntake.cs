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

        public override void OnStart(PartModule.StartState state)
        {
            Transform intakeTransform = part.FindModelTransform(intakeTransformName);
            _intake_direction = intakeTransform != null ? intakeTransform.forward.normalized : Vector3.forward;
            _resource = PartResourceLibrary.Instance.GetDefinition(InterstellarResourcesConfiguration.Instance.IntakeAtmosphere);
        }

        public void FixedUpdate()
        {
            if (vessel == null)
                return;

            airSpeed = (float)vessel.srf_velocity.magnitude + maxIntakeSpeed;

            float intakeAngle = Mathf.Clamp(Vector3.Dot((Vector3)vessel.srf_velocity.normalized, _intake_direction), 0, 1);
            float intakeExposure = (intakeAngle > aoaThreshold ? intakeAngle * (airSpeed * unitScalar + maxIntakeSpeed) : maxIntakeSpeed) * area * unitScalar;
            airFlow = ((float)vessel.atmDensity) * intakeExposure / _resource.density;
            float airThisUpdate = airFlow * TimeWarp.fixedDeltaTime;
            part.ImprovedRequestResource(_resource.name, -airThisUpdate);
        }

    }
}
