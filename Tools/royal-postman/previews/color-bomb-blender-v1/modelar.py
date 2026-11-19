"""Build the Royal Magic Color Bomb as an animated, editable Blender scene."""

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


TAU = math.tau
FPS = 24
FRAME_COUNT = 48
HEIGHT = 1.06
RADIUS = 1.013


def options():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--blend", type=Path)
    parser.add_argument("--mode", choices=("still", "motion"), default="still")
    parser.add_argument("--samples", type=int, default=48)
    parser.add_argument("--motion-percent", type=int, default=58)
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def srgb_channel(value):
    value /= 255
    if value <= 0.04045:
        return value / 12.92
    return ((value + 0.055) / 1.055) ** 2.4


def rgba(hex_value):
    return tuple(srgb_channel(int(hex_value[i:i + 2], 16)) for i in (0, 2, 4)) + (1,)


def enamel(name, color, roughness=.27, coat=.23, metal=0, emission=0):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.diffuse_color = rgba(color)
    shader.inputs["Base Color"].default_value = material.diffuse_color
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Metallic"].default_value = metal
    shader.inputs["Specular IOR Level"].default_value = .32
    shader.inputs["Coat Weight"].default_value = coat
    shader.inputs["Coat Roughness"].default_value = .22
    shader.inputs["Emission Color"].default_value = material.diffuse_color
    shader.inputs["Emission Strength"].default_value = emission
    return material


def collection(name):
    group = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(group)
    return group


def empty(name, group, parent=None):
    obj = bpy.data.objects.new(name, None)
    group.objects.link(obj)
    obj.parent = parent
    obj.empty_display_type = "PLAIN_AXES"
    obj.empty_display_size = .26
    return obj


def mesh(name, vertices, faces, mats, group, parent, material_indices=None, smooth=True):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    group.objects.link(obj)
    obj.parent = parent
    for material in mats:
        data.materials.append(material)
    for polygon in data.polygons:
        polygon.use_smooth = smooth
        if material_indices is not None:
            polygon.material_index = material_indices[polygon.index]
    return obj


def lathe(name, profile, material, group, parent, segments=128):
    vertices = []
    rings = []
    for r, z in profile:
        start = len(vertices)
        if r <= .002:
            vertices.append((0, 0, z))
            rings.append([start] * segments)
        else:
            for i in range(segments):
                angle = TAU * i / segments
                vertices.append((r * math.sin(angle), -r * math.cos(angle), z))
            rings.append(list(range(start, start + segments)))
    faces = []
    for j, ring in enumerate(rings):
        other = rings[(j + 1) % len(rings)]
        for i in range(segments):
            k = (i + 1) % segments
            face = tuple(dict.fromkeys((ring[i], ring[k], other[k], other[i])))
            if len(face) >= 3:
                faces.append(face)
    return mesh(name, vertices, faces, [material], group, parent)


def shell_radius(z):
    return RADIUS * math.sqrt(max(.000001, 1 - (z / HEIGHT) ** 2))


def shell(name, minimum, maximum, material, group, parent):
    points = [(.002, minimum)]
    for i in range(39):
        z = minimum + (maximum - minimum) * i / 38
        points.append((shell_radius(z), z))
    points.append((.002, maximum))
    return lathe(name, points, material, group, parent)


def rounded_square(radius=.23, segments=7):
    outline = []
    for index in range(4):
        offset = index * math.pi / 2
        cx = (1 - radius) * math.cos(offset + math.pi / 4) * math.sqrt(2)
        cy = (1 - radius) * math.sin(offset + math.pi / 4) * math.sqrt(2)
        for step in range(segments + 1):
            angle = offset + step / segments * math.pi / 2
            outline.append((cx + radius * math.cos(angle), cy + radius * math.sin(angle)))
    return outline


def surfaced_panel(name, outline, profiles, point, materials, group, parent, smooth=True):
    """Closed panel with a profiled edge and a round or planar painted face."""
    count = len(outline)
    vertices = [point(u * scale, v * scale, depth) for scale, depth, _ in profiles for u, v in outline]
    vertices.append(point(0, 0, profiles[-1][1]))
    faces = [tuple(reversed(range(count)))]
    indices = [profiles[0][2]]
    for level in range(len(profiles) - 1):
        start = level * count
        for i in range(count):
            j = (i + 1) % count
            faces.append((start + i, start + j, start + count + j, start + count + i))
            indices.append(profiles[level][2])
    start = (len(profiles) - 1) * count
    for i in range(count):
        faces.append((start + i, start + (i + 1) % count, len(vertices) - 1))
        indices.append(profiles[-1][2])
    return mesh(name, vertices, faces, materials, group, parent, indices, smooth)


