import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

export function createOwl() {
  const owl = new T.Group(); owl.name = 'MiUni';
  const mat = (color, roughness = .92) => new T.MeshStandardMaterial({ color, roughness });
  const blue = mat('#235bbb'), cream = mat('#fff0d3'), yellow = mat('#ffc335');
  const dark = mat('#153d79'), white = mat('#ffffff');
  const add = (parent, geo, material, pos = [0,0,0], scale = [1,1,1], name = '') => {
    const mesh = new T.Mesh(geo, material); mesh.position.set(...pos); mesh.scale.set(...scale);
    mesh.name = name; parent.add(mesh); return mesh;
  };
  const sphere = new T.SphereGeometry(1, 48, 32);
  const puff = (parent, material, pos, scale, name) => add(parent, sphere, material, pos, scale, name);
  const bodyGeometry=sphere.clone();
  const vertices=bodyGeometry.attributes.position;
  const rounded = x => Math.sign(x)*Math.pow(Math.abs(x),.78);
  for(let i=0;i<vertices.count;i++) vertices.setXYZ(i,rounded(vertices.getX(i)),rounded(vertices.getY(i)),rounded(vertices.getZ(i)));
  bodyGeometry.computeVertexNormals();
  const body = add(owl,bodyGeometry,blue,[0,1.12,0],[1.03,1.04,.69],'Cuerpo');
  // Short tapered tufts form a real geometric fleece surface, not an image.
  let seed = 84;
  const random = () => { seed = (seed * 1664525 + 1013904223) >>> 0; return seed / 4294967296; };
  const tuft = new T.ConeGeometry(.0048, .025, 3, 1);
  const furPieces = [], matrix = new T.Matrix4(), quaternion = new T.Quaternion();
  for (let i=0; i<15500; i++) {
    const y = random()*2-1, angle = random()*Math.PI*2, radius = Math.sqrt(1-y*y);
    const p = new T.Vector3(rounded(radius*Math.cos(angle))*1.034, rounded(y)*1.044+1.12, rounded(radius*Math.sin(angle))*.694);
    const normal = new T.Vector3(p.x/1.03**2, (p.y-1.12)/1.04**2, p.z/.69**2).normalize();
    quaternion.setFromUnitVectors(new T.Vector3(0,1,0), normal);
    matrix.compose(p, quaternion, new T.Vector3(1,.65+random()*.8,1));
    furPieces.push(tuft.clone().applyMatrix4(matrix));
  }
  const fur = add(owl, mergeGeometries(furPieces), mat('#326bc6')); fur.name='Felpa';
  furPieces.forEach(g=>g.dispose()); tuft.dispose();
  for (const side of [-1,1]) {
    const earShape=new T.LatheGeometry([new T.Vector2(0,-.23),new T.Vector2(.24,-.18),new T.Vector2(.25,-.07),new T.Vector2(.20,.09),new T.Vector2(.115,.25),new T.Vector2(.045,.34),new T.Vector2(0,.36)],32);
    const ear = add(owl,earShape,blue,[side*.70,2.04,-.01],[1,1,.85],'Penacho'); ear.rotation.z=side*-.32;
  }
  const left = new T.Group(); left.position.set(-.91,1.24,0); left.name='AlaSaludo'; owl.add(left);
  const wing = puff(left, blue, [-.15,.12,.08], [.24,.48,.24]); wing.rotation.z=-.64;
  const right = new T.Group(); right.position.set(.88,1.0,.22); right.name='AlaMapa'; owl.add(right);
  const rw = puff(right, blue, [.06,-.03,.12], [.25,.43,.23]); rw.rotation.z=-.25;
  const eyes=[], smiles=[];
  for (const side of [-1,1]) {
    const eye = new T.Group(); eye.position.set(side*.37,1.65,.628); eye.rotation.y=side*.12; owl.add(eye);
    puff(eye,cream,[0,0,0],[.27,.33,.105],'ContornoOjo');
    const pupil=puff(eye,dark,[side*-.018,-.015,.093],[.125,.195,.045],'Pupila');
    puff(pupil,white,[-.30,.40,.84],[.25,.17,.32],'Brillo');
    const smileCurve=new T.QuadraticBezierCurve3(new T.Vector3(-.14,-.035,.115),new T.Vector3(0,.20,.135),new T.Vector3(.14,-.035,.115));
    const smile=add(eye,new T.TubeGeometry(smileCurve,24,.027,8,false),dark);smile.name='OjoContento';smile.visible=false;smiles.push(smile);
    const brow=puff(eye,blue,[0,.385,-.015],[.23,.049,.075],'Ceja'); brow.rotation.z=side*-.12;
    eyes.push(pupil);
  }
  const beak = add(owl, new T.ConeGeometry(.125,.235,3), yellow,[0,1.43,.754]);
  beak.rotation.z=Math.PI; beak.rotation.y=Math.PI; beak.scale.z=.8; beak.name='PicoAmarillo';
  const map = new T.Group(); map.name='MapaCampus'; map.position.set(.34,.86,.73); map.rotation.z=-.07; owl.add(map);
  const paper=mat('#fff4dc'), green=mat('#9dc784'), road=mat('#d2cfc2'), path=mat('#f0c54e');
  const red=mat('#eb6451'), building=mat('#df9976'), water=mat('#86bfd0');
  const box = (parent, material,x,y,w,h,z=.019) => add(parent,new T.BoxGeometry(w,h,.012),material,[x,y,z]);
  function line(parent,material,points,width,z) {
    for(let i=1;i<points.length;i++) {
      const [x,y]=points[i-1], [a,b]=points[i];
      const piece=box(parent,material,(x+a)/2,(y+b)/2,width,Math.hypot(a-x,b-y),z);
      piece.rotation.z=-Math.atan2(a-x,b-y);
    }
  }
  for(const side of [-1,1]) {
    const panel=new T.Group(); panel.position.x=side*.205; panel.rotation.y=side*-.24; map.add(panel);
    box(panel,paper,0,0,.415,.57,0);
    box(panel,green,-.095,.15,.13,.16); box(panel,green,.105,-.13,.13,.11);
    box(panel,building,.105,.17,.075,.11);box(panel,water,-.09,-.17,.12,.065);
    box(panel,road,0,0,.035,.53,.03);box(panel,road,0,.035,.39,.032,.03);
    line(panel,path,[[-.18,-.065],[-.04,-.035],[.02,.065],[.18,.09]],.024,.045);
    if(side===1) {
      const pin=new T.Group();pin.position.set(.07,.10,.075);panel.add(pin);
      const marker=new T.Shape();marker.moveTo(0,-.105);marker.bezierCurveTo(-.13,.015,-.08,.12,0,.12);marker.bezierCurveTo(.08,.12,.13,.015,0,-.105);
      add(pin,new T.ExtrudeGeometry(marker,{depth:.012,bevelEnabled:false}),red);
      add(pin,new T.CircleGeometry(.028,24),cream,[0,.045,.014]);
    }
  }
  owl.userData.description='Búho MiUni: modelo propio, felpa geométrica simplificada, pico amarillo y mapa.';
  owl.updateMatrixWorld(true);
  return {owl, eyes, smiles, left, body};
}
