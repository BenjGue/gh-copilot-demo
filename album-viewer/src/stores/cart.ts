import { reactive, computed, watch, type ComputedRef } from 'vue'
import type { Album } from '../types/album'

const STORAGE_KEY = 'album-viewer.cart'

interface CartState {
  items: Album[]
}

function loadInitial(): Album[] {
  if (typeof window === 'undefined') return []
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw)
    return Array.isArray(parsed) ? (parsed as Album[]) : []
  } catch {
    return []
  }
}

const state = reactive<CartState>({ items: loadInitial() })

if (typeof window !== 'undefined') {
  watch(
    () => state.items,
    (items) => {
      try {
        window.localStorage.setItem(STORAGE_KEY, JSON.stringify(items))
      } catch {
        /* ignore quota / privacy errors */
      }
    },
    { deep: true }
  )
}

export interface UseCart {
  items: Album[]
  count: ComputedRef<number>
  total: ComputedRef<number>
  has: (albumId: number) => boolean
  add: (album: Album) => void
  remove: (albumId: number) => void
  clear: () => void
}

export function useCart(): UseCart {
  const count = computed(() => state.items.length)
  const total = computed(() =>
    state.items.reduce((sum, a) => sum + (typeof a.price === 'number' ? a.price : 0), 0)
  )

  const has = (albumId: number): boolean => state.items.some((a) => a.id === albumId)

  const add = (album: Album): void => {
    if (!has(album.id)) state.items.push(album)
  }

  const remove = (albumId: number): void => {
    const idx = state.items.findIndex((a) => a.id === albumId)
    if (idx !== -1) state.items.splice(idx, 1)
  }

  const clear = (): void => {
    state.items.splice(0, state.items.length)
  }

  return { items: state.items, count, total, has, add, remove, clear }
}
