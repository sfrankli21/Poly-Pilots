using AerodynamicObjects.Aerodynamics;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Aerodynamic Objects/Flow/Lifting Body Wake")]
    public class LiftingBodyWake : MonoBehaviour
    {
        AeroObject liftingBody;
        LiftModel liftModel;
        public float circulation;
        VortexFilament boundFilament;

        public float filamentNodeMaxDistance = 1f;
        public float trailingVortexLifespan = 10f;

        // Start is called before the first frame update
        void Start()
        {
            liftingBody = GetComponent<AeroObject>();
            liftModel = liftingBody.GetModel<LiftModel>();

            // We need to set up the vortex filaments for the bound vortex and the two trailing vortex filaments
            CreateBoundVortex();

            // Ignore the bound vortex
            liftingBody.IgnoreInteraction(boundFilament);
        }

        void CreateBoundVortex()
        {
            boundFilament = CreateFlowPrimitiveTools.CreateVortexFilament(transform.TransformPoint(new Vector3(-0.5f * liftingBody.relativeDimensions.x, 0)), transform.TransformPoint(new Vector3(-0.5f * liftingBody.relativeDimensions.x, 0)));
            boundFilament.name = "Bound Vortex Filament";
        }

        Vector3 quarterChordPosition;
        Vector3 spanTipPosition;

        void AlignBoundVortex()
        {
            //quarterChordPosition = liftModel.TransformBodyToLocal(new Vector3(liftModel.aerodynamicCentre_z * liftModel.sinBeta, 0, liftModel.aerodynamicCentre_z * liftModel.cosBeta));
            //quarterChordPosition = liftingBody.transform.position + liftingBody.transform.TransformDirection(quarterChordPosition);

            spanTipPosition = -0.5f * liftModel.resolvedSpan * Vector3.Normalize(Vector3.Cross(liftingBody.localRelativeVelocity, liftModel.aerodynamicLoad.force));// liftModel.TransformBodyToLocal(new Vector3(0.5f * liftModel.resolvedSpan * liftModel.cosBeta, 0, -0.5f * liftModel.resolvedSpan * liftModel.sinBeta));
            spanTipPosition = liftingBody.transform.TransformDirection(spanTipPosition);

            boundFilament.startNode.transform.position = liftingBody.transform.position - spanTipPosition;
            boundFilament.endNode.transform.position = liftingBody.transform.position + spanTipPosition;
            //boundFilament.startNode.transform.position = quarterChordPosition - spanTipPosition;
            //boundFilament.endNode.transform.position = quarterChordPosition + spanTipPosition;
        }

        VortexNode portNode, starboardNode;
        VortexFilament portFilament, starboardFilament;

        void FixedUpdate()
        {
            GetCirculation();

            AlignBoundVortex();

            boundFilament.initialStrength = -circulation;

            if (portNode == null)
            {
                SpawnPortFilament();
            }
            else if (Vector3.Distance(portNode.transform.position, boundFilament.startNode.transform.position) > filamentNodeMaxDistance)
            {
                AppendPortFilament();
            }

            if (starboardNode == null)
            {
                SpawnStarboardFilament();
            }
            else if (Vector3.Distance(starboardNode.transform.position, boundFilament.endNode.transform.position) > filamentNodeMaxDistance)
            {
                AppendStarboardFilament();
            }
        }

        private void AppendPortFilament()
        {
            VortexFilament newFilament = CreateFlowPrimitiveTools.AppendFilament(boundFilament.startNode, boundFilament.startNode.transform.position, true, true, circulation, 0.01f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) }), trailingVortexLifespan);
            newFilament.IgnoreInteraction(liftingBody);
            // This needs to be a function on the filaments to add/remove nodes
            portFilament.startNode.RemoveConnection();
            portFilament.startNode = newFilament.endNode;
            portFilament.startNode.AddConnection();
            portFilament = newFilament;
            portNode = portFilament.endNode;
        }

        private void SpawnPortFilament()
        {
            portFilament = CreateFlowPrimitiveTools.AppendFilament(boundFilament.startNode, boundFilament.startNode.transform.position, true, true, circulation, 0.01f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) }), trailingVortexLifespan);
            portNode = portFilament.endNode;
            portFilament.IgnoreInteraction(liftingBody);
        }

        private void AppendStarboardFilament()
        {
            VortexFilament newFilament = CreateFlowPrimitiveTools.AppendFilament(boundFilament.endNode, boundFilament.endNode.transform.position, true, true, -circulation, 0.01f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) }), trailingVortexLifespan);
            newFilament.IgnoreInteraction(liftingBody);

            // This needs to be a function on the filaments to add/remove nodes
            starboardFilament.startNode.RemoveConnection();
            starboardFilament.startNode = newFilament.endNode;
            starboardFilament.startNode.AddConnection();
            starboardFilament = newFilament;
            starboardNode = starboardFilament.endNode;
        }

        private void SpawnStarboardFilament()
        {
            starboardFilament = CreateFlowPrimitiveTools.AppendFilament(boundFilament.endNode, boundFilament.endNode.transform.position, true, true, -circulation, 0.01f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) }), trailingVortexLifespan);
            starboardNode = starboardFilament.endNode;
            starboardFilament.IgnoreInteraction(liftingBody);
        }

        void GetCirculation()
        {
            // Circulation is lift per unit span / (rho * v)
            //circulation = 1f / (liftModel.resolvedSpan * liftingBody.fluid.density * liftingBody.relativeVelocity.magnitude) * liftModel.aerodynamicLoad.force.magnitude;
            if (liftingBody.dynamicPressure == 0f)
            {
                circulation = 0f;
                return;
            }

            circulation = 1f / (liftingBody.fluid.density * liftingBody.relativeVelocity.magnitude) * liftModel.aerodynamicLoad.force.magnitude;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(quarterChordPosition, 0.1f);
        }
    }
}
