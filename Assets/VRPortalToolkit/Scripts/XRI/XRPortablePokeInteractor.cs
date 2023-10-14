using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{
    // TODO: This has not been implemented yet
    public class XRPortablePokeInteractor : XRPokeInteractor, IXRPortableInteractor
    {
        public IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable)
        {
            yield break;
        }
    }
}
