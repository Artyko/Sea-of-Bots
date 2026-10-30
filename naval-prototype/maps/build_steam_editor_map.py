"""Generate the native open-ocean editor level for the steam era."""
import copy
import json
from pathlib import Path

folder = Path(__file__).resolve().parent
templates = json.loads((folder / 'editor-templates.json').read_text(encoding='utf-8-sig'))
level = copy.deepcopy(templates['empty'])
level.update(
    Title='Steam Ocean',
    Description='Open sunny sea for the Naval Prototype steam era. No islands or shore obstacles.',
    AuthorName='Kirill',
    ArenaIsHidden=True,
    WorkshopUploadID=0,
    GeneratedUniqueID='93dcc5d2-f350-4be7-96f1-69ebc06cf297')
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
    field(obj, 'CustomInspectorFloatValues', 'Alpha', 0.78 if transparent else 1.0)
    field(obj, 'CustomInspectorBoolValues', 'DisableColliders', not collision)
    return obj

block('NAVAL_STEAM_OCEAN_SURFACE', (0, 39.98, 0), (30000, 0.04, 30000), (0.055, 0.31, 0.46), False, True)
block('Steam ocean seabed', (0, 4, 0), (30000, 2, 30000), (0.25, 0.31, 0.32))
# The editor needs a physical playtest spawn, but it is kept far outside the
# 450 m steam-ship encounter and is not part of the battlefield.
block('Remote launch platform', (-450, 40, -450), (12, 2, 12), (0.24, 0.27, 0.29))
spawn = node('flat', 'Steam playtest spawn', (-450, 42, -450), (1, 1, 1))
spawn['LevelObjectEntry'] = {
    '$type': 'LevelObjectEntry, Assembly-CSharp',
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

lighting = node('light', 'Steam sunny daylight', (0, 45, -20), (1, 1, 1))
changes = {
    'CustomInspectorFloatValues': {
        'DirectionalRotationX': 50, 'DirectionalRotationY': 325,
        'DirectionalIntensity': 1.10, 'DirectionalShadowStrength': 0.632,
        'SunSize': 0.04, 'AtmosphereThickness': 1.0,
        'FogStartDistance': 300, 'FogEndDistance': 5500,
        'SkyExposure': 1.20, 'SkyIntensity': 1.0},
    'CustomInspectorIntValues': {
        'SkyboxIndex': 3},
    'CustomInspectorBoolValues': {
        'IsActive': True, 'EnableDirectionalLight': True,
        'AmbientUsesSkybox': True, 'FogEnabled': True},
    'CustomInspectorColorValues': {
        'DirectionalColor': color((1.0, 0.94, 0.82)),
        'AmbientColor': color((0.65, 0.70, 0.75)),
        'SkyColor': color((0.23, 0.48, 0.72)),
        'HorizonColor': color((0.53, 0.73, 0.82)),
        'FogColor': color((0.53, 0.73, 0.82)),
        'GroundColor': color((0.04, 0.074, 0.20))}}
for category, values in changes.items():
    for name, value in values.items():
        field(lighting, category, name, value)

target = folder / 'Steam Ocean.json'
target.write_text(json.dumps(level, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')
assert len(objects) == 5
assert len({obj['UniqueIndex'] for obj in objects}) == len(objects)
assert sum(obj['Name'] == 'NAVAL_STEAM_OCEAN_SURFACE' for obj in objects) == 1
assert not any(obj['Name'].startswith('Island ') for obj in objects)
assert all(min(obj['Transform']['Scale']['$values']) > 0 for obj in objects)
print(f'Created native editor level: {target.name}; {len(objects)} objects; {target.stat().st_size:,} bytes')
