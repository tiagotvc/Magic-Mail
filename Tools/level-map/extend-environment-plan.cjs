const fs=require('node:fs'),path=require('node:path');
const directory=__dirname;
const read=file=>JSON.parse(fs.readFileSync(path.join(directory,file),'utf8'));
const write=(file,value)=>fs.writeFileSync(path.join(directory,file),JSON.stringify(value,null,2)+'\n');
const roadmap=read('mundos-seasons-roadmap.json'), green=read('brainstorm-200-fases.json'), backlog=read('asset-production-backlog.json');
const specs=read('environment-area-specs.json').profilesByChapterOrder;
const chapterSources=[...green.chapters.map(c=>({...c,world:'green-land'})),...roadmap.worlds[1].chapters.map(c=>({...c,world:'lava-land'}))];
if(chapterSources.length!==specs.length)throw Error('Chapter/profile mismatch');
const bindings=chapterSources.map((c,i)=>({
 id:c.world+'-'+c.id+'-environment',worldId:c.world,chapterId:c.id,chapterTitle:c.title,start:c.start,end:c.end,
 status:'planned-not-implemented',skyPaletteKey:specs[i].sky,horizonContent:specs[i].horizon,
 defaultWeatherProfileId:specs[i].weather,windDescription:specs[i].wind,visualDescription:specs[i].detail,
 proposedLocalVariation:specs[i].variation,
 layerRoles:['sky-base','sun-or-moon-if-compatible','far-horizon','far-clouds','near-clouds','weather-rays-or-haze','world-particles'],
 referenceOnly: true
}));
const effects={
 'sun-rays':['sun-disc','soft-sun-rays','slow-cloud-drift'],
 'partly-cloudy':['cloud-drift','soft-ray-occlusion'],
 'forest-mist':['lateral-low-mist','filtered-sun-rays','sparse-leaves'],
 'rain-soft':['sparse-rain','river-only-ripples','overcast-clouds'],
 'windy':['gust-arcs','leaves-and-letters','flag-and-tree-sway'],
 'coastal-wind':['horizontal-clouds','gentle-rays','flags-sway'],
 'storm-soft':['storm-clouds','oblique-rain','gusts','localized-cloud-lightning'],
 'sun-after-rain':['soft-rays','light-mist','optional-distant-rainbow'],
 'golden-hour':['low-warm-sun','pink-clouds'],
 'volcanic-clear':['warm-sky','volcano-smoke','filtered-rays'],
 'ember-drift':['sparse-embers','cloud-drift','wind-directed-smoke'],
 'thermal-mist':['geyser-steam','lateral-mist'],
 'ash-breeze':['chimney-smoke','sparse-ash','wind-directed-puffs'],
 'ashfall-soft':['sparse-ash','cool-clouds'],
 'volcanic-wind':['gust-arcs','wind-directed-smoke','sparse-embers'],
 'cave-haze':['low-haze','dust-motes','authored-opening-light-shafts'],
 'volcanic-storm':['storm-clouds','gusts','localized-cloud-lightning','cold-terrace-rain'],
 'ember-night':['sparse-embers','dark-cloud-drift','local-lava-light']
};
const environment={
 schemaVersion:1,date:'2026-10-13',status:'planning-only',scope:'Backgrounds and fictional area-authored weather; no generated images or runtime changes',
 defaults:{selection:'world + chapter + local segment, composed with active season',weatherSource:'authored; not real weather API or system clock',transition:'spatial reversible blend along world route',transitionExtent:'initial experiment: last 2-3 readable positions; tune in pilot',maximumConcurrentAreaProfiles:2,time:'animation only, plus explicitly authored local cycle',fallback:'world base clear profile'},
 profiles:Object.entries(effects).map(([id,effects])=>({id,effects,status:'planned-not-implemented',intensity:'authorable 0..1; final values to tune in pilot',seeded:true})),
 areaBindings:bindings,
 seasonComposition:{baseSeason:'unchanged',christmas:{overlays:'snow caps/hats/garlands from existing season plan',weatherPolicy:'authored compatible snow variant per area; does not force every area to snowfall',rainSnowMix:'choose rain or snow family for a passage unless a sleet preset is explicitly authored',lavaPolicy:'snowfall only cold edges/structures; magma, vents and world smoke remain',hatLightCompatibility:'preserve base pose, light and node readability'}},
 constraints:['preserve fixed horizontal horizon and curved world geometry','do not bake clouds, sun rays, lightning or smoke into all-in-one backgrounds','weather sprites/particles are non-interactive','central readable marker band has reduced particle density','fountains remain static; rain ripples restricted to authored river/water surfaces','no camera shake or level movement from wind','lightning localized in background; light-effects mode disables flash','disable sunlight/rain from closed cave areas except authored openings','cull unseen emitters, pause when map closed, release transitions','no gameplay/save/difficulty effects'],
 prehistoricDirections:[
 {area:'Vale dos Ovos',weather:'warm-sun-and-light-breeze'},
 {area:'Floresta dos Gigantes',weather:'tropical-shower-and-filtered-rays'},
 {area:'Pântano das Pegadas',weather:'low-mist-and-drizzle'},
 {area:'Cânion dos Fósseis',weather:'sun-and-dust-gusts'},
 {area:'Lago dos Plesiossauros',weather:'humid-clouds-and-sun-after-rain'},
 {area:'Ninhos nas Alturas',weather:'high-wind-and-distant-storm'},
 {area:'Planalto dos Herbívoros',weather:'wide-sunny-sky-and-cloud-shadows'},
 {area:'Central Postal Jurássica',weather:'warm-evening'}]
};
write('environment-weather-roadmap.json',environment);
roadmap.environmentPlan='environment-weather-roadmap.json';
roadmap.environmentDesign='PLANO-CLIMA-E-BACKGROUNDS.md';
roadmap.environmentProfileCount=bindings.length;
roadmap.worlds[0].areaEnvironmentBindings=bindings.filter(b=>b.worldId==='green-land').map(b=>b.id);
for(const chapter of roadmap.worlds[1].chapters)chapter.environmentProfileId=bindings.find(b=>b.worldId==='lava-land'&&b.chapterId===chapter.id).id;
roadmap.worlds[2].environmentStatus='directions only; no phase count defined';
write('mundos-seasons-roadmap.json',roadmap);
const extra=[
 {id:'green-area-sky-bases',world:'green-land',priority:'P0',type:'background-raster',variants:['clear-blue','warm-sunset','overcast-blue-gray','garden-lilac'],description:'Bases de céu sem objetos, com paletas próprias por área; horizonte e nuvens separados.',animation:'spatial-area-blend',implementationNotes:'Não reutilizar o mesmo postal-sky em todas as regiões. Poucas bases e variações autoradas; sem mapa inteiro pintado.'},
 {id:'weather-sun-rays',world:'shared',priority:'P0',type:'transparent-png-or-material-recipe',variants:['sun-disc','soft-ray-wide','soft-ray-narrow'],description:'Sol independente e fachos largos de luz cartoon suave.',animation:'slow-drift-and-cloud-occlusion',implementationNotes:'Raios em camadas discretas no fundo/laterais; não exigir luz volumétrica real. Sem raios solares celestes na caverna fechada/noite.'},
 {id:'weather-storm-clouds',world:'shared',priority:'P0',type:'transparent-png',variants:['storm-cloud-large','storm-cloud-strip'],description:'Nuvens densas simples e arredondadas para tempestade e céu carregado.',animation:'layered-drift',implementationNotes:'Cinza-azul/lilás e variantes vulcânicas, sem céu/raio/fumaça embutidos.'},
 {id:'weather-rain-kit',world:'shared',priority:'P0',type:'transparent-mask-and-particle-recipe',variants:['rain-streak','small-splash','river-ripple'],description:'Gotas estilizadas, pequeno impacto e ondulação para superfícies de rio.',animation:'rain-emission-and-ripple-expansion',implementationNotes:'Onde houver teto, água ou piso, usar volumes/máscaras autoradas. Fontes estáticas excluídas de efeitos de água animada.'},
 {id:'weather-wind-kit',world:'shared',priority:'P0',type:'transparent-png-or-line-recipe',variants:['gust-arc','loose-leaf'],description:'Arcos leves de vento e folhas; cartas reutilizam sprites postais existentes.',animation:'seeded-gust-burst',implementationNotes:'Mesmo vetor de vento orienta chuva, fumaça, bandeiras e folhas. Prédios e níveis não deformam.'},
 {id:'weather-lightning-kit',world:'shared',priority:'P1',type:'procedural-lines-or-transparent-mask',variants:['wide-fork','short-fork','cloud-inner-light'],description:'Raios cartoon distantes e clarão interno discreto na nuvem.',animation:'rare-localized-pulse',implementationNotes:'Evitar flashes brancos de tela inteira. Modo de efeitos leves mantém nuvens/chuva sem clarão.'},
 {id:'weather-mist-kit',world:'shared',priority:'P1',type:'transparent-png-or-material-recipe',variants:['soft-low-mist','mist-strip'],description:'Névoa leve de bosque, serra, plataforma termal e caverna.',animation:'slow-lateral-drift',implementationNotes:'Densidade autorada por região; reduzir camada no corredor dos níveis; diferenciar névoa ambiente de fumaça local.'}
];
for(const entry of extra){
 const value={...entry,status:'planned-not-generated',attachmentSlots:[]};
 const i=backlog.entries.findIndex(e=>e.id===entry.id);
 if(i<0)backlog.entries.push(value);else backlog.entries[i]=value;
}
if(!backlog.productionRules.includes('background and weather vary by authored area with reversible transitions'))backlog.productionRules.push('background and weather vary by authored area with reversible transitions');
backlog.environmentPlan='environment-weather-roadmap.json';
write('asset-production-backlog.json',backlog);
console.log(JSON.stringify({areaProfiles:bindings.length,weatherPresets:environment.profiles.length,assetFamilies:backlog.entries.length,variantsAndRecipes:backlog.entries.reduce((n,e)=>n+e.variants.length,0),status:environment.status}));

