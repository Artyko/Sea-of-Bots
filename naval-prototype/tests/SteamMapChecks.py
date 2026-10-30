import json
from pathlib import Path

path = Path(__file__).resolve().parents[1] / 'maps' / 'Steam Ocean.json'
level = json.loads(path.read_text(encoding='utf-8-sig'))
objects = level['RootLevelObject']['Children']['$values']
names = [obj['Name'] for obj in objects]

assert level['Title'] == 'Steam Ocean'
assert 'sunny' in level['Description'].lower()
assert level['ArenaIsHidden'] is True
assert len(objects) == 5
assert len({obj['UniqueIndex'] for obj in objects}) == len(objects)
assert names.count('NAVAL_STEAM_OCEAN_SURFACE') == 1
assert 'NAVAL_OCEAN_SURFACE' not in names
assert not any(name.startswith('Island ') for name in names)
assert not any('terrain' in name.lower() for name in names)
assert all(min(obj['Transform']['Scale']['$values']) > 0 for obj in objects)

water = objects[names.index('NAVAL_STEAM_OCEAN_SURFACE')]
disabled = {item['FieldName']: item['Value'] for item in water['CustomInspectorBoolValues']['$values']}
assert disabled['DisableColliders'] is True
assert any(name == 'Steam ocean seabed' for name in names)
assert any('spawn' in name.lower() for name in names)
assert any('sunny daylight' in name.lower() for name in names)

light = objects[names.index('Steam sunny daylight')]
floats = {item['FieldName']: item['Value'] for item in light['CustomInspectorFloatValues']['$values']}
bools = {item['FieldName']: item['Value'] for item in light['CustomInspectorBoolValues']['$values']}
colors = {item['FieldName']: item['Value'] for item in light['CustomInspectorColorValues']['$values']}
assert floats['DirectionalIntensity'] >= 1.0
assert floats['SkyExposure'] >= 1.0
assert floats['FogEndDistance'] >= 5000
assert bools['EnableDirectionalLight'] is True
assert bools['AmbientUsesSkybox'] is True
assert colors['DirectionalColor']['r'] > colors['DirectionalColor']['b']
assert colors['SkyColor']['b'] > colors['SkyColor']['r']

print('PASS: Steam Ocean has five native objects, one steam marker, no islands, and warm sunny daylight.')