def curved_tile(name, point, materials, group, parent):
    subdivisions = 20
    stride = subdivisions + 1
    vertices = []
    faces = []
    indices = []

    def square_to_patch(u, v):
        return u * math.sqrt(1 - .22 * v * v), v * math.sqrt(1 - .22 * u * u)

    for y in range(stride):
        v = 2 * y / subdivisions - 1
        for x in range(stride):
            u = 2 * x / subdivisions - 1
            s, t = square_to_patch(u, v)
            relief = .057 + .012 * (1 - u * u) * (1 - v * v)
            vertices.append(point(s * .923, t * .920, relief))
    for y in range(subdivisions):
        for x in range(subdivisions):
            a = y * stride + x
            faces.append((a, a + 1, a + 1 + stride, a + stride))
            indices.append(0)

    boundary = [(x, 0) for x in range(stride)]
    boundary += [(subdivisions, y) for y in range(1, stride)]
    boundary += [(x, subdivisions) for x in range(subdivisions - 1, -1, -1)]
    boundary += [(0, y) for y in range(subdivisions - 1, 0, -1)]
    edges = [[y * stride + x for x, y in boundary]]
    for scale, relief in ((.980, .048), (1, .032), (1, .016), (.960, -.012)):
        first = len(vertices)
        for x, y in boundary:
            s, t = square_to_patch(2 * x / subdivisions - 1, 2 * y / subdivisions - 1)
            vertices.append(point(s * scale, t * scale, relief))
        edges.append(list(range(first, first + len(boundary))))
    for level in range(len(edges) - 1):
        upper, lower = edges[level], edges[level + 1]
        for i in range(len(boundary)):
            j = (i + 1) % len(boundary)
            faces.append((lower[i], lower[j], upper[j], upper[i]))
            indices.append(0 if level < 2 else 1)
    faces.append(tuple(reversed(edges[-1])))
    indices.append(1)
    mesh(name, vertices, faces, materials, group, parent, indices)


def tiles(name, z_min, z_max, count, materials, group, parent, phase=0):
    half_angle = math.pi / count * .945
    mean_z, half_z = (z_min + z_max) * .5, (z_max - z_min) * .5
    for i in range(count):
        heading = TAU * i / count + phase

        def point(u, v, relief):
            z = mean_z + v * half_z
            angle = heading + u * half_angle
            r = shell_radius(z) * (1 + relief / RADIUS)
            return (r * math.sin(angle), -r * math.cos(angle), z * (1 + relief / HEIGHT))

        curved_tile(f"{name} {i + 1:02}", point, materials, group, parent)


def belt_gems(group, parent, gold, antique, dark, gems):
    bezel_outline = rounded_square(.28, 8)
    facet_outline = rounded_square(.29, 3)
    for i, gem in enumerate(gems):
        angle = TAU * (i + .5) / len(gems)
        outward = Vector((math.sin(angle), -math.cos(angle), 0))
        tangent = Vector((math.cos(angle), math.sin(angle), 0))
        center = outward * 1.095 + Vector((0, 0, -.085))

        def plane(half_width, half_height, start):
            def point(u, v, depth):
                return center + tangent * (u * half_width) + Vector((0, 0, v * half_height)) + outward * (start + depth)
            return point

        surfaced_panel(
            f"Engaste dourado {i + 1:02}", bezel_outline,
            [(1, .009, 1), (1, .037, 1), (.974, .056, 0), (.924, .069, 0),
             (.86, .077, 0), (.70, .077, 0), (.36, .077, 0)],
            plane(.224, .197, 0), [gold, antique], group, parent,
        )
        surfaced_panel(
            f"Assento {i + 1:02}", bezel_outline,
            [(1, 0, 0), (1, .009, 0), (.94, .013, 0)],
            plane(.176, .150, .073), [dark], group, parent,
        )
        surfaced_panel(
            f"{gem.name} {i + 1:02}", facet_outline,
            [(1, 0, 0), (1, .010, 0), (.953, .028, 0), (.758, .044, 0)],
            plane(.166, .141, .084), [gem], group, parent, smooth=False,
        )


