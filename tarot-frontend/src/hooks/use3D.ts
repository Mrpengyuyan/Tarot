import { useRef, useEffect, useCallback, useState } from 'react';
import * as THREE from 'three';
import { useFrame, useThree } from '@react-three/fiber';

// 3D场景基础钩子
export const use3DScene = () => {
  const { scene, camera, gl } = useThree();

  const setBackground = useCallback((background: THREE.Color | THREE.Texture | THREE.CubeTexture) => {
    scene.background = background;
  }, [scene]);

  const addObject = useCallback((object: THREE.Object3D) => {
    scene.add(object);
  }, [scene]);

  const removeObject = useCallback((object: THREE.Object3D) => {
    scene.remove(object);
  }, [scene]);

  const setCameraPosition = useCallback((x: number, y: number, z: number) => {
    camera.position.set(x, y, z);
  }, [camera]);

  const setCameraTarget = useCallback((x: number, y: number, z: number) => {
    camera.lookAt(new THREE.Vector3(x, y, z));
  }, [camera]);

  return {
    scene,
    camera,
    gl,
    setBackground,
    addObject,
    removeObject,
    setCameraPosition,
    setCameraTarget,
  };
};

// 3D对象动画钩子
interface AnimationState {
  position: THREE.Vector3;
  rotation: THREE.Euler;
  scale: THREE.Vector3;
}

export const use3DAnimation = (
  objectRef: React.RefObject<THREE.Object3D>,
  options: {
    autoStart?: boolean;
    loop?: boolean;
    duration?: number;
  } = {}
) => {
  const { autoStart = false, loop = false, duration = 1000 } = options;

  const [isPlaying, setIsPlaying] = useState(autoStart);
  const [progress, setProgress] = useState(0);
  const startTimeRef = useRef<number>(0);
  const fromStateRef = useRef<AnimationState | null>(null);
  const toStateRef = useRef<AnimationState | null>(null);

  const start = useCallback((from: AnimationState, to: AnimationState) => {
    if (!objectRef.current) return;

    fromStateRef.current = from;
    toStateRef.current = to;
    startTimeRef.current = Date.now();
    setIsPlaying(true);
    setProgress(0);
  }, [objectRef]);

  const stop = useCallback(() => {
    setIsPlaying(false);
    setProgress(0);
  }, []);

  const pause = useCallback(() => {
    setIsPlaying(false);
  }, []);

  const resume = useCallback(() => {
    setIsPlaying(true);
  }, []);

  // 动画更新
  useFrame(() => {
    if (!isPlaying || !objectRef.current || !fromStateRef.current || !toStateRef.current) {
      return;
    }

    const elapsed = Date.now() - startTimeRef.current;
    const newProgress = Math.min(elapsed / duration, 1);

    setProgress(newProgress);

    // 线性插值
    const lerp = (a: number, b: number, t: number) => a + (b - a) * t;
    const lerpVector3 = (from: THREE.Vector3, to: THREE.Vector3, t: number) => {
      return new THREE.Vector3(
        lerp(from.x, to.x, t),
        lerp(from.y, to.y, t),
        lerp(from.z, to.z, t)
      );
    };

    // 更新位置
    const newPosition = lerpVector3(fromStateRef.current.position, toStateRef.current.position, newProgress);
    objectRef.current.position.copy(newPosition);

    // 更新旋转
    objectRef.current.rotation.x = lerp(fromStateRef.current.rotation.x, toStateRef.current.rotation.x, newProgress);
    objectRef.current.rotation.y = lerp(fromStateRef.current.rotation.y, toStateRef.current.rotation.y, newProgress);
    objectRef.current.rotation.z = lerp(fromStateRef.current.rotation.z, toStateRef.current.rotation.z, newProgress);

    // 更新缩放
    const newScale = lerpVector3(fromStateRef.current.scale, toStateRef.current.scale, newProgress);
    objectRef.current.scale.copy(newScale);

    // 检查动画是否完成
    if (newProgress >= 1) {
      if (loop) {
        startTimeRef.current = Date.now();
        setProgress(0);
      } else {
        setIsPlaying(false);
      }
    }
  });

  return {
    start,
    stop,
    pause,
    resume,
    isPlaying,
    progress,
  };
};

