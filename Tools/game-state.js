/* Portable, deterministic narrative persistence. No video assets required. */
(function(root){
  'use strict';
  const SAVE_VERSION=6, MAX_STEPS=4000, MAX_ATTEMPTS=60;
  function fail(message){throw new Error(message);}
  function assert(value,message){if(!value)fail(message);}
  function choicesFor(node){return node.choices|| (node.next?[{to:node.next,label:node.nextLabel||'继续剧情'}]:[]);}
  function allowed(choice,flags){return (!choice.requires||choice.requires.every(f=>flags.has(f)))&&(!choice.needsAny||choice.needsAny.some(f=>flags.has(f)))&&(!choice.forbids||choice.forbids.every(f=>!flags.has(f)));}
  function trace(film,history){
    assert(Array.isArray(history)&&history.length<=MAX_STEPS,'剧情历史超出允许范围');
    let node='intro';const flags=new Set();
    const points=[{index:0,node,flags:[],chapter:film.nodes.intro.chapterIndex,choice:''}];
    for(let i=0;i<history.length;i++){
      const h=history[i];assert(h&&typeof h==='object','剧情步骤格式错误');
      assert(h.from===node&&typeof h.to==='string'&&film.nodes[h.to],'剧情步骤失效（节点不匹配）');
      const candidates=choicesFor(film.nodes[node]);
      let choice=candidates.find(c=>c.to===h.to&&c.label===h.choice);
      if(!choice)choice=candidates.find(c=>c.to===h.to);
      assert(choice&&allowed(choice,flags),'剧情步骤失效（分支前置条件不满足）');
      if(choice.flag)flags.add(choice.flag);
      node=h.to;
      for(const f of (film.nodes[node].entryFlags||[]))flags.add(f);
      points.push({index:i+1,node,flags:[...flags],chapter:film.nodes[node].chapterIndex,choice:h.choice||''});
    }
    return {node,flags:[...flags],points};
  }
  function casebook(){return root.Casebook||{rules:[],byId:{},state:()=>({solved:[],mistakes:{},correct:0,wrong:0})};}
  function validateAttempts(film,history,rawAttempts){
    assert(Array.isArray(rawAttempts)&&rawAttempts.length<=MAX_ATTEMPTS,'推理记录数量或格式异常');
    const book=casebook(), attempts=[];let prior=-1;const solved=new Set();
    for(const a of rawAttempts){
      assert(a&&typeof a==='object'&&Number.isInteger(a.at)&&a.at>=0&&a.at<=history.length&&a.at>=prior,'推理时间线无效');
      const rule=book.byId[a.rule];
      assert(rule&&rule.options.some(o=>o.id===a.answer),'未知推理题目或答案');
      const historical=new Set(trace(film,history.slice(0,a.at)).flags);
      assert(rule.clues.every(id=>historical.has(id)),'推理证据在该时间点尚未取得');
      assert(!solved.has(a.rule),'推理成功后不允许重复提交');
      if(a.answer===rule.correct)solved.add(a.rule);
      attempts.push({rule:a.rule,answer:a.answer,at:a.at});prior=a.at;
    }
    return attempts;
  }
  function recordAttempt(film,state,ruleId,answerId){
    const clean=normalize(film,state), book=casebook(), rule=book.byId[ruleId];
    assert(rule&&rule.options.some(o=>o.id===answerId),'未知推理答案');
    assert(rule.clues.every(id=>clean.flags.includes(id)),'尚未收齐用于推理的证据');
    assert(!book.state(clean.attempts).solved.includes(ruleId),'该推理已成功完成');
    assert(clean.attempts.length<MAX_ATTEMPTS,'推理尝试次数过多');
    return normalize(film,{...clean,attempts:[...clean.attempts,{rule:ruleId,answer:answerId,at:clean.history.length}]});
  }
  function chapterReports(film,state){
    const clean=normalize(film,state), {points}=trace(film,clean.history);
    const reports=[];
    for(let chapter=1;chapter<=5;chapter++){
      const segment=points.filter(p=>p.chapter===chapter);if(!segment.length)continue;
      const last=segment.at(-1),first=segment[0];
      const previous=first.index?points[first.index-1].flags:[];
      const discovered=last.flags.filter(f=>!previous.includes(f)&&film.evidence[f]);
      const risk=last.flags.filter(f=>!previous.includes(f)&&['operator_alert','misread_identity','coerced','justified_coercion','belief_future','pressed_lock'].includes(f));
      const book=casebook(), won=book.state(clean.attempts.filter(a=>a.at<=last.index)).solved
       .filter(id=>book.byId[id]?.chapter===chapter);
      reports.push({chapter,name:film.chapterNames[chapter],finished:!!points.some(p=>p.chapter>chapter)||(chapter===5&&!!film.nodes[clean.node]?.ending),
       steps:segment.length-1,discovered,risks:risk,solved:won});
    }
    return reports;
  }
  function normalize(film,raw){
    assert(raw&&typeof raw==='object'&&!Array.isArray(raw),'存档不是合法对象');
    const history=raw.history||[];
    const result=trace(film,history);
    assert(raw.node===result.node,'存档当前位置与剧情历史不一致');
    // Prior versions saved flags redundantly; regenerate flags from authoritative transitions.
    const endings=Array.isArray(raw.endings)?[...new Set(raw.endings)]:[];
    assert(endings.every(id=>film.nodes[id]?.ending),'存档包含无效结局');
    const canonical=history.map(h=>({from:h.from,to:h.to,choice:String(h.choice||'').slice(0,240),chapter:film.nodes[h.from].chapterIndex}));
    const attempts=validateAttempts(film,canonical,raw.attempts||[]);
    return {node:result.node,flags:result.flags,history:canonical,endings,debug:!!raw.debug,attempts};
  }
  function snapshot(film,state){
    const clean=normalize(film,state);
    return {game:'0317-echo',format:SAVE_VERSION,storyRevision:film.revision,exportedAt:new Date().toISOString(),state:clean};
  }
  function parse(film,text){
    assert(typeof text==='string'&&text.length<=1024*1024,'存档文件过大');
    let data;try{data=JSON.parse(text)}catch{fail('JSON 文件无法解析')}
    assert(data&&data.game==='0317-echo'&&[5,SAVE_VERSION].includes(data.format),'存档格式或版本不受支持');
    assert(data.storyRevision===film.revision,'剧情版本不匹配，不能安全还原');
    return normalize(film,data.state);
  }
  function checkpoints(film,state){
    const {points}=trace(film,state.history);
    const decisions=[];const chapters=[];const chapterSeen=new Set();
    for(let i=0;i<points.length;i++){
      const p=points[i];const n=film.nodes[p.node];
      if(!chapterSeen.has(n.chapterIndex)){
        chapterSeen.add(n.chapterIndex);chapters.push({...p,title:n.title,chapterName:film.chapterNames[n.chapterIndex]});
      }
      if(n.choices?.length&&i<points.length-1)decisions.push({...p,title:n.title,chapterName:film.chapterNames[n.chapterIndex],selected:state.history[i]?.choice||''});
    }
    return {decisions,chapters};
  }
  return root.GameState={version:SAVE_VERSION,allowed,trace,normalize,snapshot,parse,checkpoints,recordAttempt,chapterReports};
})(typeof window!=='undefined'?window:globalThis);
