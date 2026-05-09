import { format, parseISO } from 'date-fns'

export function formatDate(value: string | null | undefined, pattern = 'yyyy-MM-dd'): string {
  if (!value) return '—'
  try {
    return format(parseISO(value), pattern)
  } catch {
    return value
  }
}

export function formatCurrency(value: number | null | undefined): string {
  if (value == null) return '—'
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

export function downloadBlob(data: Blob, filename: string): void {
  const url = URL.createObjectURL(data)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  document.body.removeChild(anchor)
  URL.revokeObjectURL(url)
}

export function buildDefaultFilter() {
  return {
    year: null,
    month: null,
    week: null,
    region: null,
    page: 1,
    pageSize: 15,
    sortBy: 'orderDate',
    sortDir: 'desc',
  }
}
