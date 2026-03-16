// import { RouteObject } from 'react-router-dom';

// 路由路径常量
export const ROUTES = {
  // 公共路由
  HOME: '/',
  LOGIN: '/auth/login',
  REGISTER: '/auth/register',

  // 受保护的路由
  DASHBOARD: '/dashboard',
  NEW_READING: '/reading/new',
  DRAW_CARDS: '/reading/draw',
  READING_DETAIL: '/reading/:id',
  HISTORY: '/history',
  PROFILE: '/profile',

  // 演示页面
  DEMO: '/demo',

  // 错误页面
  NOT_FOUND: '/404',
} as const;

// 路由元数据类型
export interface RouteMetadata {
  title: string;
  description?: string;
  requiresAuth?: boolean;
  roles?: string[];
  icon?: string;
  showInNav?: boolean;
}

// 路由配置映射
export const ROUTE_METADATA: Record<string, RouteMetadata> = {
  [ROUTES.HOME]: {
    title: '塔罗之境',
    description: '探索神秘的塔罗世界',
    requiresAuth: false,
    showInNav: true,
    icon: 'home',
  },
  [ROUTES.LOGIN]: {
    title: '登录',
    description: '登录您的塔罗账户',
    requiresAuth: false,
    showInNav: false,
  },
  [ROUTES.REGISTER]: {
    title: '注册',
    description: '创建新的塔罗账户',
    requiresAuth: false,
    showInNav: false,
  },
  [ROUTES.DASHBOARD]: {
    title: '主控台',
    description: '您的塔罗占卜中心',
    requiresAuth: true,
    showInNav: true,
    icon: 'dashboard',
  },
  [ROUTES.NEW_READING]: {
    title: '新占卜',
    description: '开始一次新的塔罗占卜',
    requiresAuth: true,
    showInNav: true,
    icon: 'auto_awesome',
  },
  [ROUTES.DRAW_CARDS]: {
    title: '抽牌占卜',
    description: '选择牌阵进行塔罗占卜',
    requiresAuth: true,
    showInNav: true,
    icon: 'style',
  },
  [ROUTES.HISTORY]: {
    title: '占卜历史',
    description: '查看您的占卜记录',
    requiresAuth: true,
    showInNav: true,
    icon: 'history',
  },
  [ROUTES.PROFILE]: {
    title: '个人中心',
    description: '管理您的账户信息',
    requiresAuth: true,
    showInNav: true,
    icon: 'person',
  },
  [ROUTES.DEMO]: {
    title: '功能演示',
    description: '展示塔罗牌核心功能',
    requiresAuth: false,
    showInNav: true,
    icon: 'preview',
  },
};

// 导航菜单配置
export const NAV_ITEMS = Object.entries(ROUTE_METADATA)
  .filter(([_, metadata]) => metadata.showInNav)
  .map(([path, metadata]) => ({
    path,
    ...metadata,
  }));

// 受保护路由列表
export const PROTECTED_ROUTES = Object.entries(ROUTE_METADATA)
  .filter(([_, metadata]) => metadata.requiresAuth)
  .map(([path]) => path);

// 公共路由列表
export const PUBLIC_ROUTES = Object.entries(ROUTE_METADATA)
  .filter(([_, metadata]) => !metadata.requiresAuth)
  .map(([path]) => path);

// 面包屑导航类型
export interface BreadcrumbItem {
  label: string;
  path?: string;
  icon?: string;
}

// 获取路由面包屑
export const getBreadcrumbs = (pathname: string): BreadcrumbItem[] => {
  const segments = pathname.split('/').filter(Boolean);
  const breadcrumbs: BreadcrumbItem[] = [
    { label: '首页', path: ROUTES.HOME, icon: 'home' }
  ];

  if (segments.length === 0) return breadcrumbs;

  // 动态构建面包屑
  let currentPath = '';
  segments.forEach((segment, index) => {
    currentPath += `/${segment}`;
    const metadata = ROUTE_METADATA[currentPath];

    if (metadata) {
      breadcrumbs.push({
        label: metadata.title,
        path: index === segments.length - 1 ? undefined : currentPath,
        icon: metadata.icon,
      });
    } else {
      // 处理动态路由参数
      breadcrumbs.push({
        label: segment.charAt(0).toUpperCase() + segment.slice(1),
        path: index === segments.length - 1 ? undefined : currentPath,
      });
    }
  });

  return breadcrumbs;
};

// 检查路由是否需要认证
export const requiresAuth = (pathname: string): boolean => {
  return PROTECTED_ROUTES.some(route => {
    if (route.includes(':')) {
      // 处理动态路由
      const routePattern = route.replace(/:[^/]+/g, '[^/]+');
      const regex = new RegExp(`^${routePattern}$`);
      return regex.test(pathname);
    }
    return pathname === route;
  });
};

// 获取路由标题
export const getRouteTitle = (pathname: string): string => {
  const metadata = ROUTE_METADATA[pathname];
  if (metadata) return metadata.title;

  // 处理动态路由
  for (const [route, meta] of Object.entries(ROUTE_METADATA)) {
    if (route.includes(':')) {
      const routePattern = route.replace(/:[^/]+/g, '[^/]+');
      const regex = new RegExp(`^${routePattern}$`);
      if (regex.test(pathname)) {
        return meta.title;
      }
    }
  }

  return '塔罗之境';
};