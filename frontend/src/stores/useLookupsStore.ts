import { defineStore } from 'pinia'
import { ref } from 'vue'
import { lookupsService } from '@/services/lookupsService'
import type { LookupItem } from '@/types'

const TTL_MS = 5 * 60 * 1000

export const useLookupsStore = defineStore('lookups', () => {
  const customers = ref<LookupItem[]>([])
  const employees = ref<LookupItem[]>([])
  const shippers = ref<LookupItem[]>([])
  const products = ref<LookupItem[]>([])
  const regions = ref<string[]>([])
  const lastFetched = ref<number | null>(null)

  function isFresh(): boolean {
    return lastFetched.value !== null && Date.now() - lastFetched.value < TTL_MS
  }

  async function fetchAll(force = false) {
    if (!force && isFresh()) return

    const [c, e, s, p, r] = await Promise.all([
      lookupsService.getCustomers(),
      lookupsService.getEmployees(),
      lookupsService.getShippers(),
      lookupsService.getProducts(),
      lookupsService.getRegions(),
    ])

    customers.value = c.data
    employees.value = e.data
    shippers.value = s.data
    products.value = p.data
    regions.value = r.data
    lastFetched.value = Date.now()
  }

  return { customers, employees, shippers, products, regions, fetchAll }
})
