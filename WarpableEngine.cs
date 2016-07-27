using System;
using UnityEngine;
using KSP;
//4: 17 : m35 -60
//peri 169982
namespace IonWarp
{
	public class WarpableEngine : PartModule
	{
		private class EnginesWrapper
		{
			private ModuleEngines engines;
			private ModuleEnginesFX enginesFX;
			private bool isFX;
			public EnginesWrapper(Part thepart) {
				this.engines = thepart.FindModuleImplementing<ModuleEngines>();
				this.enginesFX = thepart.FindModuleImplementing<ModuleEnginesFX>();
				isFX = this.enginesFX != null;
			}
			public float minThrust(){
				if (isFX) {
					return this.enginesFX.minThrust;
				} else {
					return this.engines.minThrust;
				}
			}
			public float maxThrust(){
				if (isFX) {
					return this.enginesFX.maxThrust;
				} else {
					return this.engines.maxThrust;
				}
			}
			public System.Collections.Generic.List<UnityEngine.Transform> thrustTransforms(){
				if (isFX) {
					return this.enginesFX.thrustTransforms;
				} else {
					return this.engines.thrustTransforms;
				}
			}
			public double RequestPropellant(double mass){
				if (isFX) {
					return this.enginesFX.RequestPropellant(mass);
				} else {
					return this.engines.RequestPropellant(mass);
				}
			}
			public float realIsp(){
				if (isFX) {
					return this.enginesFX.realIsp;
				} else {
					return this.engines.realIsp;
				}
			}
			public void startEffects(){
				if (isFX) {
					this.enginesFX.ActivatePowerFX ();
				} else {
					this.engines.ActivateRunningFX ();
				}
			}
		}
		//Right-click menu GUI control for how much throttle to use while timewarping
		[KSPField(guiActive = true, guiName = "Time Warp Throttle", isPersistant=true), 
		 UI_FloatRange(controlEnabled = true, maxValue=1f, minValue=0f, stepIncrement = .01f,scene = UI_Scene.Flight)]
		public float timeWarpThrottle = 0; 

		//This is the ship we're attached to
		public Vessel theShip = null;

		//This is the engine module for the engine we're in
		private EnginesWrapper theEngines = null;

		//Whether the user wants us to activate engines during time warp
		[KSPField(guiActive = false, isPersistant = true)]
		public bool timeWarpEnabled = false;

		public bool flag = true;
	
		[KSPAction("Enable engines while warping")]
		public void enableWarpEnginesAction(KSPActionParam something) {
			enableWarpEngines ();
		}

		[KSPEvent(guiActive = true, guiName = "Activate engines while warping",active = true)]
		public void enableWarpEngines() {
			timeWarpEnabled = true;
			updateEvents ();

		}

		[KSPAction("Disable engines while warping")]
		public void disableWarpEnginesAction(KSPActionParam something) {
			disableWarpEngines ();
		}

		[KSPEvent(guiActive = true, guiName="Deactivate engines while warping",active = true)]
		public void disableWarpEngines() {
			timeWarpEnabled = false;
			updateEvents ();
		}

		[KSPAction("Toggle engine warp")]
		public void toggleWarpEngines(KSPActionParam something) {
			timeWarpEnabled = !timeWarpEnabled;
			updateEvents ();
		}

		[KSPEvent(guiActive = true,guiName = "PERFORM TEST",active=true)]
		public void testEffects() {
			theEngines.startEffects ();
		}

		public void updateEvents() {
			//Debug.Log ("ALL the spam.");
			Events ["enableWarpEngines"].active = !timeWarpEnabled;
			Events ["disableWarpEngines"].active = timeWarpEnabled;
		}

		public override void OnStart(StartState state) {
			//Debug.Log ("evenmorespam");
			timeWarpThrottle = 0;
			theEngines = new EnginesWrapper (part);
			theShip = part.vessel;
			updateEvents ();
		}
		public void FixedUpdate() {
			OnFixedUpdate ();
		}
		public override void OnFixedUpdate() {
			//Debug.Log ("spamspam");
			if (this.timeWarpEnabled && (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRate > TimeWarp.MaxPhysicsRate)) {
				//If the time warp engine mode is enabled and we're in time warp, then do the thing.
				//Also, if the engine is out of fuel, don't do anything.
				float thrust = theEngines.minThrust () + (theEngines.maxThrust () - theEngines.minThrust ()) * timeWarpThrottle;
				Vector3d accDirection = new Vector3d (); // The direction we accelerate in
				//if (theShip.Autopilot.Enabled) {
			//		switch (theShip.Autopilot.Mode) {
			//		case VesselAutopilot.AutopilotMode.Prograde:
			//			accDirection = theShip.GetObtVelocity();
			/*			break;
					case VesselAutopilot.AutopilotMode.Retrograde:
						accDirection = -1 * theShip.GetObtVelocity ();
						break;
					case VesselAutopilot.AutopilotMode.Normal:
						accDirection = -1*theShip.GetOrbit().GetOrbitNormal().xzy;
						break;
					case VesselAutopilot.AutopilotMode.Antinormal:
						accDirection = theShip.GetOrbit().GetOrbitNormal().xzy;
						break;
					case VesselAutopilot.AutopilotMode.RadialIn:
						accDirection = theShip.GetOrbit().;
						break;
					case VesselAutopilot.AutopilotMode.RadialOut:
						accDirection = theShip.GetObtVelocity ();
						break;
					default:
						foreach (Transform tf in theEngines.thrustTransforms) {
							accDirection += -1 * tf.forward;
						}
					}

				}else{*/
					foreach (Transform tf in theEngines.thrustTransforms()) {
						accDirection += -1 * tf.forward;
					}
			//	}
				accDirection = accDirection.normalized;
				Vector3d thrustVec = accDirection * thrust;
				Vector3d acceleration = thrustVec / theShip.GetTotalMass ();
				Debug.Log (acceleration);
				Debug.Log (theShip.GetTotalMass());
				Vector3d deltaV = acceleration * TimeWarp.deltaTime;

				double fuelMassUsed = theShip.GetTotalMass() - 1/((Math.Exp(deltaV.magnitude / (9.8066 * theEngines.realIsp())))/theShip.GetTotalMass());
				//That ^ should be equivalent to enginesMassUseRate * deltaTime
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

