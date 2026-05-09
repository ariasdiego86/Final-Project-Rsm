<script setup lang="ts">
import { reactive, ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useQuasar } from 'quasar'
import { useLookupsStore } from '@/stores/useLookupsStore'
import { useAddressValidation } from '@/composables/useAddressValidation'
import { ordersService } from '@/services/ordersService'
import type { Order, OrderDetailWriteDto } from '@/types'
import AddressMap from './AddressMap.vue'

const props = defineProps<{
  order?: Order | null
}>()

const emit = defineEmits<{ saved: [orderId: number] }>()

const router = useRouter()
const $q = useQuasar()
const lookups = useLookupsStore()
const { validating, result: geoResult, validationError, validate: validateAddress, reset: resetGeo } = useAddressValidation()

// ─── Form model ──────────────────────────────────────────────────────────────

const form = reactive({
  customerID: '',
  employeeID: null as number | null,
  shipVia: null as number | null,
  orderDate: '',
  requiredDate: '',
  freight: null as number | null,
  shipName: '',
  shipAddress: '',
  shipCity: '',
  shipRegion: '',
  shipPostalCode: '',
  shipCountry: '',
  latitude: null as number | null,
  longitude: null as number | null,
})

const orderLines = ref<OrderDetailWriteDto[]>([{ productID: 0, unitPrice: 0, quantity: 1, discount: 0 }])
const saving = ref(false)
const isEdit = computed(() => !!props.order?.orderID)

// ─── Init from existing order ────────────────────────────────────────────────

onMounted(async () => {
  await lookups.fetchAll()
  if (props.order) {
    form.customerID = props.order.customerID ?? ''
    form.employeeID = props.order.employeeID ?? null
    form.shipVia = props.order.shipVia ?? null
    form.orderDate = props.order.orderDate?.slice(0, 10).replace(/-/g, '/') ?? ''
    form.requiredDate = props.order.requiredDate?.slice(0, 10).replace(/-/g, '/') ?? ''
    form.freight = props.order.freight ?? null
    form.shipName = props.order.shipName ?? ''
    form.shipAddress = props.order.shipAddress ?? ''
    form.shipCity = props.order.shipCity ?? ''
    form.shipRegion = props.order.shipRegion ?? ''
    form.shipPostalCode = props.order.shipPostalCode ?? ''
    form.shipCountry = props.order.shipCountry ?? ''
    form.latitude = props.order.latitude ?? null
    form.longitude = props.order.longitude ?? null
    orderLines.value = props.order.orderDetails.map(d => ({
      productID: d.productID,
      unitPrice: d.unitPrice,
      quantity: d.quantity,
      discount: d.discount,
    }))
    if (!orderLines.value.length) addLine()
  }
})

// ─── Lookups as q-select options ─────────────────────────────────────────────

const customerOptions = computed(() =>
  lookups.customers.map(c => ({ label: c.name, value: c.id })),
)
const employeeOptions = computed(() =>
  lookups.employees.map(e => ({ label: e.name, value: Number(e.id) })),
)
const shipperOptions = computed(() =>
  lookups.shippers.map(s => ({ label: s.name, value: Number(s.id) })),
)
const productOptions = computed(() =>
  lookups.products.map(p => ({ label: p.name, value: Number(p.id) })),
)

// ─── Order lines ─────────────────────────────────────────────────────────────

function addLine() {
  orderLines.value.push({ productID: 0, unitPrice: 0, quantity: 1, discount: 0 })
}

function removeLine(index: number) {
  if (orderLines.value.length > 1) orderLines.value.splice(index, 1)
}

function lineSubtotal(line: OrderDetailWriteDto) {
  return (line.unitPrice * line.quantity * (1 - line.discount)).toFixed(2)
}

// ─── Address validation on blur ───────────────────────────────────────────────

async function onAddressBlur() {
  const address = [form.shipAddress, form.shipCity, form.shipCountry].filter(Boolean).join(', ')
  if (!address) return

  resetGeo()
  form.latitude = null
  form.longitude = null

  const result = await validateAddress(address)
  if (result?.isValid) {
    form.latitude = result.latitude
    form.longitude = result.longitude
    form.shipAddress = result.formattedAddress
    $q.notify({ message: 'Address validated successfully', color: 'positive', icon: 'check_circle' })
  }
}

// ─── Submit ───────────────────────────────────────────────────────────────────

