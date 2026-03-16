import React from 'react';
import { Box } from '@mui/material';
import './CosmicBackground.css';

interface CosmicBackgroundProps {
  showRings?: boolean;
}

const CosmicBackground: React.FC<CosmicBackgroundProps> = ({ showRings = true }) => {
  return (
    <Box
      sx={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        width: '100vw',
        height: '100vh',
        zIndex: -1,
        overflow: 'hidden',
        pointerEvents: 'none',
      }}
    >
      {/* 深空星云背景 */}
      <div className="cosmic-nebula">
        <div className="stars-layer-1"></div>
        <div className="stars-layer-2"></div>
        <div className="stars-layer-3"></div>
      </div>

      {showRings && (
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            width: { xs: '800px', md: '1200px' },
            height: { xs: '800px', md: '1200px' },
            opacity: 0.6,
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
          }}
        >
          <svg
            viewBox="0 0 1000 1000"
            xmlns="http://www.w3.org/2000/svg"
            className="magic-circle-svg"
          >
            <defs>
              <filter id="neon-glow" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur stdDeviation="3" result="coloredBlur" />
                <feMerge>
                  <feMergeNode in="coloredBlur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
              
              <filter id="neon-glow-strong" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur stdDeviation="6" result="coloredBlur" />
                <feMerge>
                  <feMergeNode in="coloredBlur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>

            {/* 旋转的外部魔法环组 */}
            <g className="rotate-slow">
              {/* 外圈虚线环 */}
              <circle
                cx="500"
                cy="500"
                r="480"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="2"
                strokeDasharray="10 20"
                filter="url(#neon-glow)"
                opacity="0.3"
              />
              
              {/* 外层主干线 */}
              <circle
                cx="500"
                cy="500"
                r="450"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="1.5"
                filter="url(#neon-glow)"
                opacity="0.5"
              />
              
              {/* 装饰性外部几何形态：六芒星 (Hexagram - Star of David) */}
              <g opacity="0.4" filter="url(#neon-glow)">
                <polygon
                  points="500,50 890,725 110,725"
                  fill="none"
                  stroke="#00F0FF"
                  strokeWidth="1.5"
                />
                <polygon
                  points="500,950 890,275 110,275"
                  fill="none"
                  stroke="#00F0FF"
                  strokeWidth="1.5"
                />
              </g>
            </g>

            {/* 逆向旋转的内圈神秘符号组 */}
            <g className="rotate-slow-reverse">
              <circle
                cx="500"
                cy="500"
                r="380"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="3"
                strokeDasharray="150 50 20 50"
                filter="url(#neon-glow-strong)"
                opacity="0.6"
              />
              
              {/* 内层八角星 */}
              <polygon
                points="500,150 590,410 850,500 590,590 500,850 410,590 150,500 410,410"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="1.5"
                opacity="0.4"
                filter="url(#neon-glow)"
              />
              
              {/* 八卦/占星定位圆点 */}
              {[0, 45, 90, 135, 180, 225, 270, 315].map((angle, i) => {
                const rad = (angle * Math.PI) / 180;
                const r = 380;
                return (
                  <circle
                    key={i}
                    cx={500 + r * Math.cos(rad)}
                    cy={500 + r * Math.sin(rad)}
                    r="6"
                    fill="#D4AF37"
                    filter="url(#neon-glow)"
                  />
                );
              })}
              {/* 装饰行星轨道环 */}
              <circle
                cx="500"
                cy="500"
                r="300"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="1"
                opacity="0.3"
                filter="url(#neon-glow)"
              />
              <circle cx="800" cy="500" r="15" fill="#00F0FF" filter="url(#neon-glow)" opacity="0.8" />
              <circle cx="200" cy="500" r="8" fill="#D4AF37" filter="url(#neon-glow-strong)" opacity="0.9" />
              <circle cx="500" cy="800" r="12" fill="#D4AF37" filter="url(#neon-glow)" opacity="0.5" />
            </g>

            {/* 新月装饰 (Crescent Moons) */}
            <g className="rotate-slow-reverse" style={{ transformOrigin: 'center', animationDuration: '45s' }}>
              <path
                d="M 500 110 A 370 370 0 0 1 550 120 A 380 380 0 0 0 500 90 Z"
                fill="#00F0FF"
                filter="url(#neon-glow)"
                opacity="0.8"
              />
              <path
                d="M 500 890 A 370 370 0 0 1 450 880 A 380 380 0 0 0 500 910 Z"
                fill="#D4AF37"
                filter="url(#neon-glow)"
                opacity="0.8"
              />
              <path
                d="M 110 500 A 370 370 0 0 1 120 450 A 380 380 0 0 0 90 500 Z"
                fill="#00F0FF"
                filter="url(#neon-glow)"
                opacity="0.8"
              />
              <path
                d="M 890 500 A 370 370 0 0 1 880 550 A 380 380 0 0 0 910 500 Z"
                fill="#D4AF37"
                filter="url(#neon-glow)"
                opacity="0.8"
              />
            </g>

            {/* 最核心区域稳定的圆环 */}
            <g className="pulse-glow">
              <circle
                cx="500"
                cy="500"
                r="250"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="1"
                opacity="0.3"
                filter="url(#neon-glow)"
              />
              <circle
                cx="500"
                cy="500"
                r="240"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="0.5"
                strokeDasharray="5 5"
                opacity="0.4"
              />

              {/* 全视之眼 Eye of Providence (Central) */}
              <polygon points="500,380 400,560 600,560" fill="none" stroke="#D4AF37" strokeWidth="2" filter="url(#neon-glow-strong)" opacity="0.8" />
              <path d="M 440 500 Q 500 460 560 500 Q 500 540 440 500" fill="none" stroke="#00F0FF" strokeWidth="2" filter="url(#neon-glow)" opacity="0.9" />
              <circle cx="500" cy="500" r="15" fill="#D4AF37" filter="url(#neon-glow-strong)" />
              <circle cx="500" cy="500" r="5" fill="#0A0512" />
            </g>
          </svg>
        </Box>
      )}
    </Box>
  );
};

export default CosmicBackground;
