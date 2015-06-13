using System;
using KSP;
using UnityEngine;


namespace IonWarp
{
	public class WarpableEngineFX : PartModule
	{
		//Right-click menu GUI control for how much throttle to use while timewarping
		[KSPField(guiActive = true, guiName = "Time Warp Throttle", isPersistant=true), 
		 UI_FloatRange(controlEnabled = true, maxValue=1f, minValue=0f, stepIncrement = .01f,scene = UI_Scene.Flight)]
		public float timeWarpThrottle = 0; 

		//This is the ship we're attached to
		public Vessel theShip = null;

		//This is the engine module for the engine we're in
		public ModuleEnginesFX theEngines = null;

		//Whether the user wants us to 
		[KSPField(guiActive = false, isPersistant = true)]
		public bool timeWarpEnabled = false;

		[KSPEvent(guiActive = true, guiName = "Activate engines while warping",active = true),
		 KSPAction("Enable engines while warping")]
		public void enableWarpEngines() {
			timeWarpEnabled = true;
			updateEvents ();

		}

		[KSPEvent(guiActive = true, guiName="Deactivate engines while warping",active = true),
		 KSPAction("Disable engines while warping")]
		public void disableWarpEngines() {
			timeWarpEnabled = false;
			updateEvents ();
		}

		[KSPAction("Toggle engine warp")]
		public void toggleWarpEngines() {
			timeWarpEnabled = !timeWarpEnabled;
			updateEvents ();
		}

		public void updateEvents() {
			Events ["enableWarpEngines"].active = !timeWarpEnabled;
			Events ["disableWarpEngines"].active = timeWarpEnabled;
		}

		public override void OnStart(StartState state) {
			timeWarpThrottle = 0;
			theEngines = part.FindModuleImplementing<ModuleEnginesFX>();
			theShip = part.vessel;
			updateEvents ();
		}
		public override void OnFixedUpdate() {
			if (this.timeWarpEnabled && (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > TimeWarp.MaxPhysicsRate)
			    && !(theEngines.flameout || theEngines.engineShutdown)) {
				//If the time warp engine mode is enabled and we're in time warp, then do the thing.
				//Also, if the engine is out of fuel, don't do anything.
				float thrust = theEngines.minThrust + (theEngines.maxThrust-theEngines.minThrust)*timeWarpThrottle;
				Vector3d accDirection = new Vector3d (); // The direction we accelerate in
				/*
				if (theShip.Autopilot.Enabled) {
					switch (theShip.Autopilot.Mode) {
					case VesselAutopilot.AutopilotMode.Prograde:
						accDirection = theShip.GetOrbit().
						break;
					default:
						foreach (Transform tf in theEngines.thrustTransforms) {
							accDirection += -1 * tf.forward;
						}
						accDirection = accDirection.normalized;
					}

				}else{*/
				foreach (Transform tf in theEngines.thrustTransforms) {
					accDirection += -1 * tf.forward;
				}
				accDirection = accDirection.normalized;
				Vector3d thrustVec = accDirection * thrust;
				Vector3d acceleration = thrustVec / theShip.GetTotalMass ();
				Vector3d deltaV = acceleration * TimeWarp.deltaTime;
				double fuelMassUsed = theShip.GetTotalMass() - 1/((Math.Exp(deltaV.magnitude / (9.8066 * theEngines.realIsp)))/theShip.GetTotalMass());
				double engineRequestResult = theEngines.RequestPropellant (fuelMassUsed);
				if (engineRequestResult > 0.5) {
					theShip.orbit.UpdateFromStateVectors (theShip.orbit.getRelativePositionAtUT (Planetarium.GetUniversalTime ()),
					                                      theShip.orbit.vel + deltaV.xzy,
					                                      theShip.orbit.referenceBody,
					                                      Planetarium.GetUniversalTime ());
				} else {
					disableWarpEngines ();
				}
			}
		}
	}
}

