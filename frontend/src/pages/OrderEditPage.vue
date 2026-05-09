<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { ordersService } from '@/services/ordersService'
import type { Order } from '@/types'
import OrderForm from '@/components/OrderForm.vue'

const route = useRoute()
const order = ref<Order | null>(null)
const loading = ref(false)

const orderId = route.params.id ? Number(route.params.id) : null
const isEdit = !!orderId

onMounted(async () => {
  if (!isEdit) return
  loading.value = true
  try {
    const res = await ordersService.getById(orderId!)
    order.value = res.data
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <q-page class="q-pa-md" style="max-width: 960px; margin: 0 auto">
    <div class="row items-center q-mb-md">
      <q-btn icon="arrow_back" flat round dense @click="$router.push('/')" />
      <span class="text-h6 q-ml-sm">
        {{ isEdit ? `Edit Order #${orderId}` : 'New Order' }}
      </span>
    </div>

    <div v-if="loading" class="row justify-center q-pa-xl">
      <q-spinner color="primary" size="3em" />
    </div>

    <OrderForm v-else :order="order" @saved="() => $router.push('/')" />
  </q-page>
</template>
