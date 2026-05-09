import api from './api'
import type {
  Order,
  OrderListItem,
  CreateOrderDto,
  UpdateOrderDto,
  OrderFilter,
  PagedResult,
} from '@/types'

function toQueryParams(filter: Partial<OrderFilter>): Record<string, string> {
  const params: Record<string, string> = {}
  for (const [key, value] of Object.entries(filter)) {
    if (value !== null && value !== undefined && value !== '') {
      params[key] = String(value)
    }
  }
  return params
}

export const ordersService = {
  getAll(filter: OrderFilter) {
    return api.get<PagedResult<OrderListItem>>('/api/orders', {
      params: toQueryParams(filter),
    })
  },

  getById(id: number) {
    return api.get<Order>(`/api/orders/${id}`)
  },

  create(dto: CreateOrderDto) {
    return api.post<Order>('/api/orders', dto)
  },

  update(id: number, dto: UpdateOrderDto) {
    return api.put<Order>(`/api/orders/${id}`, dto)
  },

  remove(id: number) {
    return api.delete(`/api/orders/${id}`)
  },

  getPdfUrl(id: number): string {
    return `${api.defaults.baseURL}/api/orders/${id}/pdf`
  },

  getExcel(filter: OrderFilter) {
    return api.get<Blob>('/api/orders/excel', {
      params: toQueryParams(filter),
      responseType: 'blob',
    })
  },
}
