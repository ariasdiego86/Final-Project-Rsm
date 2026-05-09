import { config } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { Quasar, Notify } from 'quasar'
import { beforeEach, vi } from 'vitest'

config.global.plugins = [[Quasar, { plugins: { Notify } }]]

beforeEach(() => {
  setActivePinia(createPinia())
})

// Silence console.error in tests
vi.spyOn(console, 'error').mockImplementation(() => {})
