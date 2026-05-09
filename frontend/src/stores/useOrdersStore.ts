import { defineStore } from 'pinia'
import { ref, reactive } from 'vue'
import { ordersService } from '@/services/ordersService'
import type { OrderListItem, OrderFilter, PagedResult } from '@/types'
import { buildDefaultFilter } from '@/utils'

export const useOrdersStore = defineStore('orders', () => {
  const loading = ref(false)
  const result = ref<PagedResult<OrderListItem>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 15,
    totalPages: 0,
  })
  const filter = reactive<OrderFilter>(buildDefaultFilter())

  async function fetchOrders() {
    loading.value = true
    try {
      const res = await ordersService.getAll({ ...filter })
      result.value = res.data
    } finally {
      loading.value = false
    }
  }

  async function deleteOrder(id: number) {
    await ordersService.remove(id)
    await fetchOrders()
  }

  function resetFilter() {
    Object.assign(filter, buildDefaultFilter())
  }

  return { loading, result, filter, fetchOrders, deleteOrder, resetFilter }
})
