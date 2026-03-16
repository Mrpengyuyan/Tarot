import { useState, useEffect, useRef, useCallback } from 'react';
import { ImageQuality, ImageLoadingOptions } from '../types/cardImages';
import { imageService } from '../services/imageService';

// 图片加载状态
interface ImageLoadState {
  loading: boolean;
  loaded: boolean;
  error: Error | null;
  progress?: number;
  retryCount: number;
}

// 图片加载钩子返回值
interface UseImageLoaderReturn {
  imageUrl: string | null;
  imageElement: HTMLImageElement | null;
  loadState: ImageLoadState;
  retry: () => void;
  changeQuality: (quality: ImageQuality) => void;
}

/**
 * 塔罗牌图片加载钩子
 * 处理图片的懒加载、错误重试、质量切换等功能
 */
export const useImageLoader = (
  cardId: number,
  initialQuality: ImageQuality = 'standard',
  options: ImageLoadingOptions = {}
): UseImageLoaderReturn => {
  const [quality, setQuality] = useState<ImageQuality>(initialQuality);
  const [imageUrl, setImageUrl] = useState<string | null>(null);
  const [imageElement, setImageElement] = useState<HTMLImageElement | null>(null);
  const [loadState, setLoadState] = useState<ImageLoadState>({
    loading: false,
    loaded: false,
    error: null,
    retryCount: 0
  });

  const abortControllerRef = useRef<AbortController | null>(null);
  const retryTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // 加载图片
  const loadImage = useCallback(async () => {
    // 取消之前的加载
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // 清理重试定时器
    if (retryTimeoutRef.current) {
      clearTimeout(retryTimeoutRef.current);
      retryTimeoutRef.current = null;
    }

    abortControllerRef.current = new AbortController();

    setLoadState(prev => ({
      ...prev,
      loading: true,
      error: null
    }));

    try {
      const url = imageService.getImageUrl(cardId, quality);
      setImageUrl(url);

      const img = await imageService.loadImage(cardId, quality, {
        ...options,
        lazy: false // 钩子内部处理懒加载
      });

      // 检查是否被取消
      if (abortControllerRef.current?.signal.aborted) {
        return;
      }

      setImageElement(img);
      setLoadState(prev => ({
        ...prev,
        loading: false,
        loaded: true,
        error: null
      }));

    } catch (error) {
      if (abortControllerRef.current?.signal.aborted) {
        return;
      }

      const err = error as Error;

      setLoadState(prev => {
        const newRetryCount = prev.retryCount + 1;
        const maxRetries = options.retryAttempts || 3;

        // 自动重试
        if (newRetryCount <= maxRetries) {
          retryTimeoutRef.current = setTimeout(() => {
            loadImage();
          }, Math.pow(2, newRetryCount) * 1000); // 指数退避
        }

        return {
          ...prev,
          loading: false,
          loaded: false,
          error: err,
          retryCount: newRetryCount
        };
      });
    }
  }, [cardId, quality, options]);

  // 手动重试
  const retry = useCallback(() => {
    setLoadState(prev => ({
      ...prev,
      retryCount: 0
    }));
    loadImage();
  }, [loadImage]);

  // 切换质量
  const changeQuality = useCallback((newQuality: ImageQuality) => {
    if (newQuality !== quality) {
      setQuality(newQuality);
    }
  }, [quality]);

  // 初始加载
  useEffect(() => {
    if (!options.lazy || options.preload) {
      loadImage();
    }
  }, [loadImage, options.lazy, options.preload]);

  // 质量变化时重新加载
  useEffect(() => {
    if (quality !== initialQuality) {
      loadImage();
    }
  }, [quality, loadImage, initialQuality]);

  // 清理资源
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      if (retryTimeoutRef.current) {
        clearTimeout(retryTimeoutRef.current);
      }
    };
  }, []);

  return {
    imageUrl,
    imageElement,
    loadState,
    retry,
    changeQuality
  };
};

/**
 * 批量图片预加载钩子
 */
