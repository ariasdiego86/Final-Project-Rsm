import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/pages/DashboardPage.vue'),
    },
    {
      path: '/orders/new',
      component: () => import('@/pages/OrderEditPage.vue'),
    },
    {
      path: '/orders/:id/edit',
      component: () => import('@/pages/OrderEditPage.vue'),
    },
  ],
})

export default router
