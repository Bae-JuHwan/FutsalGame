from pathlib import Path
import sys, bpy, bmesh, math, shutil
from mathutils import Vector, Quaternion
WORK=Path(sys.argv[sys.argv.index('--')+1]).resolve() if '--' in sys.argv else Path(__file__).resolve().parent
OUT=WORK/'export'
OUT.mkdir(exist_ok=True)
sys.path.insert(0,str(WORK/'mpfb2-master/src'))
bpy.utils.extension_path_user=lambda package,**kwargs:str(WORK/'mpfb-user')
import mpfb
bpy.context.preferences.addons.new().module='mpfb'
mpfb.register()
from mpfb.services.humanservice import HumanService
from mpfb.services.targetservice import TargetService
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
macro=TargetService.get_default_macro_info_dict()
macro.update({'gender':1.0,'age':0.35,'muscle':0.75,'weight':0.45,'height':0.55})
macro['race']={'asian':0.25,'caucasian':0.6,'african':0.15}
human=HumanService.create_human(macro_detail_dict=macro)
human.name='AthleteBody'
rig=HumanService.add_builtin_rig(human,'game_engine')
rig.name='AthleteRig'
def asset(path,kind='Clothes'):
 return HumanService.add_mhclo_asset(str(WORK/'system'/path),human,asset_type=kind,subdiv_levels=0)
suit=asset('clothes/male_casualsuit06/male_casualsuit06.mhclo')
hair=asset('hair/short04/short04.mhclo','Hair')
eyes=asset('eyes/low-poly/low-poly.mhclo','Eyes')
shoes=asset('clothes/shoes06/shoes06.mhclo')

def texture(src,name,maxsize=2048):
 im=bpy.data.images.load(str(WORK/'system'/src),check_existing=True)
 if max(im.size)>maxsize:
  scale=maxsize/max(im.size); im.scale(int(im.size[0]*scale),int(im.size[1]*scale))
 im.filepath_raw=str(OUT/(name+'.png')); im.file_format='PNG'; im.save()
 return im
def material(name,color=(1,1,1,1),image=None,alpha=False):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=color
 bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color
 bs.inputs['Roughness'].default_value=.72
 if image:
  n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=image
  m.node_tree.links.new(n.outputs['Color'],bs.inputs['Base Color'])
  if alpha:m.node_tree.links.new(n.outputs['Alpha'],bs.inputs['Alpha'])
 return m
skin=material('Skin',image=texture('skins/young_caucasian_male/young_lightskinned_male_diffuse.png','Skin'))
hairmat=material('Hair',image=texture('hair/short04/short04_diffuse.png','Hair',1024),alpha=True)
eyemat=material('Eyes',image=texture('eyes/materials/brown_eye.png','Eyes',512))
bootmat=material('Boots',image=texture('clothes/shoes06/shoes06_diffuse.png','Boots',1024))
jersey=material('Jersey',(0.06,.25,.8,1))
shorts=material('Shorts',(.035,.05,.09,1))
socks=material('Socks',(.06,.25,.8,1))
trim=material('Trim',(1,1,1,1))
for obj,mat in [(human,skin),(hair,hairmat),(eyes,eyemat),(shoes,bootmat)]:
 obj.data.materials.clear();obj.data.materials.append(mat)
 for poly in obj.data.polygons:poly.material_index=0

# Keep the full skin under cropped shorts. Apply only the helper-geometry mask.
bpy.context.view_layer.objects.active=human
if human.data.shape_keys:bpy.ops.object.shape_key_remove(all=True,apply_mix=True)
for mod in list(human.modifiers):
 if mod.type=='MASK':
  if mod.vertex_group=='body':bpy.ops.object.modifier_apply(modifier=mod.name)
  else:human.modifiers.remove(mod)

# Tailor the existing shirt/trousers mesh into a football shirt and above-knee shorts.
bpy.context.view_layer.objects.active=suit
if suit.data.shape_keys:bpy.ops.object.shape_key_remove(all=True,apply_mix=True)
for mod in list(suit.modifiers):
 if mod.type!='ARMATURE':suit.modifiers.remove(mod)
bm=bmesh.new();bm.from_mesh(suit.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,
 plane_co=(0,0,.61),plane_no=(0,0,1),clear_inner=True)
bm.to_mesh(suit.data);bm.free()
suit.data.materials.clear();suit.data.materials.append(jersey);suit.data.materials.append(shorts);suit.data.materials.append(trim)
bm=bmesh.new();bm.from_mesh(suit.data)
unseen=set(bm.faces)
while unseen:
 seed=unseen.pop();component={seed};stack=[seed]
 while stack:
  for edge in stack.pop().edges:
   for face in edge.link_faces:
    if face in unseen:unseen.remove(face);component.add(face);stack.append(face)
 top=max(v.co.z for face in component for v in face.verts)
 for face in component:face.material_index=0 if top>1.15 else 1
