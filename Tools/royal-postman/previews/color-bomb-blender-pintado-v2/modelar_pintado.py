# Light ball em 3D com pintura de desenho 2D (Blender 5.x).
#   blender -b -P modelar_pintado.py
# As cores são pintadas no próprio modelo (borda clara em cima, borda escura embaixo, brilho branco em cada peça)
# e o contorno roxo escuro é uma casca invertida. Não depende de luz.
# Hierarquia: LightBall > Topo (gira), Base (gira ao contrário), Cinturao (fixo).
import bpy, bmesh, math, os

HERE = os.path.dirname(os.path.abspath(__file__))
FRAMES = 48
OUTLINE = 0.02

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def rgb(hexcode):
    h = hexcode.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]


def mix(a, b, t):
    t = max(0.0, min(1.0, t))
    return [a[i] + (b[i] - a[i]) * t for i in range(3)]


class Paint:
    def __init__(self, base, light, shadow, wall):
        self.base, self.light, self.shadow, self.wall = rgb(base), rgb(light), rgb(shadow), rgb(wall)


YELLOW = Paint('#FFD61A', '#FFF486', '#F29A00', '#C46A00')
ORANGE = Paint('#FF9814', '#FFC864', '#E26600', '#B04600')
BLUE = Paint('#1E8EFF', '#74CAFF', '#0C54D2', '#0A3A9E')
BLUE2 = Paint('#1A7CF4', '#62B8FF', '#0A48C0', '#08328E')
GOLD = Paint('#FFC02A', '#FFEC90', '#E08800', '#A85800')
PURPLE = Paint('#7C2CE8', '#A866FF', '#5A18BC', '#42109A')
PINK = Paint('#F21E9E', '#FF7CCC', '#B80E7A', '#8A0A60')
GEMS = [Paint('#18C828', '#8EF27E', '#0A8E1C', '#066A14'), Paint('#F21A1A', '#FF8272', '#B40A12', '#86060C'),
        Paint('#FFC800', '#FFF292', '#E48C00', '#B06400'), Paint('#18C828', '#8EF27E', '#0A8E1C', '#066A14'),
        Paint('#1EA8FF', '#A2E0FF', '#0A64D2', '#0848A0'), Paint('#D828E0', '#F6A0F8', '#8C0AA2', '#66087A')]
CORE = rgb('#3A1082')
INK = rgb('#2C0A66')


def paint_material():
    m = bpy.data.materials.new('Pintura')
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    col = nt.nodes.new('ShaderNodeVertexColor'); col.layer_name = 'Cor'
    em = nt.nodes.new('ShaderNodeEmission')
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(col.outputs['Color'], em.inputs['Color'])
    nt.links.new(em.outputs['Emission'], out.inputs['Surface'])
    return m


def flat_material(name, color, alpha=1.0, cull=False):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Color'].default_value = list(color) + [1.0]
    out = nt.nodes.new('ShaderNodeOutputMaterial')
    if alpha < 1.0:
        tr = nt.nodes.new('ShaderNodeBsdfTransparent')
        mx = nt.nodes.new('ShaderNodeMixShader'); mx.inputs[0].default_value = alpha
        nt.links.new(tr.outputs[0], mx.inputs[1]); nt.links.new(em.outputs[0], mx.inputs[2])
        nt.links.new(mx.outputs[0], out.inputs['Surface'])
        for attr, value in (('surface_render_method', 'BLENDED'), ('blend_method', 'BLEND')):
            try:
                setattr(m, attr, value)
            except (AttributeError, TypeError):
                pass
    else:
        nt.links.new(em.outputs[0], out.inputs['Surface'])
    m.use_backface_culling = cull
    return m


PAINT = paint_material()
INK_MAT = flat_material('Contorno', INK, cull=True)
GLOSS = flat_material('Brilho branco', [1, 1, 1], alpha=0.55)