def build():
    root_group = collection("Color Bomb • modelo")
    top_group = collection("01 • Cúpula giratória")
    belt_group = collection("02 • Cinturão fixo")
    base_group = collection("03 • Base giratória")
    cap_group = collection("04 • Botão rosa fixo")
    root = empty("Color Bomb | Royal Magic", root_group)
    root.rotation_euler = (math.radians(11), math.radians(9.5), 0)
    top = empty("Cúpula | giro à esquerda", top_group, root)
    belt = empty("Cinturão | fixo", belt_group, root)
    base = empty("Base | giro à direita", base_group, root)
    cap = empty("Botão rosa | fixo", cap_group, root)

    purple_shadow = enamel("Ameixa nas junções", "381158", .39, .07)
    purple = enamel("Cinturão violeta", "7425B1", .29, .24, emission=.06)
    violet = enamel("Aro violeta", "8A27C2", .29, .24, emission=.05)
    pink = enamel("Botão fúcsia", "F225A0", .27, .30, emission=.08)
    pink_shadow = enamel("Assento do botão", "53115F", .31, .11)
    orange = enamel("Esmalte laranja", "FFAC17", .23, .28, emission=.06)
    orange_side = enamel("Laranja no chanfro", "D87808", .31, .12)
    yellow = enamel("Esmalte amarelo", "FFD633", .25, .24, emission=.08)
    yellow_side = enamel("Âmbar no chanfro", "DB8D04", .31, .14)
    blue = enamel("Esmalte azul", "138BF3", .24, .30, emission=.07)
    blue_side = enamel("Azul no chanfro", "1247AC", .29, .18)
    gold = enamel("Ouro ensolarado", "FFC742", .26, .26, metal=.16, emission=.04)
    deep_gold = enamel("Ouro âmbar", "BC6B0A", .31, .19, metal=.10)
    gem_colors = [
        enamel(name, color, .19, .35, emission=.09)
        for name, color in [
            ("Esmeralda", "28E44B"), ("Safira", "12B7FD"), ("Ametista", "E04CDD"),
            ("Rubi", "F62F49"), ("Topázio", "FFCD18"),
        ]
    ]
    green, cyan, magenta, ruby, amber = gem_colors

    lathe("Núcleo interno", [(.002, -.345), (.92, -.345), (.92, .163), (.002, .163)],
          purple_shadow, belt_group, belt)
    shell("Casco superior", .135, .934, purple_shadow, top_group, top)
    shell("Casco inferior", -1.056, -.303, purple_shadow, base_group, base)
    tiles("Gomo laranja", .465, .833, 10, (orange, orange_side), top_group, top)
    tiles("Gomo amarelo", .151, .442, 10, (yellow, yellow_side), top_group, top)
    tiles("Gomo azul", -.933, -.357, 10, (blue, blue_side), base_group, base)
    lathe("Acabamento inferior",
          [(.010, -1.062), (.174, -1.060), (.265, -1.044), (.309, -1.024),
           (.321, -1.011), (.272, -1.005), (.003, -1.021)],
          violet, base_group, base)
    lathe("Aro largo independente",
          [(.922, -.356), (1.003, -.365), (1.060, -.353), (1.106, -.323),
           (1.125, -.279), (1.134, -.20), (1.134, .045), (1.124, .107),
           (1.106, .143), (1.069, .163), (1.018, .166), (.928, .143)],
          purple, belt_group, belt)
    belt_gems(belt_group, belt, gold, deep_gold, purple_shadow,
              [green, cyan, magenta, ruby, amber, cyan, magenta, green, ruby, amber])
    lathe("Colar do botão",
          [(.37, .829), (.515, .829), (.555, .850), (.575, .877), (.570, .908),
           (.549, .926), (.519, .934), (.409, .928), (.390, .902)],
          violet, cap_group, cap)
    lathe("Assento interno",
          [(.002, .889), (.429, .889), (.462, .906), (.455, .930), (.404, .947), (.002, .947)],
          pink_shadow, cap_group, cap)
    lathe("Pedra rosa",
          [(.002, .918), (.387, .918), (.421, .940), (.417, .984), (.385, 1.031),
           (.325, 1.077), (.246, 1.106), (.158, 1.125), (.067, 1.131), (.002, 1.132)],
          pink, cap_group, cap)

    for obj, direction in ((top, -1), (base, 1)):
        for frame, angle in ((1, 0), (FRAME_COUNT + 1, direction * TAU)):
            obj.rotation_euler.z = angle
            obj.keyframe_insert("rotation_euler", index=2, frame=frame)
        action = obj.animation_data.action
        channel_bag = action.layers[0].strips[0].channelbag(obj.animation_data.action_slot)
        for curve in channel_bag.fcurves:
            for point in curve.keyframe_points:
                point.interpolation = "LINEAR"
            curve.modifiers.new(type="CYCLES")
    return root


