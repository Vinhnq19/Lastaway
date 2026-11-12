"""Rebuild the local architecture map only when indexed code/config content changes."""
import hashlib, json, re, subprocess, sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'graphify-out'
DOC = 'Docs/Network_Architecture.vi.md'
SUFFIXES = {'.cs','.py','.js','.ts','.sh','.ps1','.asmdef','.asmref','.csproj','.sln','.yaml','.yml','.json','.shader','.hlsl','.cginc'}

def dump(name, data):
    (OUT/name).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def main():
    import os
    os.chdir(ROOT)
    OUT.mkdir(exist_ok=True)
    paths = subprocess.check_output(['git','ls-files','--cached','--others','--exclude-standard','-z'], cwd=ROOT).decode('utf-8').split('\0')
    paths = sorted(set(p for p in paths if p and not p.startswith(('graphify-out/','.utmp/','.claude/','.codex/','.agents/'))))
    code = [p for p in paths if Path(p).suffix.lower() in SUFFIXES or (Path(p).name == 'Dockerfile' or Path(p).name.startswith('Caddyfile'))]
    code = [p for p in code if not p.startswith('Docs/Reports/') and Path(p).is_file() and not Path(p).is_symlink() and not any(x in p.lower() for x in ('.env','credential','private','secret'))]
    hashes = {p:hashlib.sha256(Path(p).read_bytes()).hexdigest() for p in code}
    old = json.loads((OUT/'code-state.json').read_text(encoding='utf-8')) if (OUT/'code-state.json').exists() else {}
    if hashes == old.get('hashes') and (OUT/'graph.html').exists() and (OUT/'GRAPH_TREE.html').exists():
        print('Code/config unchanged; map left untouched.')
        return
    from graphify.extract import extract
    from graphify.build import build_from_json
    from graphify.cluster import cluster, score_all
    from graphify.analyze import god_nodes, surprising_connections, suggest_questions
    from graphify.export import to_json, to_html
    from graphify.report import generate
    from graphify.detect import save_manifest
    from graphify.diagnostics import diagnose_extraction, format_diagnostic_report
    ast = extract([ROOT/p for p in code], cache_root=ROOT, root=ROOT, parallel=False)
    sem = json.loads((OUT/'architecture-semantic.json').read_text(encoding='utf-8'))
    doc_hash = hashlib.sha256((ROOT/DOC).read_bytes()).hexdigest()
    semantic_current = sem.get('source_sha256') == doc_hash
    if not semantic_current:
        print('WARNING: architecture document changed; semantic extraction must be refreshed. Stale concepts omitted.')
        sem = {'nodes':[], 'edges':[], 'input_tokens':0,'output_tokens':0}
    for n in sem['nodes']: n['_origin']='semantic'
    for e in sem['edges']: e['_origin']='semantic'
    nodes, edges = ast['nodes']+sem['nodes'], ast['edges']+sem['edges']
    ids = {n['id'] for n in nodes}
    def nid(path): return 'map_'+hashlib.sha256(path.encode()).hexdigest()[:20]
    def add(path, label, kind='concept'):
        key=nid(path)
        if key not in ids:
            nodes.append(dict(id=key,label=label,file_type=kind,source_file=path,source_location=None,_origin='structural'))
            ids.add(key)
        return key
    def link(a,b,src,relation='contains',confidence='EXTRACTED',score=1.0):
        edges.append(dict(source=a,target=b,relation=relation,confidence=confidence,confidence_score=score,source_file=src,source_location=None,weight=1.0,_origin='structural'))
    add('.', 'Lastaway')
    # Assets remain directory groups; file paths exist only for source/config and selected architecture docs.
    directories=set()
    for p in paths:
        for parent in Path(p).parents:
            if str(parent)!='.': directories.add(parent.as_posix())
    for p in sorted(directories):
        add(p,p)
        link(nid(Path(p).parent.as_posix()),nid(p),p)
    for p in code+[DOC]:
        key=add(p,Path(p).name,'code' if p!=DOC else 'document')
        link(nid(Path(p).parent.as_posix()),key,p)
        for n in ast['nodes']:
            if n.get('source_file','').replace('\\','/')==p:
                link(key,n['id'],p,'declares')
    # Bridge documented concepts to observed symbols/paths; name matching is explicitly inferred.
    for n in sem['nodes']:
        label=n['label']
        candidates=[a for a in ast['nodes'] if a.get('label')==label]
        if label.startswith('Backend/'):
            candidates=[dict(id=nid(label))] if nid(label) in ids else []
        for a in candidates[:4]: link(n['id'],a['id'],DOC,'references','INFERRED',0.85)
    extraction=dict(nodes=nodes,edges=edges,hyperedges=sem.get('hyperedges',[]),input_tokens=sem.get('input_tokens',0),output_tokens=sem.get('output_tokens',0))
    G=build_from_json(extraction,root=str(ROOT),directed=True)
    if not G.number_of_nodes(): raise RuntimeError('Empty extraction; preserving prior map')
    communities=cluster(G)
    cohesion=score_all(G,communities)
    labels={}
    for cid,members in communities.items():
        counts=Counter()
        for member in members:
            src=G.nodes[member].get('source_file','').replace('\\','/')
            parts=src.split('/')
            if src.startswith('Assets/_JungleMaze/Scripts/'): group='/'.join(parts[3:5])
            elif src.startswith('Assets/Imported/'): group='Imported '+('/'.join(parts[3:5]))
            else: group='/'.join(parts[:3])
            if group: counts[group]+=1
        labels[cid]=counts.most_common(1)[0][0] if counts else 'External references'
    gods=god_nodes(G); surprises=surprising_connections(G,communities); questions=suggest_questions(G,communities,labels)
    commit=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    if not to_json(G,communities,str(OUT/'graph.json'),force=True,built_at_commit=commit,community_labels=labels,original_links=edges): raise RuntimeError('Graph export refused')
    detection=dict(total_files=len(code)+1,total_words=sum(len((ROOT/p).read_text(encoding='utf-8',errors='replace').split()) for p in code),files={'code':code,'document':[DOC]})
    report=generate(G,communities,cohesion,labels,gods,surprises,detection,{'input':sem.get('input_tokens',0),'output':sem.get('output_tokens',0)},str(ROOT),suggested_questions=questions,built_at_commit=commit)
    health=diagnose_extraction(extraction,directed=True,root=str(ROOT))
    dump('health.json',health)
    report=report.replace('Run `graphify update .` after code changes (no API cost).', 'Run `Tools/update_project_map.py` with the configured graphify Python after code/config changes; unchanged hashes skip rebuilding.')
    report=report.replace('- Token cost:', '- Extraction-agent self-reported tokens (not metered billing; supervision excluded):')
    report+='\n\n## Scope and limits\nAll indexed source/config files; assets grouped by directory. Architecture document relations are documented claims; name bridges are INFERRED, not proof of runtime behavior. AST parser warnings do not imply C# compile errors. No gameplay testing performed.\n\n'+format_diagnostic_report(health)+'\n\n`graphify-out/` is generated inside the working tree; commit selected outputs only.\n'
    (OUT/'GRAPH_REPORT.md').write_text(report,encoding='utf-8')
    dump('.graphify_labels.json',{str(k):v for k,v in labels.items()})
    dump('.graphify_analysis.json',dict(communities=communities,cohesion=cohesion,gods=gods,surprises=surprises,questions=questions))
    print(f'Graph: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges; {len(communities)} communities')
    if G.number_of_nodes()>5000: print('WARNING: >5000 nodes; graph.html opens aggregated community view.')
    to_html(G,communities,str(OUT/'graph.html'),community_labels=labels,node_limit=5000)
    subprocess.run([sys.executable,'-m','graphify','tree','--root','.','--label','Lastaway','--output',str(OUT/'GRAPH_TREE.html')],check=True)
    save_manifest({'code':code,'document':[DOC] if semantic_current else []},root=ROOT,scan_corpus=set(code+[DOC]))
    dump('code-state.json',dict(hashes=hashes,commit=commit,architecture_current=semantic_current))
    dump('cost.json',dict(semantic_input_tokens=sem.get('input_tokens',0),semantic_output_tokens=sem.get('output_tokens',0),note='Agent-reported token counts; Codex supervision unmetered here. AST uses no model API. Not a billing or savings claim.'))
    print(format_diagnostic_report(health))

if __name__=='__main__': main()
