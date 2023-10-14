using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Examples
{
    public class MaintainScale : MonoBehaviour
    {
        [SerializeField] private Transform[] _targets;
        public Transform[] targets
        {
            get => _targets;
            set => _targets = value;
        }

        [SerializeField] private Vector2 _scale = Vector2.one;
        public Vector2 scale
        {
            get => _scale;
            set => _scale = value;
        }

        protected void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_targets == null) return;

            foreach (Transform target in _targets)
            {
                if (!target) continue;

                Transform parent = target.parent;
                target.SetParent(null, true);
                target.localScale = new Vector3(_scale.x, _scale.y, target.localScale.z);
                target.SetParent(parent, true);
            }
        }
    }
}
