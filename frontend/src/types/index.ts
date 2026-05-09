// ─── Order ────────────────────────────────────────────────────────────────────

export interface OrderListItem {
  orderID: number
  customerID: string | null
  customerName: string | null
  employeeName: string | null
  orderDate: string | null
  shippedDate: string | null
  shipCountry: string | null
  shipRegion: string | null
  freight: number | null
  total: number
  productCount: number
}

export interface OrderDetail {
  orderID: number
  productID: number
  productName: string | null
  unitPrice: number
  quantity: number
  discount: number
  subtotal: number
}

export interface Order {
  orderID: number
  customerID: string | null
  customerName: string | null
  employeeID: number | null
  employeeName: string | null
  shipVia: number | null
  shipperName: string | null
  orderDate: string | null
  requiredDate: string | null
  shippedDate: string | null
  freight: number | null
  shipName: string | null
  shipAddress: string | null
  shipCity: string | null
  shipRegion: string | null
  shipPostalCode: string | null
  shipCountry: string | null
  latitude: number | null
  longitude: number | null
  orderDetails: OrderDetail[]
}

export interface CreateOrderDto {
  customerID: string
  employeeID: number
  shipVia?: number | null
  orderDate?: string | null
  requiredDate?: string | null
  freight?: number | null
  shipName?: string | null
  shipAddress?: string | null
  shipCity?: string | null
  shipRegion?: string | null
  shipPostalCode?: string | null
  shipCountry?: string | null
  latitude?: number | null
  longitude?: number | null
  orderDetails: OrderDetailWriteDto[]
}

export interface UpdateOrderDto {
  customerID?: string | null
  employeeID?: number | null
  shipVia?: number | null
  requiredDate?: string | null
  shippedDate?: string | null
  freight?: number | null
  shipName?: string | null
  shipAddress?: string | null
  shipCity?: string | null
  shipRegion?: string | null
  shipPostalCode?: string | null
  shipCountry?: string | null
  latitude?: number | null
  longitude?: number | null
  orderDetails?: OrderDetailWriteDto[]
}

export interface OrderDetailWriteDto {
  productID: number
  unitPrice: number
  quantity: number
  discount: number
}

// ─── Filters & Pagination ─────────────────────────────────────────────────────

export interface OrderFilter {
  year?: number | null
  month?: number | null
  week?: number | null
  region?: string | null
  page: number
  pageSize: number
  sortBy: string
  sortDir: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ─── Lookups ──────────────────────────────────────────────────────────────────

export interface LookupItem {
  id: string
  name: string
}

// ─── Reports ──────────────────────────────────────────────────────────────────

export interface TimeSeriesPoint {
  label: string
  count: number
}

export interface RegionShipment {
  region: string
  count: number
}

// ─── Geo ──────────────────────────────────────────────────────────────────────

export interface AddressValidationRequest {
  address: string
}

export interface AddressValidationResult {
  formattedAddress: string
  latitude: number | null
  longitude: number | null
  isValid: boolean
  issues: string[]
}

// ─── API errors (RFC 7807 ProblemDetails) ────────────────────────────────────

export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  traceId?: string
  errors?: Record<string, string[]>
}
