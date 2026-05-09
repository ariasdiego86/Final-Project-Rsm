import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { ref } from 'vue'
import AddressMap from '@/components/AddressMap.vue'

// ── useGoogleMaps mock ───────────────────────────────────────────────────────

const mockLoad = vi.fn().mockResolvedValue(undefined)
const mapsError = ref<string | null>(null)

vi.mock('@/composables/useGoogleMaps', () => ({
  useGoogleMaps: () => ({
    load: mockLoad,
    error: mapsError,
    isLoaded: ref(false),
    getMapsLibrary: vi.fn(),
  }),
}))

// ── @googlemaps/js-api-loader mock ───────────────────────────────────────────

const mockSetCenter = vi.fn()
// Regular functions required so vi.fn() can be used as a constructor (new Map / new AdvancedMarkerElement)
const mockAdvancedMarker = vi.fn().mockImplementation(function (this: Record<string, unknown>) {
  this.position = null
})
const MockMap = vi.fn().mockImplementation(function (this: Record<string, unknown>) {
  this.setCenter = mockSetCenter
})

vi.mock('@googlemaps/js-api-loader', () => ({
  setOptions: vi.fn(),
  importLibrary: vi.fn().mockImplementation(async (lib: string) => {
    if (lib === 'marker') return { AdvancedMarkerElement: mockAdvancedMarker }
    return { Map: MockMap }
  }),
}))

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AddressMap', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mapsError.value = null
    mockLoad.mockResolvedValue(undefined)
  })

  it('shows placeholder text when latitude and longitude are null', () => {
    const wrapper = mount(AddressMap, {
      props: { latitude: null, longitude: null },
    })

    expect(wrapper.text()).toContain('Validate the address')
    expect(mockLoad).not.toHaveBeenCalled()
  })

  it('calls useGoogleMaps.load when mounted with valid coordinates', async () => {
    mount(AddressMap, {
      props: { latitude: 52.52, longitude: 13.4 },
    })

    await flushPromises()

    expect(mockLoad).toHaveBeenCalledOnce()
  })

  it('renders map container div instead of placeholder when coords are provided', async () => {
    const wrapper = mount(AddressMap, {
      props: { latitude: 40.71, longitude: -74.01 },
    })

    await flushPromises()

    // Placeholder text must NOT appear when coords exist
    expect(wrapper.text()).not.toContain('Validate the address')
    // The map container div (v-else, distinct from the placeholder) must be rendered
    const mapDiv = wrapper.find('div[style*="width: 100%"]')
    expect(mapDiv.exists()).toBe(true)
  })

  it('calls load via watcher when coordinates change from null to valid values', async () => {
    const wrapper = mount(AddressMap, {
      props: { latitude: null, longitude: null },
    })

    expect(mockLoad).not.toHaveBeenCalled()

    await wrapper.setProps({ latitude: 52.52, longitude: 13.4 })
    await flushPromises()

    expect(mockLoad).toHaveBeenCalledOnce()
  })

  it('calls setCenter when coordinates change after map is already initialised', async () => {
    const wrapper = mount(AddressMap, {
      props: { latitude: 40.71, longitude: -74.01 },
    })

    await flushPromises() // let initMap() run so mapInstance is set

    await wrapper.setProps({ latitude: 52.52, longitude: 13.4 })
    await flushPromises()

    expect(mockSetCenter).toHaveBeenCalledWith({ lat: 52.52, lng: 13.4 })
  })
})
