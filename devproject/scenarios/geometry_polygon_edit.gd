extends "res://scenarios/lib/scenario.gd"

# Edits a sealed Polygon2D partition's polygon. There's no change signal for it, so the edit is applied with VAWorld.sync_primitive, same as metadata changes

const SEALED := [Vector2(-10, -260), Vector2(10, -260), Vector2(10, 260), Vector2(-10, 260)]
const NARROW := [Vector2(-10, -25), Vector2(10, -25), Vector2(10, 25), Vector2(-10, 25)]

var partition: Polygon2D

func _init() -> void:
	scene = REVERB_ROOM

func setup() -> void:
	partition = Polygon2D.new()
	partition.polygon = PackedVector2Array(SEALED)
	partition.set_meta(DIM.MATERIAL_META, "brick")
	root.add_child(partition)
	await wait_raytraced_by_listener(root.source)

func run() -> void:
	var sealed := await measure_muffling("polygon sealed", "polygon partition sealing the room - expect muffled speech")
	edit(NARROW)
	var narrow := await measure_muffling("polygon narrowed", "polygon narrowed to 50 units - expect clear speech")
	edit(SEALED)
	var resealed := await measure_muffling("polygon resealed", "polygon resized to seal the room again - expect muffled speech")

	check(sealed.y < 0.1, "muffling HF %.4f with the sealing polygon, expected heavily muffled" % sealed.y)
	check(narrow.y > 0.9, "muffling HF %.4f after narrowing the polygon, expected almost unmuffled" % narrow.y)
	check(absf(resealed.y - sealed.y) < 0.02, "muffling HF %.4f after resealing the polygon didn't return to %.4f" % [resealed.y, sealed.y])

func edit(points: Array) -> void:
	partition.polygon = PackedVector2Array(points)
	VA.sync_primitive(root.world, partition)

func teardown() -> void:
	partition.queue_free()
	await wait_raytraced()
