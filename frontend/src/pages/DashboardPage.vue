<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  Filler,
} from 'chart.js'
import { Line, Doughnut } from 'vue-chartjs'
import { useOrdersStore } from '@/stores/useOrdersStore'
import { useLookupsStore } from '@/stores/useLookupsStore'
import { reportsService } from '@/services/reportsService'
import { ordersService } from '@/services/ordersService'
import { formatDate, formatCurrency, downloadBlob } from '@/utils'
import type { TimeSeriesPoint, RegionShipment } from '@/types'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, ArcElement, Title, Tooltip, Legend, Filler)

const $q = useQuasar()
const router = useRouter()
const store = useOrdersStore()
const lookups = useLookupsStore()

// ─── Report data ──────────────────────────────────────────────────────────────

const timeSeries = ref<TimeSeriesPoint[]>([])
const regionData = ref<RegionShipment[]>([])
const granularity = ref<'month' | 'year' | 'week'>('month')

async function fetchCharts() {
  const [tRes, rRes] = await Promise.all([
    reportsService.getOrdersPerTime(granularity.value),
    reportsService.getShipmentsByRegion(),
  ])
  timeSeries.value = tRes.data
  regionData.value = rRes.data
}

watch(granularity, fetchCharts)

// ─── Chart data ───────────────────────────────────────────────────────────────

const CHART_COLORS = ['#1976D2','#26A69A','#EF5350','#AB47BC','#FF7043','#66BB6A','#FFA726','#42A5F5','#EC407A','#8D6E63']

const lineChartData = computed(() => ({
  labels: timeSeries.value.map(d => d.label),
  datasets: [{
    label: 'Orders',
    data: timeSeries.value.map(d => d.count),
    borderColor: '#1976D2',
    backgroundColor: 'rgba(25,118,210,0.12)',
    fill: true,
    tension: 0.35,
    pointRadius: 3,
  }],
}))

const donutChartData = computed(() => ({
  labels: regionData.value.map(d => d.region),
  datasets: [{
    data: regionData.value.map(d => d.count),
    backgroundColor: regionData.value.map((_, i) => CHART_COLORS[i % CHART_COLORS.length]),
    borderWidth: 1,
  }],
}))

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: { legend: { position: 'bottom' as const } },
}

// ─── Filter state ─────────────────────────────────────────────────────────────

const yearOptions = computed(() => {
  const current = new Date().getFullYear()
  return Array.from({ length: current - 1989 }, (_, i) => current - i)
})

const monthOptions = [
  { label: 'January', value: 1 }, { label: 'February', value: 2 },
  { label: 'March', value: 3 }, { label: 'April', value: 4 },
  { label: 'May', value: 5 }, { label: 'June', value: 6 },
  { label: 'July', value: 7 }, { label: 'August', value: 8 },
  { label: 'September', value: 9 }, { label: 'October', value: 10 },
  { label: 'November', value: 11 }, { label: 'December', value: 12 },
]

const weekOptions = [1, 2, 3, 4, 5]

const regionOptions = computed(() => lookups.regions)

function applyFilters() {
  store.filter.page = 1
  store.fetchOrders()
}

function clearFilters() {
  store.resetFilter()
  store.fetchOrders()
}

// ─── q-table pagination (server-side) ────────────────────────────────────────

const pagination = reactive({
  page: store.filter.page,
  rowsPerPage: store.filter.pageSize,
  rowsNumber: 0,
  sortBy: store.filter.sortBy,
  descending: store.filter.sortDir === 'desc',
})

watch(
  () => store.result.totalCount,
  (n) => { pagination.rowsNumber = n },
)

function onRequest(req: { pagination: { page: number; rowsPerPage: number; rowsNumber?: number; sortBy: string; descending: boolean } }) {
  store.filter.page = req.pagination.page
  store.filter.pageSize = req.pagination.rowsPerPage
  store.filter.sortBy = req.pagination.sortBy ?? 'orderDate'
  store.filter.sortDir = req.pagination.descending ? 'desc' : 'asc'
  pagination.page = req.pagination.page
  pagination.rowsPerPage = req.pagination.rowsPerPage
  pagination.sortBy = req.pagination.sortBy
  pagination.descending = req.pagination.descending
  store.fetchOrders()
}

const columns = [
  { name: 'orderID', label: '#', field: 'orderID', sortable: true, align: 'left' as const },
  { name: 'customerName', label: 'Customer', field: 'customerName', sortable: true, align: 'left' as const },
  { name: 'orderDate', label: 'Date', field: (r: { orderDate: string }) => formatDate(r.orderDate), sortable: true, align: 'left' as const },
  { name: 'productCount', label: 'Products', field: 'productCount', align: 'center' as const },
  { name: 'shipCountry', label: 'Region', field: (r: { shipCountry: string | null }) => r.shipCountry ?? '—', align: 'left' as const },
  { name: 'total', label: 'Total', field: (r: { total: number }) => formatCurrency(r.total), sortable: false, align: 'right' as const },
  { name: 'actions', label: 'Actions', field: 'actions', align: 'center' as const },
]

// ─── Export ───────────────────────────────────────────────────────────────────

async function exportExcel() {
  const res = await ordersService.getExcel({ ...store.filter })
  downloadBlob(res.data, `orders-${new Date().toISOString().slice(0, 10)}.xlsx`)
}

