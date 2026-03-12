import { useState, useCallback, useEffect, useRef } from 'react';
import axios, { AxiosRequestConfig, AxiosResponse, CancelTokenSource } from 'axios';
import { useNotification } from '../components/UI/Notification';

export interface ApiState<T = any> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

export interface UseApiOptions {
  immediate?: boolean;
  showErrorNotification?: boolean;
  showSuccessNotification?: boolean;
  successMessage?: string;
  onSuccess?: (data: any) => void;
  onError?: (error: any) => void;
}

export interface UseApiReturn<T = any> {
  data: T | null;
  loading: boolean;
  error: string | null;
  execute: (config?: AxiosRequestConfig) => Promise<T | null>;
  reset: () => void;
  cancel: () => void;
}

// 基础API钩子
export const useApi = <T = any>(
  initialConfig?: AxiosRequestConfig,
  options: UseApiOptions = {}
): UseApiReturn<T> => {
  const {
    immediate = false,
    showErrorNotification = true,
    showSuccessNotification = false,
    successMessage,
    onSuccess,
    onError,
  } = options;

  const [state, setState] = useState<ApiState<T>>({
    data: null,
    loading: false,
    error: null,
  });

  const { showError, showSuccess } = useNotification();
  const cancelTokenRef = useRef<CancelTokenSource | null>(null);

  // 执行API请求
  const execute = useCallback(async (config?: AxiosRequestConfig): Promise<T | null> => {
    try {
      // 取消之前的请求
      if (cancelTokenRef.current) {
        cancelTokenRef.current.cancel('新请求已发起');
      }

      // 创建新的取消令牌
      cancelTokenRef.current = axios.CancelToken.source();

      const finalConfig: AxiosRequestConfig = {
        ...initialConfig,
        ...config,
        cancelToken: cancelTokenRef.current.token,
      };

      setState(prev => ({ ...prev, loading: true, error: null }));

      const response: AxiosResponse<T> = await axios(finalConfig);

      setState({
        data: response.data,
        loading: false,
        error: null,
      });

      // 成功回调
      if (onSuccess) {
        onSuccess(response.data);
      }

      // 显示成功通知
      if (showSuccessNotification && successMessage) {
        showSuccess(successMessage);
      }

      return response.data;
    } catch (error: any) {
      // 忽略取消的请求
      if (axios.isCancel(error)) {
        return null;
      }

      const errorMessage = error.response?.data?.message || error.message || '请求失败';

      setState({
        data: null,
        loading: false,
        error: errorMessage,
      });

      // 错误回调
      if (onError) {
        onError(error);
      }

      // 显示错误通知
      if (showErrorNotification) {
        showError(errorMessage);
      }

      return null;
    }
  }, [initialConfig, onSuccess, onError, showSuccessNotification, successMessage, showError, showSuccess, showErrorNotification]);

  // 重置状态
  const reset = useCallback(() => {
    setState({
      data: null,
      loading: false,
      error: null,
    });
  }, []);

  // 取消请求
  const cancel = useCallback(() => {
    if (cancelTokenRef.current) {
      cancelTokenRef.current.cancel('请求被用户取消');
    }
  }, []);

  // 自动执行
  useEffect(() => {
    if (immediate) {
      execute();
    }

    return () => {
      // 组件卸载时取消请求
      cancel();
    };
  }, [immediate]);

  return {
    data: state.data,
    loading: state.loading,
    error: state.error,
    execute,
    reset,
    cancel,
  };
};

// GET请求钩子
export const useGet = <T = any>(
  url: string,
  options: UseApiOptions & { params?: any } = {}
): UseApiReturn<T> => {
  const { params, ...restOptions } = options;

  return useApi<T>(
    {
      method: 'GET',
      url,
      params,
    },
    restOptions
  );
};

// POST请求钩子
export const usePost = <T = any>(
  url: string,
  options: UseApiOptions = {}
): UseApiReturn<T> & {
  post: (data?: any) => Promise<T | null>;
} => {
  const apiHook = useApi<T>(
    {
      method: 'POST',
      url,
    },
    options
  );

  const post = useCallback((data?: any) => {
    return apiHook.execute({ data });
  }, [apiHook.execute]);

  return {
    ...apiHook,
    post,
  };
};

