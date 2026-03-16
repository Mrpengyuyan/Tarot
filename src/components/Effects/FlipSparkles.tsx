import React, { useRef, useState, useEffect } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { Sparkles } from '@react-three/drei';
import * as THREE from 'three';

interface FlipSparklesProps {
  isActive: boolean;
  color?: string;
  count?: number;
  size?: number;
  speed?: number;
}

const ParticleBurst = ({ isActive, color = '#D4AF37', count = 100, size = 15, speed = 1.5 }: FlipSparklesProps) => {
  const groupRef = useRef<THREE.Group>(null);
  const [scale, setScale] = useState(0);

  useEffect(() => {
    if (isActive) {
      setScale(0); // Reset scale
    }
  }, [isActive]);

  useFrame((state, delta) => {
    if (isActive && groupRef.current) {
      // Create a rapid burst outwards then slowly fade
      if (scale < 3) {
        setScale((prev) => Math.min(prev + delta * speed * 5, 3));
      }
      groupRef.current.scale.set(scale, scale, scale);
      groupRef.current.rotation.y += delta * 0.5;
      groupRef.current.rotation.z += delta * 0.2;
    }
  });

  if (!isActive) return null;

  return (
    <group ref={groupRef}>
      <Sparkles
        count={count}
        scale={4}
        size={size}
        speed={speed}
        opacity={scale > 2 ? 1 - (scale - 2) : 1}
        color={color}
        noise={3} // Randomness of particle movement
      />
      <Sparkles
        count={count / 2}
        scale={6}
        size={size * 1.5}
        speed={speed * 0.5}
        opacity={scale > 2 ? 1 - (scale - 2) : 0.8}
        color="#00F0FF"
        noise={4}
      />
    </group>
  );
};

export const FlipSparklesContainer: React.FC<{ isActive: boolean }> = ({ isActive }) => {
  if (!isActive) return null;

  return (
    <div
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        pointerEvents: 'none',
        zIndex: 10,
      }}
    >
      <Canvas
        camera={{ position: [0, 0, 5], fov: 45 }}
        gl={{ alpha: true, antialias: false }}
        style={{ pointerEvents: 'none' }}
      >
        <ambientLight intensity={1} />
        <ParticleBurst isActive={isActive} />
      </Canvas>
    </div>
  );
};
