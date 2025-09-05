using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides a set of functions for creating various flow primitives.
    /// </summary>
    public static class CreateFlowPrimitiveTools
    {
        public static PointSource CreatePointSource()
        {
            GameObject go = new GameObject("Point Source");
            //GameObject go = CreateKinematicTriggerObject(Vector3.zero, Quaternion.identity, "Point Source");

            PointSource pointSource = go.AddComponent<PointSource>();
            //UnityEditorInternal.ComponentUtility.MoveComponentUp(pointSource); //move up
            //UnityEditorInternal.ComponentUtility.MoveComponentUp(pointSource); //and again

            return pointSource;
        }

        //public static VortexFilament CreateVortexFilament(Vector3 startPosition, Vector3 endPosition)
        //{
        //    VortexFilament filament = CreateUninitialisedVortexFilament(startPosition, endPosition);
        //    filament.Initialise();

        //    return filament;
        //}

        public static VortexFilament CreateVortexFilament(Vector3 startPosition, Vector3 endPosition, Transform parent = null)
        {
            VortexFilament filament = CreateUninitialisedVortexFilament(startPosition, endPosition, parent);
            filament.Initialise();

            return filament;
        }

        //private static VortexFilament CreateUninitialisedVortexFilament(Vector3 startPosition, Vector3 endPosition)
        //{
        //    VortexNode startNode = CreateVortexNode(startPosition, Quaternion.identity);
        //    VortexNode endNode = CreateVortexNode(endPosition, Quaternion.identity);

        //    GameObject go = new GameObject("Vortex Filament");
        //    go.transform.position = 0.5f * (startPosition + endPosition);

        //    startNode.transform.parent = go.transform;
        //    endNode.transform.parent = go.transform;

        //    GameObject filamentGo = new GameObject("Filament");
        //    VortexFilament filament = filamentGo.AddComponent<VortexFilament>();
        //    filamentGo.transform.SetParent(go.transform, false);

        //    filament.startNode = startNode;
        //    filament.endNode = endNode;
        //    startNode.AddConnection();
        //    endNode.AddConnection();

        //    return filament;
        //}

        private static VortexFilament CreateUninitialisedVortexFilament(Vector3 startPosition, Vector3 endPosition, Transform parent = null)
        {
            VortexNode startNode = CreateVortexNode(startPosition, Quaternion.identity);
            VortexNode endNode = CreateVortexNode(endPosition, Quaternion.identity);

            GameObject filamentGo = new GameObject("Vortex Filament");
            VortexFilament filament = filamentGo.AddComponent<VortexFilament>();

            if (parent != null)
            {
                filamentGo.transform.SetParent(parent, false);
                startNode.transform.parent = parent;
                endNode.transform.parent = parent;
            }

            filament.startNode = startNode;
            filament.endNode = endNode;
            startNode.AddConnection();
            endNode.AddConnection();

            return filament;
        }

        public static VortexFilament CreateVortexFilament(Vector3 startPosition, Vector3 endPosition, bool isDynamic, bool isTemporal, float initialStrength, float coreRadius, Transform parent = null)
        {
            VortexFilament filament = CreateUninitialisedVortexFilament(startPosition, endPosition, parent);
            filament.isDynamic = isDynamic;
            filament.isTemporal = isTemporal;
            filament.initialStrength = initialStrength;
            filament.coreRadius = coreRadius;

            filament.Initialise();

            return filament;
        }

        public static VortexFilament CreateVortexFilament(Vector3 startPosition, Vector3 endPosition, bool isDynamic, float lifeSpanDuration, float initialStrength, AnimationCurve strengthOverTime, float coreRadius, Transform parent = null)
        {
            VortexFilament filament = CreateUninitialisedVortexFilament(startPosition, endPosition, parent);

            filament.isDynamic = isDynamic;
            filament.isTemporal = true;
            filament.initialStrength = initialStrength;
            filament.strengthWithTime = strengthOverTime;
            filament.coreRadius = coreRadius;
            filament.life = lifeSpanDuration;

            filament.Initialise();
            return filament;
        }

        //public static VortexFilament CreateVortexFilament(Transform parent, Vector3 startPosition, Vector3 endPosition, bool isDynamic, float lifeSpanDuration, float initialStrength, AnimationCurve strengthOverTime, float coreRadius)
        //{
        //    VortexFilament filament = CreateUninitialisedVortexFilament(parent, startPosition, endPosition);
        //    filament.isDynamic = isDynamic;
        //    filament.isTemporal = true;
        //    filament.initialStrength = initialStrength;
        //    filament.strengthWithTime = strengthOverTime;
        //    filament.coreRadius = coreRadius;
        //    filament.life = lifeSpanDuration;

        //    filament.Initialise();
        //    return filament;
        //}

        public static VortexFilament AppendFilament(VortexFilament existingFilament, Vector3 endPosition)
        {
            VortexFilament filament = AppendUninitialisedFilament(existingFilament, endPosition);
            filament.Initialise();

            return filament;
        }

        public static VortexFilament AppendFilament(VortexNode existingNode, Vector3 endPosition)
        {
            VortexFilament filament = AppendUninitialisedFilament(existingNode, endPosition);
            filament.Initialise();

            return filament;
        }

        private static VortexFilament AppendUninitialisedFilament(VortexFilament existingFilament, Vector3 endPosition, Transform parent = null)
        {
            VortexNode newEndNode = CreateVortexNode(endPosition, Quaternion.identity);
            Vector3 startPosition = existingFilament.endNode.transform.position;

            GameObject go = new GameObject("Vortex Filament");
            go.transform.position = 0.5f * (startPosition + endPosition);
            VortexFilament filament = go.AddComponent<VortexFilament>();

            if (parent != null)
            {
                go.transform.parent = parent;
                newEndNode.transform.parent = parent;
            }

            filament.startNode = existingFilament.endNode;
            filament.endNode = newEndNode;
            existingFilament.endNode.AddConnection();
            newEndNode.AddConnection();

            return filament;
        }

        private static VortexFilament AppendUninitialisedFilament(VortexNode existingNode, Vector3 endPosition, Transform parent = null)
        {
            VortexNode newEndNode = CreateVortexNode(endPosition, Quaternion.identity);
            Vector3 startPosition = existingNode.transform.position;

            GameObject go = new GameObject("Vortex Filament");
            go.transform.position = 0.5f * (startPosition + endPosition);
            VortexFilament filament = go.AddComponent<VortexFilament>();

            if (parent != null)
            {

                newEndNode.transform.parent = parent;
                go.transform.parent = parent;
            }

            filament.startNode = existingNode;
            filament.endNode = newEndNode;
            existingNode.AddConnection();
            newEndNode.AddConnection();

            return filament;
        }

        public static VortexFilament AppendFilament(VortexFilament existingFilament, Vector3 endPosition, bool isDynamic, bool isTemporal, float initialStrength, float coreRadius)
        {
            VortexFilament filament = AppendUninitialisedFilament(existingFilament, endPosition);
            filament.isDynamic = isDynamic;
            filament.isTemporal = isTemporal;
            filament.initialStrength = initialStrength;
            filament.coreRadius = coreRadius;
            filament.Initialise();
            return filament;
        }

        public static VortexFilament AppendFilament(VortexNode existingNode, Vector3 endPosition, bool isDynamic, bool isTemporal, float initialStrength, float coreRadius, AnimationCurve strengthWithTime, float life, Transform parent = null)
        {
            VortexFilament filament = AppendUninitialisedFilament(existingNode, endPosition, parent);
            filament.isDynamic = isDynamic;
            filament.isTemporal = isTemporal;
            filament.initialStrength = initialStrength;
            filament.coreRadius = coreRadius;
            filament.strengthWithTime = strengthWithTime;
            filament.life = life;
            filament.Initialise();
            return filament;
        }

        private static VortexFilament CreateUninitialisedVortexFilamentWithoutNodes(Vector3 position)
        {
            //GameObject go = CreateKinematicTriggerObject(position, Quaternion.identity, "Vortex Filament");
            GameObject go = new GameObject("Vortex Filament");
            go.transform.position = position;
            VortexFilament filament = go.AddComponent<VortexFilament>();
            return filament;
        }

        public static VortexFilament CreateVortexFilamentWithoutNodes(Vector3 position)
        {
            VortexFilament filament = CreateUninitialisedVortexFilamentWithoutNodes(position);
            filament.Initialise();
            return filament;
        }

        public static VortexFilament CreateVortexFilamentWithoutNodes(Vector3 position, bool isDynamic, bool isTemporal, float initialStrength, float coreRadius)
        {
            VortexFilament filament = CreateUninitialisedVortexFilamentWithoutNodes(position);
            filament.isDynamic = isDynamic;
            filament.isTemporal = isTemporal;
            filament.initialStrength = initialStrength;
            filament.coreRadius = coreRadius;
            return filament;
        }

        public static VortexFilament[] CreateVortexRing(Vector3 position, float radius, int numNodes)
        {
            VortexFilament[] filaments = new VortexFilament[numNodes];

            // Create the first filament
            filaments[0] = CreateVortexFilament(position + (Vector3.right * radius),
                    position + (Quaternion.Euler(0, 0, 360f / numNodes) * Vector3.right * radius));

            // Create the rest of the loop/ring
            for (int i = 1; i < numNodes - 1; i++)
            {
                filaments[i] = AppendFilament(filaments[i - 1], position + (Quaternion.Euler(0, 0, (float)(i + 1) * 360f / numNodes) * Vector3.right * radius));
            }

            // Close the loop
            filaments[numNodes - 1] = CreateUninitialisedVortexFilamentWithoutNodes(Vector3.zero);
            filaments[numNodes - 1].startNode = filaments[numNodes - 2].endNode;
            filaments[numNodes - 1].startNode.AddConnection();
            filaments[numNodes - 1].endNode = filaments[0].startNode;
            filaments[numNodes - 1].endNode.AddConnection();
            filaments[numNodes - 1].Initialise();

            return filaments;
        }

        public static VortexFilament[] CreateVortexRing(Vector3 position, float radius, int numNodes, bool isDynamic, bool isTemporal, float initialStrength, float coreRadius)
        {
            VortexFilament[] filaments = new VortexFilament[numNodes];

            // Create the first filament
            filaments[0] = CreateVortexFilament(position + (Vector3.right * radius),
                    position + (Quaternion.Euler(0, 0, 360f / numNodes) * Vector3.right * radius), isDynamic, isTemporal, initialStrength, coreRadius);

            // Create the rest of the loop/ring
            for (int i = 1; i < numNodes - 1; i++)
            {
                filaments[i] = AppendFilament(filaments[i - 1], position + (Quaternion.Euler(0, 0, (float)(i + 1) * 360f / numNodes) * Vector3.right * radius), isDynamic, isTemporal, initialStrength, coreRadius);
            }

            // Close the loop
            // We're initialising twice here...
            filaments[numNodes - 1] = CreateVortexFilamentWithoutNodes(Vector3.zero, isDynamic, isTemporal, initialStrength, coreRadius);
            filaments[numNodes - 1].startNode = filaments[numNodes - 2].endNode;
            filaments[numNodes - 1].startNode.AddConnection();
            filaments[numNodes - 1].endNode = filaments[0].startNode;
            filaments[numNodes - 1].endNode.AddConnection();
            filaments[numNodes - 1].Initialise();

            return filaments;
        }

        public static VortexNode CreateVortexNode(Vector3 position, Quaternion rotation)
        {
            GameObject go = CreateKinematicTriggerObject(position, rotation, "Vortex Node");
            VortexNode node = go.AddComponent<VortexNode>();
            return node;
        }

        public static GameObject CreateKinematicTriggerObject(Vector3 position, Quaternion rotation, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, rotation);

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.angularDamping = 0;
            rb.mass = 0;

            SphereCollider sphereCollider = go.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.01f;

            return go;
        }
    }
}
