# Avenaldo, o mago: blockout em 3D para o Blender 5.x.
#   blender -b -P modelar_avenaldo.py
# Volumes, proporções e cores da ficha do personagem, feitos de formas simples. Não é escultura final.
# Hierarquia: Avenaldo > Cabeca (> Chapeu), Tronco, Braco esquerdo (segura o cajado), Braco direito, Pernas.
import bpy, math, os
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def rgb(hexcode):
    h = hexcode.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1.0]


def material(name, hexcode, rough=0.55, metallic=0.0, sss=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = rgb(hexcode)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metallic
    if sss and 'Subsurface Weight' in b.inputs:
        b.inputs['Subsurface Weight'].default_value = sss
    return m


M = {
    'pele': material('Pele', '#F1B48E', 0.5, sss=0.15), 'nariz': material('Nariz', '#EC9076', 0.45, sss=0.15),
    'bochecha': material('Bochecha', '#F2A486', 0.5, sss=0.15),
    'barba': material('Barba', '#ECE8E4', 0.8), 'cabelo': material('Cabelo', '#A29E9A', 0.75), 'sobrancelha': material('Sobrancelha', '#5A5654', 0.8),
    'casaco': material('Casaco azul', '#2A3EA8', 0.7), 'colete': material('Colete', '#232F86', 0.7), 'calca': material('Calça', '#1E2A6E', 0.7),
    'creme': material('Creme', '#F3E7C6', 0.7), 'camisa': material('Camisa', '#FAFAFA', 0.6), 'gravata': material('Gravata', '#1B2560', 0.6),
    'ouro': material('Ouro', '#F2B43A', 0.3, 0.7), 'sapato': material('Sapato', '#4A2A1E', 0.35), 'livro': material('Livro', '#6E4028', 0.5),
    'madeira': material('Madeira do cajado', '#7A4A2A', 0.6),
    'olho': material('Olho', '#FFFFFF', 0.2), 'iris': material('Íris', '#4A2E1E', 0.2), 'boca': material('Boca', '#8A2A2A', 0.5),
}


def crystal_material():
    m = bpy.data.materials.new('Cristal')
    m.use_nodes = True
    b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = rgb('#7AE6FF')
    b.inputs['Roughness'].default_value = 0.1
    b.inputs['Emission Color'].default_value = rgb('#5ADCFF')
    b.inputs['Emission Strength'].default_value = 1.6
    return m


M['cristal'] = crystal_material()


def empty(name, parent=None, loc=(0, 0, 0)):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.15
    e.location = loc
    scene.collection.objects.link(e)
    e.parent = parent
    return e


def adopt(obj, name, mat, parent, local=False):
    obj.name = name
    obj.data.materials.append(M[mat])
    for p in obj.data.polygons:
        p.use_smooth = True
    world = obj.matrix_world.copy()
    obj.parent = parent
    bpy.context.view_layer.update()
    # local=True: a malha foi feita no espaço do pai (acompanha a posição e a inclinação dele)
    obj.matrix_parent_inverse = parent.matrix_world.inverted() if not local else parent.matrix_world.inverted() @ parent.matrix_world
    return obj


def ball(name, loc, scale, mat, parent, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=48, ring_count=24, radius=1.0, location=loc, rotation=[math.radians(a) for a in rot])
    obj = bpy.context.object
    obj.scale = scale if isinstance(scale, (tuple, list)) else (scale, scale, scale)
    return adopt(obj, name, mat, parent)


def limb(name, a, b, r0, r1, mat, parent, caps=True):
    a, b = Vector(a), Vector(b)
    axis = b - a
    bpy.ops.mesh.primitive_cone_add(vertices=40, radius1=r0, radius2=r1, depth=axis.length, location=(a + b) / 2)
    obj = bpy.context.object
    obj.rotation_euler = axis.to_track_quat('Z', 'Y').to_euler()
    adopt(obj, name, mat, parent)
    if caps:
        ball(name + ' (junta a)', a, r0, mat, parent)
        ball(name + ' (junta b)', b, r1, mat, parent)
    return obj


def lathe(name, profile, mat, parent, a0=0.0, a1=360.0, segments=64, scale_y=1.0, thickness=0.0, local=False):
    """Revolução em volta de Z; a0..a1 em graus a partir da frente (-Y). Com menos de 360 fica aberta na frente."""
    full = a1 - a0 >= 360.0
    count = segments if full else segments + 1
    verts, faces = [], []
    for r, z in profile:
        for i in range(count):
            ang = math.radians(a0 + (a1 - a0) * i / segments)
            verts.append((r * math.sin(ang), -r * math.cos(ang) * scale_y, z))
    for j in range(len(profile) - 1):
        for i in range(segments):
            k = (i + 1) % count
            if not full and i + 1 >= count:
                continue
            faces.append((j * count + i, j * count + k, (j + 1) * count + k, (j + 1) * count + i))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    if thickness:
        mod = obj.modifiers.new('Espessura', 'SOLIDIFY'); mod.thickness = thickness; mod.offset = 1.0
    sub = obj.modifiers.new('Suavizar', 'SUBSURF'); sub.levels = 2; sub.render_levels = 2
    return adopt(obj, name, mat, parent, local)


def box(name, loc, size, mat, parent, rot=(0, 0, 0), bevel=0.02):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=[math.radians(a) for a in rot])
    obj = bpy.context.object
    obj.scale = size
    mod = obj.modifiers.new('Borda', 'BEVEL'); mod.width = bevel; mod.segments = 3
    adopt(obj, name, mat, parent)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def torus(name, loc, major, minor, mat, parent, rot=(90, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(location=loc, rotation=[math.radians(a) for a in rot], major_radius=major, minor_radius=minor, major_segments=48, minor_segments=12)
    return adopt(bpy.context.object, name, mat, parent)


root = empty('Avenaldo')
tronco = empty('Tronco', root, (0, 0, 0.6))
cabeca = empty('Cabeca', root, (0, 0, 1.6))
pernas = empty('Pernas', root, (0, 0, 0.6))
braco_e = empty('Braco esquerdo', root, (0.52, 0, 1.36))       # esquerdo do personagem = direita de quem olha
braco_d = empty('Braco direito', root, (-0.52, 0, 1.36))

# ---- pés (o manto cobre as pernas)
for side, x in (('esquerdo', 0.2), ('direito', -0.2)):
    ball(f'Sapato {side}', (x * 1.08, -0.2, 0.085), (0.17, 0.3, 0.1), 'sapato', pernas)
    ball(f'Sola {side}', (x * 1.08, -0.2, 0.03), (0.175, 0.31, 0.04), 'iris', pernas)

# ---- manto de mago: longo, em sino, com faixa creme na frente, barra e cinto dourados
ROBE = [(0.6, 0.1), (0.68, 0.14), (0.67, 0.45), (0.61, 0.85), (0.55, 1.15), (0.44, 1.42), (0.27, 1.57), (0.18, 1.6)]
lathe('Manto', ROBE, 'casaco', tronco, scale_y=0.84, thickness=0.05)
lathe('Faixa da frente', [(r + 0.012, z) for r, z in ROBE[1:7]], 'creme', tronco, a0=-11, a1=11, segments=10, scale_y=0.84, thickness=0.02)
for s in (1, -1):
    lathe(f'Friso da faixa {s}', [(r + 0.02, z) for r, z in ROBE[1:7]], 'ouro', tronco, a0=s * 11 - 1.6, a1=s * 11 + 1.6, segments=3, scale_y=0.84, thickness=0.02)
lathe('Barra dourada', [(0.685, 0.1), (0.7, 0.15), (0.69, 0.2)], 'ouro', tronco, scale_y=0.84, thickness=0.02)
lathe('Cinto', [(0.6, 0.86), (0.615, 0.92), (0.595, 0.98)], 'ouro', tronco, scale_y=0.84, thickness=0.02)
ball('Fivela', (0, -0.52, 0.92), (0.085, 0.03, 0.085), 'cristal', tronco)
torus('Aro da fivela', (0, -0.525, 0.92), 0.09, 0.016, 'ouro', tronco)
ball('Gola', (0, -0.06, 1.56), (0.3, 0.27, 0.1), 'creme', tronco)
for i, (x, z, size) in enumerate(((0.36, 0.55, 0.045), (-0.42, 0.4, 0.035), (-0.3, 0.68, 0.03), (0.44, 0.3, 0.03), (0.28, 1.22, 0.035), (-0.36, 1.1, 0.03))):
    y = -math.sqrt(max(0.0, 1.0 - (x / 0.69) ** 2)) * 0.69 * 0.84 * (0.97 if z < 0.7 else 0.82)
    ball(f'Estrela do manto {i + 1}', (x, y, z), (size, 0.012, size), 'ouro', tronco)

# ---- braço direito do personagem (esquerda de quem olha): manga larga, mão aberta
sh, el, wr = (-0.52, 0.0, 1.36), (-0.78, -0.22, 1.1), (-0.74, -0.6, 1.2)
limb('Braço direito', sh, el, 0.17, 0.16, 'casaco', braco_d)
limb('Manga direita', el, wr, 0.16, 0.21, 'casaco', braco_d)
torus('Friso da manga direita', (-0.74, -0.6, 1.2), 0.2, 0.022, 'ouro', braco_d, rot=(-14, 0, 0))
ball('Forro da manga direita', (-0.74, -0.61, 1.2), (0.185, 0.05, 0.185), 'creme', braco_d, rot=(-14, 0, 0))
palm = Vector((-0.73, -0.74, 1.23))
ball('Mão direita', palm, (0.115, 0.13, 0.08), 'pele', braco_d, rot=(-20, 0, 0))
for k in range(4):
    dx = -0.075 + 0.05 * k
    limb(f'Dedo {k + 1}', palm + Vector((dx, -0.08, 0.02)), palm + Vector((dx * 1.3, -0.24 - 0.02 * (1.5 - abs(k - 1.5)), 0.085)), 0.036, 0.03, 'pele', braco_d)
limb('Polegar', palm + Vector((0.09, -0.02, 0.02)), palm + Vector((0.17, -0.09, 0.11)), 0.04, 0.034, 'pele', braco_d)

# ---- braço esquerdo do personagem (direita de quem olha): segura o cajado
sh, el, wr = (0.52, 0.0, 1.36), (0.8, -0.1, 1.06), (0.7, -0.42, 1.12)
limb('Braço esquerdo', sh, el, 0.17, 0.16, 'casaco', braco_e)
limb('Manga esquerda', el, wr, 0.16, 0.21, 'casaco', braco_e)
torus('Friso da manga esquerda', wr, 0.2, 0.022, 'ouro', braco_e, rot=(-10, 0, 20))
ball('Mão esquerda', (0.69, -0.52, 1.14), (0.11, 0.1, 0.1), 'pele', braco_e)
for k in range(4):
    ball(f'Dedo no cajado {k + 1}', (0.66, -0.6, 1.2 - 0.045 * k), (0.07, 0.04, 0.026), 'pele', braco_e)
cajado = empty('Cajado', braco_e, (0, 0, 0))
limb('Haste do cajado', (0.7, -0.6, 0.03), (0.74, -0.58, 2.42), 0.04, 0.034, 'madeira', cajado)
torus('Anel do cajado', (0.735, -0.582, 2.2), 0.05, 0.02, 'ouro', cajado, rot=(0, 0, 0))
for k in range(4):
    ang = math.radians(45 + 90 * k)
    limb(f'Garra do cajado {k + 1}', (0.74, -0.58, 2.4), (0.74 + 0.13 * math.cos(ang), -0.58 + 0.13 * math.sin(ang), 2.6), 0.03, 0.018, 'ouro', cajado)
ball('Cristal do cajado', (0.745, -0.578, 2.62), 0.13, 'cristal', cajado)

# ---- cabeça
ball('Cabeça', (0, -0.02, 1.98), (0.43, 0.41, 0.43), 'pele', cabeca)
for s in (1, -1):
    ball('Orelha', (s * 0.43, 0.03, 1.97), (0.06, 0.09, 0.12), 'pele', cabeca)
    ball('Bochecha', (s * 0.24, -0.3, 1.88), (0.15, 0.13, 0.13), 'bochecha', cabeca)
    ball('Olho', (s * 0.16, -0.355, 2.03), (0.085, 0.06, 0.1), 'olho', cabeca)
    ball('Íris', (s * 0.155, -0.405, 2.025), (0.045, 0.02, 0.055), 'iris', cabeca)
    ball('Brilho do olho', (s * 0.155 - 0.02, -0.425, 2.05), 0.014, 'olho', cabeca)
    limb('Sobrancelha', (s * 0.07, -0.4, 2.165), (s * 0.27, -0.33, 2.19), 0.04, 0.03, 'sobrancelha', cabeca)
    torus('Lente dos óculos', (s * 0.16, -0.425, 2.01), 0.115, 0.012, 'ouro', cabeca)
    limb('Haste dos óculos', (s * 0.275, -0.41, 2.02), (s * 0.44, 0.0, 2.03), 0.01, 0.01, 'ouro', cabeca, caps=False)
    ball('Bigode', (s * 0.12, -0.41, 1.8), (0.15, 0.085, 0.075), 'barba', cabeca, rot=(0, s * 18, 0))
    ball('Cabelo lateral', (s * 0.38, 0.1, 2.12), (0.14, 0.27, 0.22), 'cabelo', cabeca)
limb('Ponte dos óculos', (-0.05, -0.43, 2.02), (0.05, -0.43, 2.02), 0.01, 0.01, 'ouro', cabeca, caps=False)
ball('Nariz', (0, -0.43, 1.9), (0.115, 0.11, 0.095), 'nariz', cabeca)
ball('Boca', (0, -0.385, 1.725), (0.12, 0.05, 0.06), 'boca', cabeca)
ball('Barba', (0, -0.1, 1.75), (0.44, 0.39, 0.33), 'barba', cabeca)
ball('Queixo da barba', (0, -0.22, 1.62), (0.3, 0.26, 0.2), 'barba', cabeca)
ball('Cabelo de trás', (0, 0.24, 2.02), (0.39, 0.26, 0.34), 'cabelo', cabeca)
ball('Franja', (0.1, -0.33, 2.24), (0.22, 0.1, 0.08), 'cabelo', cabeca, rot=(0, 14, 8))

# ---- chapéu de mago: aba larga, cone alto com a ponta caída, faixa dourada e estrela
chapeu = empty('Chapeu', cabeca, (0, 0.04, 0.66))
chapeu.rotation_euler = (math.radians(-9), math.radians(5), 0)
bpy.context.view_layer.update()
lathe('Aba do chapéu', [(0.74, -0.02), (0.76, 0.01), (0.6, 0.05), (0.46, 0.09)], 'casaco', chapeu, thickness=0.04, local=True)
lathe('Friso da aba', [(0.765, -0.025), (0.785, 0.005), (0.765, 0.035)], 'ouro', chapeu, thickness=0.02, local=True)
lathe('Copa do chapéu', [(0.46, 0.07), (0.43, 0.2), (0.36, 0.42), (0.27, 0.64), (0.18, 0.82), (0.1, 0.96), (0.04, 1.04), (0.01, 1.06)], 'casaco', chapeu, local=True)
lathe('Faixa do chapéu', [(0.475, 0.08), (0.47, 0.15), (0.445, 0.22)], 'ouro', chapeu, thickness=0.02, local=True)
tip = chapeu.matrix_world @ Vector((0, 0, 1.03))
limb('Ponta caída', tip, tip + Vector((0.2, 0.12, -0.13)), 0.05, 0.02, 'casaco', chapeu)
front = chapeu.matrix_world @ Vector((0, -0.455, 0.15))
ball('Estrela do chapéu', front, (0.075, 0.02, 0.075), 'cristal', chapeu)
torus('Aro da estrela', front, 0.08, 0.014, 'ouro', chapeu, rot=(80, 0, 0))

# ---- câmera e luz
cam_data = bpy.data.cameras.new('Camera'); cam_data.lens = 85
cam = bpy.data.objects.new('Camera', cam_data)
scene.collection.objects.link(cam)
target = Vector((0.05, 0, 1.68))
cam.location = (0.05, -12.5, 2.0)
cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
scene.camera = cam


def light(name, energy, location, size, color=(1, 1, 1)):
    data = bpy.data.lights.new(name, 'AREA'); data.energy = energy; data.size = size; data.color = color
    obj = bpy.data.objects.new(name, data)
    obj.location = location
    obj.rotation_euler = (target - Vector(location)).to_track_quat('-Z', 'Y').to_euler()
    scene.collection.objects.link(obj)


light('Luz principal', 620, (-3.5, -5, 5), 5, (1.0, 0.96, 0.9))
light('Preenchimento', 350, (4.5, -4, 2), 6, (0.85, 0.9, 1.0))
light('Contraluz', 600, (1.5, 4.5, 4.5), 4, (1.0, 0.9, 0.8))
world = bpy.data.worlds.new('Mundo'); world.use_nodes = True
bg = world.node_tree.nodes.get('Background'); bg.inputs[0].default_value = (0.75, 0.74, 0.78, 1); bg.inputs[1].default_value = 0.5
scene.world = world

for engine in ('BLENDER_EEVEE', 'BLENDER_EEVEE_NEXT'):
    try:
        scene.render.engine = engine
        break
    except TypeError:
        pass
scene.render.film_transparent = True
scene.render.resolution_x, scene.render.resolution_y = 720, 900
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
try:
    scene.view_settings.view_transform = 'Standard'
except TypeError:
    pass

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, 'avenaldo-mago-blockout.blend'))
for angle, name in ((0, 'frente.png'), (-35, 'tres-quartos.png'), (-90, 'lado.png')):
    root.rotation_euler = (0, 0, math.radians(angle))
    scene.render.filepath = os.path.join(HERE, name)
    bpy.ops.render.render(write_still=True)
root.rotation_euler = (0, 0, 0)
print('AVENALDO_OK')
