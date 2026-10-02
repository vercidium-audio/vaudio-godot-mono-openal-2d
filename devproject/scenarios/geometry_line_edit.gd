extends "res://scenarios/lib/scenario.gd"

# Edits a sealed Line2D partition's points, applied with VAWorld.sync_primitive. A Line2D is a zero-thickness open polyline using flat transmission. Every built-in material only loses 10% LF / 25% HF per touch, which leaves more energy than the permeation cap, so brick is made fully opaque here to be able to see the line block anything. That silences LF too, and the plugins report HF relative to LF (clamped at LF 0.01), so the sealed checks use LF

const BRICK := 1

# Line2D needs at least 3 points to become a primitive
const SEALED := [Vector2(0, -260), Vector2(0, 0), Vector2(0, 260)]
const SHORT := [Vector2(0, -25), Vector2(0, 0), Vector2(0, 25)]

var partition: Line2D
var override: Node

func _init() -> void:
	scene = REVERB_ROOM

func setup() -> void:
	override = VA.create_node(root.world, "VADefaultMaterial")
	VA.set_value(override, "material_type", BRICK)
	VA.set_value(override, "flat_transmission_lf", 1.0)
	VA.set_value(override, "flat_transmission_hf", 1.0)
	root.world.add_child(override)

	partition = Line2D.new()
	partition.points = PackedVector2Array(SEALED)
	partition.set_meta(DIM.MATERIAL_META, "brick")
	root.add_child(partition)
	await wait_raytraced_by_listener(root.source)

func run() -> void:
	var sealed := await measure_muffling("line sealed", "opaque line partition sealing the room - expect muffled speech")
	edit(SHORT)
	var short := await measure_muffling("line shortened", "line shortened to 50 units - expect clear speech")
	edit(SEALED)
	var resealed := await measure_muffling("line resealed", "line lengthened to seal the room again - expect muffled speech")

	check(sealed.x < 0.1, "muffling LF %.4f with the sealing line, expected heavily muffled" % sealed.x)
	check(short.y > 0.9, "muffling HF %.4f after shortening the line, expected almost unmuffled" % short.y)
	check(absf(resealed.x - sealed.x) < 0.02, "muffling LF %.4f after resealing the line didn't return to %.4f" % [resealed.x, sealed.x])

func edit(points: Array) -> void:
	partition.points = PackedVector2Array(points)
	VA.sync_primitive(root.world, partition)

# Freeing the VADefaultMaterial restores brick's built-in values (asserted by materials_default_override)
func teardown() -> void:
	partition.queue_free()
	override.queue_free()
	await wait_raytraced()