// 塔罗牌翻转动画钩子
export const useTarotCardFlip = (cardRef: React.RefObject<THREE.Group>) => {
  const [isFlipped, setIsFlipped] = useState(false);
  const [isAnimating, setIsAnimating] = useState(false);
  const { start, isPlaying } = use3DAnimation(cardRef, { duration: 800 });

  const flip = useCallback(() => {
    if (isAnimating || !cardRef.current) return;

    setIsAnimating(true);
    const currentRotation = cardRef.current.rotation.clone();
    const targetRotation = new THREE.Euler(
      currentRotation.x,
      currentRotation.y + Math.PI,
      currentRotation.z
    );

    start(
      {
        position: cardRef.current.position.clone(),
        rotation: currentRotation,
        scale: cardRef.current.scale.clone(),
      },
      {
        position: cardRef.current.position.clone(),
        rotation: targetRotation,
        scale: cardRef.current.scale.clone(),
      }
    );

    setIsFlipped(!isFlipped);

    // 动画完成后重置状态
    setTimeout(() => {
      setIsAnimating(false);
    }, 800);
  }, [cardRef, start, isFlipped, isAnimating]);

  return {
    flip,
    isFlipped,
    isAnimating,
  };
};

// 3D悬浮效果钩子
export const use3DFloating = (
  objectRef: React.RefObject<THREE.Object3D>,
  options: {
    amplitude?: number;
    speed?: number;
    axis?: 'x' | 'y' | 'z';
    enabled?: boolean;
  } = {}
) => {
  const { amplitude = 0.5, speed = 1, axis = 'y', enabled = true } = options;
  const initialPositionRef = useRef<THREE.Vector3 | null>(null);

  useFrame((state) => {
    if (!enabled || !objectRef.current) return;

    // 保存初始位置
    if (!initialPositionRef.current) {
      initialPositionRef.current = objectRef.current.position.clone();
    }

    // 计算悬浮偏移
    const offset = Math.sin(state.clock.elapsedTime * speed) * amplitude;

    // 应用到指定轴
    if (axis === 'x') {
      objectRef.current.position.x = initialPositionRef.current.x + offset;
    } else if (axis === 'y') {
      objectRef.current.position.y = initialPositionRef.current.y + offset;
    } else if (axis === 'z') {
      objectRef.current.position.z = initialPositionRef.current.z + offset;
    }
  });

  return {
    reset: () => {
      if (objectRef.current && initialPositionRef.current) {
        objectRef.current.position.copy(initialPositionRef.current);
      }
    },
  };
};

// 3D旋转动画钩子
export const use3DRotation = (
  objectRef: React.RefObject<THREE.Object3D>,
  options: {
    speed?: THREE.Vector3;
    enabled?: boolean;
  } = {}
) => {
  const { speed = new THREE.Vector3(0, 0.01, 0), enabled = true } = options;

  useFrame(() => {
    if (!enabled || !objectRef.current) return;

    objectRef.current.rotation.x += speed.x;
    objectRef.current.rotation.y += speed.y;
    objectRef.current.rotation.z += speed.z;
  });

  const setSpeed = useCallback((newSpeed: THREE.Vector3) => {
    speed.copy(newSpeed);
  }, [speed]);

  return { setSpeed };
};