bm.to_mesh(suit.data);bm.free()
suit.name='FootballKit'
for vertex in suit.data.vertices:
 vertex.co += vertex.normal * .009
# Sock material on the lower leg follows the anatomical mesh and bone weights.
human.data.materials.append(socks);human.data.materials.append(trim)
bm=bmesh.new();bm.from_mesh(human.data)
for height in [.09,.39,.42]:
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,
  plane_co=(0,0,height),plane_no=(0,0,1))
bm.to_mesh(human.data);bm.free()
for poly in human.data.polygons:
 z=sum(human.data.vertices[i].co.z for i in poly.vertices)/len(poly.vertices)
 if .09<z<.42:poly.material_index=1 if z<.39 else 2
shoes.data.materials.append(socks)
for poly in shoes.data.polygons:
 if min(shoes.data.vertices[i].co.z for i in poly.vertices)>.09:poly.material_index=1
for obj in [human,suit,hair,eyes,shoes]:
 for poly in obj.data.polygons:poly.use_smooth=True
 for mod in list(obj.modifiers):
  if mod.type=='SUBSURF':obj.modifiers.remove(mod)

# Root remains stationary: Unity's existing player motor supplies translation.
rig.scale=(1.85/1.6003,)*3
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
rest={pb.name:pb.bone.matrix_local.to_quaternion() for pb in rig.pose.bones}
arm_down={}
for side,sign in [('l',1),('r',-1)]:
 b=rig.data.bones['upperarm_'+side]
 arm_down[side]=(b.tail_local-b.head_local).normalized().rotation_difference(Vector((sign*.15,0,-1)).normalized())
def rotate(name,world):
 pb=rig.pose.bones.get(name)
 if pb:pb.rotation_quaternion=rest[name].inverted()@world@rest[name]
def pose(t,mode):
 for pb in rig.pose.bones:pb.rotation_quaternion=Quaternion();pb.location=(0,0,0)
 a=0 if mode=='Idle' else .52 if mode=='Jog' else .73
 phase=t*2*math.pi
 for side,sign in [('l',1),('r',-1)]:
  wave=math.sin(phase)*sign
  thigh=a*wave
  knee=max(0,-wave)*a*1.35+.07
  if mode=='Kick':
   thigh=(-1.05*math.sin(math.pi*t)) if side=='r' else .08
   knee=.12
  rotate('thigh_'+side,Quaternion((1,0,0),thigh))
  rotate('calf_'+side,Quaternion((1,0,0),knee))
  rotate('foot_'+side,Quaternion((1,0,0),-thigh*.35-knee*.4))
  rotate('upperarm_'+side,Quaternion((1,0,0),-a*wave*.7)@arm_down[side])
  rotate('lowerarm_'+side,Quaternion((1,0,0),-.25 if mode=='Idle' else -.65))
 rotate('spine_02',Quaternion((1,0,0),-.025 if mode=='Idle' else -.09))
 rig.pose.bones['pelvis'].location.z=abs(math.sin(phase))*(.005 if mode=='Idle' else .024)

rig.animation_data_create()
bpy.context.scene.render.fps=30
for name,frames in [('Idle',60),('Jog',24),('Sprint',18),('Kick',10)]:
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
 for f in range(frames+1):
  pose(f/frames,name)
  for pb in rig.pose.bones:
   pb.keyframe_insert('rotation_quaternion',frame=f+1)
   if pb.name=='pelvis':pb.keyframe_insert('location',frame=f+1)
rig.animation_data.action=bpy.data.actions['Idle']
bpy.context.scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for obj in [rig,human,suit,hair,eyes,shoes]:obj.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'Athlete.fbx'),use_selection=True,
 object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,
 bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
 bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
print('ATHLETE_EXPORT',[(o.name,len(o.data.vertices)) for o in [human,suit,hair,eyes,shoes]])

# A studio render checks the actual textured model before importing it into Unity.
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object;floor.data.materials.append(material('Floor',(.045,.07,.09,1)))
bpy.ops.object.camera_add(location=(3,-5,2.4))
cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.context.scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.35
for loc,power,size in [((2,-3,4),500,4),((-3,-1,2),250,3),((0,3,3),500,2)]:
 bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object
 light.data.energy=power;light.data.shape='DISK';light.data.size=size
 light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=768;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.filepath=str(WORK/'athlete-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(WORK/'Athlete.blend'))
bpy.ops.render.render(write_still=True)
