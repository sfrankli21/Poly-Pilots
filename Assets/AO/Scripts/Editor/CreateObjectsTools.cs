using AerodynamicObjects.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Implements create menus for various objects and tools.
    /// Accessed using right click in scene hierarchy, or the GameObject menu bar in the editor.
    /// </summary>
    public class CreateObjectsTools
    {
        static readonly Vector3 objectStartScale = new Vector3(1f, 1f, 1f);
        const int menuPriority = 21;

        #region GeneralCreate
        public static GameObject CreateAeroObject()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Aero Object";
            go.AddComponent<AeroObject>();
            SetGameObjectParent(go);
            //Undo.RegisterCreatedObjectUndo(go, "Create Aero Object");

            return go;
        }

        /// <summary>
        /// Moves the component up the prescribed number of steps on the object's list of components.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="steps"></param>
        public static void MoveComponentUpSteps(Component component, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
            }
        }

        /// <summary>
        /// Creates a game object with the flow primitive component attached. Automatically sets the scale, parenting, and scene position from context.
        /// </summary>
        /// <typeparam name="T">The flow primitive type to create</typeparam>
        /// <returns>The newly created game object</returns>
        public static GameObject CreateFlowPrimitive<T>() where T : FlowPrimitive
        {
            GameObject go = new GameObject("Flow Primitive");
            go.AddComponent<T>();
            go.transform.localScale = objectStartScale;

            SetGameObjectParent(go);
            return go;
        }

        /// <summary>
        /// Sets the parent of the game object to the current selection. If there is no selection then it will be
        /// create in the root of the scene. If the context is a prefab scene then it is created as a child of the
        /// prefab root. Also sets the current selection to the game object in question.
        /// </summary>
        /// <param name="go">The game object to set up</param>
        public static void SetGameObjectParent(GameObject go)
        {
            // Create the object as a child if the context is appropriate
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                // If we're in a prefab scene we either child the new object to the selection in the scene
                if (Selection.activeGameObject != null)
                {
                    go.transform.parent = Selection.activeGameObject.transform;
                }
                else
                {
                    // Or we use the root of the prefab as the parent
                    go.transform.parent = prefabStage.prefabContentsRoot.transform;
                }
            }
            else if (Selection.activeGameObject != null)
            {
                go.transform.parent = Selection.activeGameObject.transform;
            }

            if (EditorPrefs.GetBool("Create3DObject.PlaceAtWorldOrigin"))
            {
                go.transform.position = Vector3.zero;
            }
            else
            {
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            }

            Selection.activeObject = go;
        }

        public static GameObject CreateWing(float span, float chord, float dihedral, float eeta_t)
        {

            GameObject root = new GameObject("Wing");
            AeroGroup group = root.AddComponent<AeroGroup>();
            group.span = span;
            GameObject portRoot = new GameObject("Port Root");
            portRoot.transform.parent = root.transform;
            portRoot.transform.localScale = new Vector3(span, 0.05f * chord, chord);

            GameObject portGo = CreateAeroObject();
            portGo.name = "Port Panel";
            portGo.transform.parent = portRoot.transform;
            portGo.transform.localPosition = new Vector3(-0.25f, 0, -0.5f);
            portGo.transform.localScale = new Vector3(0.5f, 1, 1);

            portRoot.transform.localRotation = Quaternion.Euler(0, 0, -dihedral); // do dihedral rotation after AeroObject creation so everything is rotated

            AeroObject portAo = portGo.GetComponent<AeroObject>();
            portAo.hasDrag = true;
            portAo.hasLift = true;
            portAo.referenceAreaShape = AeroObject.ReferenceAreaShape.Rectangle;
            portAo.myGroup = group;
            portGo.AddComponent<ControlSurface>();

            portGo.AddComponent<LiftArrow>();
            portGo.AddComponent<DragArrow>();

            GameObject starboardRoot = new GameObject("Starboard Root");
            starboardRoot.transform.parent = root.transform;
            starboardRoot.transform.localScale = new Vector3(span, 0.05f * chord, chord);

            GameObject starboardGo = CreateAeroObject();
            starboardGo.name = "Starboard Panel";
            starboardGo.transform.parent = starboardRoot.transform;
            starboardGo.transform.localPosition = new Vector3(0.25f, 0, -0.5f);
            starboardGo.transform.localScale = new Vector3(0.5f, 1, 1);

            starboardRoot.transform.localRotation = Quaternion.Euler(0, 0, dihedral);

            AeroObject starboardAo = starboardGo.GetComponent<AeroObject>();
            starboardAo.hasDrag = true;
            starboardAo.hasLift = true;
            starboardAo.referenceAreaShape = AeroObject.ReferenceAreaShape.Rectangle;
            starboardAo.myGroup = group;
            starboardGo.AddComponent<ControlSurface>();

            starboardGo.AddComponent<LiftArrow>();
            starboardGo.AddComponent<DragArrow>();

            return root;
        }

        public static GameObject CreateKinematicTriggerObject(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.localScale = objectStartScale;

            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.angularDamping = 0;
            rb.mass = 0;

            SphereCollider sphereCollider = go.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 1e-5f;

            return go;
        }

        #endregion GeneralCreate

        #region Examples

        [MenuItem("GameObject/Aerodynamic Objects/Examples/Aero Object (Cuboid)", false, menuPriority)]
        public static GameObject CreateRigidAeroObjectCube()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Aero Object";
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.angularDamping = 0;
            AeroObject ao = go.AddComponent<AeroObject>();
            ao.rb = rb;
            ao.hasLift = true;
            ao.hasDrag = true;
            ao.referenceAreaShape = AeroObject.ReferenceAreaShape.Rectangle;
            go.AddComponent<LiftArrow>();
            go.AddComponent<DragArrow>();

            MoveComponentUpSteps(ao, 5);

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Cube Aero Object");

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Examples/Aero Object (Ellipsoid)", false, menuPriority)]
        public static GameObject CreateRigidAeroObjectEllipsoid()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Aero Object";
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.angularDamping = 0;
            AeroObject ao = go.AddComponent<AeroObject>();
            ao.rb = rb;
            ao.hasLift = true;
            ao.hasDrag = true;
            ao.referenceAreaShape = AeroObject.ReferenceAreaShape.Ellipse;
            go.AddComponent<LiftArrow>();
            go.AddComponent<DragArrow>();

            MoveComponentUpSteps(ao, 5);

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Ellipsoid Aero Object");

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Examples/Glider &g", false, menuPriority)]
        public static GameObject CreateGlider()
        {
            //Set up glider design parameters
            //+z is forwards, +x is to starboard and +y is up
            //Origin is leading edge of root chord of wing. Using varible l for local z measurements relative to the wing quarter chord.
            //Using Cook, Flight Dynamic Principles as reference text
            //Wing
            float wingSpan = 2.5f;
            float wingAspectRatio = 6;
            float c = wingSpan / wingAspectRatio; //wing chord
            float dihedral = 6f; // wing dihedral in degrees. Positive value is wing tips up
            float wingArea = wingSpan * c;
            float a = 2 * Mathf.PI * wingAspectRatio / (2 + wingAspectRatio);// lift curve slope of wing
            //Horizontal stabiliser
            float tailplaneArea = wingArea / 5f; // Using tail area is one fifth wing area empirical design rule
            float tailplaneAspectRatio = wingAspectRatio / 2;
            float tailplaneChord = Mathf.Sqrt(tailplaneArea / tailplaneAspectRatio);
            float tailplaneSpan = tailplaneAspectRatio * tailplaneChord;
            float a_1 = 2 * Mathf.PI * tailplaneAspectRatio / (2 + tailplaneAspectRatio); // lift curve slope of tailplane
            float VbarT = 0.6f;// Tail volume coefficient, empirical value
            float lt = VbarT * wingArea * c / tailplaneArea; // longitudinal location of tailplane quarter chord       
            //Vertical stabiliser
            float finArea = tailplaneArea / 2;
            float finAspectRatio = 1.5f;
            float finChord = Mathf.Sqrt(finArea / finAspectRatio);
            float finSpan = finAspectRatio * finChord;
            float lf = lt;
            //CoM set up based on required stability
            float ho = 0.25f; // location of aerodynamic centre of wing relative to leading edge
            float staticMargin = 0.2f; // level of stability. Distance between CoM and overall aerodynamic centre, non dimensionalised by c
            float hn = ho + (a_1 / a * VbarT); // Dimensionless longitudinal location of overall aerodynamic centre aft of wing leading edge
            float h = hn - staticMargin; //Dimensionless CoM location for given static margin and calculated neutral point hn
            //tailplane setting angle for trim at given CL
            float trimCL = 0.5f;
            float eeta_t = trimCL * (((h - ho) / (VbarT * a_1)) - (1 / a)); // tailplane-wing rigging angle in radians, tailplane leading edge up positive. For this example the calculated angle is used to set the wing angle relative to the fuselage, and the tail angle relative to the fuselage is zero (parallel). The goal is at the aircraft trims at the design lift coefficient with the fuselage parallel to the wind (or thereabouts)
            GameObject root = new GameObject("Glider");
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.angularDamping = 0;
            rb.automaticCenterOfMass = false;
            rb.centerOfMass = new Vector3(0, 0, -h * c);
            //add centre of mass marker
            GameObject CoMmarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            CoMmarker.transform.name = "Centre of Mass Marker";
            CoMmarker.transform.parent = root.transform;
            CoMmarker.transform.localScale = 0.3f * c * Vector3.one;
            CoMmarker.transform.position = rb.centerOfMass;
            Material check = Resources.Load<Material>("check");
            CoMmarker.GetComponent<MeshRenderer>().material = check;
            CoMmarker.GetComponent<SphereCollider>().isTrigger = true;

            GameObject wing = CreateWing(wingSpan, c, dihedral, eeta_t);
            wing.transform.parent = root.transform;
            wing.transform.localPosition = Vector3.zero;

            // Horizontal Stabiliser
            GameObject tail = CreateAeroObject();
            tail.name = "Horizontal Stabiliser";
            tail.transform.parent = root.transform;
            tail.transform.localPosition = new Vector3(0, 0, -lt);
            tail.transform.localScale = new Vector3(tailplaneSpan, 0.05f * tailplaneChord, tailplaneChord);
            tail.transform.localEulerAngles = new Vector3(-eeta_t * Mathf.Rad2Deg, 0, 0);
            AeroObject tailAo = tail.GetComponent<AeroObject>();
            tailAo.hasDrag = true;
            tailAo.hasLift = true;
            tailAo.referenceAreaShape = AeroObject.ReferenceAreaShape.Rectangle;
            tail.AddComponent<ControlSurface>();
            tail.AddComponent<LiftArrow>();
            tail.AddComponent<DragArrow>();

            // Fin
            GameObject fin = CreateAeroObject();
            fin.name = "Fin";
            fin.transform.parent = root.transform;
            fin.transform.localPosition = new Vector3(0, finSpan / 2, -lt);
            fin.transform.localScale = new Vector3(finChord * 0.05f, finSpan, finChord);
            // Need this rotation so the control surface behaves appropriately! Would be better to create the fin like a wing then rotate 90 about z axis in my opinion
            fin.transform.localEulerAngles = new Vector3(0, 0, 180);
            AeroObject finAo = fin.GetComponent<AeroObject>();
            finAo.hasDrag = true;
            finAo.hasLift = true;
            finAo.referenceAreaShape = AeroObject.ReferenceAreaShape.Rectangle;
            fin.AddComponent<ControlSurface>();

            // Fuselage
            GameObject fuselage = CreateAeroObject();
            fuselage.name = "Fuselage";
            fuselage.transform.parent = root.transform;
            fuselage.transform.localPosition = new Vector3(0, -c / 8f, -lt / 4f);
            fuselage.transform.localScale = new Vector3(c / 4, c / 4, lt * 1.7f);
            AeroObject fuselageAo = fuselage.GetComponent<AeroObject>();
            fuselageAo.hasDrag = true;
            fuselage.AddComponent<WindArrow>();

            AeroObject[] aos = root.GetComponentsInChildren<AeroObject>();
            for (int i = 0; i < aos.Length; i++)
            {
                aos[i].rb = rb;
            }

            SetGameObjectParent(root);
            Undo.RegisterCreatedObjectUndo(root, "Create Glider");

            return root;
        }

        #endregion Examples

        #region FlowTools

        [MenuItem("GameObject/Aerodynamic Objects/Flow Tools/Flow Sensor", false, menuPriority)]
        public static GameObject CreateFlowSensor()
        {
            GameObject go = new GameObject("Flow Sensor");
            FlowSensor fs = go.AddComponent<FlowSensor>();
            go.AddComponent<WindArrow>();
            UnityEditorInternal.ComponentUtility.MoveComponentUp(fs);

            // Add the collider, and rigid body so the system responds to fluid zones
            //go.AddComponent<SphereCollider>().isTrigger = true;
            //Rigidbody rb = go.AddComponent<Rigidbody>();
            //rb.isKinematic = true;
            //rb.mass = 1e-07f;

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Flow Sensor");

            return go;
        }

        #endregion FlowTools

        #region FlowPrimitives

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Uniform Flow", false, menuPriority)]
        public static GameObject CreateUniformFlow()
        {
            GameObject go = CreateFlowPrimitive<UniformFlow>();
            go.name = "Uniform Flow";
            Undo.RegisterCreatedObjectUndo(go, "Create Uniform Flow");
            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Area Source", false, menuPriority)]
        public static GameObject CreateAreaSource()
        {
            GameObject go = CreateFlowPrimitive<AreaSource>();
            go.name = "Area Source";
            Undo.RegisterCreatedObjectUndo(go, "Create Area Source");
            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Fluid Volume", false, menuPriority)]
        public static GameObject CreateFluidVolume()
        {
            GameObject go = new GameObject("Fluid Volume");
            go.AddComponent<FluidVolume>();
            go.transform.localScale = objectStartScale;
            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Fluid Volume");
            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Point Source")]
        public static void CreatePointSource()
        {
            GameObject go = CreateFlowPrimitive<PointSource>();
            go.name = "Point Source";
            Undo.RegisterCreatedObjectUndo(go, "Create Point Source");
        }

        //[MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Cylindrical Source")]
        //public static void CreateCylindricalSource()
        //{
        //    GameObject go = CreateFlowPrimitive<CylindricalSourceFlowPrimitive>();
        //    go.name = "Cylindrical Source";
        //    Undo.RegisterCreatedObjectUndo(go, "Create Cylindrical Source");
        //}

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Line Source")]
        public static void CreateLineSource()
        {
            GameObject go = CreateFlowPrimitive<LineSource>();
            go.name = "Line Source";
            Undo.RegisterCreatedObjectUndo(go, "Create Line Source");
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Displacement Body")]
        public static void CreateDisplacementBody()
        {
            GameObject go = CreateFlowPrimitive<DisplacementBody>();
            go.name = "Displacement Body";
            go.GetComponent<FlowSensor>().velocitySource = VelocitySource.Transform;
            Undo.RegisterCreatedObjectUndo(go, "Create Displacement Body");
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Filament Vortex")]
        public static void CreateFilament()
        {
            Vector3 scenePos = SceneView.lastActiveSceneView.pivot;
            GameObject go = new GameObject("Filament Vortex");
            go.transform.position = scenePos;
            VortexFilament filament = CreateFlowPrimitiveTools.CreateVortexFilament(scenePos + Vector3.forward, scenePos - Vector3.forward);
            filament.transform.parent = go.transform;
            filament.startNode.transform.parent = go.transform;
            filament.endNode.transform.parent = go.transform;
            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Filament Vortex");
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Vortex Node")]
        public static void CreateVortexNode()
        {
            Vector3 scenePos = SceneView.lastActiveSceneView.pivot;
            GameObject go = CreateFlowPrimitiveTools.CreateVortexNode(scenePos, Quaternion.identity).gameObject;
            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Vortex Node");
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Primitives/Ring Vortex")]
        public static void CreateRingVortex()
        {
            Vector3 scenePos = SceneView.lastActiveSceneView.pivot;

            // ===================================================
            // Add the vortex ring script to the root!!
            // ===================================================

            GameObject go = new GameObject("Ring Vortex");
            go.AddComponent<RingVortex>().Initialise();
            go.transform.position = scenePos;

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Vortex Node");
        }

        #endregion FlowPrimitives

        #region FlowVisualisation

        public static GameObject CreateParticleGameObject(string name, bool includeWalls)
        {
            GameObject go = CreateKinematicTriggerObject(name);

            // Set up the particle system
            ParticleSystem particles = go.AddComponent<ParticleSystem>();
            ParticleSystem.EmissionModule particleEmission = particles.emission;
            particleEmission.rateOverDistance = 0;
            particleEmission.rateOverTime = 100;
            ParticleSystem.MainModule main = particles.main;
            main.startSpeed = 0;
            main.startSize = 0.02f;
            main.startLifetime = 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 10000;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Set the materials
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            particles.GetComponent<ParticleSystemRenderer>().trailMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");

            if (includeWalls)
            {
                // Create the walls for the particle system
                GameObject east = new GameObject("East");
                east.transform.position = go.transform.position + new Vector3(0.5f, 0, 0);
                east.transform.up = Vector3.left;
                east.transform.parent = go.transform;

                GameObject west = new GameObject("West");
                west.transform.position = go.transform.position + new Vector3(-0.5f, 0, 0);
                west.transform.up = Vector3.right;
                west.transform.parent = go.transform;

                GameObject north = new GameObject("North");
                north.transform.position = go.transform.position + new Vector3(0, 0, 0.5f);
                north.transform.up = Vector3.back;
                north.transform.parent = go.transform;

                GameObject south = new GameObject("South");
                south.transform.position = go.transform.position + new Vector3(0, 0, -0.5f);
                south.transform.up = Vector3.forward;
                south.transform.parent = go.transform;

                GameObject top = new GameObject("Top");
                top.transform.position = go.transform.position + new Vector3(0, 0.5f, 0);
                top.transform.up = Vector3.down;
                top.transform.parent = go.transform;

                GameObject bottom = new GameObject("Bottom");
                bottom.transform.position = go.transform.position + new Vector3(0, -0.5f, 0);
                bottom.transform.up = Vector3.up;
                bottom.transform.parent = go.transform;

                ParticleSystem.CollisionModule collisionModule = particles.collision;
                collisionModule.enabled = true;
                collisionModule.dampen = 1;
                collisionModule.bounce = 0;
                collisionModule.lifetimeLoss = 0;
                collisionModule.AddPlane(east.transform);
                collisionModule.AddPlane(west.transform);
                collisionModule.AddPlane(north.transform);
                collisionModule.AddPlane(south.transform);
                collisionModule.AddPlane(top.transform);
                collisionModule.AddPlane(bottom.transform);
            }

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Flow Particles", false, menuPriority)]
        public static GameObject CreateFlowParticles()
        {
            GameObject go = CreateParticleGameObject("Flow Particles", true);

            // No trails for the flow particles
            FlowFieldParticles flowFieldParticles = go.AddComponent<FlowFieldParticles>();
            flowFieldParticles.enableParticleTrails = false;
            flowFieldParticles.isBounded = true;

            MoveComponentUpSteps(flowFieldParticles, 5);

            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            // Add a size over lifetime curve
            //ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            //sizeOverLifetime.enabled = true;
            //sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 0) }));

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Flow Point Particles");

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Path Lines", false, menuPriority)]
        public static GameObject CreatePathLineParticles()
        {
            GameObject go = CreateParticleGameObject("Path Lines", true);
            ParticleSystem particles = go.GetComponent<ParticleSystem>();

            FlowFieldParticles flowFieldParticles = go.AddComponent<FlowFieldParticles>();
            flowFieldParticles.isBounded = true;

            MoveComponentUpSteps(flowFieldParticles, 5);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;

            // Add trails per particle
            ParticleSystem.TrailModule trailModule = particles.trails;
            trailModule.enabled = true;
            trailModule.mode = ParticleSystemTrailMode.PerParticle;
            trailModule.colorOverLifetime = new ParticleSystem.MinMaxGradient { color = new Color(1, 1, 1, 0.2f) };
            // Need to add width over trail curve so the trails aren't the same size
            trailModule.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0) }));
            trailModule.minVertexDistance = 0.01f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0) }));

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Path Line Particles");

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Streak Line", false, menuPriority)]
        public static GameObject CreateStreakLineParticles()
        {
            GameObject go = CreateParticleGameObject("Streak Line", false);
            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            // Need to add width over trail curve so the trails aren't the same size

            MoveComponentUpSteps(go.AddComponent<FlowPointParticles>(), 5);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1e-5f;
            shape.enabled = false;

            // Add ribbon trails
            ParticleSystem.TrailModule trailModule = particles.trails;
            trailModule.enabled = true;
            trailModule.mode = ParticleSystemTrailMode.Ribbon;
            trailModule.colorOverLifetime = new ParticleSystem.MinMaxGradient { color = new Color(1, 1, 1, 0.2f) };
            trailModule.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.9f, 1), new Keyframe(1, 0) }));
            trailModule.minVertexDistance = 0.01f;

            //ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            //sizeOverLifetime.enabled = true;
            //sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0) }));

            // Add trails per particle
            //ParticleSystem.TrailModule trailModule = particles.trails;
            //trailModule.enabled = true;
            //trailModule.mode = ParticleSystemTrailMode.PerParticle;
            //trailModule.colorOverLifetime = new ParticleSystem.MinMaxGradient { color = new Color(1, 1, 1, 0.2f) };
            //trailModule.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0) }));
            //trailModule.minVertexDistance = 0.01f;

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Streak Line");

            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Flow Rake Sensor", false, menuPriority)]
        public static GameObject CreateFlowRakeSensor()
        {
            GameObject go = CreateKinematicTriggerObject("Flow Rake Sensor");
            go.AddComponent<SensorRake>().SetUpRake();
            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Flow Rake Sensor");
            return go;
        }

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Particle Rake", false, menuPriority)]
        public static GameObject CreateFlowRakeParticles()
        {
            GameObject go = CreateParticleGameObject("Flow Rake", false);
            ParticleSystem particles = go.GetComponent<ParticleSystem>();
            // Need to add width over trail curve so the trails aren't the same size

            FlowPointParticles flowPointParticles = go.AddComponent<FlowPointParticles>();
            flowPointParticles.enableParticleTrails = false;
            MoveComponentUpSteps(flowPointParticles, 5);

            // Spawn particles along an edge
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radiusMode = ParticleSystemShapeMultiModeValue.Loop;
            shape.radiusSpeed = 10f;
            shape.radiusSpread = 0.1f;
            shape.position = new Vector3(0, 0, 0.5f);

            //ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            //sizeOverLifetime.enabled = true;
            //sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0) }));

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Flow Rake");

            return go;
        }

        //[MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Surface Particles", false, menuPriority)]
        //public static GameObject CreateDustParticles()
        //{
        //    GameObject go = new GameObject("Surface Particles");

        //    MoveComponentUpSteps(go.AddComponent<SurfaceParticles>(), 5);

        //    ParticleSystem particles = go.AddComponent<ParticleSystem>();
        //    ParticleSystem.EmissionModule particleEmission = particles.emission;
        //    particleEmission.rateOverDistance = 100;
        //    particleEmission.rateOverTime = 0;
        //    ParticleSystem.MainModule main = particles.main;
        //    main.startSpeed = 0;
        //    main.startSize = 0.5f;
        //    main.maxParticles = 1000;
        //    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        //    particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = Resources.Load("Dust") as Material;
        //    ParticleSystem.ShapeModule shape = particles.shape;
        //    shape.enabled = true;
        //    shape.shapeType = ParticleSystemShapeType.Box;

        //    // Create the walls for the particle system. Side walls are far away but bottom plane is set to y=0
        //    GameObject east = new GameObject("East");
        //    east.transform.position = go.transform.position + new Vector3(500f, 0, 0);
        //    east.transform.up = Vector3.left;
        //    east.transform.parent = go.transform;

        //    GameObject west = new GameObject("West");
        //    west.transform.position = go.transform.position + new Vector3(-500f, 0, 0);
        //    ;
        //    west.transform.up = Vector3.right;
        //    west.transform.parent = go.transform;

        //    GameObject north = new GameObject("North");
        //    north.transform.position = go.transform.position + new Vector3(0, 0, 500f);
        //    north.transform.up = Vector3.back;
        //    north.transform.parent = go.transform;

        //    GameObject south = new GameObject("South");
        //    south.transform.position = go.transform.position + new Vector3(0, 0, -500f);
        //    south.transform.up = Vector3.forward;
        //    south.transform.parent = go.transform;

        //    GameObject top = new GameObject("Top");
        //    top.transform.position = go.transform.position + new Vector3(0, 500f, 0);
        //    top.transform.up = Vector3.down;
        //    top.transform.parent = go.transform;

        //    GameObject bottom = new GameObject("Bottom");
        //    bottom.transform.position = go.transform.position + new Vector3(0, 0, 0);
        //    bottom.transform.up = Vector3.up;
        //    bottom.transform.parent = go.transform;

        //    ParticleSystem.CollisionModule collisionModule = particles.collision;
        //    collisionModule.enabled = false;
        //    collisionModule.dampen = 1;
        //    collisionModule.bounce = 0;
        //    collisionModule.lifetimeLoss = 0;
        //    collisionModule.AddPlane(east.transform);
        //    collisionModule.AddPlane(west.transform);
        //    collisionModule.AddPlane(north.transform);
        //    collisionModule.AddPlane(south.transform);
        //    collisionModule.AddPlane(top.transform);
        //    collisionModule.AddPlane(bottom.transform);

        //    ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        //    sizeOverLifetime.enabled = true;
        //    sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0) }));

        //    // Add trails
        //    ParticleSystem.TrailModule trailModule = particles.trails;
        //    trailModule.enabled = false;

        //    go.transform.localScale = objectStartScale;

        //    // Add the collider, and rigid body so the system responds to fluid zones
        //    go.AddComponent<SphereCollider>().isTrigger = true;
        //    Rigidbody rb = go.AddComponent<Rigidbody>();
        //    rb.isKinematic = true;
        //    rb.mass = 1e-07f;

        //    SetGameObjectParent(go);
        //    Undo.RegisterCreatedObjectUndo(go, "Create Surface Particles");

        //    return go;
        //}

        [MenuItem("GameObject/Aerodynamic Objects/Flow Visualisation/Field Arrows", false, menuPriority)]
        public static GameObject CreateFlowArrows()
        {
            GameObject go = new GameObject("Flow Field Arrows");
            go.transform.localScale = objectStartScale;
            go.AddComponent<FlowFieldArrows>();

            // Add the collider, and rigid body so the system responds to fluid zones
            go.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = 1e-07f;

            SetGameObjectParent(go);
            Undo.RegisterCreatedObjectUndo(go, "Create Flow Field Arrows");

            return go;
        }

        #endregion FlowVisualisation
    }
}