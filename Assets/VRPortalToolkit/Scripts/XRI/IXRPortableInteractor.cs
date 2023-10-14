using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{
    public interface IXRPortableInteractor
    {
        IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable);
    }
}