// 鼠标交互钩子
export const use3DMouseInteraction = (
  objectRef: React.RefObject<THREE.Object3D>,
  options: {
    onHover?: (hovered: boolean) => void;
    onClick?: () => void;
    hoverScale?: number;
    clickScale?: number;
  } = {}
) => {
  const { onHover, onClick, hoverScale = 1.1, clickScale = 0.95 } = options;
  const [hovered, setHovered] = useState(false);
  const [clicked, setClicked] = useState(false);
  const originalScaleRef = useRef<THREE.Vector3 | null>(null);

  // 保存原始缩放
  useEffect(() => {
    if (objectRef.current && !originalScaleRef.current) {
      originalScaleRef.current = objectRef.current.scale.clone();
    }
  }, [objectRef]);

  const handlePointerEnter = useCallback(() => {
    if (!objectRef.current || !originalScaleRef.current) return;

    setHovered(true);
    objectRef.current.scale.copy(originalScaleRef.current).multiplyScalar(hoverScale);
    onHover?.(true);
  }, [objectRef, hoverScale, onHover]);

  const handlePointerLeave = useCallback(() => {
    if (!objectRef.current || !originalScaleRef.current) return;

    setHovered(false);
    setClicked(false);
    objectRef.current.scale.copy(originalScaleRef.current);
    onHover?.(false);
  }, [objectRef, onHover]);

  const handlePointerDown = useCallback(() => {
    if (!objectRef.current || !originalScaleRef.current) return;

    setClicked(true);
    objectRef.current.scale.copy(originalScaleRef.current).multiplyScalar(clickScale);
  }, [objectRef, clickScale]);

  const handlePointerUp = useCallback(() => {
    if (!objectRef.current || !originalScaleRef.current) return;

    setClicked(false);
    const scale = hovered ? hoverScale : 1;
    objectRef.current.scale.copy(originalScaleRef.current).multiplyScalar(scale);
  }, [objectRef, hovered, hoverScale]);

  const handleClick = useCallback(() => {
    onClick?.();
  }, [onClick]);

  return {
    hovered,
    clicked,
    eventHandlers: {
      onPointerEnter: handlePointerEnter,
      onPointerLeave: handlePointerLeave,
      onPointerDown: handlePointerDown,
      onPointerUp: handlePointerUp,
      onClick: handleClick,
    },
  };
};

// 3D粒子系统钩子
export const use3DParticleSystem = (
  count: number,
  options: {
    spread?: number;
    speed?: number;
    size?: number;
    color?: THREE.Color;
  } = {}
) => {
  const { spread = 10, speed = 0.01, size = 0.1, color = new THREE.Color(0xffffff) } = options;
  const particlesRef = useRef<THREE.Points | null>(null);
  const velocitiesRef = useRef<Float32Array | null>(null);

  // 初始化粒子系统
  const initParticles = useCallback(() => {
    const geometry = new THREE.BufferGeometry();
    const positions = new Float32Array(count * 3);
    const velocities = new Float32Array(count * 3);

    for (let i = 0; i < count; i++) {
      const i3 = i * 3;

      // 随机位置
      positions[i3] = (Math.random() - 0.5) * spread;
      positions[i3 + 1] = (Math.random() - 0.5) * spread;
      positions[i3 + 2] = (Math.random() - 0.5) * spread;

      // 随机速度
      velocities[i3] = (Math.random() - 0.5) * speed;
      velocities[i3 + 1] = (Math.random() - 0.5) * speed;
      velocities[i3 + 2] = (Math.random() - 0.5) * speed;
    }

    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    velocitiesRef.current = velocities;

    const material = new THREE.PointsMaterial({
      size,
      color,
      transparent: true,
      opacity: 0.6,
    });

    particlesRef.current = new THREE.Points(geometry, material);

    return particlesRef.current;
  }, [count, spread, speed, size, color]);

  // 更新粒子
  useFrame(() => {
    if (!particlesRef.current || !velocitiesRef.current) return;

    const positions = particlesRef.current.geometry.attributes.position.array as Float32Array;

    for (let i = 0; i < count; i++) {
      const i3 = i * 3;

      positions[i3] += velocitiesRef.current[i3];
      positions[i3 + 1] += velocitiesRef.current[i3 + 1];
      positions[i3 + 2] += velocitiesRef.current[i3 + 2];

      // 边界检查和重置
      if (Math.abs(positions[i3]) > spread / 2) {
        positions[i3] = -positions[i3];
      }
      if (Math.abs(positions[i3 + 1]) > spread / 2) {
        positions[i3 + 1] = -positions[i3 + 1];
      }
      if (Math.abs(positions[i3 + 2]) > spread / 2) {
        positions[i3 + 2] = -positions[i3 + 2];
      }
    }

    particlesRef.current.geometry.attributes.position.needsUpdate = true;
  });

  return {
    initParticles,
    particlesRef,
  };
};

export default use3DScene;