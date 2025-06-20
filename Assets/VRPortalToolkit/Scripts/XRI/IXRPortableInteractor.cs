using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Interface for XR interactors that can determine which portals are relevant to a given interactable.
    /// </summary>
    public interface IXRPortableInteractor
    {
        /// <summary>
        /// Gets the portals need to travel to specified interactable.
        /// </summary>
        /// <param name="interactable">The XR interactable.</param>
        /// <returns>An enumerable of portals.</returns>
        IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable);
    }
}