function viewPdf(orderId: number) {
  window.open(ordersService.getPdfUrl(orderId), '_blank')
}

// ─── Delete ───────────────────────────────────────────────────────────────────

function confirmDelete(orderId: number) {
  $q.dialog({
    title: 'Confirm Delete',
    message: `Delete order #${orderId}? This action cannot be undone.`,
    cancel: true,
    persistent: true,
    ok: { label: 'Delete', color: 'negative', flat: true },
  }).onOk(async () => {
    await store.deleteOrder(orderId)
    $q.notify({ message: `Order #${orderId} deleted.`, color: 'positive' })
  })
}

// ─── Init ─────────────────────────────────────────────────────────────────────

onMounted(async () => {
  await Promise.all([
    lookups.fetchAll(),
    store.fetchOrders(),
    fetchCharts(),
  ])
  pagination.rowsNumber = store.result.totalCount
})
</script>

<template>
  <q-page class="q-pa-md">

    <!-- ── Filters ─────────────────────────────────────────────────────────── -->
    <q-card class="q-mb-md">
      <q-card-section>
        <div class="row items-center q-gutter-md">
          <q-select
            v-model="store.filter.year"
            :options="yearOptions"
            label="Year"
            dense
            outlined
            clearable
            style="min-width: 100px"
            @update:model-value="applyFilters"
          />
          <q-select
            v-model="store.filter.month"
            :options="monthOptions"
            option-value="value"
            option-label="label"
            emit-value
            map-options
            label="Month"
            dense
            outlined
            clearable
            style="min-width: 130px"
            :disable="!store.filter.year"
            @update:model-value="applyFilters"
          />
          <q-select
            v-model="store.filter.week"
            :options="weekOptions"
            label="Week"
            dense
            outlined
            clearable
            style="min-width: 90px"
            :disable="!store.filter.month"
            @update:model-value="applyFilters"
          />
          <q-select
            v-model="store.filter.region"
            :options="regionOptions"
            label="Region / Country"
            dense
            outlined
            clearable
            use-input
            input-debounce="0"
            style="min-width: 160px"
            @update:model-value="applyFilters"
          />
          <q-btn label="Clear" flat dense icon="clear" @click="clearFilters" />

          <q-space />

          <q-btn
            label="New Order"
            color="primary"
            icon="add"
            @click="router.push('/orders/new')"
          />
        </div>
      </q-card-section>
    </q-card>

    <!-- ── Charts ──────────────────────────────────────────────────────────── -->
    <div class="row q-col-gutter-md q-mb-md">
      <div class="col-12 col-md-7">
        <q-card style="height: 280px">
          <q-card-section class="q-pb-none">
            <div class="row items-center">
              <span class="text-subtitle2 col">Orders Over Time</span>
              <q-btn-toggle
                v-model="granularity"
                :options="[
                  { label: 'Month', value: 'month' },
                  { label: 'Year', value: 'year' },
                  { label: 'Week', value: 'week' },
                ]"
                dense
                flat
                toggle-color="primary"
                size="xs"
              />
            </div>
          </q-card-section>
          <q-card-section style="height: 210px">
            <Line
              v-if="timeSeries.length"
              :data="lineChartData"
              :options="chartOptions"
            />
            <div v-else class="row justify-center items-center full-height text-grey-5">
              No data
            </div>
          </q-card-section>
        </q-card>
      </div>

      <div class="col-12 col-md-5">
        <q-card style="height: 280px">
          <q-card-section class="q-pb-none">
            <span class="text-subtitle2">Shipments by Country / Region</span>
          </q-card-section>
          <q-card-section style="height: 210px">
            <Doughnut
              v-if="regionData.length"
              :data="donutChartData"
              :options="chartOptions"
            />
            <div v-else class="row justify-center items-center full-height text-grey-5">
              No data
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- ── Orders Table ─────────────────────────────────────────────────────── -->
    <q-card>
      <q-table
        :rows="store.result.items"
        :columns="columns"
        :loading="store.loading"
        row-key="orderID"
        v-model:pagination="pagination"
        :rows-per-page-options="[10, 15, 25, 50]"
        binary-state-sort
        @request="onRequest"
      >
        <!-- Export toolbar -->
        <template #top>
          <div class="text-subtitle2 col">Orders</div>
          <q-btn
            label="Export Excel"
            icon="table_chart"
            flat
            dense
            color="positive"
            class="q-mr-sm"
            @click="exportExcel"
          />
        </template>

        <!-- Actions per row -->
        <template #body-cell-actions="{ row }">
          <q-td auto-width>
            <q-btn
              icon="picture_as_pdf"
              flat
              round
              dense
              color="primary"
              title="View PDF"
              @click="viewPdf(row.orderID)"
            />
            <q-btn
              icon="edit"
              flat
              round
              dense
              color="secondary"
              title="Edit"
              @click="router.push(`/orders/${row.orderID}/edit`)"
            />
            <q-btn
              icon="delete"
              flat
              round
              dense
              color="negative"
              title="Delete"
              @click="confirmDelete(row.orderID)"
            />
          </q-td>
        </template>

        <template #no-data>
          <div class="full-width row flex-center text-grey-5 q-pa-md">
            <q-icon name="inbox" size="2em" class="q-mr-sm" />
            No orders found for the current filters.
          </div>
        </template>
      </q-table>
    </q-card>

  </q-page>
</template>
