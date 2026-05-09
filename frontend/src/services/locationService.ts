import api from './api'
import type { AddressValidationResult } from '@/types'

export const locationService = {
  validate(address: string) {
    return api.post<AddressValidationResult>('/api/location/validate', { address })
  },
}
