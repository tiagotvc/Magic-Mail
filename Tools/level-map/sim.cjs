const [R,h,d,pd,fd]=process.argv.slice(2).map(Number); const pitch=pd*Math.PI/180, fov=fd*Math.PI/180, asp=9/16, sp=2.3;
const pt=(s,hh=0)=>{const th=s/R;return [R*(Math.cos(th)-1)+hh*Math.cos(th),R*Math.sin(th)+hh*Math.sin(th)]};
const t=Math.tan(fov/2);
for(let k=-1;k<13;k++){const s=1.5+k*sp-2.0;const [y,z]=pt(s,0.16);const vy=y-h,vz=z+d;
 const zz=vy*-Math.sin(pitch)+vz*Math.cos(pitch); if(zz<=0.1)continue; const yy=vy*Math.cos(pitch)+vz*Math.sin(pitch);
 console.log(`k=${k} s=${s.toFixed(1)} screenY=${(0.5+yy/(zz*t)/2).toFixed(2)} tokW=${(0.92*1.25/(zz*t*asp*2)).toFixed(2)} dist=${zz.toFixed(1)}`)}
