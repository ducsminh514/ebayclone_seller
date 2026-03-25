import { test, expect } from '@playwright/test';
import { API_BASE } from './support/config';
import { getE2ECredentials } from './support/auth';

test.describe('API auth & guards', () => {
  test('login sai mật khẩu -> 401', async ({ request }) => {
    const { email } = getE2ECredentials();
    const res = await request.post(`${API_BASE}/api/auth/login`, {
      data: { email, password: 'wrong-password' },
    });
    expect(res.status()).toBe(401);
  });

  test('login đúng -> trả token + expiration', async ({ request }) => {
    const { email, password } = getE2ECredentials();
    const res = await request.post(`${API_BASE}/api/auth/login`, {
      data: { email, password },
    });
    expect(res.ok(), await res.text()).toBeTruthy();
    const body: { token?: string; expiration?: string } = await res.json();
    expect(body.token).toBeTruthy();
    expect(body.expiration).toBeTruthy();
  });

  test('products endpoint yêu cầu auth -> 401/403 khi anonymous', async ({ request }) => {
    const res = await request.get(`${API_BASE}/api/products`);
    expect([401, 403]).toContain(res.status());
  });

  test('orders endpoint yêu cầu auth -> 401/403 khi anonymous', async ({ request }) => {
    const res = await request.get(`${API_BASE}/api/orders`);
    expect([401, 403]).toContain(res.status());
  });
});
