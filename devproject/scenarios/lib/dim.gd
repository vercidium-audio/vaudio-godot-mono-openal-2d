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

# Listener spot in reverb_room.tscn on the source's side of the partition, 120 units from the source (150, 60) with clear line of sight
const REVERB_ROOM_LOS_LISTENER := Vector2(150, -60)

# x for a partition between the source (x = 150) and the right wall, so the listener and source are on the same side of it
const REVERB_ROOM_BEYOND_SOURCE_X := 275.0

# Listener spot on the same side of y = 0 as the source - a long partition turned to run along x leaves them both on the same side
const REVERB_ROOM_SAME_SIDE_LISTENER := Vector2(-150, 120)

# Divider long enough to seal reverb_room.tscn along either axis, so a quarter turn moves it from between the listener and source to beside them
static func add_reverb_room_long_partition(root: Node, material: String) -> Node:
	return add_box(root, Vector2(0, 0), Vector2(20, 820), material)

static func turn(node: Node, quarter_turns: int) -> void:
	node.rotation = quarter_turns * PI * 0.5

# Scales a partition along its length, e.g. 0.05 leaves a narrow column that sound goes around
static func squash(node: Node, factor: float) -> void:
	node.scale = Vector2(1, factor)

static func make_body(center: Vector2, size: Vector2, collision_layer: int) -> StaticBody2D:
	var shape := RectangleShape2D.new()
	shape.size = size
	var collider := CollisionShape2D.new()
	collider.shape = shape
	var body := StaticBody2D.new()
	body.position = center
	body.collision_layer = collision_layer
	body.add_child(collider)
	return body

# Wall-to-wall divider built from StaticBody2D pieces, with the material only on their shared parent. The subtree is built off-tree and added at once like an instanced scene, so each collider has to inherit the material from two levels up
static func add_reverb_room_body_partition(root: Node, material: String, pieces := 20, collision_layer := 1) -> Node:
	var container := Node2D.new()
	container.set_meta(MATERIAL_META, material)
	var height := 520.0 / pieces
	for i in pieces:
		container.add_child(make_body(Vector2(0, -260 + height * (i + 0.5)), Vector2(20, height + 4), collision_layer))
	root.add_child(container)
	return container

static func colliders(node: Node) -> Array[Node]:
	return node.find_children("*", "CollisionShape2D", true, false)
