
Mesh Explosion Documentation
==============================
Mesh Explosion - Version 1.0
Author: GolemKin

Mesh Explosion is a Unity tool designed to dynamically slice 3D meshes into multiple pieces and apply realistic physics-based explosions. It leverages the EzySlice library for efficient mesh cutting and integrates with Unity’s Job System for performance optimization. This tool is ideal for creating stunning visual effects, such as shattering objects into smaller pieces, which can be blown apart with customizable forces and realistic debris scattering.

Problem It Solves:
Complex Mesh Slicing: Mesh Explosion provides a solution to break down any 3D model into smaller, randomized pieces, without requiring pre-cut assets. This helps in creating realistic destruction and fragmentation effects for 3D objects.

Realistic Physics-Based Explosions: Instead of relying on simple rigidbody forces, Mesh Explosion allows for detailed control over the explosion force, radius, shockwave effects, and debris scattering, all of which enhance the realism of explosion effects in games.

Performance-Optimized Destruction: The tool uses Unity’s Job System to run the debris physics calculations in parallel, reducing the computational load and improving performance in scenes with complex explosions.

Easy Customization: Developers can easily tweak various parameters like explosion force, shockwave radius, debris properties, and slicing details through an intuitive custom editor, making it user-friendly and flexible for a variety of use cases.

Overall, Mesh Explosion is a highly customizable and optimized solution for creating realistic mesh destruction in Unity.

The ExplosionMeshCutter uses the EzySlice library for performing mesh slicing operations.

Features:
1. Custom Foldouts:
   - The editor provides foldouts to group related settings together, reducing clutter in the Inspector. Each section is collapsible and includes:
     - Explosion Settings
     - Debris Settings
     - Slicing Settings

2. Clear Debris Button:
   - A button in the Inspector labeled "Clear Debris" will find and destroy all GameObjects tagged as "Debris". This is useful for clearing the scene after debris is spawned from an explosion.

3. Scene View Handles:
   - The editor adds interactive handles in the Scene View, allowing you to visualize the explosion radius and the shockwave radius. The explosion position can be moved interactively in the Scene View.

Inspector Settings:
1. Explosion Settings:
   - Explosion Force: Controls the magnitude of the explosion force applied to objects.
   - Explosion Radius: The radius within which objects are affected by the explosion.
   - Explosion Curve: An AnimationCurve that defines how the explosion force falls off over distance.
   - Shockwave Force: A separate force applied as a shockwave beyond the explosion radius.
   - Explosion Sound: The sound effect played when the explosion is triggered.
   - Debris Particles: A ParticleSystem that plays when the explosion happens.

2. Debris Settings:
   - Debris Prefabs: Array of prefabs representing debris objects.
   - Debris Count: Number of debris pieces to spawn.
   - Debris Size Range: The range of random sizes for debris objects (e.g., from 0.1 to 0.5).
   - Debris Lifetime: Time (in seconds) before the debris is destroyed.
   - Piece Material: The material applied to exploded pieces.
   - Piece Mass: The mass of each exploded piece for physics calculations.

3. Slicing Settings:
   - Number of Slices: Defines how many times the object is sliced recursively.
   - Minimum Piece Size: Prevents slicing the object below a certain size.
   - Rigidbody Mode: Defines whether the exploded pieces will have a Rigidbody attached (WithRigidbody or WithoutRigidbody).

Scene View Interactions:
- Explosion Radius: A red wireframe disc representing the explosion radius is displayed in the Scene View.
- Shockwave Radius: A blue wireframe disc represents the shockwave radius, which typically extends beyond the explosion radius.

How to Use:
1. Explosion Setup:
   - Assign all necessary parameters such as force, radius, particle systems, and debris in the Inspector.
   
2. Debris Management:
   - Use the Clear Debris button to remove all debris from the scene that has been tagged as "Debris".
   
3. Interactive Explosion Position:
   - In the Scene View, you can drag the position handle to move the explosion’s center interactively. The explosion’s radius and shockwave will update accordingly.

Visual Enhancements:
- White Foldout Arrows: The foldout arrows and text in the Inspector are styled with white color to improve readability, especially on dark backgrounds.
- Custom Header: The Explosion Mesh Cutter label is bold, centered, and uses a larger font to improve the visual hierarchy of the Inspector.

Known Limitations:
- The editor does not currently support triggering explosions directly from the Inspector. Explosions must be triggered via game logic or custom scripting.


Support:
--------
For any issues, questions, or support requests, please contact:
- Email: support@golemkin.com
