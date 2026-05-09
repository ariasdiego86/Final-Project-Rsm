import { ref } from 'vue'
import { locationService } from '@/services/locationService'
import type { AddressValidationResult } from '@/types'

export function useAddressValidation() {
  const validating = ref(false)
  const result = ref<AddressValidationResult | null>(null)
  const validationError = ref<string | null>(null)

  async function validate(address: string): Promise<AddressValidationResult | null> {
    if (!address.trim()) return null

    validating.value = true
    validationError.value = null
    result.value = null

    try {
      const res = await locationService.validate(address)
      result.value = res.data
      if (!res.data.isValid) {
        validationError.value = 'Address could not be fully validated.'
      }
      return res.data
    } catch {
      validationError.value = 'Address validation failed. Please check the address.'
      return null
    } finally {
      validating.value = false
    }
  }

  function reset() {
    result.value = null
    validationError.value = null
  }

  return { validating, result, validationError, validate, reset }
}
