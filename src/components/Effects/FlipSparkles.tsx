import React, { useEffect, useRef } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { Sparkles } from '@react-three/drei';
import * as THREE from 'three';

type SparkleQuality = 'full' | 'lite' | 'off';

interface ParticleBurstProps {
  isActive: boolean;
  quality?: SparkleQuality;
}

const PARTICLE_PRESETS: Record<Exclude<SparkleQuality, 'off'>, { count: number; size: number; speed: number; scale: number }> = {
  full: { count: 100, size: 15, speed: 1.5, scale: 4 },
  lite: { count: 48, size: 10, speed: 1.1, scale: 3.2 },
};

const ParticleBurst: React.FC<ParticleBurstProps> = ({ isActive, quality = 'full' }) => {
  const groupRef = useRef<THREE.Group>(null);
  const scaleRef = useRef(0);
  const presetKey = quality === 'off' ? 'lite' : quality;

  useEffect(() => {
    if (!isActive) {
      scaleRef.current = 0;
      if (groupRef.current) {
        groupRef.current.scale.set(0, 0, 0);
      }
    }
  }, [isActive]);

  useFrame((_, delta) => {
    if (!isActive || !groupRef.current) {
      return;
    }

    if (scaleRef.current < 3) {
      scaleRef.current = Math.min(scaleRef.current + delta * PARTICLE_PRESETS[presetKey].speed * 5, 3);
    }

    groupRef.current.scale.setScalar(scaleRef.current);
    groupRef.current.rotation.y += delta * 0.45;
    groupRef.current.rotation.z += delta * 0.18;
  });

  if (!isActive) {
    return null;
  }

  const preset = PARTICLE_PRESETS[presetKey];
  const fadeOpacity = scaleRef.current > 2 ? 1 - (scaleRef.current - 2) : 1;

  return (
    <group ref={groupRef}>
      <Sparkles
        count={preset.count}
        scale={preset.scale}
        size={preset.size}
        speed={preset.speed}
        opacity={fadeOpacity}
        color="#D4AF37"
        noise={quality === 'full' ? 3 : 2}
      />
      {quality === 'full' && (
        <Sparkles
          count={Math.round(preset.count / 2)}
          scale={preset.scale + 2}
          size={preset.size * 1.35}
          speed={preset.speed * 0.55}
          opacity={fadeOpacity * 0.78}
          color="#00F0FF"
          noise={4}
        />
      )}
    </group>
  );
};

export const FlipSparklesContainer: React.FC<{ isActive: boolean; quality?: SparkleQuality }> = ({
  isActive,
  quality = 'full',
}) => {
  if (!isActive || quality === 'off') {
    return null;
  }

  return (
    <div
      style={{
        position: 'absolute',
        inset: 0,
        pointerEvents: 'none',
        zIndex: 10,
      }}
    >
      <Canvas
        camera={{ position: [0, 0, 5], fov: 45 }}
        gl={{ alpha: true, antialias: quality === 'full' }}
        dpr={quality === 'full' ? [1, 1.5] : [1, 1.2]}
        style={{ pointerEvents: 'none' }}
      >
        <ambientLight intensity={quality === 'full' ? 1 : 0.85} />
        <ParticleBurst isActive={isActive} quality={quality} />
      </Canvas>
    </div>
  );
};
