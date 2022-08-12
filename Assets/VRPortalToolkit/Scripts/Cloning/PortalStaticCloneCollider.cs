using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;
using Misc;
using Misc.EditorHelpers;

// TODO: May not need transform clones :?
// Primative colliders dont need a hierachy to be replicated (need to figure out the math for how they actually calculate themselves
// Mesh colliders do, but if, when creating the sliced mesh, we modified the transform of each vertex, we could caculate a new one indepented of transforms
// --- My only worry is that this could be slow enough to not be worth it
// Could only replicate the hierachy for meshes?

// TODO: Might wanna rewrite slicing to modify transform
// Might even want to allow multislicing

// Okay so you can get the actual transform of a cube as:
// world scale of a cube collider as the lossy scale

// Caspule probably
// transform.position = capsule.transform.TransformPoint(cube.centre);
// transform.rotation = capsule.transform.rotation;
// transform.localScale = capsule.transform.TransformVector(new Vector3(capsule.radius, capsule.hieght * 0.5f, capsule.radius));
// then make localScale.x = localScale.z = Mathf.Max(localScale.x, localScale.z);

// Sphere probably
// transform.position = sphere.transform.TransformPoint(cube.centre);
// transform.rotation = sphere.transform.rotation; // Probably not even necessary
// transform.localScale = sphere.transform.TransformVector(new Vector3(sphere.radius, sphere.radius, sphere.radius));
// then make localScale.x = localScale.y = localScale.z = Mathf.Max(localScale.x, localScale.y, localScale.z);

// Also, not on this behaviour, but do need 
namespace VRPortalToolkit.Cloning
{
    public class PortalStaticCloneCollider : TriggerHandler
    {
        [SerializeField] private PortalLayer _portalLayer;
        public PortalLayer portalLayer {
            get => _portalLayer;
            set => _portalLayer = value;
        }
        public void ClearPortalSpace() => portalLayer = null;

        [SerializeField] private bool _staticCollidersOnly = true;
        public bool staticCollidersOnly {
            get => _staticCollidersOnly;
            set {
                if (_staticCollidersOnly != value)
                {
                    Validate.UpdateField(this, nameof(_staticCollidersOnly), _staticCollidersOnly = value);

                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        if (_staticCollidersOnly)
                        {
                            foreach (Collider collider in GetColliders())
                                if (collider && !collider.gameObject.isStatic) OnTriggerLastExit(collider);
                        }
                        else
                        {
                            foreach (Collider collider in GetColliders())
                                if (collider && !collider.gameObject.isStatic) OnTriggerFirstEnter(collider);
                        }
                    }
                }
            }
        }

        [SerializeField] private UpdateMode _updateMode = UpdateMode.UpdateEachFixedUpdate;
        public UpdateMode updateMode {
            get => _updateMode;
            set => _updateMode = value;
        }

        public enum UpdateMode
        {
            None = 0,
            UpdateEachFixedUpdate = 1,
            RecalculateEachFixedUpdate = 2,
        }

        //[SerializeField] protected List<Transform> _sliceNormals;
        //public HeapAllocationFreeReadOnlyList<Transform> ReadOnlySliceNormals => _sliceNormals;

        protected class ColliderClones
        {
            public Portal portal;

            public Collider original;

            public Collider localClone;
            public GameObject localCloneObject;

            public Collider connectedClone;
            public GameObject connectedCloneObject;
        }

        protected Dictionary<Collider, ColliderClones> _colliderClones = new Dictionary<Collider, ColliderClones>();
        protected ObjectPool<ColliderClones> _colliderPool = new ObjectPool<ColliderClones>(() => new ColliderClones());

