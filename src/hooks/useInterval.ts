import { useEffect, useRef, useCallback } from 'react';

// 基础定时器钩子
export const useInterval = (
  callback: () => void,
  delay: number | null,
  immediate: boolean = false
): void => {
  const savedCallback = useRef<() => void>(() => {});

  // 保存最新的回调函数
  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  // 设置定时器
  useEffect(() => {
    if (delay === null) {
      return;
    }

    const tick = () => {
      savedCallback.current?.();
    };

    // 是否立即执行
    if (immediate) {
      tick();
    }

    const id = setInterval(tick, delay);

    return () => {
      clearInterval(id);
    };
  }, [delay, immediate]);
};

// 可控制的定时器钩子
export const useControllableInterval = (
  callback: () => void,
  delay: number = 1000
): {
  start: () => void;
  stop: () => void;
  reset: () => void;
  isRunning: boolean;
} => {
  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  const savedCallback = useRef<() => void>(() => {});
  const isRunningRef = useRef<boolean>(false);

  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  const start = useCallback(() => {
    if (intervalRef.current || isRunningRef.current) {
      return;
    }

    isRunningRef.current = true;
    intervalRef.current = setInterval(() => {
      savedCallback.current?.();
    }, delay);
  }, [delay]);

  const stop = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    isRunningRef.current = false;
  }, []);

  const reset = useCallback(() => {
    stop();
    start();
  }, [start, stop]);

  useEffect(() => {
    return () => {
      stop();
    };
  }, [stop]);

  return {
    start,
    stop,
    reset,
    isRunning: isRunningRef.current,
  };
};

// 倒计时钩子
export const useCountdown = (
  initialTime: number,
  options: {
    onComplete?: () => void;
    onTick?: (timeLeft: number) => void;
    autoStart?: boolean;
    interval?: number;
  } = {}
): {
  timeLeft: number;
  start: () => void;
  pause: () => void;
  reset: (time?: number) => void;
  isRunning: boolean;
  isCompleted: boolean;
} => {
  const {
    onComplete,
    onTick,
    autoStart = false,
    interval = 1000,
  } = options;

  const timeLeftRef = useRef<number>(initialTime);
  const isRunningRef = useRef<boolean>(autoStart);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  const initialTimeRef = useRef<number>(initialTime);

  const start = useCallback(() => {
    if (isRunningRef.current || timeLeftRef.current <= 0) {
      return;
    }

    isRunningRef.current = true;

    intervalRef.current = setInterval(() => {
      timeLeftRef.current -= 1;

      onTick?.(timeLeftRef.current);

      if (timeLeftRef.current <= 0) {
        isRunningRef.current = false;
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
          intervalRef.current = null;
        }
        onComplete?.();
      }
    }, interval);
  }, [onComplete, onTick, interval]);

  const pause = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    isRunningRef.current = false;
  }, []);

  const reset = useCallback((time?: number) => {
    pause();
    const resetTime = time ?? initialTimeRef.current;
    timeLeftRef.current = resetTime;
    initialTimeRef.current = resetTime;
  }, [pause]);

  useEffect(() => {
    if (autoStart && timeLeftRef.current > 0) {
      start();
    }

    return () => {
      pause();
    };
  }, [autoStart, start, pause]);

  return {
    timeLeft: timeLeftRef.current,
    start,
    pause,
    reset,
    isRunning: isRunningRef.current,
    isCompleted: timeLeftRef.current <= 0,
  };
};

