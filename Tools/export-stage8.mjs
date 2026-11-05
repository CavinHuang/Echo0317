import fs from 'node:fs';
import vm from 'node:vm';
import path from 'node:path';
const dir=path.dirname(new URL(import.meta.url).pathname);
const ctx={window:{}};
for(const f of ['story.js','casebook.js','captions.js','video-registry.js']){
  ctx.globalThis=ctx.window;
  vm.runInNewContext(fs.readFileSync(path.join(dir,f),'utf8'),ctx,{filename:f});
}
const {FILM:film,Casebook:book,FilmCaptions:captions,VIDEO_REGISTRY:registry}=ctx.window;
const nodes=Object.entries(film.nodes).map(([id,n])=>({id,...n,choices:n.choices||[],entryFlags:n.entryFlags||[],reactions:n.reactions||[],lines:n.lines||[]}));
const evidence=Object.entries(film.evidence).map(([id,label])=>({id,label}));
const media=Object.entries(film.media).map(([id,m])=>({id,...m}));
const chapters=Object.entries(film.chapterNames).map(([index,name])=>({index:Number(index),name}));
const graph={title:film.title,revision:film.revision,totalClips:film.totalClips,nodes,evidence,media,chapters};
const rules=book.rules.map(r=>({...r}));
const tracks=Object.entries(captions.tracks).map(([id,cues])=>({id,cues}));
const shots=Object.entries(registry).map(([id,r])=>({id,chapter:r.chapter,status:r.status,path:r.path||'',duration:r.duration_sec,referenceIds:r.reference_ids||[]}));
for(const [name,data] of [['story.json',graph],['cases.json',{rules}],['captions.json',{tracks}],['shots.json',{shots}]]) {
 fs.writeFileSync(path.join(dir,'..','Assets','StreamingAssets','Data',name),JSON.stringify(data,null,2)+'\n');
}
console.log(JSON.stringify({nodes:nodes.length,evidence:evidence.length,chapters:chapters.length,media:media.length,cases:rules.length,shotTasks:shots.length,tracks:tracks.length}));
