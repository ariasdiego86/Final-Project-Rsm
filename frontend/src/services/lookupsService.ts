import api from './api'
import type { LookupItem } from '@/types'

export const lookupsService = {
  getCustomers: () => api.get<LookupItem[]>('/api/lookups/customers'),
  getEmployees: () => api.get<LookupItem[]>('/api/lookups/employees'),
  getShippers: () => api.get<LookupItem[]>('/api/lookups/shippers'),
  getProducts: () => api.get<LookupItem[]>('/api/lookups/products'),
  getRegions: () => api.get<string[]>('/api/lookups/regions'),
}