def empty(name, parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.4
    scene.collection.objects.link(e)
    e.parent = parent
    return e


def finish(name, bm, parent, outline=True, material=None):
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(material or PAINT)
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    obj.parent = parent
    if outline:
        mesh.materials.append(INK_MAT)
        mod = obj.modifiers.new('Contorno', 'SOLIDIFY')
        mod.thickness = OUTLINE
        mod.offset = 1.0
        mod.use_flip_normals = True
        mod.use_rim = False
        mod.material_offset = 1
    return obj


def rounded_square(radius=0.24, segments=5):
    pts = []
    for q in range(4):
        cx, cy = ((1, 1), (-1, 1), (-1, -1), (1, -1))[q]
        for k in range(segments + 1):
            a = q * math.pi / 2 + k / segments * math.pi / 2
            pts.append((cx * (1 - radius) + radius * math.cos(a), cy * (1 - radius) + radius * math.sin(a)))
    return pts


def panel(name, mapping, rings, color, parent, outline_pts=None, outline=True, material=None):
    """Peça fechada por anéis concêntricos. rings = [(escala, relevo)], color(faixa, u, v, escala) -> cor."""
    pts = outline_pts or rounded_square()
    n = len(pts)
    bm = bmesh.new()
    layer = bm.loops.layers.float_color.new('Cor')
    info = {}
    rows = []
    for s, d in rings:
        row = []
        for u, v in pts:
            vert = bm.verts.new(mapping(u * s, v * s, d))
            info[vert] = (u, v, s)
            row.append(vert)
        rows.append(row)
    center = bm.verts.new(mapping(0, 0, rings[-1][1]))
    info[center] = (0.0, 0.0, 0.0)

    def face(verts, band):
        f = bm.faces.new(verts)
        for loop in f.loops:
            u, v, s = info[loop.vert]
            loop[layer] = list(color(band, u, v, s)) + [1.0]

    for band in range(len(rows) - 1):
        a, b = rows[band], rows[band + 1]
        for j in range(n):
            k = (j + 1) % n
            face((a[j], a[k], b[k], b[j]), band)
    last = rows[-1]
    for j in range(n):
        face((last[j], last[(j + 1) % n], center), len(rows) - 1)
    return finish(name, bm, parent, outline, material)


def revolve(name, profile, bands, parent, segments=96, outline=True):
    """profile = [(raio, z)], bands = cor de cada faixa entre dois pontos do perfil."""
    bm = bmesh.new()
    layer = bm.loops.layers.float_color.new('Cor')
    rows = [[bm.verts.new((r * math.sin(2 * math.pi * i / segments), -r * math.cos(2 * math.pi * i / segments), z)) for i in range(segments)]
            for r, z in profile]
    for band in range(len(rows) - 1):
        for i in range(segments):
            j = (i + 1) % segments
            f = bm.faces.new((rows[band][i], rows[band][j], rows[band + 1][j], rows[band + 1][i]))
            for loop in f.loops:
                loop[layer] = list(bands[band]) + [1.0]
    return finish(name, bm, parent, outline)


def edge_light(u, v):
    """Luz pintada na borda: vem de cima, um pouco da esquerda. +1 = borda clara, -1 = borda escura."""
    length = math.hypot(u, v) or 1.0
    return (-0.4 * u + 0.92 * v) / length


def tile_colors(p):
    def color(band, u, v, s):
        if band == 0:
            return p.wall
        if band == 1:
            k = edge_light(u, v)
            return mix(p.base, p.light, k * 1.3) if k > 0 else mix(p.base, p.shadow, -k * 1.4)
        height = v * s
        return mix(p.base, p.light, 0.45 * height) if height > 0 else mix(p.base, p.shadow, -0.35 * height)
    return color


def sphere_map(latc, half_lat, lonc, half_lon):
    def mapping(u, v, d):
        lat = math.radians(latc + v * half_lat); lon = math.radians(lonc + u * half_lon); r = 1.0 + d
        return (r * math.cos(lat) * math.sin(lon), -r * math.cos(lat) * math.cos(lon), r * math.sin(lat))
    return mapping


TILE_RINGS = [(1.0, -0.07), (1.0, 0.012), (0.9, 0.05), (0.8, 0.056), (0.5, 0.058), (0.25, 0.058)]


def tile_row(prefix, lat0, lat1, count, p, parent, fill=0.9, phase=0.0):
    half_lon = 180.0 / count * fill
    for i in range(count):
        lonc = phase + i * 360.0 / count
        m = sphere_map((lat0 + lat1) / 2, (lat1 - lat0) / 2, lonc, half_lon)
        panel(f'{prefix} {i + 1:02d}', m, TILE_RINGS, tile_colors(p), parent)
        # brilho branco pintado no canto de cima, à esquerda
        gloss = lambda u, v, d, m=m: m(-0.3 + u * 0.36, 0.44 + v * 0.17, 0.064 + d)
        panel(f'{prefix} {i + 1:02d} brilho', gloss, [(1.0, 0.0), (0.5, 0.0)], lambda *_: [1, 1, 1], parent,
              rounded_square(0.6, 4), outline=False, material=GLOSS)


root = empty('LightBall')
topo, base, cinturao = empty('Topo', root), empty('Base', root), empty('Cinturao', root)

# miolo roxo escuro (aparece nas juntas)
bpy.ops.mesh.primitive_uv_sphere_add(segments=96, ring_count=48, radius=0.985)
miolo = bpy.context.object
miolo.name = 'Miolo'
miolo.data.materials.append(flat_material('Miolo', CORE))
miolo.parent = root
ink_shell = miolo.modifiers.new('Contorno', 'SOLIDIFY')
miolo.data.materials.append(INK_MAT)
ink_shell.thickness = OUTLINE; ink_shell.offset = 1.0; ink_shell.use_flip_normals = True; ink_shell.material_offset = 1

# ---- TOPO
tile_row('Ladrilho amarelo', 13, 40, 8, YELLOW, topo)
tile_row('Ladrilho laranja', 44, 64, 8, ORANGE, topo, fill=0.86)


def ring_profile(points):
    return [((1.0 + d) * math.cos(math.radians(a)), (1.0 + d) * math.sin(math.radians(a))) for a, d in points]


revolve('Colar roxo', ring_profile([(66.5, -0.03), (67, 0.045), (70, 0.07), (74, 0.075), (77, 0.06), (79, 0.02)]),
        [PURPLE.wall, PURPLE.shadow, PURPLE.base, PURPLE.light, PURPLE.base], topo)
button = [(0.31, 0.955), (0.325, 0.99), (0.30, 1.04), (0.24, 1.085), (0.15, 1.115), (0.06, 1.128), (0.001, 1.13)]
revolve('Botão rosa', button, [PINK.shadow, PINK.base, mix(PINK.base, PINK.light, 0.3), mix(PINK.base, PINK.light, 0.55), mix(PINK.base, PINK.light, 0.7), PINK.light], topo, segments=64)
panel('Botão rosa brilho', lambda u, v, d: (-0.07 + u * 0.13, -0.33 - d, 1.05 + v * 0.03), [(1.0, 0.0), (0.5, 0.0)], lambda *_: [1, 1, 1], topo,
      rounded_square(0.7, 4), outline=False, material=GLOSS)

# ---- BASE
tile_row('Ladrilho azul', -52, -24, 8, BLUE, base)
tile_row('Ladrilho azul de baixo', -77, -56, 8, BLUE2, base, fill=0.86)
revolve('Tampa azul', ring_profile([(-90, 0.05), (-86, 0.05), (-82, 0.045), (-80, 0.02)]), [BLUE2.shadow, BLUE2.base, BLUE2.wall], base)

# ---- CINTURÃO: faixa roxa e 10 pedras quadradas engastadas em ouro
Z0, Z1 = -0.36, 0.17
belt = [(0.93, Z0 - 0.005), (1.02, Z0), (1.04, Z0 + 0.035), (1.05, Z0 + 0.1), (1.053, Z1 - 0.13), (1.05, Z1 - 0.07), (1.04, Z1 - 0.03), (1.02, Z1), (0.95, Z1 + 0.005)]
revolve('Faixa roxa', belt, [PURPLE.wall, PURPLE.shadow, mix(PURPLE.base, PURPLE.shadow, 0.5), PURPLE.base, mix(PURPLE.base, PURPLE.light, 0.45), PURPLE.light,
                             mix(PURPLE.base, PURPLE.light, 0.4), PURPLE.shadow], cinturao)
ZC = (Z0 + Z1) / 2
COUNT = 10
GEM_ORDER = [GEMS[1], GEMS[2], GEMS[0], GEMS[4], GEMS[5]]      # de frente: vermelha, amarela, verde, azul


def belt_map(angle, half):
    def mapping(u, v, d):
        a = angle + u * half / 1.05          # acompanha a curva do cinturão
        r = 1.05 + d
        return (r * math.sin(a), -r * math.cos(a), ZC + v * half)
    return mapping


def gold_colors(band, u, v, s):
    if band == 0:
        return GOLD.wall
    k = edge_light(u, v)
    if band in (1, 2):
        return mix(GOLD.base, GOLD.light, k * 1.2) if k > 0 else mix(GOLD.base, GOLD.shadow, -k * 1.3)
    return mix(GOLD.shadow, GOLD.wall, 0.5 + 0.5 * k)          # parede de dentro, onde a pedra assenta


def gem_colors(p):
    def color(band, u, v, s):
        if band == 0:
            return p.wall
        if band == 1:                                           # facetas: clara em cima, escura embaixo
            k = edge_light(u, v)
            return mix(p.base, p.light, k * 1.5) if k > 0 else mix(p.base, p.shadow, -k * 1.6)
        return mix(p.base, p.light, 0.25 + 0.35 * v * s)       # mesa da pedra
    return color


for i in range(COUNT):
    angle = 2 * math.pi * (i + 0.5) / COUNT
    panel(f'Engaste {i + 1:02d}', belt_map(angle, 0.238), [(1.0, -0.02), (1.0, 0.03), (0.9, 0.058), (0.74, 0.058), (0.7, 0.03), (0.4, 0.03)], gold_colors, cinturao,
          rounded_square(0.3, 6))
    stone = belt_map(angle, 0.238)
    panel(f'Pedra {i + 1:02d}', stone, [(0.68, 0.03), (0.68, 0.062), (0.44, 0.092), (0.2, 0.092)], gem_colors(GEM_ORDER[(i + 2) % len(GEM_ORDER)]), cinturao,
          rounded_square(0.3, 2), outline=False)
    panel(f'Pedra {i + 1:02d} brilho', lambda u, v, d, m=stone: m(-0.2 + u * 0.2, 0.2 + v * 0.1, 0.097 + d), [(1.0, 0.0), (0.5, 0.0)], lambda *_: [1, 1, 1], cinturao,
          rounded_square(0.6, 4), outline=False, material=GLOSS)

# ---- animação: topo e base giram em sentidos opostos; o cinturão fica parado (laço de 48 quadros)
bpy.context.preferences.edit.keyframe_new_interpolation_type = 'LINEAR'
scene.frame_start, scene.frame_end = 1, FRAMES
scene.render.fps = 24
for obj, turn in ((topo, 1.0), (base, -1.0)):
    obj.rotation_euler = (0, 0, 0)
    obj.keyframe_insert('rotation_euler', index=2, frame=1)
    obj.rotation_euler = (0, 0, turn * 2 * math.pi)
    obj.keyframe_insert('rotation_euler', index=2, frame=FRAMES + 1)

# ---- câmera (a pintura não usa luz)
cam_data = bpy.data.cameras.new('Camera')
cam_data.type = 'ORTHO'
cam_data.ortho_scale = 2.6
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
pitch = math.radians(9)
cam.location = (0, -8 * math.cos(pitch), 8 * math.sin(pitch) + 0.03)
cam.rotation_euler = (math.radians(90) - pitch, 0, 0)
scene.camera = cam
world = bpy.data.worlds.new('Mundo')
scene.world = world

for engine in ('BLENDER_EEVEE', 'BLENDER_EEVEE_NEXT'):
    try:
        scene.render.engine = engine
        break
    except TypeError:
        pass
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = 768
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
try:
    scene.view_settings.view_transform = 'Standard'
except TypeError:
    pass

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, 'light-ball-pintado.blend'))
for frame, name in ((1, 'previa.png'), (3, 'previa-giro.png')):
    scene.frame_set(frame)
    scene.render.filepath = os.path.join(HERE, name)
    bpy.ops.render.render(write_still=True)
print('LIGHTBALL_OK', scene.render.engine)
