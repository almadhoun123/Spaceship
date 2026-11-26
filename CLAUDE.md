# Project rules for Claude

## Game
- A space ship game. Engine: Unity 6000.3.27f2 (URP). 3D assets: Blender.
- The repo calls it "Spaceship Rail Shooter". Ask me before big design decisions; do not guess them.

## Git
- Repo: https://github.com/almadhoun123/Spaceship. Each team member works on their own branch (main, Ahmad, mohammed, azayzeh).
- My branch is azayzeh. Do not push to main or to other people's branches.
- The blocks between "BEGIN unity vcs doctor" and "END unity vcs doctor" in .gitignore and .gitattributes are managed by Unity. Add our own rules below them.

## Folders
- The Unity project is at the repo root: Assets/, Packages/, ProjectSettings/.
- Never edit Library/, Temp/ or any .meta file by hand.
- blender/ : Blender source files (.blend). Never edit these without making a copy first.
- blender/scripts/ : scripts that build the assets. blender/renders/ : review videos and pictures. blender/scripts/_review/ : temporary frames, safe to empty.
- blender/textures/ : source textures.
- audio/ : scripts that make the placeholder sounds, and previews to listen to.
- docs/ : the design documents and reference images.
- Do not move blender/ or audio/: scripts use these paths.
- Test scenes go in Assets/Scenes/Test/, and Editor test scripts in Assets/Editor/Tests/.

## Naming
- Static meshes: SM_Name, skeletal meshes: SK_Name, materials: M_Name, textures: T_Name_Type (e.g. T_Hull_Albedo).
- No spaces in names. Use PascalCase.

## Blender to Unity export
- Units: 1 Blender unit = 1 meter.
- Before export: apply rotation and scale (Ctrl+A), origin at the center of the ship (or the base for props), clean names.
- Check before export: bad normals, floating parts, non-manifold meshes.
- Export FBX with: Apply Scalings = "FBX All", Forward = -Z, Up = Y, Apply Transform = on, only selected objects.
- Export to Assets/Art/Models/<Category>/.
- After import in Unity, check: scale is 1, rotation is 0, materials are assigned.

## Working rules
- Commit with git before any big change.
- Use the Unity CLI for Unity tasks when possible.
- Test Blender MCP actions on copies of files.
- Explain in simple English (my level is B1).
