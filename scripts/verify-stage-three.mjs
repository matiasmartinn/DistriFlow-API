import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { mkdirSync, writeFileSync } from 'node:fs';

const base = process.env.STAGE3_API_URL ?? 'http://localhost:5116/api';
if (!['localhost', '127.0.0.1'].includes(new URL(base).hostname)) throw Error('Local API required');
const run = `stage3-${Date.now()}`;
const password = `Test-${run}-Only!`;
const results = [];
const identities = [];
async function request(method, path, body, token, key, expected = [200, 201, 204]) {
  const response = await fetch(base + path, {
    method, signal: AbortSignal.timeout(15000),
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(key ? { 'Idempotency-Key': key } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  assert.ok(expected.includes(response.status), `${method} ${path}: ${response.status} ${data?.code ?? ''} ${data?.detail ?? ''}`);
  return { status: response.status, data };
}
const get = async (path, token) => (await request('GET', path, undefined, token)).data;
const post = async (path, body, token, key) => (await request('POST', path, body, token, key)).data;
async function check(name, test) {
  try { await test(); results.push({ name, passed: true }); console.log(`PASS ${name}`); }
  catch (error) { results.push({ name, passed: false, error: error.message }); console.log(`FAIL ${name}: ${error.message}`); }
}
const login = async (email) => (await post('/Auth/login', { email, password })).accessToken;
async function tenant(suffix) {
  const email = `${run}-${suffix}@example.test`;
  const registered = await post('/Distributors', {
    distributor: { name: `${run}-${suffix}`, email, phone: '1123456789' },
    administrator: { name: 'Stage 3 Administrator', email, password },
  });
  identities.push({ email, distributorId: registered.distributor.id });
  return { email, token: await login(email), id: registered.distributor.id };
}
const stock = (id, token) => get(`/Stocks/by-product/${id}`, token);
const start = (customerId, token, key = randomUUID()) => post('/Orders', { customerId }, token, key);
async function line(order, productId, amount, token) {
  return post(`/Orders/${order.id}/lines`, { productId, amount, rowVersion: order.rowVersion }, token);
}
const confirm = async (order, token, key = randomUUID(), expected) => request('PATCH', `/Orders/${order.id}/confirm`, { rowVersion: order.rowVersion }, token, key, expected);
const cancel = async (order, token, key = randomUUID(), expected) => request('PATCH', `/Orders/${order.id}/cancel`, { rowVersion: order.rowVersion, cancellationReason: 'Stage 3 verification' }, token, key, expected);

try {
  const a = await tenant('a');
  const b = await tenant('b');
  const repEmail = `${run}-rep@example.test`;
  const rep = await post('/Users', { name: 'Stage 3 Rep', email: repEmail, password, role: 'SalesRep' }, a.token);
  const repToken = await login(repEmail);
  const product = await post('/Products', { name: `${run} Product`, sku: `${run}-MAIN`, saleUnit: 'unidad', basePrice: 100 }, a.token);
  const customer = await post('/Customers', { name: `${run} Customer`, email: `${run}-customer@example.test`, phone: '1123456789', taxCondition: 'FinalConsumer' }, a.token);
  await check('Onboarding, login and current session', async () => {
    assert.equal((await get('/Auth/me', a.token)).distributorId, a.id);
    assert.equal((await get('/Auth/me', repToken)).role, 'SalesRep');
    await request('POST', '/Auth/login', { email: a.email, password: 'incorrect' }, undefined, undefined, [401]);
    await request('GET', '/Products', undefined, undefined, undefined, [401]);
  });
  await check('CORS permits frontend origin', async () => {
    const r = await fetch(base + '/Products', { method: 'OPTIONS', headers: {
      Origin: 'http://localhost:5173', 'Access-Control-Request-Method': 'GET', 'Access-Control-Request-Headers': 'authorization',
    } });
    assert.equal(r.headers.get('access-control-allow-origin'), 'http://localhost:5173');
  });
  await check('SalesRep administrative permissions denied', async () => {
    for (const path of ['/Users', '/PriceLists', '/DistributorConfigurations/current']) {
      await request('GET', path, undefined, repToken, undefined, [403]);
    }
    await request('POST', `/Stocks/by-product/${product.id}/increase`, { amount: 1 }, repToken, randomUUID(), [403]);
    await request('POST', '/Products', { name: 'Forbidden', saleUnit: 'unidad', basePrice: 1 }, repToken, undefined, [403]);
    assert.ok((await get('/Products', repToken)).some((p) => p.id === product.id));
  });
  await check('Tenant isolation on direct reads and mutations', async () => {
    for (const path of [`/Products/${product.id}`, `/Customers/${customer.id}`, `/Stocks/by-product/${product.id}`]) {
      await request('GET', path, undefined, b.token, undefined, [404]);
    }
    await request('PUT', `/Products/${product.id}`, { name: 'Forbidden', saleUnit: 'unidad', basePrice: 0 }, b.token, undefined, [404]);
    assert.equal((await get('/Products', b.token)).length, 0);
  });
  await check('Stock entry replay does not duplicate inventory', async () => {
    const key = randomUUID();
    const path = `/Stocks/by-product/${product.id}/increase`;
    await post(path, { amount: 10, isInitialLoad: true }, a.token, key);
    await post(path, { amount: 10, isInitialLoad: true }, a.token, key);
    assert.equal((await stock(product.id, a.token)).available, 10);
  });
  let order = await start(customer.id, repToken);
  await check('Draft replay returns same order', async () => {
    const key = randomUUID();
    const first = await start(customer.id, a.token, key);
    const replay = await start(customer.id, a.token, key);
    assert.equal(first.id, replay.id);
  });
  await check('Base price snapshot and draft leave stock unchanged', async () => {
    order = await line(order, product.id, 3, repToken);
    assert.equal(order.lines[0].appliedUnitPrice, 100);
    assert.equal(order.total, 300);
    assert.equal((await stock(product.id, a.token)).available, 10);
  });
  await check('SalesRep cannot read or edit another owner order', async () => {
    const other = await start(customer.id, a.token);
    await request('GET', `/Orders/${other.id}`, undefined, repToken, undefined, [404]);
    await request('POST', `/Orders/${other.id}/lines`, { productId: product.id, amount: 1, rowVersion: other.rowVersion }, repToken, undefined, [404]);
    const list = await get('/Orders?page=1&pageSize=100', repToken);
    assert.ok(list.items.every((item) => item.salesRepId === rep.id));
    await request('GET', `/Orders/${order.id}`, undefined, b.token, undefined, [404]);
  });
  await check('Confirmation, snapshot preservation and replay', async () => {
    await request('PUT', `/Products/${product.id}`, { name: product.name, sku: product.sku, saleUnit: 'unidad', basePrice: 150 }, a.token);
    const key = randomUUID();
    const before = order;
    order = (await confirm(before, repToken, key)).data;
    await confirm(before, repToken, key);
    assert.equal(order.status, 'Confirmed');
    assert.equal(order.total, 300);
    assert.equal((await stock(product.id, a.token)).available, 7);
    await request('PUT', `/Orders/${order.id}/lines/${product.id}`, { amount: 4, rowVersion: order.rowVersion }, repToken, undefined, [400]);
  });
  await check('Cancellation reverses stock exactly once', async () => {
    const before = order;
    const key = randomUUID();
    order = (await cancel(before, repToken, key)).data;
    await cancel(before, repToken, key);
    assert.equal(order.status, 'Cancelled');
    assert.equal((await stock(product.id, a.token)).available, 10);
  });
  await check('Draft cancellation does not change stock', async () => {
    const draft = await line(await start(customer.id, a.token), product.id, 2, a.token);
    await cancel(draft, a.token);
    assert.equal((await stock(product.id, a.token)).available, 10);
  });
  await check('Empty confirmation and insufficient stock rejected', async () => {
    await confirm(await start(customer.id, a.token), a.token, randomUUID(), [400]);
    const draft = await line(await start(customer.id, a.token), product.id, 11, a.token);
    await confirm(draft, a.token, randomUUID(), [400]);
    assert.equal((await stock(product.id, a.token)).available, 10);
    assert.equal((await get(`/Orders/${draft.id}`, a.token)).status, 'Draft');
  });
  await check('Optimistic order and stock concurrency', async () => {
    const original = await start(customer.id, a.token);
    await line(original, product.id, 1, a.token);
    await request('POST', `/Orders/${original.id}/lines`, { productId: product.id, amount: 2, rowVersion: original.rowVersion }, a.token, undefined, [409]);
    const current = await stock(product.id, a.token);
    const body = { amount: 9, rowVersion: current.rowVersion, reason: 'Stage 3 count' };
    await request('PUT', `/Stocks/by-product/${product.id}/adjust`, body, a.token, randomUUID());
    await request('PUT', `/Stocks/by-product/${product.id}/adjust`, { ...body, amount: 8 }, a.token, randomUUID(), [409]);
    await request('PUT', `/Stocks/by-product/${product.id}/adjust`, {
      amount: 10, rowVersion: (await stock(product.id, a.token)).rowVersion, reason: 'Stage 3 restore',
    }, a.token, randomUUID());
  });
  await check('Simultaneous confirmations cannot oversell', async () => {
    const one = await line(await start(customer.id, a.token), product.id, 7, a.token);
    const two = await line(await start(customer.id, a.token), product.id, 7, a.token);
    const responses = await Promise.all([confirm(one, a.token, randomUUID(), [200, 400, 409]), confirm(two, a.token, randomUUID(), [200, 400, 409])]);
    assert.equal(responses.filter((r) => r.status === 200).length, 1);
    assert.equal((await stock(product.id, a.token)).available, 3);
    await cancel(responses.find((r) => r.status === 200).data, a.token);
  });
  await check('Price list applies to customer and survives base-price changes', async () => {
    const list = await post('/PriceLists', { name: `${run} List` }, a.token);
    await request('PUT', `/PriceLists/${list.id}/products/${product.id}`, { price: 80 }, a.token);
    await request('PATCH', `/PriceLists/${list.id}/activate`, { validFromUtc: new Date(Date.now() - 60000).toISOString() }, a.token, randomUUID());
    await request('PUT', `/Customers/${customer.id}/price-list`, { priceListId: list.id }, a.token);
    const priced = await line(await start(customer.id, a.token), product.id, 1, a.token);
    assert.equal(priced.lines[0].appliedUnitPrice, 80);
    assert.equal(priced.lines[0].priceOrigin, 'PriceList');
  });
  await check('Negative stock policy is explicit and reversible', async () => {
    const configuration = await get('/DistributorConfigurations/current', a.token);
    await request('PUT', '/DistributorConfigurations/current', { ...configuration, stockPolicy: 'AllowNegativeStock' }, a.token);
    const draft = await line(await start(customer.id, a.token), product.id, 11, a.token);
    const confirmed = (await confirm(draft, a.token)).data;
    assert.equal((await stock(product.id, a.token)).available, -1);
    await cancel(confirmed, a.token);
    await request('PUT', '/DistributorConfigurations/current', { ...await get('/DistributorConfigurations/current', a.token), stockPolicy: 'RequireAvailableStock' }, a.token);
  });
  await check('Pagination and SKU search beyond 100 real products', async () => {
    for (let index = 1; index <= 101; index++) {
      await post('/Products', { name: `${run} Extra ${String(index).padStart(3, '0')}`, sku: `${run}-${index}`, saleUnit: 'unidad', basePrice: 1 }, a.token);
    }
    const page = await get('/Products/with-stock?page=6&pageSize=20', a.token);
    assert.equal(page.totalCount, 102);
    assert.equal(page.items.length, 2);
    const search = await get(`/Products/with-stock?searchText=${run}-101`, a.token);
    assert.equal(search.items.length, 1);
    assert.equal(search.items[0].sku, `${run}-101`);
  });
  await check('Role changes and deactivation invalidate old tokens', async () => {
    await request('PATCH', `/Users/${rep.id}/role`, { role: 'Administrator' }, a.token);
    await request('GET', '/Auth/me', undefined, repToken, undefined, [403]);
    const updatedToken = await login(repEmail);
    assert.equal((await get('/Auth/me', updatedToken)).role, 'Administrator');
    await request('PATCH', `/Users/${rep.id}/deactivate`, { reason: 'Stage 3 complete' }, a.token);
    await request('GET', '/Auth/me', undefined, updatedToken, undefined, [403]);
  });
} catch (error) {
  results.push({ name: 'Suite setup or prerequisite', passed: false, error: error.message });
  console.log(`FAIL setup: ${error.message}`);
} finally {
  const report = { run, base, completedAtUtc: new Date().toISOString(), identities, results };
  mkdirSync(new URL('../docs/validation/', import.meta.url), { recursive: true });
  writeFileSync(new URL('../docs/validation/stage-three-api.json', import.meta.url), JSON.stringify(report, null, 2));
  console.log(`Run ${run}: ${results.filter((r) => r.passed).length}/${results.length} passed`);
  process.exitCode = results.some((r) => !r.passed) ? 1 : 0;
}
