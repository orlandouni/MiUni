"""Add non-destructive animation clips to the supplied rigged GLB."""
from pathlib import Path
import json, struct, math

source = Path(r'C:\Users\orlan\Downloads\Meshy_AI_Character_output.glb')
data = source.read_bytes()
json_length = struct.unpack_from('<I', data, 12)[0]
doc = json.loads(data[20:20 + json_length])
binary_start = 20 + json_length
binary_length, binary_kind = struct.unpack_from('<II', data, binary_start)
assert binary_kind == 0x004e4942
blob = bytearray(data[binary_start + 8:binary_start + 8 + binary_length])
nodes = {n.get('name'): i for i, n in enumerate(doc['nodes'])}

def accessor(values, width, kind, bounds=False):
    while len(blob) % 4: blob.append(0)
    offset = len(blob)
    flat = [v for row in values for v in row] if width > 1 else values
    blob.extend(struct.pack('<' + 'f' * len(flat), *flat))
    view = len(doc.setdefault('bufferViews', []))
    doc['bufferViews'].append({'buffer': 0, 'byteOffset': offset, 'byteLength': len(flat)*4})
    a = {'bufferView': view, 'componentType': 5126, 'count': len(values), 'type': kind}
    if bounds: a.update(min=[min(values)], max=[max(values)])
    index = len(doc.setdefault('accessors', [])); doc['accessors'].append(a)
    return index

def multiply(q, r):
    x,y,z,w=q; a,b,c,d=r
    result=[w*a+x*d+y*c-z*b,w*b-x*c+y*d+z*a,w*c+x*b-y*a+z*d,w*d-x*a-y*b-z*c]
    length=math.sqrt(sum(v*v for v in result))
    return [v/length for v in result]

def rotation_track(clip, name, times, angles, axis):
    node=nodes[name]; base=doc['nodes'][node].get('rotation',[0,0,0,1])
    values=[multiply(base,[axis[0]*math.sin(a/2),axis[1]*math.sin(a/2),axis[2]*math.sin(a/2),math.cos(a/2)]) for a in angles]
    sampler=len(clip['samplers'])
    clip['samplers'].append({'input':accessor(times,1,'SCALAR',True),'output':accessor(values,4,'VEC4'),'interpolation':'LINEAR'})
    clip['channels'].append({'sampler':sampler,'target':{'node':node,'path':'rotation'}})

clips=[]
idle={'name':'Reposo','samplers':[],'channels':[]}
rotation_track(idle,'Bone_008',[0,1,2,3,4],[0,.035,0,-.035,0],[0,0,1])
rotation_track(idle,'SmartRigArmature',[0,1,2,3,4],[0,.012,0,-.012,0],[0,0,1])
clips.append(idle)
wave={'name':'Saludo','samplers':[],'channels':[]}
rotation_track(wave,'Bone_011',[0,.4,.7,1,1.3,1.6,1.9,2.4,3.2,4.5],[0,.65,.10,.65,.10,.65,.10,0,0,0],[0,0,1])
rotation_track(wave,'Bone_010',[0,.4,.7,1,1.3,1.6,1.9,2.4,4.5],[0,.22,-.12,.22,-.12,.22,-.12,0,0],[1,0,0])
rotation_track(wave,'Bone_008',[0,.5,1.7,2.4,4.5],[0,-.10,-.10,0,0],[0,0,1])
clips.append(wave)
nod={'name':'Asentir','samplers':[],'channels':[]}
rotation_track(nod,'Bone_008',[0,.3,.6,.9,1.2,1.5],[0,.10,0,.10,0,0],[1,0,0])
clips.append(nod)
deny={'name':'Negar','samplers':[],'channels':[]}
rotation_track(deny,'Bone_008',[0,.25,.55,.85,1.15,1.5,1.9],[0,.24,-.24,.24,-.24,.08,0],[0,1,0])
clips.append(deny)
doc['animations']=clips
doc['buffers'][0]['byteLength']=len(blob)
doc.setdefault('asset',{}).setdefault('extras',{})['MiUni']='Animaciones de prueba añadidas a una copia del modelo de Meshy. Original intacto.'
js=json.dumps(doc,separators=(',',':')).encode()
js+=b' '*((-len(js))%4); blob.extend(b'\0'*((-len(blob))%4))
total=12+8+len(js)+8+len(blob)
result=struct.pack('<III',0x46546c67,2,total)+struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(blob),0x004e4942)+blob
out=Path(__file__).parent/'miuni-meshy-animado.glb';out.write_bytes(result)
print(f'{out.name}: {len(result)/1048576:.2f} MB; animaciones: '+', '.join(c['name'] for c in clips))
