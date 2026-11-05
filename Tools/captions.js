/* Stage 7 temporary cinematic cues for Stage 3 previs; not a final dialogue subtitle master. */
(function(root){
 'use strict';
 const tracks={
  V01:[{start:0.3,end:2.4,text:'凌晨 03:17。手机忽然震动。'},{start:2.5,end:6.7,text:'「不要开门。那不是你认识的她。」'}],
  V02:[{start:0.4,end:2.7,text:'猫眼里的女人没有动。'},{start:2.7,end:5.8,text:'她的影子，却向相反方向延伸。'}],
  V03:[{start:0.5,end:3.2,text:'那张合照的日期，被人改动过。'},{start:3.2,end:5.8,text:'你认得自己的字迹。'}],
  V04:[{start:0.7,end:2.2,text:'咚。'},{start:2.5,end:4.1,text:'咚。'},{start:4.8,end:7.5,text:'第三下敲门，迟了半拍。'}],
  V05:[{start:0.7,end:2.5,text:'旧录音开始播放。'},{start:2.5,end:5.5,text:'同样的声音，未必是同一个人。'}],
  V23:[{start:0.8,end:3.1,text:'回执标记着一条不应存在的投递记录。'},{start:3.1,end:6.3,text:'签名尾码：46。'}],
  V24:[{start:0.6,end:2.6,text:'你把手搭在了门锁上。'},{start:2.6,end:6.5,text:'门后传来了另一声呼吸。'}],
  V24B:[{start:0.5,end:3.4,text:'你没有触碰门锁。'},{start:3.4,end:6.3,text:'走廊重新安静下来。'}]
 };
 function cue(clip,time){const list=tracks[clip]||[];return list.find(c=>time>=c.start&&time<c.end)?.text||'';}
 function validate(){for(const [id,list] of Object.entries(tracks)){
   let end=0;for(const c of list){if(!(c.start>=0&&c.end>c.start&&c.start>=end&&c.text))throw new Error('无效字幕 '+id);end=c.end;}
 }return Object.keys(tracks).length;}
 root.FilmCaptions={tracks,cue,validate};
})(typeof window!=='undefined'?window:globalThis);
