import { test, expect } from '@playwright/test'
import path from 'node:path'

test.describe('Cart feature', () => {
  test.beforeEach(async ({ context }) => {
    // Ensure a clean cart for each test (cart is persisted in localStorage).
    await context.addInitScript(() => {
      try {
        window.localStorage.removeItem('album-viewer.cart')
      } catch {
        /* ignore */
      }
    })
  })

  test('add first album to cart, open drawer, verify content', async ({ page }, testInfo) => {
    // 1. Open the Album App.
    await page.goto('/')
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    // First album card (assumes albums load successfully).
    const firstCard = page.locator('.album-card').first()
    await expect(firstCard).toBeVisible()
    const firstAlbumTitle = (await firstCard.locator('h3').innerText()).trim()

    // 2. Click "Add to Cart" on the first tile.
    const addBtn = firstCard.getByRole('button', { name: /add to cart/i })
    await expect(addBtn).toBeEnabled()
    await addBtn.click()

    // Button flips to "In cart" and is disabled.
    await expect(firstCard.getByRole('button', { name: /in cart/i })).toBeDisabled()

    // Cart badge shows 1.
    const cartButton = page.getByRole('button', { name: /open cart/i })
    await expect(cartButton).toContainText('1')

    // 3. Click the cart button to display the cart.
    await cartButton.click()
    const drawer = page.getByRole('dialog', { name: /your cart/i })
    await expect(drawer).toBeVisible()

    // 4. Check that the cart contains the added album.
    const cartItems = drawer.getByRole('listitem')
    await expect(cartItems).toHaveCount(1)
    await expect(cartItems.first()).toContainText(firstAlbumTitle)

    // 5. Take a screenshot of the cart.
    const screenshotPath = path.join(
      testInfo.outputDir,
      'cart-with-first-album.png',
    )
    await page.screenshot({ path: screenshotPath, fullPage: false })
    await testInfo.attach('cart-with-first-album', {
      path: screenshotPath,
      contentType: 'image/png',
    })
  })
})