async function submit() {
  if (!form.customerID || !form.employeeID) {
    $q.notify({ message: 'Customer and Employee are required.', color: 'warning' })
    return
  }
  if (!orderLines.value.some(l => l.productID > 0)) {
    $q.notify({ message: 'Add at least one valid product line.', color: 'warning' })
    return
  }

  saving.value = true
  try {
    const lines = orderLines.value.filter(l => l.productID > 0)
    const isoDate = (d: string) => d ? d.replace(/\//g, '-') + 'T00:00:00Z' : undefined

    if (isEdit.value && props.order) {
      await ordersService.update(props.order.orderID, {
        customerID: form.customerID,
        employeeID: form.employeeID,
        shipVia: form.shipVia,
        requiredDate: isoDate(form.requiredDate),
        freight: form.freight,
        shipName: form.shipName || undefined,
        shipAddress: form.shipAddress || undefined,
        shipCity: form.shipCity || undefined,
        shipRegion: form.shipRegion || undefined,
        shipPostalCode: form.shipPostalCode || undefined,
        shipCountry: form.shipCountry || undefined,
        latitude: form.latitude,
        longitude: form.longitude,
        orderDetails: lines,
      })
      $q.notify({ message: 'Order updated successfully', color: 'positive' })
      emit('saved', props.order.orderID)
    } else {
      const res = await ordersService.create({
        customerID: form.customerID,
        employeeID: form.employeeID!,
        shipVia: form.shipVia,
        orderDate: isoDate(form.orderDate),
        requiredDate: isoDate(form.requiredDate),
        freight: form.freight,
        shipName: form.shipName || undefined,
        shipAddress: form.shipAddress || undefined,
        shipCity: form.shipCity || undefined,
        shipRegion: form.shipRegion || undefined,
        shipPostalCode: form.shipPostalCode || undefined,
        shipCountry: form.shipCountry || undefined,
        latitude: form.latitude,
        longitude: form.longitude,
        orderDetails: lines,
      })
      $q.notify({ message: 'Order created successfully', color: 'positive' })
      emit('saved', res.data.orderID)
    }
    router.push('/')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <q-form @submit.prevent="submit" class="q-gutter-y-md">
    <!-- ── Order header ─────────────────────────────────────────────────── -->
    <q-card>
      <q-card-section>
        <div class="text-subtitle1 q-mb-md">Order Details</div>
        <div class="row q-col-gutter-md">
          <div class="col-12 col-md-4">
            <q-select
              v-model="form.customerID"
              :options="customerOptions"
              option-value="value"
              option-label="label"
              emit-value
              map-options
              label="Customer *"
              outlined
              dense
              use-input
              input-debounce="0"
            />
          </div>
          <div class="col-12 col-md-4">
            <q-select
              v-model="form.employeeID"
              :options="employeeOptions"
              option-value="value"
              option-label="label"
              emit-value
              map-options
              label="Employee *"
              outlined
              dense
            />
          </div>
          <div class="col-12 col-md-4">
            <q-select
              v-model="form.shipVia"
              :options="shipperOptions"
              option-value="value"
              option-label="label"
              emit-value
              map-options
              label="Shipper"
              outlined
              dense
              clearable
            />
          </div>

          <div class="col-12 col-md-4">
            <q-input
              v-model="form.orderDate"
              label="Order Date"
              outlined
              dense
              mask="####/##/##"
            >
              <template #append>
                <q-icon name="event" class="cursor-pointer">
                  <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                    <q-date v-model="form.orderDate" mask="YYYY/MM/DD">
                      <div class="row items-center justify-end">
                        <q-btn v-close-popup label="Close" color="primary" flat />
                      </div>
                    </q-date>
                  </q-popup-proxy>
                </q-icon>
              </template>
            </q-input>
          </div>
          <div class="col-12 col-md-4">
            <q-input
              v-model="form.requiredDate"
              label="Required Date"
              outlined
              dense
              mask="####/##/##"
            >
              <template #append>
                <q-icon name="event" class="cursor-pointer">
                  <q-popup-proxy cover transition-show="scale" transition-hide="scale">
                    <q-date v-model="form.requiredDate" mask="YYYY/MM/DD">
                      <div class="row items-center justify-end">
                        <q-btn v-close-popup label="Close" color="primary" flat />
                      </div>
                    </q-date>
                  </q-popup-proxy>
                </q-icon>
              </template>
            </q-input>
          </div>
          <div class="col-12 col-md-4">
            <q-input
              v-model.number="form.freight"
              label="Freight ($)"
              outlined
              dense
              type="number"
              min="0"
              step="0.01"
            />
          </div>
        </div>
      </q-card-section>
    </q-card>

    <!-- ── Shipping address ───────────────────────────────────────────────── -->
    <q-card>
      <q-card-section>
        <div class="text-subtitle1 q-mb-md">Shipping Address</div>
        <div class="row q-col-gutter-md">
          <div class="col-12 col-md-6">
            <q-input v-model="form.shipName" label="Ship Name" outlined dense />
          </div>
          <div class="col-12 col-md-6">
            <q-input
              v-model="form.shipAddress"
              label="Address"
              outlined
              dense
              @blur="onAddressBlur"
            >
              <template #append>
                <q-spinner v-if="validating" color="primary" size="1.2em" />
                <q-icon v-else-if="geoResult?.isValid" name="check_circle" color="positive" />
                <q-icon v-else-if="validationError" name="warning" color="warning" />
              </template>
            </q-input>
          </div>

          <div class="col-12 col-md-4">
            <q-input v-model="form.shipCity" label="City" outlined dense @blur="onAddressBlur" />
          </div>
          <div class="col-12 col-md-4">
            <q-input v-model="form.shipRegion" label="Region / State" outlined dense />
          </div>
          <div class="col-12 col-md-4">
            <q-input v-model="form.shipPostalCode" label="Postal Code" outlined dense />
          </div>
          <div class="col-12 col-md-6">
            <q-input v-model="form.shipCountry" label="Country" outlined dense @blur="onAddressBlur" />
          </div>
        </div>

        <q-banner v-if="validationError" class="q-mt-md text-warning" rounded>
          <template #avatar>
            <q-icon name="warning" color="warning" />
          </template>
          {{ validationError }}
          <span v-if="geoResult?.issues?.length" class="q-ml-xs text-caption">
            ({{ geoResult.issues.join('; ') }})
          </span>
        </q-banner>

        <div class="q-mt-md">
          <AddressMap :latitude="form.latitude" :longitude="form.longitude" />
        </div>
      </q-card-section>
    </q-card>

    <!-- ── Order lines ────────────────────────────────────────────────────── -->
    <q-card>
      <q-card-section>
        <div class="row items-center q-mb-md">
          <div class="text-subtitle1 col">Order Lines</div>
          <q-btn
            icon="add"
            label="Add Line"
            color="primary"
            flat
            dense
            @click="addLine"
          />
        </div>

        <div
          v-for="(line, idx) in orderLines"
          :key="idx"
          class="row q-col-gutter-sm q-mb-sm items-center"
        >
          <div class="col-12 col-md-4">
            <q-select
              v-model="line.productID"
              :options="productOptions"
              option-value="value"
              option-label="label"
              emit-value
              map-options
              :label="`Product #${idx + 1} *`"
              outlined
              dense
              use-input
              input-debounce="0"
            />
          </div>
          <div class="col-6 col-md-2">
            <q-input
              v-model.number="line.unitPrice"
              label="Unit Price"
              outlined
              dense
              type="number"
              min="0"
              step="0.01"
              prefix="$"
            />
          </div>
          <div class="col-6 col-md-2">
            <q-input
              v-model.number="line.quantity"
              label="Qty"
              outlined
              dense
              type="number"
              min="1"
            />
          </div>
          <div class="col-6 col-md-2">
            <q-input
              v-model.number="line.discount"
              label="Discount"
              outlined
              dense
              type="number"
              min="0"
              max="1"
              step="0.01"
              suffix="%"
              :hint="line.discount > 0 ? (line.discount * 100).toFixed(0) + '%' : undefined"
            />
          </div>
          <div class="col-6 col-md-1 text-right">
            <div class="text-caption text-grey-7">Subtotal</div>
            <div class="text-weight-medium">${{ lineSubtotal(line) }}</div>
          </div>
          <div class="col-auto">
            <q-btn
              icon="delete"
              flat
              round
              dense
              color="negative"
              :disable="orderLines.length <= 1"
              @click="removeLine(idx)"
            />
          </div>
        </div>

        <div class="row justify-end q-mt-md">
          <div class="text-subtitle2">
            Total: ${{
              orderLines
                .reduce((sum, l) => sum + l.unitPrice * l.quantity * (1 - l.discount), 0)
                .toFixed(2)
            }}
          </div>
        </div>
      </q-card-section>
    </q-card>

    <!-- ── Actions ────────────────────────────────────────────────────────── -->
    <div class="row justify-end q-gutter-x-md">
      <q-btn label="Cancel" flat @click="router.push('/')" />
      <q-btn
        :label="isEdit ? 'Update Order' : 'Create Order'"
        type="submit"
        color="primary"
        :loading="saving"
      />
    </div>
  </q-form>
</template>
