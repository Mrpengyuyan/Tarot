import React from 'react';
import { Box } from '@mui/material';
import './CosmicBackground.css';

interface CosmicBackgroundProps {
  showRings?: boolean;
  performanceMode?: 'full' | 'lite' | 'minimal';
}

const ORBIT_DOTS_FULL = [0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300, 330];
const ORBIT_DOTS_LITE = [0, 90, 180, 270];

const CosmicBackground: React.FC<CosmicBackgroundProps> = ({
  showRings = true,
  performanceMode = 'full',
}) => {
  const isLite = performanceMode === 'lite';
  const isMinimal = performanceMode === 'minimal';
  const orbitDots = isLite ? ORBIT_DOTS_LITE : ORBIT_DOTS_FULL;

  return (
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        width: '100vw',
        height: '100vh',
        zIndex: -1,
        overflow: 'hidden',
        pointerEvents: 'none',
      }}
    >
      <div className={`cosmic-nebula is-${performanceMode}`}>
        <div className="cosmic-aurora"></div>
        <div className="cosmic-vignette"></div>
        <div className="stars-layer-1"></div>
        <div className="stars-layer-2"></div>
        <div className="stars-layer-3"></div>
        {!isLite && !isMinimal && <div className="stars-layer-4"></div>}
        {!isLite && !isMinimal && <div className="stars-layer-5"></div>}
        {!isLite && !isMinimal && (
          <>
            <div className="star-orbit star-orbit-1"><span></span></div>
            <div className="star-orbit star-orbit-2"><span></span></div>
            <div className="shooting-star shooting-star-1"></div>
          </>
        )}
      </div>

      {showRings && !isMinimal && (
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            width: isLite ? { xs: '760px', md: '980px' } : { xs: '860px', md: '1120px' },
            height: isLite ? { xs: '760px', md: '980px' } : { xs: '860px', md: '1120px' },
            opacity: isLite ? 0.46 : 0.58,
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
          }}
        >
          <svg viewBox="0 0 1000 1000" xmlns="http://www.w3.org/2000/svg" className={isLite ? 'magic-circle-svg-lite' : 'magic-circle-svg'}>
            <g className="rotate-slow" style={isLite ? { animationDuration: '56s' } : { animationDuration: '66s' }}>
              <circle
                cx="500"
                cy="500"
                r="470"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="1.5"
                strokeDasharray={isLite ? '8 16' : '10 20'}
                opacity="0.28"
              />
              <circle
                cx="500"
                cy="500"
                r="436"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="1.5"
                opacity="0.38"
              />
              <polygon
                points="500,88 850,704 150,704"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="1.4"
                opacity={isLite ? '0.2' : '0.3'}
              />
              <polygon
                points="500,912 850,296 150,296"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="1.4"
                opacity={isLite ? '0.18' : '0.28'}
              />
              {!isLite && (
                <>
                  <path d="M 500 78 A 412 412 0 0 1 548 86 A 430 430 0 0 0 500 56 Z" fill="#00F0FF" opacity="0.5" />
                  <path d="M 500 922 A 412 412 0 0 1 452 914 A 430 430 0 0 0 500 944 Z" fill="#D4AF37" opacity="0.5" />
                  <path d="M 78 500 A 412 412 0 0 1 86 452 A 430 430 0 0 0 56 500 Z" fill="#00F0FF" opacity="0.42" />
                  <path d="M 922 500 A 412 412 0 0 1 914 548 A 430 430 0 0 0 944 500 Z" fill="#D4AF37" opacity="0.42" />
                </>
              )}
            </g>

            <g className="rotate-slow-reverse" style={isLite ? { animationDuration: '72s' } : { animationDuration: '96s' }}>
              <circle
                cx="500"
                cy="500"
                r="360"
                fill="none"
                stroke="#D4AF37"
                strokeWidth="2.4"
                strokeDasharray={isLite ? '120 28' : '150 50 20 50'}
                opacity="0.42"
              />
              <polygon
                points="500,176 610,390 824,500 610,610 500,824 390,610 176,500 390,390"
                fill="none"
                stroke="#00F0FF"
                strokeWidth="1.2"
                opacity={isLite ? '0.22' : '0.32'}
              />

              {orbitDots.map((angle) => {
                const rad = (angle * Math.PI) / 180;
                const radius = 360;
                return (
                  <circle
                    key={angle}
                    cx={500 + radius * Math.cos(rad)}
                    cy={500 + radius * Math.sin(rad)}
                    r={isLite ? '4' : '5.5'}
                    fill="#D4AF37"
                    opacity="0.78"
                  />
                );
              })}

              {!isLite && (
                <>
                  <circle cx="800" cy="500" r="12" fill="#00F0FF" opacity="0.68" />
                  <circle cx="200" cy="500" r="7" fill="#D4AF37" opacity="0.82" />
                  <circle cx="500" cy="800" r="10" fill="#D4AF37" opacity="0.48" />
                  <circle cx="500" cy="500" r="302" fill="none" stroke="#00F0FF" strokeWidth="1" opacity="0.24" />
                </>
              )}
            </g>

            <g className="pulse-glow-lite">
              <circle cx="500" cy="500" r="248" fill="none" stroke="#D4AF37" strokeWidth="1.1" opacity="0.24" />
              <circle cx="500" cy="500" r="236" fill="none" stroke="#00F0FF" strokeWidth="0.9" strokeDasharray="5 7" opacity="0.26" />
              <polygon points="500,392 412,554 588,554" fill="none" stroke="#D4AF37" strokeWidth="1.8" opacity="0.7" />
              <path d="M 444 502 Q 500 462 556 502 Q 500 542 444 502" fill="none" stroke="#00F0FF" strokeWidth="1.8" opacity="0.76" />
              <circle cx="500" cy="502" r="12" fill="#D4AF37" opacity="0.82" />
              <circle cx="500" cy="502" r="4" fill="#0A0512" opacity="0.9" />
            </g>
          </svg>
        </Box>
      )}
    </Box>
  );
};

export default React.memo(CosmicBackground);
