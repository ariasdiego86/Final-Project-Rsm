import { ref } from 'vue'
import { setOptions, importLibrary } from '@googlemaps/js-api-loader'

let initialized = false
let loadPromise: Promise<void> | null = null

export function useGoogleMaps() {
  const isLoaded = ref(false)
  const error = ref<string | null>(null)

  async function load(): Promise<void> {
    if (isLoaded.value) return

    if (loadPromise) {
      await loadPromise
      isLoaded.value = true
      return
    }

    const apiKey = import.meta.env.VITE_GOOGLE_MAPS_API_KEY as string
    if (!apiKey) {
      error.value = 'VITE_GOOGLE_MAPS_API_KEY is not set.'
      throw new Error(error.value)
    }

    if (!initialized) {
      setOptions({ key: apiKey, v: 'weekly' })
      initialized = true
    }

    loadPromise = importLibrary('maps').then(() => {
      isLoaded.value = true
    })

    try {
      await loadPromise
    } catch (e) {
      error.value = 'Failed to load Google Maps.'
      loadPromise = null
      throw e
    }
  }

  async function getMapsLibrary(): Promise<google.maps.MapsLibrary> {
    await load()
    return importLibrary('maps') as Promise<google.maps.MapsLibrary>
  }

  return { isLoaded, error, load, getMapsLibrary }
}
