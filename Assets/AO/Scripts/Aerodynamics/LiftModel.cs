using System;
using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// The model used to determine lift and lift induced drag acting on an object. Also calculates pitching moment from camber and control surfaces.
    /// As well as the moment produced by the lift acting at the centre of pressure instead of the object's centre.
    /// Uses thin aerofoil theory and a stall model based on the normal force model, with aspect ratio and thickness to ratio corrections to model lift on a complete range of geometries.
    /// </summary>
    [Serializable]
    public class LiftModel : IAerodynamicModel
    {
        public float angleOfAttack;
        public float alpha_0, alphaForStall, effectiveAlpha;
        public float sinBeta, cosBeta;
        float cosAngleToVelocity;

        float span, thickness, chord;
        public float resolvedSpan, resolvedChord;
        Vector3 bodyRelativeVelocity;
        float lateralBodyVelocityMagnitude;
        Vector3 angleOfAttackRotationVector;
        Quaternion bodyRotation, bodyInverseRotation;
        Vector3 bodyCamber;
        float planformArea;

        float aspectRatio;

        /// <summary>
        /// Aspect ratio correction is used to correct the lifting line theory for a finite span wing. (dimensionless)
        /// </summary>
        float aspectRatioCorrection;

        float thicknessToChordRatio;

        /// <summary>
        /// An empirical correction in the form of a Gaussian-like function that has value 1 at zero thickness
        /// and blends to zero as the thickness to chord ratio approaches 1. (dimensionless)
        /// </summary>
        float thicknessCorrection;

        /// <summary>
        /// Blending constant used in the thickness correction. Larger values will cause the lift produced by an object
        /// to drop faster and for smaller thickness to chord ratios. Default value of 6. (dimensionless)
        /// </summary>
        public float thicknessCorrectionAggressiveness = 6f;

        /// <summary>
        /// The starting angle for blending between pre and post stall.
        /// At this angle of attack, the object will begin to stall.
        /// Default value of 0.261799 (15 deg). (radians)
        /// </summary>
        public float stallAngleMin = 0.261799f;

        /// <summary>
        /// How far along the mean aerodynamic chord of the object the aerodynamic centre is positioned at zero angle of attack.
        /// The aerodynamic centre position is blended to zero at 90 degree angle of attack, placing it at the centre of the object's dimensions.
        /// Expressed as a fraction of the mean aerodynamic chord. Default value of 0.25 (dimensionless)
        /// </summary>
        public float aerodynamicCentrePositionAtZeroAlpha = 0.25f;

        /// <summary>
        /// The end angle for blending between pre and post stall.
        /// By this angle of attack, the object will have completely stalled.
        /// Default value of 0.610865 (35 deg). (radians)
        /// </summary>
        public float stallAngleMax = 0.610865f;

        /// <summary>
        /// How abruptly the blending between pre and post stall occurs.
        /// A large value will produce a sharp transition from pre stall levels of lift to post stall levels.
        /// Some low order numerical methods might struggle with such sharp changes.
        /// Default value of 43. (dimensionless)
        /// </summary>
        public float stallSharpness = 43f;

        /// <summary>
        /// The angle at which the object will stall. Depends on an empirical relation in the model.
        /// Stall leads to a large decrease in the lift an object produces. (degrees)
        /// </summary>
        public float stallAngle;

        /// <summary>
        /// Upper and lower sigmoid are used to blend between the lift coefficient pre and post stall.
        /// </summary>
        public float upperSigmoid;

        /// <summary>
        /// Upper and lower sigmoid are used to blend between the lift coefficient pre and post stall.
        /// </summary>
        public float lowerSigmoid;

        /// <summary>
        /// The lift coefficient of the object. (dimensionless)
        /// </summary>
        public float CL;

        /// <summary>
        /// The maximum normal coefficient of a flat plate. Usually set as 1 in the literature. (dimensionless)
        /// </summary>
        public float CZmax = 1f;

        /// <summary>
        /// The lift curve slope is the rate of change of the lift coefficient with respect to angle of attack.
        /// (dimensionless)
        /// </summary>
        public float liftCurveSlope;

        /// <summary>
        /// The lift coefficient of the object before stall is considered. (dimensionless)
        /// </summary>
        public float CL_preStall;

        /// <summary>
        /// The lift coefficient of the object after stall is considered. (dimensionless)
        /// </summary>
        public float CL_postStall;

        /// <summary>
        /// The lift induced drag coefficient of the object. (dimensionless)
        /// </summary>
        public float CD_induced;

        /// <summary>
        /// The overall pitching moment coefficient of the object. (dimensionless)
        /// </summary>
        public float CM;

        /// <summary>
        /// The pitching moment coefficient of the object due to camber. (dimensionless)
        /// </summary>
        public float CM_0;

        /// <summary>
        /// The pitching moment coefficient of the object due to aerodynamic centre movement. (dimensionless)
        /// </summary>
        public float CM_delta;

        /// <summary>
        /// The distance of the aerodynamic centre from the object's centre. This is the point at which the lift and induced drag forces act.
        /// With CM_0 == 0 the aerodynamic centre is identical to the centre of pressure. (m)
        /// </summary>
        public float aerodynamicCentre_z;

        /// <summary>
        /// This is the largest dimension of the object, including the group dimensions. We use this when calculating
        /// the effective aspect ratio of the object as panels on a wing need to use the aspect ratio of the entire wing
        /// to determine their aspect ratio correction - not their individual aspect ratios!
        /// </summary>
        public float groupSpan;

        public float preStallFilter;
        public float CZMax = 1;
        Vector3 bodyVelocityDirection;
        Vector3 liftDirection;
        float qS;
        Vector3 lift_bodyFrame, inducedDrag_bodyFrame;
        Vector3 resultantForce_bodyFrame, moment_bodyFrame;
        float cosAlpha;
        ControlSurface surface;
        Vector3 controlSurfaceBodyDirection;
        float deltaAlpha;
        public float resolvedCamber;

        public AerodynamicLoad aerodynamicLoad;

        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            // Resolving the velocity and getting angles
            bodyRelativeVelocity = TransformLocalToBody(ao.localRelativeVelocity);
            bodyCamber = TransformLocalToBody(ao.camber);

            lateralBodyVelocityMagnitude = Mathf.Sqrt((bodyRelativeVelocity.x * bodyRelativeVelocity.x) + (bodyRelativeVelocity.z * bodyRelativeVelocity.z));

            // Need to do this to avoid divisions by zero
            if (lateralBodyVelocityMagnitude < 0.0000001f)
            {
                resolvedChord = chord;
                resolvedSpan = span;

                sinBeta = 0;
                cosBeta = 1;
                //cosAngleToVelocity = 0;
                // It doesn't matter what rotation vector we use here, the angle of attack is 90 degrees
                // so it's undefined as there is no horizontal velocity to define this vector
                angleOfAttackRotationVector = new Vector3(1, 0, 0);
                angleOfAttack = 0;

                // Need to do something here about camber, for now I'm just resolving camber into the body axes and getting some kind
                // of "overall" camber along the zero sideslip velocity direction
                if (((bodyCamber.x * bodyCamber.x) + (bodyCamber.z * bodyCamber.z)) == 0)
                {
                    resolvedCamber = 0;
                }
                else
                {
                    // I know this is a mess but I'm saving a bit of memory by caching with the same variable
                    // There should be a more elegant way to do this, but what I'm trying to do is preserve the signs of the cambers
                    // This method should combine a positive and negative camber, with a result of zero camber when c.x = -c.z and beta = 45 deg
                    resolvedCamber = (Mathf.Abs(bodyCamber.z) * bodyCamber.z * cosBeta * cosBeta) + (Mathf.Abs(bodyCamber.x) * bodyCamber.x * sinBeta * sinBeta);
                    resolvedCamber = Mathf.Sign(resolvedCamber) * Mathf.Sqrt(Mathf.Abs(resolvedCamber));
                }

                alpha_0 = -resolvedCamber / resolvedChord;
                alphaForStall = angleOfAttack;
                effectiveAlpha = alphaForStall - alpha_0;
            }
            else
            {
                sinBeta = bodyRelativeVelocity.x / lateralBodyVelocityMagnitude;
                cosBeta = bodyRelativeVelocity.z / lateralBodyVelocityMagnitude;

                // Resolve sideslip while keeping the planform area of the object the same
                // Cached some values here - 2 represents a squared value, consecutive values are products
                // e.g. chord2 = chord * chord, spanChord = span * chord
                resolvedSpan = Mathf.Sqrt(chord2 + (span2minusChord2 * cosBeta * cosBeta));
                resolvedChord = spanChord / resolvedSpan;

                angleOfAttackRotationVector = new Vector3(cosBeta, 0, -sinBeta);
                // Angle between the total horizontal velocity and vertical velocity
                angleOfAttack = -Mathf.Atan(bodyRelativeVelocity.y / lateralBodyVelocityMagnitude);

                // Need to do something here about camber, for now I'm just resolving camber into the body axes and getting some kind
                // of "overall" camber along the zero sideslip velocity direction
                if (((bodyCamber.x * bodyCamber.x) + (bodyCamber.z * bodyCamber.z)) == 0)
                {
                    resolvedCamber = 0;
                }
                else
                {
                    // I know this is a mess but I'm saving a bit of memory by caching with the same variable
                    // There should be a more elegant way to do this, but what I'm trying to do is preserve the signs of the cambers
                    // This method should combine a positive and negative camber, with a result of zero camber when c.x = -c.z and beta = 45 deg
                    resolvedCamber = (Mathf.Abs(bodyCamber.z) * bodyCamber.z * cosBeta * cosBeta) + (Mathf.Abs(bodyCamber.x) * bodyCamber.x * sinBeta * sinBeta);
                    resolvedCamber = Mathf.Sign(resolvedCamber) * Mathf.Sqrt(Mathf.Abs(resolvedCamber));
                }

                alpha_0 = -resolvedCamber / resolvedChord;
                alphaForStall = angleOfAttack;
                effectiveAlpha = alphaForStall - alpha_0;

                // Add the effects of any existing control surfaces
                for (int i = 0; i < ao.controlSurfaces.Count; i++)
                {
                    // Iterate over the control surfaces, get their directions resolved into equivalent body axes and then apply their effects
                    surface = ao.controlSurfaces[i];
                    controlSurfaceBodyDirection = TransformLocalToBody(surface.forwardAxis).normalized;
                    cosAngleToVelocity = ((controlSurfaceBodyDirection.x * bodyRelativeVelocity.x) + (controlSurfaceBodyDirection.z * bodyRelativeVelocity.z)) / lateralBodyVelocityMagnitude;

                    // If cos angle to velocity is greater than zero, we're facing the wind as a flap
                    // Otherwise, we're backwards to the wind and the surface should be treated as a slat
                    deltaAlpha = cosAngleToVelocity * surface.GetDeltaAlpha();

                    if (cosAngleToVelocity > 0)
                    {
                        // Flap affects the CL through effective alpha
                        effectiveAlpha += deltaAlpha;
                        alpha_0 -= deltaAlpha;
                    }
                    else
                    {
                        //  Slat affects alpha for stall
                        alphaForStall -= deltaAlpha;
                    }
                }
            }

            cosAlpha = Mathf.Cos(angleOfAttack);

            // Calculate planform

            switch (ao.referenceAreaShape)
            {
                case AeroObject.ReferenceAreaShape.Ellipse:
                    // We use 0.85 here as the mean aerodynamic chord for an ellipsoid body, this is important for objects like a frisbee
                    // 0.85 is because the mean aerodynamic chord of an ellipse is 0.85 * the length in the chord axis
                    aerodynamicCentre_z = aerodynamicCentrePositionAtZeroAlpha * 0.85f * resolvedChord * cosAlpha * cosAlpha;
                    planformArea = 0.25f * Mathf.PI * resolvedSpan * resolvedChord;
                    break;
                case AeroObject.ReferenceAreaShape.Rectangle:
                    aerodynamicCentre_z = aerodynamicCentrePositionAtZeroAlpha * resolvedChord * cosAlpha * cosAlpha;
                    planformArea = resolvedChord * resolvedSpan;
                    break;
                default:
                    break;
            }

            // We can't cache this unfortunately because it changes based on sideslip
            // This is the resolved group span (squared) divided by the planform area
            aspectRatio = (chord2 + ((groupSpan2 - chord2) * cosBeta * cosBeta)) / planformArea;

            //aspectRatio = (((groupSpan2 / planformArea) - (planformArea / groupSpan2)) * cosBeta * cosBeta) + (planformArea / groupSpan2);

            // Prandtl Theory
            // Use a better aspect ratio correction for low aspect ratios. Note if AR < 0.35 we ignore the linear portion of lift altogether!
            if (aspectRatio < 2)
            {
                aspectRatioCorrection = aspectRatio / 4f;
            }
            else
            {
                aspectRatioCorrection = aspectRatio / (2 + aspectRatio);
            }

            // We need to do something about large thickness to chord ratios as that also implies the lifting plane needs to be able to switch and use the thickness as a chord
            // Empirical correction to account for viscous effects across all thickness to chord ratios
            thicknessToChordRatio = thickness / resolvedChord;
            thicknessCorrection = Mathf.Exp(-thicknessCorrectionAggressiveness * thicknessToChordRatio * thicknessToChordRatio);

            // Emperical relation to allow for viscous effects
            // This could do with being in radians!
            stallAngle = stallAngleMin + ((stallAngleMax - stallAngleMin) * Mathf.Exp(-aspectRatio / 2f));

            // Thin airfoil theory
            liftCurveSlope = 2f * Mathf.PI * aspectRatioCorrection * thicknessCorrection;

            if (aspectRatio > 0.35f)
            {

                // Lift before and after stall
                CL_preStall = liftCurveSlope * effectiveAlpha;

                // This could be simplified based on sin(2a) == 2sin(a)cos(a) but we're using effective alpha, not geometric alpha
                CL_postStall = 0.5f * CZMax * thicknessCorrection * Mathf.Sin(2f * effectiveAlpha);

                // Sigmoid function for blending between pre and post stall
                // REMOVED EFFECTIVE ALPHA HERE
                upperSigmoid = 1f / (1f + Mathf.Exp((alphaForStall + stallAngle) * stallSharpness));
                lowerSigmoid = 1f / (1f + Mathf.Exp((alphaForStall - stallAngle) * stallSharpness));
                preStallFilter = lowerSigmoid - upperSigmoid;

                CL = (preStallFilter * CL_preStall) + ((1 - preStallFilter) * CL_postStall);

            }
            else
            {
                // Assigning all values here just for consistency
                // We're assuming the object has a pure normal force model at this low value of aspect ratio
                preStallFilter = 0;
                CL_preStall = 0.5f * CZMax * thicknessCorrection * Mathf.Sin(2f * effectiveAlpha);
                CL_postStall = CL_preStall;
                CL = CL_preStall;
            }

            // Induced drag - needs some kind of fader for low AR
            CD_induced = CL * CL / (Mathf.PI * aspectRatio);

            // Calculate the direction of lift based on the angle of attack rotation vector and the wind direction
            // Note that lift acts in the normal direction to the wind
            bodyVelocityDirection = Vector3.Normalize(bodyRelativeVelocity);
            liftDirection = Vector3.Normalize(Vector3.Cross(bodyVelocityDirection, angleOfAttackRotationVector));

            // Convert coefficients to forces
            qS = ao.dynamicPressure * planformArea;
            lift_bodyFrame = qS * CL * liftDirection;
            inducedDrag_bodyFrame = -qS * CD_induced * bodyVelocityDirection;

            // ============================================================
            // We use CDi to change the direction of the lift but not to change its magnitude!
            // I.e.
            // L' = L + Di
            // L actual = Normalise(L') * Magnitude(L)
            // ============================================================
            // Changes to lift calculation start here:
            resultantForce_bodyFrame = lift_bodyFrame + inducedDrag_bodyFrame;
            // Magnitude of the lift force is: 1/2 rho V^2 S CL
            resultantForce_bodyFrame = Math.Abs(CL) * qS * Vector3.Normalize(resultantForce_bodyFrame);

            // Note that now the induced drag does not change the magnitude of the force, it only
            // changes the direction of the force. There is probably a faster way to compute this
            // but for now it will suffice to normalise the resulting vector!

            // This might be better off as
            // CM_delta = (CL * cosAlpha + CD_induced * sinAlpha) * CoP_z / ao.equivalentBody.chord;
            CM_delta = CL * aerodynamicCentre_z * cosAlpha / resolvedChord;

            // Cm0 should only be active during pre stall conditions!
            // New model for camber and Cm0
            //CM_0 = alpha_0;
            //CM_0 = 0.25f * liftCurveSlope * alpha_0 * preStallFilter;
            CM_0 = alpha_0 * preStallFilter;
            CM = CM_0 + CM_delta;

            // I worry about this version, my moments aren't great
            // Notice the minus sign in the quaternion! That's because we're resolving back out of the sideslip aligned axes
            //moment_bodyFrame = Vector3.Transform(new Vector3(qS * ebChord * CM, 0, 0), new Quaternion(0, -Math.Sin(ao.AngleOfSideslip / 2f), 0, Math.Cos(ao.AngleOfSideslip / 2f)));
            moment_bodyFrame = new Vector3(-qS * resolvedChord * CM * cosBeta, 0, qS * resolvedChord * CM * sinBeta);

            // Return the force in the object's local frame of reference, NOT the body frame of reference!
            aerodynamicLoad = new AerodynamicLoad
            {
                // Transform the force from the body frame to the local frame
                moment = TransformBodyToLocal(moment_bodyFrame),
                // Transform the force from the body frame to the local frame
                force = TransformBodyToLocal(resultantForce_bodyFrame)
            };

            return aerodynamicLoad;
        }

        public Vector3 GetLocalAerodynamicCentre()
        {
            return TransformBodyToLocal(new Vector3(aerodynamicCentre_z * sinBeta, 0, aerodynamicCentre_z * cosBeta));
        }

        float x, y, z;
        float chord2, span2, span2minusChord2, spanChord;
        float groupSpan2;
        public void UpdateDimensionValues(AeroObject ao)
        {
            // Bear with me here...
            // We are using the group dimensions of the aero object to determine the orientation of the span thickness chord axes
            // But then we need to store the regular dimensions of the object in that frame of reference
            // This is to get past the case where a wing is created with panels which have a chord larger than their individual span
            // but then we store the group span dimension separately for use in calculating the effective aspect ratio of the panels
            x = ao.groupDimensions.x;
            y = ao.groupDimensions.y;
            z = ao.groupDimensions.z;

            /* The frames of reference and their notations in this model are:
                * 
                *  - Global (Earth) Frame: the global coordinates which are fixed for
                *  all intents and purposes.
                *  
                *  - Local Frame: has position and rotation equal to the aerodynamic
                *  object's position and rotation.
                *  
                *  - Body Frame: is a rotation relative to the local frame
                *  such that (x, y, z) are aligned with (span, thickness, chord)
                *  Thickness chord and span are selected in order of ascending
                *  dimensions of the object, i.e. span >= chord >= thickness
                */

            // The normal to the lifting plane is aligned with the minor axis (thickness) of the ellipsoid,
            // The span is aligned to X and chord to Z
            // Coordinates are aligned so that [x, y, z] == [span, thickness, chord]

            // The order of the following checks ensures that if x == y == z then they become (span, thickness, chord)

            // The rotation needed to line up the regular x y z axes
            // with the span thickness and chord convention
            bodyRotation = Quaternion.identity;

            if (x >= y)
            {
                if (y > z)
                {
                    // Then x > y > z and we need to swap thickness and chord
                    groupSpan = x;
                    SetSpanThicknessChord(ao.dimensions.x, ao.dimensions.z, ao.dimensions.y);
                    bodyRotation = new Quaternion(0, 0.7071068f, 0.7071068f, 0);
                }
                else
                {
                    if (x >= z)
                    {
                        // Then x > z > y so we don't need to do anything
                        groupSpan = x;
                        SetSpanThicknessChord(ao.dimensions.x, ao.dimensions.y, ao.dimensions.z);
                    }
                    else
                    {
                        // Then z > x > y so we need to swap chord and span
                        groupSpan = z;
                        SetSpanThicknessChord(ao.dimensions.z, ao.dimensions.y, ao.dimensions.x);
                        bodyRotation = new Quaternion(0, 0.7071068f, 0, 0.7071068f);
                    }
                }
            }
            else
            {
                // y > x
                if (y >= z)
                {
                    if (x >= z)
                    {
                        // Then y > x > z
                        groupSpan = y;
                        SetSpanThicknessChord(ao.dimensions.y, ao.dimensions.z, ao.dimensions.x);
                        bodyRotation = new Quaternion(0.5f, 0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        // Then y > z > x
                        groupSpan = y;
                        SetSpanThicknessChord(ao.dimensions.y, ao.dimensions.x, ao.dimensions.z);
                        bodyRotation = new Quaternion(0, 0, -0.7071068f, 0.7071068f);
                    }
                }
                else
                {
                    // Then z > y > x
                    groupSpan = z;
                    SetSpanThicknessChord(ao.dimensions.z, ao.dimensions.x, ao.dimensions.y);
                    bodyRotation = new Quaternion(0.5f, 0.5f, 0.5f, -0.5f);
                }
            }

            // Rotate the reference axes to line up with (span, thickness, chord) == (x, y, z)
            SetBodyRotation(bodyRotation);

            // Now we need to calculate our constant values, though it seems they are all dependent on sideslip...
            chord2 = chord * chord;
            span2 = span * span;
            span2minusChord2 = span2 - chord2;
            spanChord = span * chord;
            groupSpan2 = groupSpan * groupSpan;
        }

        private void SetSpanThicknessChord(float span, float thickness, float chord)
        {
            this.span = span;
            this.thickness = thickness;
            this.chord = chord;
        }

        /// <summary>
        /// Sets the rotation of the body frame of reference relative to the local frame of reference.
        /// </summary>
        /// <param name="rotation">Quaternion rotation of the body frame of reference.</param>
        private void SetBodyRotation(Quaternion rotation)
        {
            bodyRotation = rotation;
            bodyInverseRotation = Quaternion.Inverse(rotation);
        }

        /// <summary>
        /// Rotates a vector by the quaternion rotation from the body frame of reference to the local frame of reference.
        /// </summary>
        /// <param name="vector">The vector given in the body frame of reference</param>
        /// <returns>The vector in the local frame of reference</returns>
        public Vector3 TransformBodyToLocal(Vector3 vector)
        {
            return bodyRotation * vector;
        }

        /// <summary>
        /// Rotates a vector by the quaternion 
        /// rotation from the local frame of reference to the body frame of reference.
        /// </summary>
        /// <param name="vector">The vector given in the local frame of reference</param>
        /// <returns>The vector in the body frame of reference</returns>
        public Vector3 TransformLocalToBody(Vector3 vector)
        {
            return bodyInverseRotation * vector;
        }
    }
}
