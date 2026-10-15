"""Generate a native editable level using verified built-in object templates."""
import copy
import json
import math
import uuid
from pathlib import Path

folder = Path(__file__).resolve().parent
templates = json.loads((folder / 'editor-templates.json').read_text(encoding='utf-8-sig'))
level = copy.deepcopy(templates['empty'])
level.update(Title='Naval Ocean', Description='Editable sea base. Sea surface Y=40; seabed Y=5. F6 starts naval gameplay with Naval Prototype. Build freely on the islands.',
             AuthorName='Kirill', ArenaIsHidden=True, WorkshopUploadID=0,
             GeneratedUniqueID=str(uuid.uuid4()))
level['RootLevelObject']['Children']['$values'] = objects = []
index = 0

def node(template, name, position, scale):
    global index
    obj = copy.deepcopy(templates[template])
    index += 1
    obj['Name'], obj['UniqueIndex'] = name, index
    obj['Transform']['Position']['$values'] = list(position)
    obj['Transform']['Scale']['$values'] = list(scale)
    obj['Transform']['Rotation']['$values'] = [0, 0, 0, 1]
    obj['Children']['$values'] = []
    objects.append(obj)
    return obj

def field(obj, category, name, value):
    for item in obj[category]['$values']:
        if item['FieldName'] == name:
            item['Value'] = value
            return
    raise ValueError(f'Missing native field {name}')

def color(rgb):
    return {'$type': 'UnityEngine.Color, UnityEngine.CoreModule',
            'r': rgb[0], 'g': rgb[1], 'b': rgb[2], 'a': 1.0}

def block(name, position, size, rgb, collision=True, transparent=False):
    obj = node('water' if transparent else 'flat', name, position, size)
    field(obj, 'CustomInspectorColorValues', 'Color', color(rgb))
    field(obj, 'CustomInspectorFloatValues', 'GlowAmount', 0.0)
    field(obj, 'CustomInspectorFloatValues', 'Alpha', 0.75 if transparent else 1.0)
    field(obj, 'CustomInspectorBoolValues', 'DisableColliders', not collision)
    return obj

block('NAVAL_OCEAN_SURFACE', (0, 39.98, 0), (30000, 0.04, 30000), (0.045, 0.30, 0.39), False, True)
block('Ocean seabed - editable', (0, 4, 0), (30000, 2, 30000), (0.55, 0.46, 0.30))
# The test spawn platform is away from both ship spawn positions.
block('Launch pier', (-35, 40, -40), (16, 2, 16), (0.43, 0.31, 0.19))
spawn = node('flat', 'Sea playtest spawn', (-35, 42, -40), (1, 1, 1))
spawn['LevelObjectEntry'] = {'$type': 'LevelObjectEntry, Assembly-CSharp',
    'DisplayName': 'LevelEditorPlaytestSpawnPoint',
    'PathUnderResources': 'Prefabs/LevelObjects/StoryMode/LevelEditorPlaytestSpawnPoint',
    'PreviewPathUnderResources': 'Prefabs/LevelObjects/StoryMode/LevelEditorPlaytestSpawnPoint.png'}
for key in ('CustomInspectorFloatValues', 'CustomInspectorIntValues', 'CustomInspectorStringValues', 'CustomInspectorBoolValues', 'CustomInspectorColorValues'):
    spawn[key]['$values'] = []
spawn['CustomInspectorBoolValues']['$values'] = [{
    '$type': 'CustomInspectorBoolValue, Assembly-CSharp', 'Value': True,
    'ClassName': 'LevelEditorPlaytestSpawnpoint', 'FieldName': 'IsActivated',
    'OverrideClassDisplayName': None, 'IsHiddenField': False,
    'IsHiddenFromNonUnityEditor': False, 'ChangesFieldVisibility': False}]

def height(x, z, radius, seed):
    angle = math.atan2(z, x)
    coast = radius * (1 + 0.10 * math.sin(angle * 3 + seed) + 0.06 * math.cos(angle * 5 - seed))
    t = math.hypot(x, z) / coast
    if t > 1.25:
        return -35
    h = (1 - t) * 22 - 1.4 + 0.8 * math.sin(x * .18 + seed) * math.cos(z * .17 - seed)
    return math.floor(h / 1.2) * 1.2

for island, (cx, cz, radius) in enumerate(((150, 80, 28), (-150, 140, 35), (110, -200, 24), (-240, -120, 42), (360, 260, 30)), 1):
    extent = math.ceil(radius * 1.25 / 6) * 6
    seed = island + 6
    for x in range(-extent, extent, 6):
        for z in range(-extent, extent, 6):
            top = height(x + 3, z + 3, radius, seed)
            if top <= -34:
                continue
            rgb = (0.68, 0.56, 0.34) if top < 2 else (0.34, 0.33, 0.27) if top > 12 else (0.27, 0.40, 0.16)
            block(f'Island {island} terrain {x} {z}', (cx + x + 3, 40 + (top - 35) / 2, cz + z + 3), (6, top + 35, 6), rgb)
    for tree in range(2):
        x, z = -6 + tree * 12, 4 - tree * 6
        y = 40 + height(math.floor(x / 6) * 6 + 3, math.floor(z / 6) * 6 + 3, radius, seed)
        for step in range(6):
            block(f'Island {island} palm {tree} trunk {step}', (cx + x + step * .12, y + step + .5, cz + z), (.65, 1, .65), (.30, .20, .105))
        for branch in range(6):
            angle = branch * math.pi / 3
            for segment in range(3):
                block(f'Island {island} palm {tree} leaf {branch}-{segment}',
                      (cx+x+.6+math.cos(angle)*(segment+1), y+6.2-segment*.3, cz+z+math.sin(angle)*(segment+1)),
                      (1.4, .3, 1.4), (.20, .40, .12))

lighting = node('light', 'Sea daylight - editable', (0, 45, -20), (1, 1, 1))
for key, changes in {
    'CustomInspectorFloatValues': {'DirectionalRotationX': 50, 'DirectionalRotationY': 325, 'DirectionalIntensity': 1.1, 'FogStartDistance': 300, 'FogEndDistance': 5500},
    'CustomInspectorBoolValues': {'IsActive': True, 'EnableDirectionalLight': True, 'FogEnabled': True},
    'CustomInspectorColorValues': {'DirectionalColor': color((1, .94, .82)), 'AmbientColor': color((.65, .70, .75)), 'SkyColor': color((.23, .48, .72)), 'HorizonColor': color((.53, .73, .82)), 'FogColor': color((.53, .73, .82))}
}.items():
    existing = {v['FieldName'] for v in lighting[key]['$values']}
    for name, value in changes.items():
        if name in existing:
            field(lighting, key, name, value)

target = folder / 'Naval Ocean.json'
target.write_text(json.dumps(level, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
assert len({obj['UniqueIndex'] for obj in objects}) == len(objects)
assert sum(obj['Name'] == 'NAVAL_OCEAN_SURFACE' for obj in objects) == 1
assert all(min(obj['Transform']['Scale']['$values']) > 0 for obj in objects)
print(f'Created native editor level: {target.name}; {len(objects)} objects; {target.stat().st_size:,} bytes')
