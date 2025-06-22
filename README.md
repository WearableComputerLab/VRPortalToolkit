# VRPortalToolkit
By Daniel Ablett, Andrew Cunningham, Gun Lee and Bruce Thomas

[![IMAGE ALT TEXT](https://github.com/user-attachments/assets/cc901978-9529-44dd-9e28-914c0f5ead4f)](http://www.youtube.com/watch?v=Coo1kQwj0x8 "Point & Portal")

A toolkit for portals in Unity, specifically intended for virtual reality (but also works in standard mono). Portal rendering is implemented using Unity's Universal Render Pipeline (URP) and does **not** require custom shaders for objects. This toolkit was designed to be easily added to existing projects with minimal effort.

## Features
* Multi-pass and single-pass stereo support
* Stencil and render texture portals
* "Infinite" recursion
* Optimised culling through portals
* Seamless teleportation through portals
* Physics interactions through portals
* XR Interaction Toolkit support for interacting through portals
* A variety of portal interaction techniques:
  * **Point & Portal** - Interact with distant objects through portals
  * **Adaptive Portals** - Self-adjusting portals that respond to user interactions
  * **Portal Overlays** - Create overlapping virtual worlds visible through portals

## Requirements
* Unity 2020.3.32f1 or similar
* Universal Render Pipeline (URP)
* Unity's XR Interaction Toolkit (Optional)

## Quick Start
1. Add the `PortalRenderFeature` to your URP Renderer
2. Add a `BasicPortalPair` prefab (or `XRPortalPair` prefab for VR) to your scene
3. Play and interact with your first portal!

## Examples
The toolkit includes example scenes demonstrating:
* The Point & Portal interaction technique
* Adaptive Portals interaction techniques
* Portal Overlays visualisation techniques

## Research
This toolkit was developed in conjunction with the following research papers:
* [Portal Rendering and Creation Interactions in Virtual Reality](https://ieeexplore.ieee.org/abstract/document/9995211)
* [Point & Portal: A New Action at a Distance Technique For Virtual Reality](https://ieeexplore.ieee.org/abstract/document/10316441)
* [Adaptive Portals: Enhancing Virtual Reality Interaction Spaces With Real-Time Self-Adjusting Portals](https://ieeexplore.ieee.org/abstract/document/10765380)
* [Simultaneous Presence Continuum: Portal Overlays for Overlapping Worlds in Virtual Reality](https://ieeexplore.ieee.org/abstract/document/10949819)

