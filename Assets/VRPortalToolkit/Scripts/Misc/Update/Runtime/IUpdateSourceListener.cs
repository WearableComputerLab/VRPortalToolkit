using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Misc.Update
{
    public interface IUpdateSourceListener
    {
        void SubscribeToFixedUpdate();
        void UnsubscribeFromFixedUpdate();

        void SubscribeToWaitForFixedUpdate();
        void UnsubscribeFromWaitForFixedUpdate();

        void SubscribeToUpdate();
        void UnsubscribeFromUpdate();

        void SubscribeToNullUpdate();
        void UnsubscribeFromNullUpdate();

        void SubscribeToLateUpdate();
        void UnsubscribeFromLateUpdate();

        void SubscribeToWaitForEndOfFrame();
        void UnsubscribeFromWaitForEndOfFrame();

        void SubscribeToPreCull();
        void UnsubscribeFromPreCull();

        void SubscribeToPreRender();
        void UnsubscribeFromPreRender();

        void SubscribeToPostRender();
        void UnsubscribeFromPostRender();

        void SubscribeToBeginCameraRendering();
        void UnsubscribeFromBeginCameraRendering();
        
        void SubscribeToBeginContextRendering();
        void UnsubscribeFromBeginContextRendering();
        
        void SubscribeToBeginFrameRendering();
        void UnsubscribeFromBeginFrameRendering();

        void SubscribeToEndCameraRendering();
        void UnsubscribeFromEndCameraRendering();
        
        void SubscribeToEndContextRendering();
        void UnsubscribeFromEndContextRendering();
        
        void SubscribeToEndFrameRendering();
        void UnsubscribeFromEndFrameRendering();

        void ForceInvoke();

        void SubscribeToSource(EventSource source);
        void UnsubscribeFromSource(EventSource source);

        void SubscribeToWaitForSeconds(float seconds);
        void UnsubscribeFromWaitForSeconds(float seconds);

        void SubscribeToOnEnabled();
        void UnsubscribeFromOnEnabled();

        void SubscribeToOnDisabled();
        void UnsubscribeFromOnDisabled();
    }
}