namespace vaudio_godot_mono_openal;

public partial class VAWorld
{
    void AddPrimitive(Node node, vaudio.MaterialType material, bool recursive) =>
        AddPrimitive(node, material, false, PropagateFilter.Default, recursive);

    void AddPrimitive(Node node, vaudio.MaterialType material, bool useFlatTransmission, PropagateFilter filter, bool recursive)
    {
        // A node's own material meta always wins and resets the propagation filter for its subtree
        bool hasOwnMaterial = node.HasMeta(MATERIAL_META_KEY);

        if (hasOwnMaterial)
        {
            material = GetMaterial(node);
            filter = PropagateFilter.Default;
        }

        // Use this specific transmission setting rather than the parent's
        if (node.HasMeta(USE_FLAT_TRANSMISSION_META_KEY))
            useFlatTransmission = node.GetMeta(USE_FLAT_TRANSMISSION_META_KEY).As<bool>();

        // A propagation filter declared here constrains the cascade into this node's descendants
        filter = ReadPropagateFilter(node, filter);

        // An inherited material only applies to this node if it passes the inherited filter
        var effectiveMaterial = hasOwnMaterial || PassesPropagationFilter(node, filter)
            ? material
            : vaudio.MaterialType.Air;

        // Ignore nodes without materials
        if (effectiveMaterial != vaudio.MaterialType.Air)
        {
            if (node is CollisionShape2D collisionShape)
                CreateVAudioPrimitive(collisionShape, effectiveMaterial);
            else if (node is Polygon2D polygon)
                CreateVAudioPrimitive(polygon, effectiveMaterial, useFlatTransmission);
            else if (node is Line2D line)
                CreateVAudioPrimitive(line, effectiveMaterial);
        }

        if (recursive)
            foreach (Node child in node.GetChildren())
                AddPrimitive(child, material, useFlatTransmission, filter, true);
    }

    // Whether a cascading material should apply to this node.
    // Only inherited materials are gated - a node with its own material is always included
    bool PassesPropagationFilter(Node node, PropagateFilter filter)
    {
        bool isCollider = node is CollisionShape2D;

        switch (filter.Mode)
        {
            case PropagateMode.Colliders when !isCollider:
                return false;
            case PropagateMode.Visuals when isCollider:
                return false;
        }

        // 2D has no render-layer concept for visuals - only colliders are layer-filtered,
        // against the VAWorld's CollisionLayers mask. Every non-collider node always passes.
        if (isCollider && node.GetParentOrNull<CollisionObject2D>() is { } body)
            return (body.CollisionLayer & CollisionLayers) != 0;

        return true;
    }

    public void SyncPrimitive(Node node)
    {
        if (world == null)
            return;

        RemovePrimitive(node, true);
        AddPrimitive(node, vaudio.MaterialType.Air, true, PropagateFilter.Default, true);
    }

    // Re-evaluate every node against the current layer masks - used when CollisionLayers changes
    // at runtime, so nodes that now match get added and nodes that no longer match get removed.
    void RebuildPrimitives()
    {
        // Godot runs [Export] setters during scene deserialization, before _EnterTree. This world is still null then, so bail early
        if (world == null || !IsInsideTree())
            return;

        Node root = GetTree().Root;

        if (root == null)
            return;

        foreach (var child in root.GetChildren())
        {
            RemovePrimitive(child, true);
            AddPrimitive(child, vaudio.MaterialType.Air, true);
        }
    }

    void RemovePrimitive(Node node, bool recursive)
    {
        // When a node is removed from the scene, remove it from the raytracing simulation too
        if (node.HasMeta(PRIMITIVE_META_KEY))
        {
            var wrapper = node.GetMeta(PRIMITIVE_META_KEY).As<VAPrimitiveRef>();

            wrapper.Watcher?.QueueFree();

            if (wrapper.ShapeCallable is Callable shapeCallable && node is CollisionShape2D cs && cs.Shape != null)
                if (cs.Shape.IsConnected(Resource.SignalName.Changed, shapeCallable))
                    cs.Shape.Disconnect(Resource.SignalName.Changed, shapeCallable);

            world.RemovePrimitive(wrapper.Primitive);
            node.RemoveMeta(PRIMITIVE_META_KEY);
        }

        if (recursive)
            foreach (Node child in node.GetChildren())
                RemovePrimitive(child, true);
    }

    static VAPrimitiveRef AttachWatcher(Node2D node, vaudio.Primitive prim, Action update)
    {
        var watcher = new TransformWatcher { OnTransformChanged = update };
        node.AddChild(watcher);
        return new VAPrimitiveRef { Primitive = prim, Watcher = watcher };
    }

    static void Decompose(Transform2D transform, out vaudio.Vector position, out float rotation, out Vector2 scale)
    {
        position = ToVAudio(transform.Origin);
        rotation = transform.Rotation;
        scale = transform.Scale;
    }

