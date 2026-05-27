<template>
  <Teleport to="body">
    <transition name="cart-fade">
      <div v-if="open" class="cart-backdrop" @click="$emit('close')"></div>
    </transition>
    <transition name="cart-slide">
      <aside
        v-if="open"
        class="cart-drawer"
        role="dialog"
        aria-modal="true"
        :aria-label="t('cart.title')"
      >
        <header class="cart-header">
          <h2>{{ t('cart.title') }}</h2>
          <button
            class="close-btn"
            type="button"
            :aria-label="t('cart.close')"
            @click="$emit('close')"
          >
            ×
          </button>
        </header>

        <div v-if="items.length === 0" class="cart-empty">
          <p>{{ t('cart.empty') }}</p>
        </div>

        <ul v-else class="cart-list">
          <li v-for="item in items" :key="item.id" class="cart-item">
            <img
              class="cart-thumb"
              :src="item.image_url"
              :alt="item.title"
              loading="lazy"
              @error="onImgError"
            />
            <div class="cart-info">
              <p class="cart-title">{{ item.title }}</p>
              <p class="cart-artist">{{ artistName(item) }}</p>
              <p class="cart-price">${{ item.price.toFixed(2) }}</p>
            </div>
            <button
              class="remove-btn"
              type="button"
              :aria-label="t('cart.remove')"
              @click="remove(item.id)"
            >
              {{ t('cart.remove') }}
            </button>
          </li>
        </ul>

        <footer class="cart-footer">
          <div class="cart-total">
            <span>{{ t('cart.total') }}</span>
            <span class="cart-total-value">${{ total.toFixed(2) }}</span>
          </div>
          <button class="checkout-btn" type="button" :disabled="items.length === 0">
            {{ t('cart.checkout') }}
          </button>
        </footer>
      </aside>
    </transition>
  </Teleport>
</template>

<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { useCart } from '../stores/cart'
import type { Album } from '../types/album'

defineProps<{ open: boolean }>()
defineEmits<{ (e: 'close'): void }>()

const { t } = useI18n()
const { items, total, remove } = useCart()

const artistName = (album: Album): string => {
  const a = album.artist as unknown
  if (a && typeof a === 'object' && 'name' in (a as Record<string, unknown>)) {
    return String((a as { name: unknown }).name ?? '')
  }
  return String(a ?? '')
}

const onImgError = (event: Event): void => {
  const target = event.target as HTMLImageElement
  target.src = 'https://via.placeholder.com/80x80/667eea/white?text=%E2%99%AB'
}
</script>

<style scoped>
.cart-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  z-index: 999;
}

.cart-drawer {
  position: fixed;
  top: 0;
  right: 0;
  height: 100vh;
  width: min(420px, 100vw);
  background: white;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  box-shadow: -10px 0 30px rgba(0, 0, 0, 0.3);
}

.cart-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1.25rem 1.5rem;
  border-bottom: 1px solid #eee;
}

.cart-header h2 {
  margin: 0;
  font-size: 1.4rem;
  color: #333;
}

.close-btn {
  background: transparent;
  border: none;
  font-size: 2rem;
  line-height: 1;
  color: #666;
  cursor: pointer;
  padding: 0 0.25rem;
}

.close-btn:hover {
  color: #333;
}

.cart-empty {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #777;
  padding: 2rem;
  text-align: center;
}

.cart-list {
  list-style: none;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  flex: 1;
}

.cart-item {
  display: grid;
  grid-template-columns: 80px 1fr auto;
  gap: 0.75rem;
  align-items: center;
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid #f1f1f1;
}

.cart-thumb {
  width: 80px;
  height: 80px;
  object-fit: cover;
  border-radius: 8px;
  background: #eee;
}

.cart-info {
  min-width: 0;
}

.cart-info p {
  margin: 0;
  line-height: 1.3;
}

.cart-title {
  font-weight: 600;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cart-artist {
  color: #777;
  font-size: 0.9rem;
}

.cart-price {
  color: #667eea;
  font-weight: 700;
  margin-top: 0.25rem !important;
}

.remove-btn {
  background: transparent;
  border: 1px solid #e44;
  color: #e44;
  padding: 0.35rem 0.7rem;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
  transition: background 0.2s ease, color 0.2s ease;
}

.remove-btn:hover {
  background: #e44;
  color: white;
}

.cart-footer {
  border-top: 1px solid #eee;
  padding: 1rem 1.5rem 1.25rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.cart-total {
  display: flex;
  justify-content: space-between;
  font-size: 1.1rem;
  color: #333;
}

.cart-total-value {
  font-weight: 700;
  color: #667eea;
}

.checkout-btn {
  background: #667eea;
  color: white;
  border: none;
  padding: 0.85rem;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease;
}

.checkout-btn:hover:not(:disabled) {
  background: #5a6fd8;
}

.checkout-btn:disabled {
  background: #b9c1e8;
  cursor: not-allowed;
}

.cart-slide-enter-from,
.cart-slide-leave-to {
  transform: translateX(100%);
}

.cart-slide-enter-active,
.cart-slide-leave-active {
  transition: transform 0.25s ease;
}

.cart-fade-enter-from,
.cart-fade-leave-to {
  opacity: 0;
}

.cart-fade-enter-active,
.cart-fade-leave-active {
  transition: opacity 0.25s ease;
}
</style>