// 秒表钩子
export const useStopwatch = (
  options: {
    autoStart?: boolean;
    interval?: number;
    onTick?: (elapsedTime: number) => void;
  } = {}
): {
  elapsedTime: number;
  start: () => void;
  pause: () => void;
  reset: () => void;
  isRunning: boolean;
} => {
  const {
    autoStart = false,
    interval = 100,
    onTick,
  } = options;

  const elapsedTimeRef = useRef<number>(0);
  const startTimeRef = useRef<number>(0);
  const pausedTimeRef = useRef<number>(0);
  const isRunningRef = useRef<boolean>(false);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const start = useCallback(() => {
    if (isRunningRef.current) {
      return;
    }

    isRunningRef.current = true;
    startTimeRef.current = Date.now() - pausedTimeRef.current;

    intervalRef.current = setInterval(() => {
      elapsedTimeRef.current = Date.now() - startTimeRef.current;
      onTick?.(elapsedTimeRef.current);
    }, interval);
  }, [interval, onTick]);

  const pause = useCallback(() => {
    if (!isRunningRef.current) {
      return;
    }

    isRunningRef.current = false;
    pausedTimeRef.current = elapsedTimeRef.current;

    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  const reset = useCallback(() => {
    pause();
    elapsedTimeRef.current = 0;
    startTimeRef.current = 0;
    pausedTimeRef.current = 0;
  }, [pause]);

  useEffect(() => {
    if (autoStart) {
      start();
    }

    return () => {
      pause();
    };
  }, [autoStart, start, pause]);

  return {
    elapsedTime: elapsedTimeRef.current,
    start,
    pause,
    reset,
    isRunning: isRunningRef.current,
  };
};

// 时钟钩子
export const useClock = (
  options: {
    format?: '12' | '24';
    updateInterval?: number;
    onTick?: (time: Date) => void;
  } = {}
): {
  time: Date;
  formattedTime: string;
  start: () => void;
  stop: () => void;
  isRunning: boolean;
} => {
  const {
    format = '24',
    updateInterval = 1000,
    onTick,
  } = options;

  const timeRef = useRef<Date>(new Date());
  const isRunningRef = useRef<boolean>(true);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const formatTime = useCallback((date: Date): string => {
    const options: Intl.DateTimeFormatOptions = {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: format === '12',
    };

    return date.toLocaleTimeString('zh-CN', options);
  }, [format]);

  const updateTime = useCallback(() => {
    const now = new Date();
    timeRef.current = now;
    onTick?.(now);
  }, [onTick]);

  const start = useCallback(() => {
    if (isRunningRef.current) {
      return;
    }

    isRunningRef.current = true;
    intervalRef.current = setInterval(updateTime, updateInterval);
  }, [updateTime, updateInterval]);

  const stop = useCallback(() => {
    isRunningRef.current = false;
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
  }, []);

  useEffect(() => {
    start();
    return () => {
      stop();
    };
  }, [start, stop]);

  return {
    time: timeRef.current,
    formattedTime: formatTime(timeRef.current),
    start,
    stop,
    isRunning: isRunningRef.current,
  };
};

// 动画帧钩子（使用 requestAnimationFrame）
export const useAnimationFrame = (
  callback: (deltaTime: number) => void,
  running: boolean = true
): {
  start: () => void;
  stop: () => void;
  isRunning: boolean;
} => {
  const requestRef = useRef<number | null>(null);
  const previousTimeRef = useRef<number>(0);
  const isRunningRef = useRef<boolean>(running);
  const savedCallback = useRef<(deltaTime: number) => void>(() => {});

  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  const animate = useCallback((time: number) => {
    if (previousTimeRef.current) {
      const deltaTime = time - previousTimeRef.current;
      savedCallback.current?.(deltaTime);
    }
    previousTimeRef.current = time;

    if (isRunningRef.current) {
      requestRef.current = requestAnimationFrame(animate);
    }
  }, []);

  const start = useCallback(() => {
    if (isRunningRef.current) {
      return;
    }

    isRunningRef.current = true;
    previousTimeRef.current = 0;
    requestRef.current = requestAnimationFrame(animate);
  }, [animate]);

  const stop = useCallback(() => {
    isRunningRef.current = false;
    if (requestRef.current) {
      cancelAnimationFrame(requestRef.current);
      requestRef.current = null;
    }
    previousTimeRef.current = 0;
  }, []);

  useEffect(() => {
    if (running) {
      start();
    } else {
      stop();
    }

    return () => {
      stop();
    };
  }, [running, start, stop]);

  return {
    start,
    stop,
    isRunning: isRunningRef.current,
  };
};

// 节流钩子（基于时间间隔）
export const useThrottle = <T extends (...args: any[]) => any>(
  callback: T,
  delay: number
): T => {
  const lastCallTime = useRef<number>(0);
  const savedCallback = useRef<T | undefined>(undefined);

  useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  const throttledCallback = useCallback((...args: Parameters<T>) => {
    const now = Date.now();

    if (now - lastCallTime.current >= delay) {
      lastCallTime.current = now;
      savedCallback.current?.(...args);
    }
  }, [delay]) as T;

  return throttledCallback;
};

export default useInterval;