// PUT请求钩子
export const usePut = <T = any>(
  url: string,
  options: UseApiOptions = {}
): UseApiReturn<T> & {
  put: (data?: any) => Promise<T | null>;
} => {
  const apiHook = useApi<T>(
    {
      method: 'PUT',
      url,
    },
    options
  );

  const put = useCallback((data?: any) => {
    return apiHook.execute({ data });
  }, [apiHook.execute]);

  return {
    ...apiHook,
    put,
  };
};

// DELETE请求钩子
export const useDelete = <T = any>(
  url: string,
  options: UseApiOptions = {}
): UseApiReturn<T> & {
  remove: () => Promise<T | null>;
} => {
  const apiHook = useApi<T>(
    {
      method: 'DELETE',
      url,
    },
    options
  );

  const remove = useCallback(() => {
    return apiHook.execute();
  }, [apiHook.execute]);

  return {
    ...apiHook,
    remove,
  };
};

// 分页数据钩子
export interface PaginationState {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface UsePaginatedApiReturn<T = any> extends UseApiReturn<T[]> {
  pagination: PaginationState;
  goToPage: (page: number) => Promise<void>;
  nextPage: () => Promise<void>;
  prevPage: () => Promise<void>;
  changePageSize: (limit: number) => Promise<void>;
  refresh: () => Promise<void>;
}

export const usePaginatedApi = <T = any>(
  url: string,
  initialPage = 1,
  initialLimit = 10,
  options: UseApiOptions = {}
): UsePaginatedApiReturn<T> => {
  const [pagination, setPagination] = useState<PaginationState>({
    page: initialPage,
    limit: initialLimit,
    total: 0,
    totalPages: 0,
  });

  const apiHook = useApi<{
    data: T[];
    pagination: PaginationState;
  }>(
    {
      method: 'GET',
      url,
      params: {
        page: pagination.page,
        limit: pagination.limit,
      },
    },
    {
      ...options,
      onSuccess: (response) => {
        if (response.pagination) {
          setPagination(response.pagination);
        }
        options.onSuccess?.(response);
      },
    }
  );

  const goToPage = useCallback(async (page: number) => {
    setPagination(prev => ({ ...prev, page }));
    await apiHook.execute({
      params: { page, limit: pagination.limit },
    });
  }, [apiHook.execute, pagination.limit]);

  const nextPage = useCallback(async () => {
    if (pagination.page < pagination.totalPages) {
      await goToPage(pagination.page + 1);
    }
  }, [pagination.page, pagination.totalPages, goToPage]);

  const prevPage = useCallback(async () => {
    if (pagination.page > 1) {
      await goToPage(pagination.page - 1);
    }
  }, [pagination.page, goToPage]);

  const changePageSize = useCallback(async (limit: number) => {
    setPagination(prev => ({ ...prev, limit, page: 1 }));
    await apiHook.execute({
      params: { page: 1, limit },
    });
  }, [apiHook.execute]);

  const refresh = useCallback(async () => {
    await apiHook.execute({
      params: { page: pagination.page, limit: pagination.limit },
    });
  }, [apiHook.execute, pagination.page, pagination.limit]);

  const executeWithPagination = async (config?: AxiosRequestConfig): Promise<T[] | null> => {
    const result = await apiHook.execute(config);
    return result?.data || null;
  };

  return {
    data: apiHook.data?.data || null,
    loading: apiHook.loading,
    error: apiHook.error,
    execute: executeWithPagination,
    reset: apiHook.reset,
    cancel: apiHook.cancel,
    pagination,
    goToPage,
    nextPage,
    prevPage,
    changePageSize,
    refresh,
  };
};

// 文件上传钩子
export const useUpload = (
  url: string,
  options: UseApiOptions = {}
): UseApiReturn<any> & {
  upload: (file: File, onProgress?: (progress: number) => void) => Promise<any>;
} => {
  const apiHook = useApi(
    {
      method: 'POST',
      url,
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    },
    options
  );

  const upload = useCallback((file: File, onProgress?: (progress: number) => void) => {
    const formData = new FormData();
    formData.append('file', file);

    return apiHook.execute({
      data: formData,
      onUploadProgress: (progressEvent) => {
        if (onProgress && progressEvent.total) {
          const progress = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(progress);
        }
      },
    });
  }, [apiHook.execute]);

  return {
    ...apiHook,
    upload,
  };
};

export default useApi;