def studio(scene, samples):
    group = collection("Estúdio • câmera e luz")
    scene.render.engine = "CYCLES"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 5
    prefs = bpy.context.preferences.addons["cycles"].preferences
    try:
        prefs.compute_device_type = "OPTIX"
        prefs.get_devices()
        for device in prefs.devices:
            device.use = device.type == "OPTIX"
        if any(device.use for device in prefs.devices):
            scene.cycles.device = "GPU"
            scene.cycles.denoiser = "OPTIX"
    except (RuntimeError, TypeError):
        pass
    print(f"Cycles: {scene.cycles.device}", flush=True)
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.fps = FPS
    scene.frame_start, scene.frame_end = 1, FRAME_COUNT
    scene.view_settings.view_transform = "Khronos PBR Neutral"
    scene.view_settings.exposure = .02

    world = bpy.data.worlds.new("Ambiente violeta suave")
    world.use_nodes = True
    ambient = world.node_tree.nodes.get("Background")
    ambient.inputs["Color"].default_value = (.51, .46, .66, 1)
    ambient.inputs["Strength"].default_value = .32
    scene.world = world
    cam = bpy.data.objects.new("Câmera | tabuleiro", bpy.data.cameras.new("Câmera ortográfica"))
    group.objects.link(cam)
    cam.location = (0, -11, .012)
    cam.rotation_euler = (Vector((0, 0, .012)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 2.86
    scene.camera = cam

    for name, pos, power, size, size_y, tint in (
        ("Luz principal", (-3.4, -4.3, 5.1), 305, 4.2, 2.65, (1.0, .94, .83)),
        ("Preenchimento", (3.6, -4.2, .8), 137, 3.5, 4.6, (.71, .85, 1.0)),
        ("Recorte dourado", (1.25, 2.0, 4.0), 185, 4.75, 1.87, (1.0, .85, .68)),
        ("Reflexo inferior", (-1.3, -2.5, -3.0), 71, 4.3, 2.05, (.68, .74, 1.0)),
        ("Brilho desenhado", (-1.75, -4.15, 2.55), 37, 1.18, .78, (1.0, .96, .85)),
    ):
        lamp = bpy.data.lights.new(name, "AREA")
        lamp.shape = "RECTANGLE"
        lamp.energy, lamp.size, lamp.size_y, lamp.color = power, size, size_y, tint
        obj = bpy.data.objects.new(name, lamp)
        group.objects.link(obj)
        obj.location = pos
        obj.rotation_euler = (-obj.location).to_track_quat("-Z", "Y").to_euler()


def main():
    settings = options()
    settings.out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    studio(scene, settings.samples)
    root = build()
    scene.frame_set(1)
    bpy.context.view_layer.objects.active = root
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type == "VIEW_3D":
                area.spaces.active.region_3d.view_perspective = "CAMERA"
                area.spaces.active.shading.type = "MATERIAL"
                area.spaces.active.shading.use_scene_lights = True
                area.spaces.active.shading.use_scene_world = True
                area.spaces.active.overlay.show_overlays = False
    if settings.blend:
        scene.render.filepath = "//render/frame-"
        scene.cycles.samples = max(64, settings.samples)
        bpy.context.preferences.filepaths.save_version = 0
        bpy.ops.wm.save_as_mainfile(filepath=str(settings.blend.resolve()), compress=True)
    if settings.mode == "motion":
        scene.cycles.samples = settings.samples
        scene.render.resolution_percentage = settings.motion_percent
        scene.render.filepath = str(settings.out / "frame-")
        bpy.ops.render.render(animation=True)
    else:
        for frame, filename in ((1, "repouso.png"), (8, "giro.png")):
            scene.frame_set(frame)
            scene.render.filepath = str(settings.out / filename)
            bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
