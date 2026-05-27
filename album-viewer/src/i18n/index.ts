import { createI18n } from 'vue-i18n'
import en from './locales/en.json'
import fr from './locales/fr.json'
import de from './locales/de.json'

export const SUPPORTED_LOCALES = ['en', 'fr', 'de'] as const
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number]

const STORAGE_KEY = 'album-viewer.locale'

function detectInitialLocale(): SupportedLocale {
  if (typeof window !== 'undefined') {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    if (stored && (SUPPORTED_LOCALES as readonly string[]).includes(stored)) {
      return stored as SupportedLocale
    }
  }
  return 'en'
}

export const i18n = createI18n({
  legacy: false,
  locale: detectInitialLocale(),
  fallbackLocale: 'en',
  messages: { en, fr, de }
})

export function setLocale(locale: SupportedLocale): void {
  i18n.global.locale.value = locale
  if (typeof window !== 'undefined') {
    window.localStorage.setItem(STORAGE_KEY, locale)
    document.documentElement.setAttribute('lang', locale)
  }
}
