import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref, nextTick } from 'vue'
import { QInput, QBanner } from 'quasar'
import OrderForm from '@/components/OrderForm.vue'

// ── Composable mock (shared refs so tests can control reactive state) ────────

const mockValidateAddress = vi.fn()
const mockReset = vi.fn()
const geoValidationError = ref<string | null>(null)
const geoResult = ref<any>(null)

vi.mock('@/composables/useAddressValidation', () => ({
  useAddressValidation: () => ({
    validating: ref(false),
    result: geoResult,
    validationError: geoValidationError,
    validate: mockValidateAddress,
    reset: mockReset,
  }),
}))

// ── Service mocks ────────────────────────────────────────────────────────────
// vi.mock factories are hoisted before variable declarations, so we use vi.hoisted
// to make mockCreate/mockUpdate available inside the factory.

const { mockCreate, mockUpdate } = vi.hoisted(() => ({
  mockCreate: vi.fn(),
  mockUpdate: vi.fn(),
}))

vi.mock('@/services/ordersService', () => ({
  ordersService: { create: mockCreate, update: mockUpdate },
}))

// ── Store mock ───────────────────────────────────────────────────────────────

vi.mock('@/stores/useLookupsStore', () => ({
  useLookupsStore: () => ({
    customers: [{ id: 'ALFKI', name: 'Alfreds Futterkiste' }],
    employees: [{ id: 1, name: 'Nancy Davolio' }],
    shippers: [],
    products: [{ id: 1, name: 'Chai' }],
    fetchAll: vi.fn().mockResolvedValue(undefined),
  }),
}))

// ── Router mock ──────────────────────────────────────────────────────────────

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: vi.fn() }),
}))

// ── AddressMap stub (avoids Google Maps init) ────────────────────────────────

vi.mock('@/components/AddressMap.vue', () => ({
  default: { name: 'AddressMap', template: '<div class="map-stub" />' },
}))

// ── Tests ────────────────────────────────────────────────────────────────────

describe('OrderForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    geoValidationError.value = null
    geoResult.value = null
    mockValidateAddress.mockResolvedValue(null)
  })

  it('does not call ordersService when customerID and employeeID are empty', async () => {
    const wrapper = mount(OrderForm)
    await flushPromises()

    // Submit form with empty required fields
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mockCreate).not.toHaveBeenCalled()
    expect(mockUpdate).not.toHaveBeenCalled()
  })

  it('calls validate with combined address string when address field loses focus', async () => {
    mockValidateAddress.mockResolvedValue(null)

    const wrapper = mount(OrderForm)
    await flushPromises()

    // Find the QInput for "Address" and update its v-model by emitting at Vue level
    const addressQInput = wrapper
      .findAllComponents(QInput)
      .find(c => c.props('label') === 'Address')!

    // Emit update:modelValue to update form.shipAddress via the v-model binding
    await addressQInput.vm.$emit('update:modelValue', 'Obere Str. 57')
    await nextTick()

    // Emit blur at component level so OrderForm's @blur="onAddressBlur" fires
    await addressQInput.vm.$emit('blur')
    await flushPromises()

    expect(mockValidateAddress).toHaveBeenCalledWith(
      expect.stringContaining('Obere Str. 57'),
    )
  })

  it('shows warning banner when geo validation returns an error', async () => {
    geoValidationError.value = 'Address could not be fully validated.'

    const wrapper = mount(OrderForm)
    await flushPromises()

    const banner = wrapper.findComponent(QBanner)
    expect(banner.exists()).toBe(true)
    expect(banner.text()).toContain('Address could not be fully validated.')
  })
})
