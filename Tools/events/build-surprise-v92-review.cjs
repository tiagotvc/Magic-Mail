const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..'),source=path.join(root,'Library/MapValidation/events-surprise-v92'),out=path.join(__dirname,'previews/surprise-v92');
fs.mkdirSync(out,{recursive:true});
for(const name of ['events-surprise.png','animated.png','narrow-phone.png','motion-base.png','validation.txt','play-validation.txt','review-layout.json'])fs.copyFileSync(path.join(source,name),path.join(out,name));
const layout=JSON.parse(fs.readFileSync(path.join(source,'review-layout.json'),'utf8'));
const base=fs.readFileSync(path.join(out,'motion-base.png')).toString('base64');
const letter=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/EventsV92/letter.png')).toString('base64');
const html=`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Magic Mail — Eventos</title>
<style>*{box-sizing:border-box}body{margin:0;background:#1d102b;color:#fff5da;font:16px system-ui;display:flex;justify-content:center;gap:28px;padding:24px;align-items:center;min-height:100vh}canvas{width:min(46vh,90vw);height:auto;border-radius:18px;box-shadow:0 12px 60px #0008}aside{max-width:300px}h1{font-size:24px;margin:0 0 12px}p{line-height:1.5;color:#d6bedf}button{background:#ffe0a2;color:#482052;border:0;border-radius:10px;padding:12px 20px;font-weight:700;cursor:pointer}input{display:block;width:100%;margin:24px 0 10px}small{color:#bda5c8}@media(max-width:650px){body{flex-direction:column}aside{text-align:center}canvas{width:min(80vw,40vh)}}</style>
<canvas id="view" width="720" height="1280" aria-label="Prévia animada da tela de Eventos"></canvas><aside><h1>Uma surpresa a caminho</h1><p>O carteiro espia enquanto a coruja segura a aba. Cartinhas mágicas escapam do envelope, flutuam e dão uma pausa.</p><button id="toggle">Pausar</button><input id="time" type="range" min="0" max="8" step=".01" value="0" aria-label="Posição da animação"><small id="clock"></small><p><small>Prévia interativa do movimento integrado na Unity. A captura da interface é nativa; o primeiro ciclo dura 8 segundos.</small></p></aside>
<script>
const layout=${JSON.stringify(layout)},canvas=document.getElementById('view'),ctx=canvas.getContext('2d'),slider=document.getElementById('time'),clock=document.getElementById('clock'),button=document.getElementById('toggle');
const backdrop=new Image(),letter=new Image();backdrop.src='data:image/png;base64,${base}';letter.src='data:image/png;base64,${letter}';
let running=!matchMedia('(prefers-reduced-motion: reduce)').matches,time=0,last=performance.now();
const clamp=x=>Math.max(0,Math.min(1,x)),smooth=x=>{x=clamp(x);return x*x*(3-2*x)},inverse=(a,b,x)=>clamp((x-a)/(b-a)),repeat=(x,n)=>((x%n)+n)%n;
const point=(x,y)=>[layout.low.x+(x-layout.uv.x)/layout.uv.width*(layout.high.x-layout.low.x),1280-layout.low.y-(y-layout.uv.y)/layout.uv.height*(layout.high.y-layout.low.y)];
function render(){ctx.globalAlpha=1;ctx.drawImage(backdrop,0,0,720,1280);const width=(layout.high.x-layout.low.x)/layout.uv.width,cycle=repeat(time,8);
for(let i=0;i<3;i++){const age=(cycle-i*.95)/3.2,p=clamp(age),alpha=age>=0&&age<1?smooth(inverse(.015,.16,p))*(1-smooth(inverse(.76,1,p))):0,t=smooth(p),drift=(i-1)*.038*t+Math.sin(p*Math.PI*2+i)*.018*t,xy=point(.55+drift,.548+.275*t),size=.2+.8*smooth(inverse(0,.35,p));ctx.save();ctx.globalAlpha=alpha;ctx.translate(...xy);ctx.rotate(-Math.sin(p*5+i)*13*Math.PI/180);ctx.scale(size,size);const w=width*.128;ctx.drawImage(letter,-w/2,-w/3,w,w/1.5);ctx.restore()}
for(let i=0;i<6;i++){const p=repeat(time*.28+i*.167,1),t=smooth(p),xy=point(.55+Math.sin(i*2.4)*.085*t,.548+.23*t),size=width*(.009+.008*Math.sin(p*Math.PI));ctx.save();ctx.globalAlpha=Math.sin(p*Math.PI)*.72;ctx.fillStyle='#ffe67a';ctx.translate(...xy);ctx.rotate(-(p*50+i*17)*Math.PI/180);ctx.beginPath();for(let j=0;j<8;j++){const a=j*Math.PI*.25,r=(j%2===0?1:.24)*size*.5;const x=Math.cos(a)*r,y=Math.sin(a)*r;j?ctx.lineTo(x,y):ctx.moveTo(x,y)}ctx.closePath();ctx.fill();ctx.restore()}ctx.globalAlpha=1;clock.textContent=repeat(time,8).toFixed(1)+' / 8,0 s';slider.value=repeat(time,8)}
button.textContent=running?'Pausar':'Reproduzir';button.onclick=()=>{running=!running;button.textContent=running?'Pausar':'Reproduzir';last=performance.now()};slider.oninput=()=>{time=Number(slider.value);render()};
function frame(now){if(running)time+=Math.min((now-last)/1000,.1);last=now;if(backdrop.complete&&backdrop.naturalWidth&&letter.complete&&letter.naturalWidth)render();requestAnimationFrame(frame)}requestAnimationFrame(frame);
</script></html>`;
new Function(html.match(/<script>([\s\S]*?)<\/script>/)[1]);
fs.writeFileSync(path.join(out,'animation-preview.html'),html);
console.log('Saved native captures, validation reports and self-contained interactive animation preview.');
