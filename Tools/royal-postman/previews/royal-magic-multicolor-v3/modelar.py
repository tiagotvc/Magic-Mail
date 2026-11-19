"""Build and render the two Royal Magic concepts in Blender background mode."""

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Quaternion, Vector


def arguments():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, required=True)
    parser.add_argument("--mode", choices=("still", "motion"), default="still")
    parser.add_argument("--samples", type=int, default=20)
    parser.add_argument("--percent", type=int, default=100)
    parser.add_argument("--blend", type=Path)
    parser.add_argument("--cpu", action="store_true")
    return parser.parse_args(sys.argv[sys.argv.index("--") + 1 :])


def radians(degrees):
    return math.radians(degrees)


def linear(channel):
    channel /= 255
    if channel <= 0.04045:
        return channel / 12.92
    return ((channel + 0.055) / 1.055) ** 2.4


def color(hex_code):
    return tuple(linear(int(hex_code[i : i + 2], 16)) for i in (0, 2, 4)) + (1,)


def dot(tree, vector, direction):
    node = tree.nodes.new("ShaderNodeVectorMath")
    node.operation = "DOT_PRODUCT"
    tree.links.new(vector, node.inputs[0])
    node.inputs[1].default_value = direction
    return node.outputs["Value"]


def ramp(tree, value, stops):
    node = tree.nodes.new("ShaderNodeValToRGB")
    node.color_ramp.interpolation = "EASE"
    for index, (position, hex_code) in enumerate(stops):
        element = node.color_ramp.elements[index] if index < 2 else node.color_ramp.elements.new(position)
        element.position = position
        element.color = color(hex_code)
    tree.links.new(value, node.inputs[0])
    return node.outputs["Color"]


def mix(tree, factor, base, tint):
    node = tree.nodes.new("ShaderNodeMixRGB")
    for index, value in enumerate((factor, base, tint)):
        if isinstance(value, bpy.types.NodeSocket):
            tree.links.new(value, node.inputs[index])
        else:
            node.inputs[index].default_value = value
    return node.outputs["Color"]


def shade(tree, value, low, high, strength):
    node = tree.nodes.new("ShaderNodeMapRange")
    node.interpolation_type = "SMOOTHSTEP"
    node.clamp = True
    node.inputs["From Min"].default_value = low
    node.inputs["From Max"].default_value = high
    node.inputs["To Max"].default_value = strength
    tree.links.new(value, node.inputs["Value"])
    return node.outputs["Result"]


def paint_material(name, tint):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = color(tint)
    mat.cycles.emission_sampling = "NONE"
    mat.node_tree.nodes.clear()
    return mat


def finish_paint(tree, base, normal, gloss, highlight, reflection_start=0.91):
    reflection = dot(tree, normal, Vector((-0.39, -0.87, 0.38)).normalized())
    shine = shade(tree, reflection, reflection_start, 0.995, gloss)
    lit = mix(tree, shine, base, color(highlight))
    emitter = tree.nodes.new("ShaderNodeEmission")
    output = tree.nodes.new("ShaderNodeOutputMaterial")
    tree.links.new(lit, emitter.inputs["Color"])
    tree.links.new(emitter.outputs[0], output.inputs["Surface"])


def material(name, shadows, mids, lights, gloss=0.15, highlight="FFF2C3"):
    mat = paint_material(name, mids)
    tree = mat.node_tree
    normal = tree.nodes.new("ShaderNodeNewGeometry").outputs["Normal"]
    key = dot(tree, normal, Vector((-0.57, -0.57, 0.68)).normalized())
    body = ramp(tree, key, [(0.0, shadows), (0.47, mids), (1.0, lights)])
    finish_paint(tree, body, normal, gloss, highlight)
    return mat


def prism_material():
    mat = paint_material("Prisma • luz colorida", "9B73FE")
    tree = mat.node_tree
    coords = tree.nodes.new("ShaderNodeTexCoord")
    gradient = dot(tree, coords.outputs["Generated"], (0.30, 0.83, 0.03))
    stops = [
        (0.10, "117C64"), (0.29, "2CEDAB"), (0.43, "2DEBFF"),
        (0.55, "5590FF"), (0.67, "8D56FF"), (0.78, "F34EC2"), (0.92, "FF926C"),
    ]
    normal = tree.nodes.new("ShaderNodeNewGeometry").outputs["Normal"]
    facing = dot(tree, normal, (0, -1, 0))
    rim = shade(tree, facing, 0.07, 0.57, 0.71)
    body = mix(tree, rim, color("643CB2"), ramp(tree, gradient, stops))
    finish_paint(tree, body, normal, 0.76, "FFF4E8", 0.935)
    return mat


def empty(name, location=(0, 0, 0), parent=None):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    return obj


