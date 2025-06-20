using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Portal-aware poke interactor that supports interacting through portals.
    /// </summary>
    public class XRPortablePokeInteractor : XRPokeInteractor, IXRPortableInteractor
    {
        /// <summary>
        /// Gets the portals needed to travel to the specified interactable.
        /// </summary>
        /// <param name="interactable">The XR interactable.</param>
        /// <returns>An enumerable of portals.</returns>
        public IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable)
        {
            yield break;
        }
    }
}
