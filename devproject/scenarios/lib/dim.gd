extends RefCounted

# 2D geometry helpers - the 3D version has the same functions, so scenarios using them stay dimension-agnostic

const MATERIAL_META := "vercidium_audio_material"

# Metadata must be set before the node enters the tree, since that's when the plugin creates its primitive
static func add_box(parent: Node, center: Vector2, size: Vector2, material: String) -> Node:
	var half := size * 0.5
	var box := Polygon2D.new()
	box.polygon = PackedVector2Array([Vector2(-half.x, -half.y), Vector2(half.x, -half.y), Vector2(half.x, half.y), Vector2(-half.x, half.y)])
	box.position = center
	box.color = Color(0.62, 0.27, 0.2)
	box.set_meta(MATERIAL_META, material)
	parent.add_child(box)
	return box

# 20 units at reverb_room.tscn's meters_per_unit of 0.02
const PARTITION_THICKNESS_METRES := 0.4

# Wall-to-wall divider across the middle of reverb_room.tscn, between the listener (x = -150) and source (x = 150)
static func add_reverb_room_partition(root: Node, material: String) -> Node:
	return add_box(root, Vector2(0, 0), Vector2(20, 520), material)
