extends "res://scenarios/lib/scenario.gd"

# Walks a clustered source out of a group, through a doorway into the listener's half of the room, around the corner and back. It should share the group's muffling and grouped EAX slot while parked with it, leave the cluster once it's through the doorway, and rejoin when it walks back. Run with --listen to hear the transitions

const CONCRETE := "concrete"
const GROUP_SIZE := 3
const SEED := 20261028
const WALK_SECONDS := 3.0

# Behind the partition's upper half from the listener at (-150, 0), so the group is muffled
const GROUP_POINTS: Array[Vector2] = [Vector2(100, -150), Vector2(130, -125), Vector2(160, -150)]

# Parked with the group, then through the doorway at y = 0, then around the corner on the listener's side, out of the group's line of sight
const PARKED := Vector2(130, -95)
const WALK_PATH: Array[Vector2] = [Vector2(30, 0), Vector2(-60, 0), Vector2(-80, -150)]

var pieces: Array[Node] = []
var group: Array[Node] = []
var walker: Node
var markers: Array[Node] = []
var original_target_clustering: bool
var original_target_distance: float
var original_source_volume: float

func _init() -> void:
	scene = REVERB_ROOM

func setup() -> void:
	await wait_raytraced_by_listener(root.source)
	original_target_clustering = VA.get_value(root.listener, "target_clustering_enabled")
	original_target_distance = VA.get_value(root.listener, "target_clustering_distance")
	VA.set_value(root.listener, "target_clustering_enabled", true)
	VA.set_value(root.listener, "target_clustering_distance", DIM.CLUSTER_DISTANCE)

	# Partition across x = 0 with a 100 unit (2 m) doorway between y = -50 and 50
	pieces.append(DIM.add_box(root, Vector2(0, -150), Vector2(20, 200), CONCRETE))
	pieces.append(DIM.add_box(root, Vector2(0, 150), Vector2(20, 200), CONCRETE))

	var rng := RandomNumberGenerator.new()
	rng.seed = SEED
	for i in GROUP_SIZE:
		group.append(spawn_clustered("Group%d" % i, GROUP_POINTS[i], rng.randi()))
	walker = spawn_clustered("Walker", PARKED, rng.randi())

	# Only the walker is audible, since the same looping stream playing in sync from several sources sounds robotic. The scene's own source is silenced too
	original_source_volume = root.source.get("volume_db")
	root.source.set("volume_db", -80.0)
	for source in group:
		source.set("volume_db", -80.0)

	# Visible with godottest --windowed. ColorRect and Label aren't geometry, unlike Polygon2D
	add_marker(root.listener, "Listener", Color.GREEN)
	add_marker(root.source, "Source (silent)", Color.DIM_GRAY)
	for source in group:
		add_marker(source, "%s (silent)" % source.name, Color.CORNFLOWER_BLUE)
	add_marker(walker, "Walker", Color.YELLOW)

	for source in group:
		await wait_raytraced_by_listener(source)
	await wait_raytraced_by_listener(walker)

func spawn_clustered(source_name: String, position: Vector2, scattering_seed: int) -> Node:
	var source := spawn_source(source_name, position, scattering_seed)
	VA.set_value(source, "clustering_enabled", true)
	VA.set_value(source, "clustering_distance", DIM.CLUSTER_DISTANCE)
	return source

func add_marker(node: Node, text: String, colour: Color) -> void:
	var rect := ColorRect.new()
	rect.color = colour
	rect.size = Vector2(12, 12)
	rect.position = Vector2(-6, -6)
	var label := Label.new()
	label.text = text
	label.position = Vector2(8, -24)
	label.add_theme_color_override("font_color", colour)
	rect.add_child(label)
	node.add_child(rect)
	markers.append(rect)

func run() -> void:
	await check_parked("parked with the group", "walker parked with the group behind the wall - one voice, the group's muffling")

	for point in WALK_PATH:
		await walk_to(point, "walking to (%d, %d)" % [point.x, point.y])

	# Sound bends through the doorway in the reflective room, so the group is only slightly muffled. Leaving the cluster shows up as the walker getting its own results
	var away := await measure("around the corner", "walker on the listener's side, out of the group's line of sight - its own muffling and reverb")
	var group_gains := Vector2(VA.muffling_lf(group[0]), VA.muffling_hf(group[0]))
	check(away != group_gains, "walker muffling (%.4f, %.4f) on the listener's side still matches the group's, so it may still be in the group's cluster" % [away.x, away.y])
	check(VA.grouped_eax_index(walker) != VA.grouped_eax_index(group[0]), "walker still shares the group's grouped EAX index %d on the listener's side" % VA.grouped_eax_index(walker))

	for i in range(WALK_PATH.size() - 2, -1, -1):
		await walk_to(WALK_PATH[i], "walking back to (%d, %d)" % [WALK_PATH[i].x, WALK_PATH[i].y])
	await walk_to(PARKED, "walking back to the group")

	await check_parked("back with the group", "walker back with the group - muffled again, sharing the group's results")

func check_parked(label: String, banner: String) -> void:
	var gains := await measure(label, banner)
	var expected := Vector2(VA.muffling_lf(group[0]), VA.muffling_hf(group[0]))
	check(gains == expected, "walker muffling (%.4f, %.4f) %s doesn't match the group's (%.4f, %.4f)" % [gains.x, gains.y, label, expected.x, expected.y])

	var index := VA.grouped_eax_index(group[0])
	check(index >= 0, "the group isn't grouped (%s)" % label)
	check(VA.grouped_eax_index(walker) == index, "walker grouped EAX index %d %s doesn't match the group's %d" % [VA.grouped_eax_index(walker), label, index])

func measure(label: String, banner: String) -> Vector2:
	await wait_raytraced(SETTLE_PASSES)
	await step(banner, 1.0)
	var gains := Vector2(VA.muffling_lf(walker), VA.muffling_hf(walker))
	print("[devproject] %s -> walker muffling LF %.4f HF %.4f, grouped EAX index %d (group %d)" % [label, gains.x, gains.y, VA.grouped_eax_index(walker), VA.grouped_eax_index(group[0])])
	return gains

func walk_to(target: Vector2, banner: String) -> void:
	if listen:
		print(">> %s" % banner)

	var start: Vector2 = walker.position
	var elapsed := 0.0
	var end := duration(WALK_SECONDS)

	while elapsed < end:
		await root.get_tree().process_frame
		elapsed += root.get_process_delta_time()
		walker.position = start.lerp(target, minf(elapsed / end, 1.0))

func teardown() -> void:
	VA.set_value(root.listener, "target_clustering_enabled", original_target_clustering)
	VA.set_value(root.listener, "target_clustering_distance", original_target_distance)
	root.source.set("volume_db", original_source_volume)
	for marker in markers:
		if is_instance_valid(marker):
			marker.queue_free()
	markers.clear()
	for source in group:
		if is_instance_valid(source):
			source.queue_free()
	if is_instance_valid(walker):
		walker.queue_free()
	group.clear()
	for piece in pieces:
		piece.queue_free()
	pieces.clear()
	await wait_raytraced()
