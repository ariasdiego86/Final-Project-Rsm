import api from './api'
import type { TimeSeriesPoint, RegionShipment } from '@/types'

export const reportsService = {
  getOrdersPerTime(granularity: 'month' | 'year' | 'week' = 'month') {
    return api.get<TimeSeriesPoint[]>('/api/reports/orders-per-time', {
      params: { granularity },
    })
  },

  getShipmentsByRegion() {
    return api.get<RegionShipment[]>('/api/reports/shipments-by-region')
  },
}
