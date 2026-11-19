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


def material(name, hex_code, metallic=0, roughness=0.3, coat=0, glow=0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    shader = mat.node_tree.nodes.get("Principled BSDF")
    rgba = color(hex_code)
    shader.inputs["Base Color"].default_value = rgba
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    shader.inputs["Coat Weight"].default_value = coat
    shader.inputs["Coat Roughness"].default_value = 0.13
    shader.inputs["Emission Color"].default_value = rgba
    shader.inputs["Emission Strength"].default_value = glow
    mat.diffuse_color = rgba
    return mat


def prism_material():
    mat = material("Prisma • refração estilizada", "9B73FE", 0.30, 0.175, 0.59, 0.21)
    tree = mat.node_tree
    shader = tree.nodes.get("Principled BSDF")
    coords = tree.nodes.new("ShaderNodeTexCoord")
    gradient = tree.nodes.new("ShaderNodeVectorMath")
    gradient.operation = "DOT_PRODUCT"
    gradient.inputs[1].default_value = (0.30, 0.83, 0.03)
    spectrum = tree.nodes.new("ShaderNodeValToRGB")
    spectrum.color_ramp.interpolation = "EASE"
    stops = [
        (0.10, "088547"), (0.29, "03DCAB"), (0.43, "08CCFF"),
        (0.55, "284DE1"), (0.67, "8528DC"), (0.78, "ED38B9"), (0.92, "FB5D59"),
    ]
    for index, (position, hex_code) in enumerate(stops):
        element = spectrum.color_ramp.elements[index] if index < 2 else spectrum.color_ramp.elements.new(position)
        element.position = position
        element.color = color(hex_code)
    tree.links.new(coords.outputs["Generated"], gradient.inputs[0])
    tree.links.new(gradient.outputs["Value"], spectrum.inputs[0])
    tree.links.new(spectrum.outputs["Color"], shader.inputs["Base Color"])
    tree.links.new(spectrum.outputs["Color"], shader.inputs["Emission Color"])
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
    modifier = bevel(obj, width, 4)
    modifier.harden_normals = True
    return obj


def faceted(name, outline, depth, mat, parent, location, upper=0.72):
    """A smooth table surrounded by broad, planar gemstone facets."""
    count = len(outline)
    vertices = [(x, y, 0) for x, y in outline]
    vertices += [(x, y, depth * 0.29) for x, y in outline]
    vertices += [(x * upper, y * upper, depth) for x, y in outline]
    faces = [tuple(reversed(range(count))), tuple(range(2 * count, 3 * count))]
    for level in range(2):
        for i in range(count):
            j = (i + 1) % count
            faces.append((level * count + i, level * count + j,
                          (level + 1) * count + j, (level + 1) * count + i))
    obj = mesh(name, vertices, faces, mat, parent, location)
    bevel(obj, 0.012, 2)
    return obj


def octagon(width, height, corner):
    x, y = width / 2, height / 2
    return [(x, -y + corner), (x, y - corner), (x - corner, y), (-x + corner, y),
            (-x, y - corner), (-x, -y + corner), (-x + corner, -y), (x - corner, -y)]


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


def build_cube(parent, gold, deep_gold, gems, prism):
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
                box("Carcaça de ouro", (0.918, 0.918, 0.918), gold, piece, 0.102)
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
                    faceted("Aro rebaixado", octagon(0.758, 0.758, 0.135), 0.031,
                            deep_gold, anchor, (0, 0, 0), upper=0.97)
                    if position == (0, -1, 0) and normal == (0, -1, 0):
                        gem = faceted("Prisma central", octagon(0.522, 0.522, 0.055), 0.145,
                                      prism, anchor, (0, 0, 0.035), upper=0.64)
                        gem.rotation_euler.z = radians(45)
                    else:
                        faceted("Cristal", octagon(0.653, 0.653, 0.12), 0.085,
                                gems[shade], anchor, (0, 0, 0.026), upper=0.70)
    sphere("Coração interno", (0.88, 0.88, 0.88), deep_gold, parent, (0, 0, 0))
    return pieces


def build_talisman(parent, gold, bright_gold, deep_gold, dark, gems, prism):
    annulus("Corpo fixo", [(0.001, -0.08), (1.43, -0.08), (1.50, -0.01), (1.43, 0.10), (0.001, 0.10)],
            dark, parent)
    rail("Assento metálico", 1.45, -0.029, 0.16, 0.17, deep_gold, parent)
    rail("Colar do prisma", 0.67, 0.31, 0.148, 0.18, gold, parent)
    rail("Lábio polido", 0.611, 0.371, 0.065, 0.06, bright_gold, parent)
    sphere("Prisma fixo", (0.606, 0.606, 0.376), prism, parent, (0, 0, 0.337))

    inner = empty("Coroa interna • gira", parent=parent)
    rail("Trilho interno inferior", 0.729, 0.20, 0.094, 0.106, deep_gold, inner)
    rail("Trilho interno superior", 1.168, 0.206, 0.096, 0.115, gold, inner)
    gem_outline = [(0.30, 0), (0.11, 0.27), (-0.15, 0.25), (-0.245, 0), (-0.15, -0.25), (0.11, -0.27)]
    for i, gem_index in enumerate((1, 0, 3, 4, 2)):
        angle = radians(18 + i * 72)
        rg = 0.948
        cell = empty("Engaste da coroa", (rg * math.cos(angle), rg * math.sin(angle), 0.20), inner)
        cell.rotation_euler.z = angle
        faceted("Escudo dourado", [(x * 1.04, y * 1.16) for x, y in gem_outline], 0.105,
                gold, cell, (0, 0, 0), upper=0.9)
        faceted("Pedra da coroa", [(x * 0.82, y * 0.82) for x, y in gem_outline], 0.114,
                gems[gem_index], cell, (0, 0, 0.088), upper=0.58)

    outer = empty("Coroa externa • contragiro", parent=parent)
    rail("Círculo exterior", 1.508, 0.145, 0.181, 0.252, gold, outer)
    for i in range(4):
        sector = i * 90
        rail("Quarto externo", 1.489, 0.229, 0.125, 0.136, gold, outer, sector + 9, 72)
        points = []
        for j in range(43):
            fraction = j / 42
            angle = radians(sector + 7 + 76 * fraction)
            radius = 1.474 - 0.217 * math.sin(math.pi * fraction) ** 1.08
            depth = 0.236 + 0.17 * math.sin(math.pi * fraction)
            points.append((radius * math.cos(angle), radius * math.sin(angle), depth))
        curve_tube("Arco de ouro independente", points, 0.049, gold, outer)
    return inner, outer


def setup_lighting(scene):
    world = bpy.data.worlds.new("Estúdio violeta")
    scene.world = world
    world.use_nodes = True
    background = world.node_tree.nodes.get("Background")
    background.inputs["Color"].default_value = (0.37, 0.30, 0.49, 1)
    background.inputs["Strength"].default_value = 0.27
    for center in (-2.39, 2.39):
        for name, shift, target, watts, size, size_y, tint in [
            ("Reflexo principal", (-2.6, -4.5, 4.9), (0, 0, 0), 440, 4.0, 2.25, (1.0, 0.93, 0.79)),
            ("Preenchimento azul", (2.75, -4.1, 0.2), (0, 0, 0), 175, 3.0, 5.1, (0.64, 0.79, 1.0)),
            ("Recorte quente", (1.85, 1.3, 4.0), (0, 0, 0), 360, 3.65, 1.4, (1.0, 0.73, 0.43)),
            ("Faixa inferior", (-1.3, -2.75, -3.4), (0, 0, 0), 108, 4.0, 1.7, (0.71, 0.65, 1.0)),
            ("Ponto de brilho", (-0.8, -3.6, 1.02), (0, 0, 0.1), 42, 0.58, 0.38, (1.0, 0.98, 0.94)),
        ]:
            light = bpy.data.lights.new(name, "AREA")
            light.shape = "RECTANGLE"
            light.energy = watts
            light.size = size
            light.size_y = size_y
            light.color = tint
            lamp = bpy.data.objects.new(name, light)
            bpy.context.collection.objects.link(lamp)
            lamp.location = (center + shift[0], shift[1], shift[2])
            aim = Vector((center + target[0], target[1], target[2]))
            lamp.rotation_euler = (aim - lamp.location).to_track_quat("-Z", "Y").to_euler()


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
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 4
    scene.cycles.glossy_bounces = 3
    scene.cycles.diffuse_bounces = 2
    prefs = bpy.context.preferences.addons["cycles"].preferences
    try:
        prefs.compute_device_type = "OPTIX"
        prefs.get_devices()
        for device in prefs.devices:
            device.use = device.type == "OPTIX"
        if any(device.use for device in prefs.devices):
            scene.cycles.device = "GPU"
            scene.cycles.denoiser = "OPTIX"
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
    scene.view_settings.view_transform = "Khronos PBR Neutral"
    scene.view_settings.exposure = -0.12
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
    setup_lighting(scene)

    gold = material("Ouro quente polido", "F1AE47", 0.93, 0.275, 0.10)
    bright_gold = material("Ouro claro", "FFC15D", 0.88, 0.23, 0.13)
    deep_gold = material("Ouro escuro nas juntas", "684012", 0.80, 0.36)
    dark = material("Liga de metal azul", "221C39", 0.80, 0.31)
    gems = [
        material(name, hex_code, 0.12, 0.24, 0.28, 0.13)
        for name, hex_code in [
            ("Rubi", "E9234C"), ("Safira", "087FF2"), ("Esmeralda", "0ABF78"),
            ("Âmbar", "FFAC28"), ("Ametista", "8D4AEC"),
        ]
    ]
    prism = prism_material()
    cube = empty("Cubo do Caos", (-2.36, 0, 0))
    cube.rotation_euler = (radians(19), 0, radians(-28))
    cube.scale = (0.94,) * 3
    talisman = empty("Talismã dos Elementos", (2.36, 0, 0))
    talisman.rotation_euler.x = radians(84)
    talisman.scale = (1.09,) * 3

    pieces = build_cube(cube, gold, deep_gold, gems, prism)
    inner, outer = build_talisman(talisman, gold, bright_gold, deep_gold, dark, gems, prism)
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