    void CreateVAudioPrimitive(CollisionShape2D collisionShape, vaudio.MaterialType material)
    {
        Debug.Assert(material != vaudio.MaterialType.Air);

        // Skip if it's already been added to the raytracing scene
        if (collisionShape.HasMeta(PRIMITIVE_META_KEY))
        {
            Debug.Assert(false);
            return;
        }

        var shape = collisionShape.Shape;
        Decompose(collisionShape.GlobalTransform, out var position, out var rotation, out var scale);

        vaudio.Primitive prim = null;

        if (shape is RectangleShape2D rect)
        {
            world.AddPrimitive(prim = new vaudio.BoxPrimitive()
            {
                position = position,
                size = ToVAudio(rect.Size * scale),
                rotation = rotation,
                material = material,
            });
        }
        else if (shape is CircleShape2D circle)
        {
            world.AddPrimitive(prim = new vaudio.CirclePrimitive()
            {
                center = position,
                radius = circle.Radius * scale.X,
                material = material,
            });
        }
        else if (shape is CapsuleShape2D capsule)
        {
            // TODO - make a capsule primitive in 2D
            world.AddPrimitive(prim = new vaudio.OvalPrimitive()
            {
                center = position,
                radiusX = capsule.Radius * scale.X,
                radiusY = (capsule.Height / 2f) * scale.Y,
                rotation = rotation,
                material = material,
            });
        }
        else if (shape is SegmentShape2D segment)
        {
            world.AddPrimitive(prim = new vaudio.LinePrimitive()
            {
                start = ToVAudio(collisionShape.GlobalTransform * segment.A),
                end = ToVAudio(collisionShape.GlobalTransform * segment.B),
                material = material,
            });
        }
        else if (shape is SeparationRayShape2D ray)
        {
            world.AddPrimitive(prim = new vaudio.LinePrimitive()
            {
                start = position,
                end = ToVAudio(collisionShape.GlobalTransform * new Vector2(0, ray.Length)),
                material = material,
            });
        }
        else if (shape is WorldBoundaryShape2D worldBoundary)
        {
            // WorldBoundaryShape2D is an infinite half-plane edge - approximate with a long line
            // along the boundary, sized to comfortably cover the world.
            var normal = worldBoundary.Normal;
            var along = new Vector2(-normal.Y, normal.X);
            var origin = collisionShape.GlobalPosition + normal * worldBoundary.Distance;
            var half = world.Size.Magnitude * 2f;

            world.AddPrimitive(prim = new vaudio.LinePrimitive()
            {
                start = ToVAudio(origin - along * half),
                end = ToVAudio(origin + along * half),
                material = material,
            });
        }
        else if (shape is ConvexPolygonShape2D convexPolygon)
        {
            var points = Conversions.ConvertPointsToVectorList(convexPolygon.Points);

            if (points.Count >= 3)
                world.AddPrimitive(prim = new vaudio.PolygonPrimitive()
                {
                    points = points,
                    position = position,
                    rotation = rotation,
                    scale = ToVAudio(scale),
                    enclosed = true,
                    material = material,
                });
        }
        else if (shape is ConcavePolygonShape2D concavePolygon)
        {
            var points = Conversions.ConvertPointsToVectorList(concavePolygon.Segments);

            if (points.Count >= 3)
                world.AddPrimitive(prim = new vaudio.PolygonPrimitive()
                {
                    points = points,
                    position = position,
                    rotation = rotation,
                    scale = ToVAudio(scale),
                    enclosed = false,
                    material = material,
                });
        }

        if (prim != null)
        {
            void update() => UpdateCollisionShapePrimitive(collisionShape, prim);
            var wrapper = AttachWatcher(collisionShape, prim, update);

            if (collisionShape.Shape is RectangleShape2D)
            {
                var callable = Callable.From(update);
                collisionShape.Shape.Connect(Resource.SignalName.Changed, callable);
                wrapper.ShapeCallable = callable;
            }

            collisionShape.SetMeta(PRIMITIVE_META_KEY, wrapper);
        }
    }

