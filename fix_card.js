const fs = require('fs');
const path = require('path');
const filePath = path.join(__dirname, 'src', 'pages', 'Reading', 'DrawCards.tsx');
let content = fs.readFileSync(filePath, 'utf-8');

const marker = "border: '2px solid rgba(212, 175, 55, 0.45)',";
const startIdx = content.indexOf(marker);
if (startIdx === -1) { console.log('NOT FOUND'); process.exit(1); }

const autoIdx = content.indexOf('<AutoAwesome', startIdx);
const closeIdx = content.indexOf('/>', autoIdx) + 2;

const newContent = `border: '2.5px solid rgba(212, 175, 55, 0.85)',
                    background: 'linear-gradient(155deg, #0c0920 0%, #1a1040 35%, #0f1c38 65%, #080614 100%)',
                    boxShadow: '0 24px 60px rgba(0, 0, 0, 0.8), 0 0 30px rgba(0, 240, 255, 0.2)',
                    position: 'relative',
                    zIndex: 2,
                    transition: 'all 0.3s ease',
                    overflow: 'hidden',
                    '&:hover': {
                      boxShadow: '0 30px 70px rgba(0, 0, 0, 0.82), 0 0 50px rgba(0, 240, 255, 0.32)',
                      transform: 'translateY(-5px)',
                    },
                  }}
                >
                  <svg viewBox="0 0 400 660" style={{ position: 'absolute', inset: 0, width: '100%', height: '100%' }} xmlns="http://www.w3.org/2000/svg">
                    <defs>
                      <radialGradient id="pg" cx="50%" cy="50%" r="42%"><stop offset="0%" stopColor="#f5d97b" stopOpacity="0.4" /><stop offset="60%" stopColor="#c9a84c" stopOpacity="0.1" /><stop offset="100%" stopColor="#f5d97b" stopOpacity="0" /></radialGradient>
                      <radialGradient id="pgc" cx="50%" cy="50%" r="45%"><stop offset="0%" stopColor="#56bdf8" stopOpacity="0.1" /><stop offset="100%" stopColor="#56bdf8" stopOpacity="0" /></radialGradient>
                      <filter id="pn" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="2.5" result="b" /><feMerge><feMergeNode in="b" /><feMergeNode in="SourceGraphic" /></feMerge></filter>
                    </defs>
                    {[[32,45,1.4,'#f5d97b',0.5],[370,28,1.1,'#8cd3ff',0.4],[55,120,1,'#fff',0.35],[345,95,1.3,'#c99bff',0.4],[28,250,1.2,'#f5d97b',0.3],[372,200,0.9,'#8cd3ff',0.35],[40,400,1.1,'#fff',0.3],[360,440,1.3,'#f5d97b',0.4],[50,550,1,'#8cd3ff',0.35],[350,580,1.2,'#c99bff',0.3],[200,25,1.1,'#fff',0.3],[200,635,1,'#fff',0.3]].map(([cx,cy,r,f,o],i)=>(<circle key={i} cx={cx as number} cy={cy as number} r={r as number} fill={f as string} opacity={o as number} />))}
                    <rect x="12" y="12" width="376" height="636" rx="18" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.8" />
                    <rect x="22" y="22" width="356" height="616" rx="14" fill="none" stroke="#d4af37" strokeWidth="1" opacity="0.35" />
                    <rect x="30" y="30" width="340" height="600" rx="11" fill="none" stroke="#56bdf8" strokeWidth="0.6" opacity="0.2" strokeDasharray="5 6" />
                    <g opacity="0.65" filter="url(#pn)">
                      <path d="M32 40 L32 72 L64 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="38" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M368 40 L368 72 L336 40 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="362" cy="46" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M32 620 L32 588 L64 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="38" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
                      <path d="M368 620 L368 588 L336 620 Z" fill="none" stroke="#d4af37" strokeWidth="1.5" /><circle cx="362" cy="614" r="2.5" fill="#d4af37" opacity="0.7" />
                    </g>
                    <circle cx="200" cy="330" r="200" fill="url(#pgc)" /><circle cx="200" cy="330" r="160" fill="url(#pg)" />
                    <circle cx="200" cy="330" r="140" fill="none" stroke="#d4af37" strokeWidth="2.5" opacity="0.7" filter="url(#pn)" />
                    <circle cx="200" cy="330" r="128" fill="none" stroke="#d4af37" strokeWidth="0.8" opacity="0.3" strokeDasharray="10 5" />
                    <circle cx="200" cy="330" r="100" fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.4" filter="url(#pn)" />
                    <circle cx="200" cy="330" r="70" fill="none" stroke="#d4af37" strokeWidth="1.2" opacity="0.5" />
                    <polygon points="200,185 222,300 340,330 222,360 200,475 178,360 60,330 178,300" fill="none" stroke="#f5d97b" strokeWidth="3" opacity="0.75" filter="url(#pn)" />
                    <g transform="rotate(22.5, 200, 330)"><polygon points="200,225 212,305 292,330 212,355 200,435 188,355 108,330 188,305" fill="none" stroke="#56bdf8" strokeWidth="1.5" opacity="0.35" /></g>
                    {[0,45,90,135,180,225,270,315].map((a,i)=>{const r=(a*Math.PI)/180;return <circle key={\\\`p\\\${i}\\\`} cx={200+140*Math.cos(r)} cy={330+140*Math.sin(r)} r={i%2===0?4:3} fill={i%2===0?'#f5d97b':'#56bdf8'} opacity={i%2===0?0.85:0.6} filter="url(#pn)" />;})}
                    <circle cx="200" cy="330" r="20" fill="#f5d97b" opacity="0.8" filter="url(#pn)" /><circle cx="200" cy="330" r="10" fill="#0d0820" /><circle cx="200" cy="330" r="4.5" fill="#56bdf8" opacity="0.9" />
                    <path d="M172 330 Q200 310 228 330 Q200 350 172 330" fill="none" stroke="#f5d97b" strokeWidth="1.8" opacity="0.6" />
                    <text x="200" y="88" textAnchor="middle" fontSize="26" fontFamily="Georgia, serif" fill="#f5d97b" letterSpacing="8" opacity="0.85" filter="url(#pn)">TAROT</text>
                    <text x="200" y="115" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">\\u2726   \\u2726   \\u2726</text>
                    <text x="200" y="575" textAnchor="middle" fontSize="15" fontFamily="Georgia, serif" fill="#8cd3ff" letterSpacing="7" opacity="0.55">ARCANA</text>
                    <text x="200" y="553" textAnchor="middle" fontSize="11" fill="#d4af37" opacity="0.5" letterSpacing="3">\\u2726   \\u2726   \\u2726</text>
                  </svg>`;

content = content.substring(0, startIdx) + newContent + content.substring(closeIdx);
fs.writeFileSync(filePath, content, 'utf-8');
console.log('SUCCESS');