        protected static HashSet<Collider> _ignoredColliders = new HashSet<Collider>();
        private static Transform _actualRoot;
        protected Transform _root => _actualRoot ? _actualRoot : _actualRoot = new GameObject("Portal Static Colliders").transform;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_staticCollidersOnly), nameof(staticCollidersOnly));
        }

        protected virtual void Reset()
        {
            portalLayer = GetComponentInParent<PortalLayer>();
            if (!portalLayer) portalLayer = GetComponentInChildren<PortalLayer>(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PortalPhysics.lateFixedUpdate += LateFixedUpdate;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            PortalPhysics.lateFixedUpdate -= LateFixedUpdate;
        }

        protected virtual void LateFixedUpdate()
        {
            if (updateMode == UpdateMode.UpdateEachFixedUpdate)
                UpdateColliderClones();
            else if (updateMode == UpdateMode.RecalculateEachFixedUpdate)
                RecalculateColliderClones();
        }

        private void UpdateColliderClones()
        {
            foreach (ColliderClones colliders in _colliderClones.Values)
                UpdateColliderClones(colliders);
        }

        /*public void DoAddSlicingNormal(Transform normal) => AddSlicingNormal(normal);

        public virtual bool AddSlicingNormal(Transform normal)
        {
            if (_sliceNormals == null) _sliceNormals = new List<Transform>();

            _sliceNormals.Add(normal);

            return true;
        }

        public bool DoRemoveSlicingNormal(Transform normal) => RemoveSlicingNormal(normal);

        public virtual bool RemoveSlicingNormal(Transform normal)
        {
            if (_sliceNormals == null) return false;

            return _sliceNormals.Remove(normal);
        }

        public virtual void ClearSlicingNormals()
        {
            if (_sliceNormals == null) return;

            while (_sliceNormals.Count > 0)
                _sliceNormals.Remove(_sliceNormals[_sliceNormals.Count - 1]);
        }*/

        protected virtual void UpdateColliderClones(ColliderClones clones)
        {
            // Make Sure the clones exist
            if (!clones.localClone || !clones.connectedClone)
                CreateColliderClones(clones);

            if (clones.localClone && clones.connectedClone)
            {
                if (portalLayer)
                {
                    if (portalLayer.portal && portalLayer.portal.usesTeleport)
                    {
                        Matrix4x4 matrix = portalLayer.portal.ModifyMatrix(clones.localClone.transform.localToWorldMatrix);

                        clones.connectedClone.transform.SetPositionAndRotation(matrix.GetColumn(3), matrix.rotation);
                        clones.connectedClone.transform.localScale = matrix.lossyScale;
                    }
                    else
                    {
                        clones.connectedClone.transform.SetPositionAndRotation(clones.localClone.transform.position, clones.localClone.transform.rotation);
                        clones.connectedClone.transform.localScale = clones.localClone.transform.localScale;
                    }

                    clones.localCloneObject.layer = portalLayer.ConvertOutsideToInside(clones.original.gameObject.layer);

                    if (portalLayer.connectedLayer)
                    {
                        int connectedLayer = clones.original.gameObject.layer;

                        if (portalLayer.portal && portalLayer.portal.usesLayers)
                            connectedLayer = portalLayer.portal.ModifyLayer(connectedLayer);

                        clones.connectedCloneObject.layer = portalLayer.connectedLayer.ConvertOutsideToInside(connectedLayer);
                    }
                }
            }
        }

        public virtual void RecalculateColliderClones()
        {
            foreach (ColliderClones clones in _colliderClones.Values)
            {
                CreateColliderClones(clones);
                UpdateColliderClones(clones);
            }
        }

        protected virtual void CreateColliderClones(ColliderClones clones)
        {
            if (!clones.localCloneObject) clones.localCloneObject = new GameObject($"{clones.original.name} (Local Clone)");
            if (!clones.connectedCloneObject) clones.connectedCloneObject = new GameObject($"{clones.original.name} (Connected Clone)");

            clones.portal = portalLayer?.portal;

            Transform local = clones.localCloneObject.transform, connected = clones.connectedCloneObject.transform;

            if (local.parent != _root) local.SetParent(_actualRoot);

            if (connected.parent != _actualRoot) connected.SetParent(_actualRoot);

            Type type = clones.original.GetType();

            // Copy specific collider
            if (type == typeof(MeshCollider))
            {
                MeshCollider originalClone = (MeshCollider)clones.original;

                Mesh mesh = originalClone.sharedMesh;

                GetCollider(clones.localCloneObject, ref clones.localClone, out MeshCollider localClone);
                GetCollider(clones.connectedCloneObject, ref clones.connectedClone, out MeshCollider connectedClone);

                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                GetSubMeshes(mesh, out int[][] subMeshes, out int subMeshCount);

                if (clones.original.transform.parent != null)
                {
                    for (int i = 0; i < vertices.Length; i++)
                        vertices[i] = clones.original.transform.TransformPoint(vertices[i]);

                    local.position = connected.position = Vector3.zero;
                    local.rotation = connected.rotation = Quaternion.identity;
                    local.localScale = connected.localScale = Vector3.one;
                }
                else
                {
                    local.position = connected.position = clones.original.transform.position;
                    local.rotation = connected.rotation = clones.original.transform.rotation;
                    local.localScale = connected.localScale = clones.original.transform.localScale;
                }

                bool hasInside = true;
                Mesh newMesh;

                if (TryGetCuttingPlanes(clones.original.transform.parent ? clones.original.transform : null, out Plane[] cuttingPlanes, out int planeCount)
                    && MeshSlicing.Slice(vertices, null, normals, null, subMeshes, vertices.Length, subMeshCount, null, cuttingPlanes, planeCount, 0, new Rect(0f, 0f, 1f, 1f), out newMesh, out hasInside))
                    localClone.sharedMesh = connectedClone.sharedMesh = newMesh;
                else if (!hasInside)
                {
                    // Not creating a mesh is an option if it was all sliced
                    if (localClone) localClone.enabled = false;
                    if (connectedClone) connectedClone.enabled = false;
                }
                else
                {
                    // Transform has been modified
                    if (clones.original.transform.parent != null)
                    {
                        newMesh = new Mesh();

                        newMesh.subMeshCount = subMeshCount;
                        newMesh.vertices = vertices;
                        newMesh.normals = normals;

                        for (int i = 0; i < subMeshCount; i++)
                            newMesh.SetTriangles(subMeshes[i], i, false);
                    }
                    else
                        localClone.sharedMesh = connectedClone.sharedMesh = originalClone.sharedMesh;
                }

                localClone.cookingOptions = connectedClone.cookingOptions = originalClone.cookingOptions;
                localClone.convex = connectedClone.convex = originalClone.convex;
            }
            else if (type == typeof(BoxCollider))
            {
                BoxCollider originalClone = (BoxCollider)clones.original;

                local.position = connected.position = originalClone.transform.TransformPoint(originalClone.center);
                local.rotation = connected.rotation = originalClone.transform.rotation;
                local.localScale = connected.localScale = originalClone.transform.TransformVector(originalClone.size);

                Mesh mesh = PrimativeMeshes.Get(PrimitiveType.Cube);

                if (!TrySliceMesh(clones, mesh))
                {
                    GetCollider(clones.localCloneObject, ref clones.localClone, out BoxCollider localClone);
                    GetCollider(clones.connectedCloneObject, ref clones.connectedClone, out BoxCollider connectedClone);

                    localClone.center = connectedClone.center = Vector3.zero;
                    localClone.size = connectedClone.size = Vector3.one;
                }
            }
            else if (type == typeof(SphereCollider))
            {
                SphereCollider originalClone = (SphereCollider)clones.original;

                local.position = connected.position = originalClone.transform.TransformPoint(originalClone.center);
                local.rotation = connected.rotation = originalClone.transform.rotation;

                Vector3 scale = originalClone.transform.TransformVector(new Vector3(originalClone.radius, originalClone.radius, originalClone.radius));
                float scaleUnit = Mathf.Max(scale.x, scale.y, scale.z);

                local.localScale = connected.localScale = new Vector3(scaleUnit, scaleUnit, scaleUnit);

                Mesh mesh = PrimativeMeshes.Get(PrimitiveType.Sphere);

                if (!TrySliceMesh(clones, mesh))
                {
                    GetCollider(clones.localCloneObject, ref clones.localClone, out SphereCollider localClone);
                    GetCollider(clones.connectedCloneObject, ref clones.connectedClone, out SphereCollider connectedClone);

                    localClone.center = connectedClone.center = Vector3.zero;
                    localClone.radius = connectedClone.radius = 0.5f;
                }
            }
            else if (type == typeof(CapsuleCollider))
            {
                CapsuleCollider originalClone = (CapsuleCollider)clones.original;

                local.position = connected.position = originalClone.transform.TransformPoint(originalClone.center);

                if (originalClone.direction == 1)
                {
                    local.rotation = connected.rotation = originalClone.transform.rotation;

                    Vector3 scale = originalClone.transform.TransformVector(new Vector3(originalClone.radius, originalClone.height * 0.5f, originalClone.radius));
                    float scaleUnit = Mathf.Max(scale.x, scale.z);
                    local.localScale = connected.localScale = new Vector3(Mathf.Min(scale.x, scaleUnit * 0.5f), scaleUnit, scaleUnit);
                }
                else if (originalClone.direction == 0)
                {
                    local.rotation = connected.rotation = originalClone.transform.rotation;

                    Vector3 scale = originalClone.transform.TransformVector(new Vector3(originalClone.height * 0.5f, originalClone.radius, originalClone.radius));
                    float scaleUnit = Mathf.Max(scale.y, scale.z);
                    local.localScale = connected.localScale = new Vector3(scaleUnit, Mathf.Min(scale.y, scaleUnit * 0.5f), scaleUnit);
                }
                else
                {
                    local.rotation = connected.rotation = originalClone.transform.rotation;

                    Vector3 scale = originalClone.transform.TransformVector(new Vector3(originalClone.radius, originalClone.radius, originalClone.height * 0.5f));
                    float scaleUnit = Mathf.Max(scale.x, scale.y);
                    local.localScale = connected.localScale = new Vector3(scaleUnit, scaleUnit, Mathf.Min(scale.z, scaleUnit * 0.5f));
                }

                Mesh mesh = PrimativeMeshes.Get(PrimitiveType.Capsule);

                if (!TrySliceMesh(clones, mesh))
                {
                    GetCollider(clones.localCloneObject, ref clones.localClone, out CapsuleCollider localClone);
                    GetCollider(clones.connectedCloneObject, ref clones.connectedClone, out CapsuleCollider connectedClone);

                    localClone.direction = connectedClone.direction = 0;
                    localClone.center = connectedClone.center = Vector3.zero;
                    localClone.radius = connectedClone.radius = 0.5f;
                    localClone.height = connectedClone.height = 2f;
                }
            }
            else if (type == typeof(TerrainCollider))
            {
                TerrainCollider originalClone = (TerrainCollider)clones.original;
                //cloneTerain.terrainData = originalTerrain.terrainData;

                // TODO: Don't know what to do with a terrain collider

                return;
            }
            else return;

            // Copy original
            clones.localClone.contactOffset = clones.connectedClone.contactOffset = clones.original.contactOffset;
            clones.localClone.isTrigger = clones.connectedClone.isTrigger = clones.original.isTrigger;
            clones.localClone.sharedMaterial = clones.connectedClone.sharedMaterial = clones.original.sharedMaterial;

            clones.localCloneObject.layer = clones.connectedCloneObject.layer = clones.original.gameObject.layer;

            // Add to cloning
            if (clones.original && clones.localClone)
            {
                PortalCloning.AddClone(clones.original, clones.localClone);
                PortalCloning.AddClone(clones.original.transform, clones.localClone.transform);
            }

            if (clones.original && clones.connectedClone)
            {
                Portal[] portalAsArray = new Portal[] { clones.portal };
                PortalCloning.AddClone(clones.original, clones.connectedClone, portalAsArray);
                PortalCloning.AddClone(clones.original.transform, clones.connectedClone.transform, portalAsArray);
            }
        }

        protected virtual bool TrySliceMesh(ColliderClones colliderClones, Mesh mesh)
        {
            if (TryGetCuttingPlanes(colliderClones.localCloneObject.transform, out Plane[] cuttingPlanes, out int planeCount))
            {
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                //Vector4[] tangents = mesh.tangents;

                GetSubMeshes(mesh, out int[][] subMeshes, out int subMeshCount);

                if (MeshSlicing.Slice(vertices, null, normals, null, subMeshes, vertices.Length, subMeshCount, null, cuttingPlanes, planeCount, 0, new Rect(0f, 0f, 1f, 1f), out Mesh newMesh, out bool hasInside))
                {
                    GetCollider(colliderClones.localCloneObject, ref colliderClones.localClone, out MeshCollider localClone);
                    GetCollider(colliderClones.connectedCloneObject, ref colliderClones.connectedClone, out MeshCollider connectedClone);

                    localClone.sharedMesh = connectedClone.sharedMesh = newMesh;
                    localClone.convex = connectedClone.convex = true;

                    return true;
                }

                // Not creating a mesh is an option if it was all sliced
                if (!hasInside)
                {
                    GetCollider(colliderClones.localCloneObject, ref colliderClones.localClone, out MeshCollider localClone);
                    GetCollider(colliderClones.connectedCloneObject, ref colliderClones.connectedClone, out MeshCollider connectedClone);

                    if (localClone) localClone.enabled = false;
                    if (connectedClone) connectedClone.enabled = false;

                    return true;
                }

                return false;
            }

            return false;
        }

        protected Plane[] _planes;
        protected virtual bool TryGetCuttingPlanes(Transform space, out Plane[] cuttingPlanes, out int planeCount)
        {
            if (_planes == null || _planes.Length < 1)//_sliceNormals.Count)
                _planes = new Plane[1];//_sliceNormals.Count];

            cuttingPlanes = _planes;

            Transform plane;
            /*for (int i = 0; i < _sliceNormals.Count; i++)
            {
                plane = _sliceNormals[i];

                if (plane)
                {
                    if (space)
                        cuttingPlanes[i] = new Plane(space.InverseTransformDirection(plane.forward), space.InverseTransformPoint(plane.position));
                    else
                        cuttingPlanes[i] = new Plane(plane.forward, plane.position);
                }
            }
            
            planeCount = _sliceNormals.Count;
            return planeCount != 0;
            */

            plane = portalLayer && portalLayer.portalTransition ? portalLayer.portalTransition.transitionPlane : null; 

            if (plane)
            {
                if (space)
                    cuttingPlanes[0] = new Plane(space.InverseTransformDirection(plane.forward), space.InverseTransformPoint(plane.position));
                else
                    cuttingPlanes[0] = new Plane(plane.forward, plane.position);

                planeCount = 1;
                return true;
            }

            planeCount = 0;
            return planeCount != 0;
        }

        protected int[][] _indices;
        protected virtual void GetSubMeshes(Mesh sharedMesh, out int[][] subMeshes, out int subMeshCount)
        {
            if (_indices == null || _planes.Length < sharedMesh.subMeshCount)
                _indices = new int[sharedMesh.subMeshCount][];

            subMeshes = _indices;

            for (int i = 0; i < sharedMesh.subMeshCount; i++)
                _indices[i] = sharedMesh.GetTriangles(i);

            subMeshCount = sharedMesh.subMeshCount;
        }

        protected virtual void GetCollider<TCollider>(GameObject cloneObject, ref Collider clone, out TCollider cloneAsT) where TCollider : Collider
        {
            if (clone)
            {
                if (clone is TCollider)
                {
                    cloneAsT = (TCollider)clone;
                    clone.enabled = true;
                    return;
                }
                else
                    clone.enabled = false;
            }

            if (cloneObject.TryGetComponent(out cloneAsT))
            {
                clone = cloneAsT;
                clone.enabled = true;

                // Incase this collider has been cheekily added
                _ignoredColliders.Add(clone);
            }
            else
            {
                clone = cloneAsT = cloneObject.AddComponent<TCollider>();
                _ignoredColliders.Add(clone);
            }
        }

        protected override void OnTriggerEnter(Collider collider)
        {
            if (!_ignoredColliders.Contains(collider))
                base.OnTriggerEnter(collider);
        }

        protected override void OnTriggerFirstEnter(Collider collider)
        {
            if (staticCollidersOnly && !collider.gameObject.isStatic)
                return;

            if (!_colliderClones.ContainsKey(collider))
            {
                ColliderClones clones = _colliderPool.Get();

                if (clones.localCloneObject) clones.connectedCloneObject.name = $"{clones.localCloneObject.name} (Static Collider Clone)";
                if (clones.connectedCloneObject) clones.connectedCloneObject.name = $"{clones.connectedCloneObject.name} (Static Collider Clone)";

                _colliderClones[collider] = clones;

                clones.original = collider;

                CreateColliderClones(clones);
                UpdateColliderClones(clones);
            }
        }

        protected override void OnTriggerLastExit(Collider collider)
        {
            if (_colliderClones.TryGetValue(collider, out ColliderClones trackedCollider))
            {
                RemoveTrackedCollider(trackedCollider);
                _colliderClones.Remove(collider);
            }
        }

        protected virtual void RemoveTrackedCollider(ColliderClones trackedCollider)
        {
            if (trackedCollider.localCloneObject) trackedCollider.localCloneObject.SetActive(false);
            if (trackedCollider.connectedCloneObject) trackedCollider.connectedCloneObject.SetActive(false);

            _colliderPool.Release(trackedCollider);
        }
    }
}