    static void UpdateCollisionShapePrimitive(CollisionShape2D collisionShape, vaudio.Primitive primitive)
    {
        Decompose(collisionShape.GlobalTransform, out var position, out var rotation, out var scale);

        if (primitive is vaudio.BoxPrimitive box)
        {
            var rect = collisionShape.Shape as RectangleShape2D;
            box.position = position;
            box.size = ToVAudio(rect.Size * scale);
            box.rotation = rotation;
        }
        else if (primitive is vaudio.CirclePrimitive circlePrim)
        {
            var circle = collisionShape.Shape as CircleShape2D;
            circlePrim.center = position;
            circlePrim.radius = circle.Radius * scale.X;
        }
        else if (primitive is vaudio.OvalPrimitive ovalPrim)
        {
            var capsule = collisionShape.Shape as CapsuleShape2D;
            ovalPrim.center = position;
            ovalPrim.radiusX = capsule.Radius * scale.X;
            ovalPrim.radiusY = (capsule.Height / 2f) * scale.Y;
            ovalPrim.rotation = rotation;
        }
        else if (primitive is vaudio.LinePrimitive linePrim)
        {
            if (collisionShape.Shape is SegmentShape2D segment)
            {
                linePrim.start = ToVAudio(collisionShape.GlobalTransform * segment.A);
                linePrim.end = ToVAudio(collisionShape.GlobalTransform * segment.B);
            }
            else if (collisionShape.Shape is SeparationRayShape2D ray)
            {
                linePrim.start = position;
                linePrim.end = ToVAudio(collisionShape.GlobalTransform * new Vector2(0, ray.Length));
            }
            else if (collisionShape.Shape is WorldBoundaryShape2D worldBoundary)
            {
                var normal = worldBoundary.Normal;
                var along = new Vector2(-normal.Y, normal.X);
                var origin = collisionShape.GlobalPosition + normal * worldBoundary.Distance;

                // Keep the same half-length the line was created with (see CreateVAudioPrimitive) -
                // recovered from the existing endpoints so this stays a pure position update.
                var half = (FromVAudio(linePrim.end) - FromVAudio(linePrim.start)).Length() / 2f;

                linePrim.start = ToVAudio(origin - along * half);
                linePrim.end = ToVAudio(origin + along * half);
            }
        }
        else if (primitive is vaudio.PolygonPrimitive polygonPrim)
        {
            polygonPrim.position = position;
            polygonPrim.rotation = rotation;
            polygonPrim.scale = ToVAudio(scale);
        }
    }

    void CreateVAudioPrimitive(Polygon2D polygon, vaudio.MaterialType material, bool useFlatTransmission)
    {
        Debug.Assert(material != vaudio.MaterialType.Air);

        if (polygon.HasMeta(PRIMITIVE_META_KEY))
        {
            Debug.Assert(false);
            return;
        }

        var points = Conversions.ConvertPointsToVectorList(polygon.Polygon);

        if (points.Count < 3)
        {
            LogWarning($"Polygon2D {polygon.Name} will not affect raytracing as it has fewer than 3 points");
            return;
        }

        Decompose(polygon.GlobalTransform, out var position, out var rotation, out var scale);

        vaudio.PolygonPrimitive prim = new()
        {
            points = points,
            position = position,
            rotation = rotation,
            scale = ToVAudio(scale),
            enclosed = true,
            material = material,
            UseFlatTransmission = useFlatTransmission || IsConcave(points),
        };

        world.AddPrimitive(prim);

        polygon.SetMeta(PRIMITIVE_META_KEY, AttachWatcher(polygon, prim, () =>
        {
            Decompose(polygon.GlobalTransform, out var updatedPosition, out var updatedRotation, out var updatedScale);
            prim.position = updatedPosition;
            prim.rotation = updatedRotation;
            prim.scale = ToVAudio(updatedScale);
        }));
    }

    void CreateVAudioPrimitive(Line2D line, vaudio.MaterialType material)
    {
        Debug.Assert(material != vaudio.MaterialType.Air);

        if (line.HasMeta(PRIMITIVE_META_KEY))
        {
            Debug.Assert(false);
            return;
        }

        var points = Conversions.ConvertPointsToVectorList(line.Points);

        if (points.Count < 3)
        {
            LogWarning($"Line2D {line.Name} will not affect raytracing as it has fewer than 3 points");
            return;
        }

        Decompose(line.GlobalTransform, out var position, out var rotation, out var scale);

        vaudio.PolygonPrimitive prim = new()
        {
            points = points,
            position = position,
            rotation = rotation,
            scale = ToVAudio(scale),
            // A Line2D is an open polyline, never a closed loop
            enclosed = false,
            material = material,
        };

        world.AddPrimitive(prim);

        line.SetMeta(PRIMITIVE_META_KEY, AttachWatcher(line, prim, () =>
        {
            Decompose(line.GlobalTransform, out var updatedPosition, out var updatedRotation, out var updatedScale);
            prim.position = updatedPosition;
            prim.rotation = updatedRotation;
            prim.scale = ToVAudio(updatedScale);
        }));
    }

    static bool IsConcave(List<vaudio.Vector> points)
    {
        int sign = 0;

        for (int i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var c = points[(i + 2) % points.Count];

            var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

            if (cross == 0)
                continue;

            int s = cross > 0 ? 1 : -1;

            if (sign == 0)
                sign = s;
            else if (s != sign)
                return true;
        }

        return false;
    }
}
