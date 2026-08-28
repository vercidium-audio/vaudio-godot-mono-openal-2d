namespace vaudio_godot_mono_openal;

// This is a Node2D purely so the editor can draw a gizmo showing the world's Position/Size rect
// (see the 2D gizmo plugin) - the node's transform is otherwise unused by vaudio, which always
// treats Position/Size as absolute world-space coordinates.
//
// All dimension-agnostic VAWorld logic lives in common/world/*.cs as further `partial class VAWorld`
// files; this file only pins the Godot base type for the 2D addon. The 3D addon has its own
// world/VAWorld.cs pinning Node3D.
[Tool]
public partial class VAWorld : Node2D
{
}
