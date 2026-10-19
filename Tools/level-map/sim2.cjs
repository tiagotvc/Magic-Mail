const sp=2.3, asp=9/16;
function run(R,h,d,pd,fd,s0){const pitch=pd*Math.PI/180,fov=fd*Math.PI/180,t=Math.tan(fov/2);
 const f=s=>{const th=s/R;const y=R*(Math.cos(th)-1)+0.16*Math.cos(th),z=R*Math.sin(th)+0.16*Math.sin(th);const vy=y-h,vz=z+d;
  const zz=vy*-Math.sin(pitch)+vz*Math.cos(pitch);const yy=vy*Math.cos(pitch)+vz*Math.sin(pitch);return [0.5+yy/(zz*t)/2,0.92*1.25/(zz*t*asp*2),zz]};
 let max=0;for(let s=0;s<120;s+=0.2){const [y]=f(s);max=Math.max(max,y)}
 let out=[];for(let k=0;k<9;k++){const [y,w,z]=f(s0+k*sp);out.push(`${y.toFixed(2)}/${w.toFixed(2)}`)}
 console.log(`R${R} h${h} d${d} p${pd} fov${fd} s0=${s0} limb=${max.toFixed(2)} :: `+out.join(" "))}
for(const a of [[120,4,7,23,36,6],[120,4,5,23,36,3],[150,4,6,22,36,4],[100,3.5,6,21,38,4.5],[120,5,6,25,38,3],[100,4,8,22,36,5]]) run(...a)
