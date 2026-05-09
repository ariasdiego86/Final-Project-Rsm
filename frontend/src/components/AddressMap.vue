<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { importLibrary } from '@googlemaps/js-api-loader'
import { useGoogleMaps } from '@/composables/useGoogleMaps'

const props = defineProps<{
  latitude: number | null
  longitude: number | null
}>()

const mapContainer = ref<HTMLElement | null>(null)
const { load, error: mapsError } = useGoogleMaps()

let mapInstance: google.maps.Map | null = null
let markerInstance: google.maps.marker.AdvancedMarkerElement | null = null

const hasCoords = () =>
  props.latitude !== null && props.longitude !== null

async function initMap() {
  if (!mapContainer.value || !hasCoords()) return

  await load()
  const { Map } = (await importLibrary('maps')) as google.maps.MapsLibrary
  const { AdvancedMarkerElement } = (await importLibrary('marker')) as google.maps.MarkerLibrary

  const position = { lat: Number(props.latitude), lng: Number(props.longitude) }

  mapInstance = new Map(mapContainer.value, {
    center: position,
    zoom: 13,
    mapId: 'DEMO_MAP_ID',
  })

  markerInstance = new AdvancedMarkerElement({ map: mapInstance, position })
}

function updatePosition(lat: number, lng: number) {
  if (!mapInstance || !markerInstance) return
  const position = { lat: Number(lat), lng: Number(lng) }
  mapInstance.setCenter(position)
  markerInstance.position = position
}

onMounted(() => {
  if (hasCoords()) initMap()
})

watch(
  [() => props.latitude, () => props.longitude],
  ([lat, lng]) => {
    if (lat !== null && lng !== null) {
      if (!mapInstance) initMap()
      else updatePosition(lat, lng)
    } else {
      mapInstance = null
      markerInstance = null
    }
  },
  { flush: 'post' },
)

onUnmounted(() => {
  markerInstance = null
  mapInstance = null
})
</script>

<template>
  <div>
    <div v-if="mapsError" class="q-pa-md text-negative text-caption">
      {{ mapsError }}
    </div>

    <div
      v-else-if="!hasCoords()"
      class="row items-center justify-center text-grey-5 q-pa-md"
      style="height: 260px; border: 1px dashed #ccc; border-radius: 4px"
    >
      <div class="text-center">
        <q-icon name="location_off" size="2rem" />
        <p class="q-mt-sm text-caption">Validate the address to see the map</p>
      </div>
    </div>

    <div
      v-else
      ref="mapContainer"
      style="height: 260px; width: 100%; border-radius: 4px; overflow: hidden"
    />
  </div>
</template>
