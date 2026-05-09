import axios from 'axios'
import type { AxiosInstance } from 'axios'
import { Notify } from 'quasar'
import type { ProblemDetails } from '@/types'

const api: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080',
  headers: { 'Content-Type': 'application/json' },
})

function isProblemDetails(data: unknown): data is ProblemDetails {
  return (
    typeof data === 'object' &&
    data !== null &&
    ('title' in data || 'detail' in data)
  )
}

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const data = error.response?.data

    let message = 'An unexpected error occurred.'
    if (isProblemDetails(data)) {
      message = data.detail ?? data.title ?? message
    } else if (error.message) {
      message = error.message
    }

    Notify.create({
      message,
      color: 'negative',
      icon: 'error',
      timeout: 5000,
      position: 'top-right',
    })

    return Promise.reject(error)
  },
)

export default api
