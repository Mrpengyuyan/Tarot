import { useState, useEffect, useCallback, useRef } from 'react';

// 基础防抖钩子
export const useDebounce = <T>(value: T, delay: number): T => {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
};

// 防抖回调钩子
export const useDebouncedCallback = <T extends (...args: any[]) => any>(
  callback: T,
  delay: number,
  dependencies?: React.DependencyList
): T => {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const callbackRef = useRef<T>(callback);

  // 更新回调引用
  useEffect(() => {
    callbackRef.current = callback;
  }, dependencies ? [callback, ...dependencies] : [callback]);

  const debouncedCallback = useCallback((...args: Parameters<T>) => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      callbackRef.current(...args);
    }, delay);
  }, [delay]) as T;

  // 清理函数
  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return debouncedCallback;
};

// 立即执行防抖钩子（第一次调用立即执行，后续调用防抖）
export const useImmediateDebounce = <T extends (...args: any[]) => any>(
  callback: T,
  delay: number,
  dependencies?: React.DependencyList
): T => {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const callbackRef = useRef<T>(callback);
  const isImmediateRef = useRef<boolean>(true);

  useEffect(() => {
    callbackRef.current = callback;
  }, dependencies ? [callback, ...dependencies] : [callback]);

  const debouncedCallback = useCallback((...args: Parameters<T>) => {
    if (isImmediateRef.current) {
      callbackRef.current(...args);
      isImmediateRef.current = false;
    }

    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }

    timeoutRef.current = setTimeout(() => {
      callbackRef.current(...args);
      isImmediateRef.current = true;
    }, delay);
  }, [delay]) as T;

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        clearTimeout(timeoutRef.current);
      }
    };
  }, []);

  return debouncedCallback;
};

// 搜索防抖钩子
export const useSearchDebounce = (
  initialValue: string = '',
  delay: number = 300
): {
  searchTerm: string;
  debouncedSearchTerm: string;
  setSearchTerm: (value: string) => void;
  isSearching: boolean;
  clearSearch: () => void;
} => {
  const [searchTerm, setSearchTerm] = useState<string>(initialValue);
  const [isSearching, setIsSearching] = useState<boolean>(false);
  const debouncedSearchTerm = useDebounce(searchTerm, delay);

  useEffect(() => {
    if (searchTerm !== debouncedSearchTerm) {
      setIsSearching(true);
    } else {
      setIsSearching(false);
    }
  }, [searchTerm, debouncedSearchTerm]);

  const clearSearch = useCallback(() => {
    setSearchTerm('');
  }, []);

  return {
    searchTerm,
    debouncedSearchTerm,
    setSearchTerm,
    isSearching,
    clearSearch,
  };
};

// 输入防抖钩子（用于表单输入）
export const useInputDebounce = <T = string>(
  initialValue: T,
  delay: number = 300,
  validator?: (value: T) => boolean
): {
  value: T;
  debouncedValue: T;
  setValue: (value: T) => void;
  isValid: boolean;
  isPending: boolean;
  reset: () => void;
} => {
  const [value, setValue] = useState<T>(initialValue);
  const debouncedValue = useDebounce(value, delay);
  const [isPending, setIsPending] = useState<boolean>(false);

  useEffect(() => {
    setIsPending(value !== debouncedValue);
  }, [value, debouncedValue]);

  const isValid = validator ? validator(debouncedValue) : true;

  const reset = useCallback(() => {
    setValue(initialValue);
  }, [initialValue]);

  return {
    value,
    debouncedValue,
    setValue,
    isValid,
    isPending,
    reset,
  };
};

// API调用防抖钩子
export const useApiDebounce = <T, R>(
  apiCall: (params: T) => Promise<R>,
  delay: number = 500
): {
  execute: (params: T) => void;
  data: R | null;
  loading: boolean;
  error: Error | null;
  cancel: () => void;
} => {
  const [data, setData] = useState<R | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);

  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const execute = useDebouncedCallback(async (params: T) => {
    try {
      // 取消之前的请求
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }

      // 创建新的AbortController
      abortControllerRef.current = new AbortController();

      setLoading(true);
      setError(null);

      const result = await apiCall(params);

      if (!abortControllerRef.current?.signal.aborted) {
        setData(result);
      }
    } catch (err) {
      if (!abortControllerRef.current?.signal.aborted) {
        setError(err as Error);
        setData(null);
      }
    } finally {
      if (!abortControllerRef.current?.signal.aborted) {
        setLoading(false);
      }
    }
  }, delay);

  const cancel = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
    }
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setLoading(false);
  }, []);

  useEffect(() => {
    return () => {
      cancel();
    };
  }, [cancel]);

  return {
    execute,
    data,
    loading,
    error,
    cancel,
  };
};

// 滚动防抖钩子
export const useScrollDebounce = (
  delay: number = 100
): {
  scrollY: number;
  debouncedScrollY: number;
  isScrolling: boolean;
} => {
  const [scrollY, setScrollY] = useState<number>(0);
  const [isScrolling, setIsScrolling] = useState<boolean>(false);
  const debouncedScrollY = useDebounce(scrollY, delay);

  useEffect(() => {
    const handleScroll = () => {
      setScrollY(window.scrollY);
      setIsScrolling(true);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });

    return () => {
      window.removeEventListener('scroll', handleScroll);
    };
  }, []);

  useEffect(() => {
    if (scrollY === debouncedScrollY) {
      setIsScrolling(false);
    }
  }, [scrollY, debouncedScrollY]);

  return {
    scrollY,
    debouncedScrollY,
    isScrolling,
  };
};

// 窗口大小防抖钩子
export const useWindowSizeDebounce = (
  delay: number = 200
): {
  windowSize: { width: number; height: number };
  debouncedWindowSize: { width: number; height: number };
  isResizing: boolean;
} => {
  const [windowSize, setWindowSize] = useState({
    width: typeof window !== 'undefined' ? window.innerWidth : 0,
    height: typeof window !== 'undefined' ? window.innerHeight : 0,
  });

  const [isResizing, setIsResizing] = useState<boolean>(false);
  const debouncedWindowSize = useDebounce(windowSize, delay);

  useEffect(() => {
    const handleResize = () => {
      setWindowSize({
        width: window.innerWidth,
        height: window.innerHeight,
      });
      setIsResizing(true);
    };

    window.addEventListener('resize', handleResize);

    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, []);

  useEffect(() => {
    if (windowSize.width === debouncedWindowSize.width &&
        windowSize.height === debouncedWindowSize.height) {
      setIsResizing(false);
    }
  }, [windowSize, debouncedWindowSize]);

  return {
    windowSize,
    debouncedWindowSize,
    isResizing,
  };
};

export default useDebounce;