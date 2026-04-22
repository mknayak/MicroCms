import { test, expect } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5174';

test.describe('Smoke: Login Flow', () => {
  test('shows login page when not authenticated', async ({ page }) => {
    await page.goto(BASE_URL);
    await expect(page).toHaveURL(/\/login/);
    await expect(page.getByRole('heading', { name: /microcms admin/i })).toBeVisible();
  });

  test('can login with valid credentials', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.getByLabel(/email address/i).fill('admin@example.com');
    await page.getByLabel(/password/i).fill('password');
    await page.getByRole('button', { name: /sign in/i }).click();
    // After successful login, redirect to dashboard
    await expect(page).toHaveURL(BASE_URL + '/');
    await expect(page.getByText(/welcome back/i)).toBeVisible({ timeout: 10_000 });
  });

  test('shows error for invalid credentials', async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.getByLabel(/email address/i).fill('wrong@example.com');
    await page.getByLabel(/password/i).fill('wrong');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page.getByText(/invalid credentials/i)).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Smoke: Content Types', () => {
  test.beforeEach(async ({ page }) => {
    // Set mock token in sessionStorage to bypass login
    await page.goto(`${BASE_URL}/login`);
    await page.evaluate(() => {
      sessionStorage.setItem('mcms_token', 'mock.jwt.token');
    });
    await page.goto(`${BASE_URL}/content-types`);
  });

  test('shows content types page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /content types/i })).toBeVisible();
  });

  test('can navigate to create new content type', async ({ page }) => {
    await page.getByRole('link', { name: /new content type/i }).click();
    await expect(page).toHaveURL(/\/content-types\/new/);
  });
});

test.describe('Smoke: Entries', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.evaluate(() => {
      sessionStorage.setItem('mcms_token', 'mock.jwt.token');
    });
    await page.goto(`${BASE_URL}/entries`);
  });

  test('shows entries page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /entries/i })).toBeVisible();
  });

  test('can navigate to create new entry', async ({ page }) => {
    await page.getByRole('link', { name: /new entry/i }).click();
    await expect(page).toHaveURL(/\/entries\/new/);
  });
});