def mesh(name, vertices, faces, mat, parent, location=(0, 0, 0)):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    obj.location = location
    obj.data.materials.append(mat)
    return obj


def bevel(obj, width, segments=3):
    modifier = obj.modifiers.new("Chanfro", "BEVEL")
    modifier.width = width
    modifier.segments = segments
    return modifier


def box(name, span, mat, parent, width):
    xyz = [n / 2 for n in span]
    vertices = [
        (x * xyz[0], y * xyz[1], z * xyz[2])
        for x, y, z in [
            (-1, -1, -1), (1, -1, -1), (1, 1, -1), (-1, 1, -1),
            (-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1),
        ]
    ]
    obj = mesh(
        name, vertices,
        [(3, 2, 1, 0), (4, 5, 6, 7), (0, 1, 5, 4),
         (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
        mat, parent,
    )
    for face in obj.data.polygons:
        face.use_smooth = True
    bevel(obj, width, 6)
    modifier = obj.modifiers.new("Normais suaves", "WEIGHTED_NORMAL")
    modifier.keep_sharp = True
    return obj


def rounded_plate(name, width, height, depth, mat, parent, location, exponent=4):
    count = 48
    outline = []
    for i in range(count):
        angle = math.tau * i / count
        x, y = math.cos(angle), math.sin(angle)
        outline.append((width / 2 * math.copysign(abs(x) ** (2 / exponent), x),
                        height / 2 * math.copysign(abs(y) ** (2 / exponent), y)))
    profile = [(0.94, 0), (1, 0.15), (0.99, 0.39), (0.94, 0.67), (0.85, 0.89),
               (0.69, 0.99), (0.50, 1.05), (0.27, 1.085), (0.10, 1.098)]
    vertices = [(x * radius, y * radius, z * depth) for radius, z in profile for x, y in outline]
    vertices.append((0, 0, 1.1 * depth))
    faces = [tuple(reversed(range(count)))]
    for level in range(len(profile) - 1):
        start = level * count
        faces.extend((start + i, start + (i + 1) % count,
                      start + count + (i + 1) % count, start + count + i) for i in range(count))
    start = (len(profile) - 1) * count
    faces.extend((start + i, start + (i + 1) % count, len(vertices) - 1) for i in range(count))
    obj = mesh(name, vertices, faces, mat, parent, location)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def annulus(name, profile, mat, parent, start=0, arc=360, segments=128):
    closed = arc == 360
    samples = segments if closed else segments + 1
    vertices = []
    for i in range(samples):
        angle = radians(start + arc * i / segments)
        vertices.extend((r * math.cos(angle), r * math.sin(angle), z) for r, z in profile)
    count = len(profile)
    faces = []
    for i in range(segments):
        other = ((i + 1) % samples) * count
        for j in range(count):
            k = (j + 1) % count
            faces.append((i * count + j, i * count + k, other + k, other + j))
    if not closed:
        faces.extend((tuple(reversed(range(count))), tuple(range(segments * count, samples * count))))
    obj = mesh(name, vertices, [tuple(reversed(face)) for face in faces], mat, parent)
    for face in obj.data.polygons[:segments * count]:
        face.use_smooth = True
    return obj


def rail(name, r, z, width, depth, mat, parent, start=0, arc=360):
    half_width, half_depth = width / 2, depth / 2
    profile = [
        (r + half_width * math.cos(radians(i * 22.5)), z + half_depth * math.sin(radians(i * 22.5)))
        for i in range(16)
    ]
    return annulus(name, profile, mat, parent, start, arc, max(14, round(128 * arc / 360)))


def curve_tube(name, points, radius, mat, parent):
    data = bpy.data.curves.new(name, "CURVE")
    data.dimensions = "3D"
    data.bevel_depth = radius
    data.bevel_resolution = 4
    data.use_fill_caps = True
    path = data.splines.new("POLY")
    path.points.add(len(points) - 1)
    for point, xyz in zip(path.points, points):
        point.co = (*xyz, 1)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    data.materials.append(mat)
    return obj


def sphere(name, size, mat, parent, location):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=72, ring_count=36)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.location = location
    obj.scale = size
    obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    return obj


def build_cube(parent, gold, dark, gems, prism):
    visible_colors = {
        (0, -1, 0): ((4, 1, 1), (3, 4, 2), (0, 0, 2)),
        (1, 0, 0): ((2, 1, 1), (1, 2, 0), (4, 3, 3)),
        (0, 0, 1): ((1, 0, 0), (1, 4, 2), (3, 4, 2)),
    }
    directions = (*visible_colors, (-1, 0, 0), (0, 1, 0), (0, 0, -1))
    pieces = {}
    for x in (-1, 0, 1):
        for y in (-1, 0, 1):
            for z in (-1, 0, 1):
                position = (x, y, z)
                if not any(position):
                    continue
                piece = empty(f"Cubolet {x} {y} {z}", position, parent)
                piece.rotation_mode = "QUATERNION"
                pieces[position] = piece
                box("Carcaça azul", (0.929, 0.929, 0.929), dark, piece, 0.135)
                for normal in directions:
                    if sum(p * n for p, n in zip(position, normal)) != 1:
                        continue
                    n = Vector(normal)
                    anchor = empty("Engaste", n * 0.452, piece)
                    anchor.rotation_euler = n.to_track_quat("Z", "Y").to_euler()
                    if normal == (0, -1, 0):
                        row, col = 1 - z, x + 1
                    elif normal == (1, 0, 0):
                        row, col = 1 - z, y + 1
                    else:
                        row, col = y + 1, x + 1
                    palette = visible_colors.get(normal)
                    shade = palette[row][col] if palette else (x + 2 * y + 3 * z + 13) % 5
                    rounded_plate("Friso de ouro", 0.837, 0.837, 0.041,
                                  gold, anchor, (0, 0, 0), exponent=5.4)
                    if position == (0, -1, 0) and normal == (0, -1, 0):
                        rounded_plate("Base do prisma central", 0.744, 0.744, 0.073,
                                      dark, anchor, (0, 0, 0.029), exponent=5)
                        gem = rounded_plate("Prisma central", 0.520, 0.520, 0.130,
                                            prism, anchor, (0, 0, 0.082), exponent=3.25)
                        gem.rotation_euler.z = radians(45)
                    else:
                        rounded_plate("Placa esmaltada", 0.775, 0.775, 0.105,
                                      gems[shade], anchor, (0, 0, 0.031), exponent=5.25)
    sphere("Coração interno", (0.88, 0.88, 0.88), dark, parent, (0, 0, 0))
    return pieces


def build_talisman(parent, gold, deep_gold, dark, ring_blue, gems, prism):
    annulus("Corpo fixo", [(0.001, -0.08), (1.43, -0.08), (1.50, -0.01), (1.43, 0.10), (0.001, 0.10)],
            dark, parent)
    rail("Assento metálico", 1.446, -0.027, 0.182, 0.17, deep_gold, parent)
    rail("Colar do prisma", 0.662, 0.317, 0.194, 0.198, gold, parent)
    sphere("Prisma fixo", (0.606, 0.606, 0.376), prism, parent, (0, 0, 0.337))

    inner = empty("Coroa interna • gira", parent=parent)
    annulus("Coroa azul", [(0.755, 0.17), (1.183, 0.17), (1.213, 0.202),
            (1.195, 0.248), (1.153, 0.269), (0.781, 0.269), (0.755, 0.239)], ring_blue, inner)
    rail("Friso interno", 1.163, 0.255, 0.071, 0.071, gold, inner)
    for i, gem_index in enumerate((1, 0, 3, 4, 2)):
        angle = radians(18 + i * 72)
        rg = 0.977
        cell = empty("Engaste da coroa", (rg * math.cos(angle), rg * math.sin(angle), 0.25), inner)
        cell.rotation_euler.z = angle
        rounded_plate("Engaste dourado", 0.435, 0.530, 0.080,
                      gold, cell, (0, 0, 0), exponent=2.8)
        rounded_plate("Joia arredondada", 0.352, 0.438, 0.121,
                      gems[gem_index], cell, (0, 0, 0.062), exponent=2.6)

    outer = empty("Coroa externa • contragiro", parent=parent)
    rail("Círculo exterior", 1.491, 0.164, 0.228, 0.252, gold, outer)
    for i in range(4):
        sector = i * 90
        points = []
        for j in range(43):
            fraction = j / 42
            angle = radians(sector + 7 + 76 * fraction)
            radius = 1.461 - 0.166 * math.sin(math.pi * fraction) ** 1.11
            depth = 0.198 + 0.16 * math.sin(math.pi * fraction)
            points.append((radius * math.cos(angle), radius * math.sin(angle), depth))
        curve_tube("Arco de ouro independente", points, 0.074, gold, outer)
    return inner, outer


def set_transform(obj, position, rotation, frame):
    obj.location = position
    obj.rotation_quaternion = rotation
    obj.keyframe_insert("location", frame=frame)
    obj.keyframe_insert("rotation_quaternion", frame=frame)


def animate(pieces, inner, outer):
    states = {obj: (Vector(pos), Quaternion()) for pos, obj in pieces.items()}
    for obj, (position, rotation) in states.items():
        set_transform(obj, position, rotation, 1)
    for start, end, axis_index, layer, direction in [
        (10, 25, 2, 0, -1),
        (33, 48, 0, 1, 1),
        (62, 77, 0, 1, -1),
        (85, 100, 2, 0, 1),
    ]:
        axis = Vector(tuple(float(i == axis_index) for i in range(3)))
        selected = [(obj, pos, q) for obj, (pos, q) in states.items() if round(pos[axis_index]) == layer]
        assert len(selected) == (8 if layer == 0 else 9)
        for frame in range(start, end + 1):
            progress = (frame - start) / (end - start)
            eased = progress * progress * (3 - 2 * progress)
            turn = Quaternion(axis, radians(90) * direction * eased)
            for obj, position, rotation in selected:
                set_transform(obj, turn @ position, turn @ rotation, frame)
                if frame == end:
                    states[obj] = (Vector(tuple(round(v) for v in turn @ position)), turn @ rotation)
    for initial, obj in pieces.items():
        position, rotation = states[obj]
        assert (position - Vector(initial)).length < 1e-5 and abs(rotation.angle) < 1e-5
        set_transform(obj, Vector(initial), Quaternion(), 108)
    for ring, rotation in ((inner, 144), (outer, -108)):
        for frame, fraction in ((1, 0), (10, 0), (48, 1), (62, 1), (100, 0), (108, 0)):
            ring.rotation_euler.z = radians(rotation) * fraction
            ring.keyframe_insert("rotation_euler", index=2, frame=frame)


def configuration(scene, options):
    scene.render.engine = "CYCLES"
    scene.cycles.samples = options.samples
    scene.cycles.use_denoising = False
    scene.cycles.max_bounces = 0
    if not options.cpu:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        try:
            prefs.compute_device_type = "OPTIX"
            prefs.get_devices()
            for device in prefs.devices:
                device.use = device.type == "OPTIX"
            if any(device.use for device in prefs.devices):
                scene.cycles.device = "GPU"
        except (TypeError, RuntimeError):
            pass
    print(f"Royal Magic render device: {scene.cycles.device}", flush=True)
    scene.render.film_transparent = True
    scene.render.resolution_x = 1152
    scene.render.resolution_y = 540
    scene.render.resolution_percentage = options.percent
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.fps = 24
    scene.frame_start = 1
    scene.frame_end = 108
    scene.view_settings.view_transform = "Standard"
    camera = bpy.data.objects.new("Câmera ortográfica", bpy.data.cameras.new("Câmera ortográfica"))
    bpy.context.collection.objects.link(camera)
    camera.location = (0, -17, 0)
    camera.rotation_euler = (Vector((0, 0, 0)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 9.10
    scene.camera = camera


def main():
    options = arguments()
    options.out.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    configuration(scene, options)
    gold = material("Ouro de desenho", "C35C11", "FFAD14", "FFE452", 0.29, "FFF5AE")
    deep_gold = material("Âmbar na base", "71320E", "C96C17", "F8AA2A", 0.12)
    dark = material("Estrutura azul-marinho", "14102D", "262F65", "557CC6", 0.16, "A1C6FF")
    ring_blue = material("Coroa azul", "191333", "283B83", "5584DC", 0.16, "A1C6FF")
    gems = [
        material(name, shadow, mid, light, 0.37, glow)
        for name, shadow, mid, light, glow in [
            ("Rubi", "A51042", "E9213C", "FF7161", "FFE3CF"),
            ("Safira", "0D4AB5", "158FEB", "4CDBFF", "D5F8FF"),
            ("Esmeralda", "118438", "29C746", "89EB46", "EDFFD4"),
            ("Âmbar", "CE6310", "FFA610", "FFE941", "FFFBC7"),
            ("Ametista", "5623AB", "973FDD", "D371FF", "F7DFFF"),
        ]
    ]
    prism = prism_material()
    cube = empty("Cubo do Caos", (-2.36, 0, 0))
    cube.rotation_euler = (radians(19), 0, radians(-28))
    cube.scale = (0.94,) * 3
    talisman = empty("Talismã dos Elementos", (2.36, 0, 0))
    talisman.rotation_euler.x = radians(84)
    talisman.scale = (1.09,) * 3

    pieces = build_cube(cube, gold, dark, gems, prism)
    inner, outer = build_talisman(talisman, gold, deep_gold, dark, ring_blue, gems, prism)
    animate(pieces, inner, outer)
    scene.frame_set(1)
    if options.blend:
        scene.render.filepath = "//render/frame-"
        bpy.ops.wm.save_as_mainfile(filepath=str(options.blend.resolve()), compress=True)
    if options.mode == "motion":
        scene.render.filepath = str(options.out / "frame-")
        bpy.ops.render.render(animation=True)
    else:
        for frame, name in ((1, "repouso"), (17, "giro-fileira"), (40, "giro-coluna")):
            scene.frame_set(frame)
            scene.render.filepath = str(options.out / f"{name}.png")
            bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