export const useBatchImageLoader = (
  cardIds: number[],
  quality: ImageQuality = 'thumbnail'
) => {
  const [loadProgress, setLoadProgress] = useState<{
    total: number;
    loaded: number;
    failed: number;
    progress: number;
  }>({
    total: cardIds.length,
    loaded: 0,
    failed: 0,
    progress: 0
  });

  const [isLoading, setIsLoading] = useState(false);

  const preloadImages = useCallback(async () => {
    if (cardIds.length === 0) return;

    setIsLoading(true);
    setLoadProgress({
      total: cardIds.length,
      loaded: 0,
      failed: 0,
      progress: 0
    });

    let loaded = 0;
    let failed = 0;

    const updateProgress = () => {
      const progress = ((loaded + failed) / cardIds.length) * 100;
      setLoadProgress({
        total: cardIds.length,
        loaded,
        failed,
        progress
      });
    };

    // 批量预加载
    const promises = cardIds.map(async (cardId) => {
      try {
        await imageService.preloadImage(cardId, quality);
        loaded++;
      } catch (error) {
        failed++;
        console.warn(`Failed to preload card ${cardId}:`, error);
      } finally {
        updateProgress();
      }
    });

    await Promise.all(promises);
    setIsLoading(false);
  }, [cardIds, quality]);

  return {
    loadProgress,
    isLoading,
    preloadImages
  };
};

/**
 * 图片懒加载钩子
 * 使用 Intersection Observer 实现真正的懒加载
 */
export const useLazyImageLoader = (
  cardId: number,
  quality: ImageQuality = 'standard',
  options: ImageLoadingOptions & {
    rootMargin?: string;
    threshold?: number;
  } = {}
) => {
  const imgRef = useRef<HTMLImageElement>(null);
  const [isVisible, setIsVisible] = useState(false);

  const imageLoader = useImageLoader(cardId, quality, {
    ...options,
    lazy: true
  });

  // Intersection Observer
  useEffect(() => {
    const img = imgRef.current;
    if (!img) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      {
        rootMargin: options.rootMargin || '50px',
        threshold: options.threshold || 0.1
      }
    );

    observer.observe(img);

    return () => {
      observer.disconnect();
    };
  }, [options.rootMargin, options.threshold]);

  // 当可见时开始加载图片
  useEffect(() => {
    if (isVisible && !imageLoader.loadState.loaded && !imageLoader.loadState.loading) {
      // 触发图片加载
      const url = imageService.getImageUrl(cardId, quality);
      if (imgRef.current) {
        imgRef.current.src = url;
      }
    }
  }, [isVisible, imageLoader.loadState.loaded, imageLoader.loadState.loading, cardId, quality]);

  return {
    ...imageLoader,
    imgRef,
    isVisible
  };
};

/**
 * 渐进式图片加载钩子
 * 先加载低质量图片，再加载高质量图片
 */
export const useProgressiveImageLoader = (
  cardId: number,
  targetQuality: ImageQuality = 'high'
) => {
  const [currentQuality, setCurrentQuality] = useState<ImageQuality>('thumbnail');
  const [finalImageLoaded, setFinalImageLoaded] = useState(false);

  const thumbnailLoader = useImageLoader(cardId, 'thumbnail', { preload: true });
  const finalLoader = useImageLoader(cardId, targetQuality, { lazy: true });

  // 当缩略图加载完成后，开始加载最终图片
  useEffect(() => {
    if (thumbnailLoader.loadState.loaded && !finalImageLoaded) {
      setCurrentQuality(targetQuality);
    }
  }, [thumbnailLoader.loadState.loaded, targetQuality, finalImageLoaded]);

  // 当最终图片加载完成
  useEffect(() => {
    if (finalLoader.loadState.loaded) {
      setFinalImageLoaded(true);
    }
  }, [finalLoader.loadState.loaded]);

  return {
    currentImage: finalImageLoaded ? finalLoader.imageElement : thumbnailLoader.imageElement,
    currentUrl: finalImageLoaded ? finalLoader.imageUrl : thumbnailLoader.imageUrl,
    isLoadingFinal: finalLoader.loadState.loading,
    loadState: finalImageLoaded ? finalLoader.loadState : thumbnailLoader.loadState,
    progress: finalImageLoaded ? 100 : (thumbnailLoader.loadState.loaded ? 50 : 0)
  };
};

export default useImageLoader;