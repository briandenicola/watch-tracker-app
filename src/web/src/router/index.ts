import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/LoginPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/pages/RegisterPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/setup',
      name: 'setup',
      component: () => import('@/pages/SetupPage.vue'),
      meta: { guest: true },
    },
    {
      // Public by design: no auth meta, so the guard lets anyone through.
      path: '/w/:token',
      name: 'shared-wishlist',
      component: () => import('@/pages/SharedWishlistPage.vue'),
    },
    {
      // Public by design: no auth meta, so the guard lets anyone through.
      path: '/s/:token',
      name: 'shared-watch',
      component: () => import('@/pages/SharedWatchPage.vue'),
    },
    {
      path: '/oidc/callback',
      name: 'oidc-callback',
      component: () => import('@/pages/OidcCallbackPage.vue'),
    },
    {
      path: '/',
      component: () => import('@/components/common/AppLayout.vue'),
      meta: { auth: true },
      children: [
        { path: '', name: 'collection', component: () => import('@/pages/CollectionPage.vue') },
        { path: 'storage', name: 'storage', component: () => import('@/pages/StoragePage.vue') },
        { path: 'recommend', name: 'recommend', component: () => import('@/pages/RecommendationPage.vue') },
        { path: 'advisor', name: 'advisor', component: () => import('@/pages/AdvisorPage.vue') },
        { path: 'review', name: 'review', component: () => import('@/pages/ReviewPage.vue') },
        { path: 'watches/:id', name: 'watch-detail', component: () => import('@/pages/WatchDetailPage.vue') },
        { path: 'watches/new', name: 'add-watch', component: () => import('@/pages/AddWatchPage.vue') },
        { path: 'watches/:id/edit', name: 'edit-watch', component: () => import('@/pages/EditWatchPage.vue') },
        { path: 'wishlist/new', name: 'add-wishlist', component: () => import('@/pages/AddWishListPage.vue') },
        { path: 'wishlist/order', name: 'order-wishlist', component: () => import('@/pages/WishlistOrderPage.vue') },
        { path: 'wishlist/:id/edit', name: 'edit-wishlist', component: () => import('@/pages/EditWishListPage.vue') },
        { path: 'stats', name: 'stats', component: () => import('@/pages/StatsPage.vue') },
        { path: 'wear-log', name: 'wear-log', component: () => import('@/pages/WearLogPage.vue') },
        { path: 'retired', name: 'retired', component: () => import('@/pages/RetiredWatchesPage.vue') },
        { path: 'settings', name: 'settings', component: () => import('@/pages/SettingsPage.vue') },
        { path: 'admin', name: 'admin', component: () => import('@/pages/AdminPage.vue'), meta: { admin: true } },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (to.meta.auth && !auth.isAuthenticated) {
    return { name: 'login' }
  }

  if (to.meta.guest && auth.isAuthenticated) {
    return { name: 'collection' }
  }

  if (to.meta.admin && auth.user?.role !== 'Admin') {
    return { name: 'collection' }
  }
})

export default router
