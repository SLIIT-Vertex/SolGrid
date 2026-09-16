import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, readFileSync, writeFileSync, rmSync, statSync, mkdirSync, copyFileSync, chmodSync, realpathSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createServer } from 'node:http';
import { spawnSync, spawn } from 'node:child_process';
import { configuration, initializeEnv, checkSecrets, request, root, descriptions } from './tasks.mjs';

function fixture(t) {
  const directory = mkdtempSync(join(tmpdir(), 'SolGrid tasks '));
  t.after(() => rmSync(directory, { recursive: true, force: true }));
  writeFileSync(join(directory, '.env.example'), 'MONGO_ROOT_PASSWORD=\r\nJWT_SIGNING_KEY=\r\n');
  return directory;
}

test('initialization generates valid secrets and preserves existing files', t => {
  const directory = fixture(t);
  initializeEnv(directory);
  const original = readFileSync(join(directory, '.env'));
  const config = configuration(directory, {});
  assert.match(config.MONGO_ROOT_PASSWORD, /^[a-f0-9]{64}$/);
  assert.match(config.JWT_SIGNING_KEY, /^[a-f0-9]{128}$/);
  checkSecrets(config, directory);
  initializeEnv(directory);
  initializeEnv(directory, false);
  assert.deepEqual(readFileSync(join(directory, '.env')), original);
  if (process.platform !== 'win32') assert.equal(statSync(join(directory, '.env')).mode & 0o777, 0o600);
});

test('example creation preserves existing configuration', t => {
  const directory = fixture(t);
  initializeEnv(directory, false);
  assert.equal(readFileSync(join(directory, '.env'), 'utf8'), readFileSync(join(directory, '.env.example'), 'utf8'));
  assert.throws(() => checkSecrets(configuration(directory, {}), directory), /MONGO_ROOT_PASSWORD/);
});

test('dotenv handles CRLF, quoted secrets and configuration precedence', t => {
  const directory = fixture(t);
  writeFileSync(join(directory, '.env'), 'MONGO_ROOT_PASSWORD="a#b$&\\c"\r\nMONGO_PORT=1234\r\nJWT_SIGNING_KEY=short\r\n');
  const config = configuration(directory, { MONGO_PORT: '5678' });
  assert.equal(config.MONGO_ROOT_PASSWORD, 'a#b$&\\c');
  assert.equal(config.MONGO_PORT, '5678');
  assert.ok(config.MONGO_CONNECTION_STRING.includes('a%23b%24%26%5Cc@localhost:5678'));
  assert.throws(() => checkSecrets(config, directory), /at least 32 bytes/);
  assert.equal(configuration(directory, { MONGO_CONNECTION_STRING: 'custom' }).MONGO_CONNECTION_STRING, 'custom');
});

test('HTTP login preserves special characters and rejects failed responses', async t => {
  let received;
  const server = createServer(async (req, res) => {
    let body = '';
    for await (const chunk of req) body += chunk;
    received = body ? JSON.parse(body) : undefined;
    res.writeHead(req.url === '/fail' ? 401 : 200);
    res.end('{}');
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => new Promise(resolve => server.close(resolve)));
  const config = { BASE_URL: `http://127.0.0.1:${server.address().port}` };
  const credentials = { email: 'a@example.com', password: `quote" slash\\ dollar$ apostrophe'` };
  await request(config, '/api/v1/auth/login', credentials);
  assert.deepEqual(received, credentials);
  await assert.rejects(request(config, '/fail'), /HTTP 401/);
});

test('CLI runs outside repository root and reports invalid tasks and overrides', () => {
  const script = join(root, 'scripts/tasks.mjs');
  const invoke = (...args) => spawnSync(process.execPath, [script, ...args], { cwd: tmpdir(), encoding: 'utf8' });
  assert.equal(invoke('help').status, 0);
  const unknown = invoke('unknown');
  assert.equal(unknown.status, 1);
  assert.match(unknown.stderr, /Unknown task/);
  assert.equal(invoke('help', 'invalid').status, 1);
});

test('all command tasks invoke tools correctly through Make in an isolated project', { skip: process.platform === 'win32' }, t => {
  const directory = fixture(t);
  mkdirSync(join(directory, 'scripts'));
  mkdirSync(join(directory, 'web-app'));
  mkdirSync(join(directory, 'tools'));
  copyFileSync(join(root, 'scripts/tasks.mjs'), join(directory, 'scripts/tasks.mjs'));
  copyFileSync(join(root, 'Makefile'), join(directory, 'Makefile'));
  writeFileSync(join(directory, '.env'), 'MONGO_ROOT_PASSWORD="mock#password"\nJWT_SIGNING_KEY=' + 'x'.repeat(64) + '\n');
  const original = readFileSync(join(directory, '.env'));
  const log = join(directory, 'calls.jsonl');
  for (const tool of ['dotnet', 'docker', 'npm']) {
    const path = join(directory, 'tools', tool);
    writeFileSync(path, `#!/usr/bin/env node\nrequire('node:fs').appendFileSync(process.env.TASK_TEST_LOG, JSON.stringify({ tool: ${JSON.stringify(tool)}, args: process.argv.slice(2), cwd: process.cwd(), environment: process.env.ASPNETCORE_ENVIRONMENT }) + '\\n');\nprocess.exit(Number(process.env.TASK_TEST_EXIT || 0));\n`);
    chmodSync(path, 0o755);
  }
  const env = { ...process.env, PATH: `${join(directory, 'tools')}:${process.env.PATH}`, TASK_TEST_LOG: log };
  // These tools record arguments only: no secrets, containers, or dependencies are modified.
  const invoke = (task, ...overrides) => spawnSync('make', [task, ...overrides], { cwd: directory, env, encoding: 'utf8' });
  const calls = () => readFileSync(log, 'utf8').trim().split('\n').filter(Boolean).map(JSON.parse);
  const expectedCounts = { restore: 5, clean: 5, format: 5, test: 4, verify: 5, 'secrets-set': 20 };
  for (const task of ['help', 'env-init', 'env-example', 'check-runtime-secrets', ...Object.keys(descriptions).filter(name => !['env-init', 'env-example', 'health', 'openapi', 'login-backoffice', 'login-grid-operator'].includes(name))]) {
    writeFileSync(log, '');
    const result = invoke(task, 'ASPNETCORE_ENVIRONMENT=Development', 'MONGO_PORT=3456');
    assert.equal(result.status, 0, `${task}: ${result.stderr}`);
    const recorded = calls();
    const noTool = ['help', 'env-init', 'env-example', 'check-runtime-secrets'].includes(task);
    assert.equal(recorded.length, noTool ? 0 : expectedCounts[task] ?? 1, task);
    for (const call of recorded) assert.equal(call.environment, 'Development', task);
    if (task.startsWith('web-')) {
      assert.equal(recorded[0].tool, 'npm');
      assert.equal(recorded[0].cwd, realpathSync(join(directory, 'web-app')));
      assert.deepEqual(recorded[0].args, task === 'web-install' ? ['install'] : ['run', task === 'web-dev' ? 'dev' : 'build']);
    }
    if (task === 'secrets-set') {
      assert.equal(recorded[0].args[3], 'mongodb://solgrid_admin:mock%23password@localhost:3456/SolGrid?authSource=admin');
      assert.ok(recorded.every(call => call.args.slice(-2).join(' ') === '--project web-service/src/SolGrid.Api/SolGrid.Api.csproj'));
    }
    if (task === 'verify') {
      assert.equal(recorded[0].args[0], 'build');
      assert.ok(recorded.slice(1).every(call => call.args[0] === 'test'));
    }
    if (task === 'docker-up') assert.deepEqual(recorded[0].args, ['compose', '--env-file', '.env', 'up', '-d', '--build']);
    if (task === 'docker-build') assert.deepEqual(recorded[0].args, ['compose', '--env-file', '.env', 'build', 'backend']);
  }
  assert.deepEqual(readFileSync(join(directory, '.env')), original);
  writeFileSync(log, '');
  const failed = spawnSync('make', ['verify'], { cwd: directory, env: { ...env, TASK_TEST_EXIT: '7' }, encoding: 'utf8' });
  assert.notEqual(failed.status, 0);
  assert.equal(calls().length, 1, 'verify stops when build fails');
});

test('Make HTTP tasks reach the expected endpoints and send valid login JSON', { skip: process.platform === 'win32' }, async t => {
  const received = [];
  const server = createServer(async (req, res) => {
    let body = '';
    for await (const chunk of req) body += chunk;
    received.push({ path: req.url, method: req.method, body: body ? JSON.parse(body) : undefined });
    res.end('{}');
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => new Promise(resolve => server.close(resolve)));
  const password = `quote" slash\\ dollar$ apostrophe' #`;
  const env = { ...process.env, BACKOFFICE_SEED_EMAIL: 'backoffice@example.com', BACKOFFICE_SEED_PASSWORD: password, GRID_OPERATOR_SEED_EMAIL: 'grid@example.com', GRID_OPERATOR_SEED_PASSWORD: password };
  for (const task of ['health', 'openapi', 'login-backoffice', 'login-grid-operator']) {
    const status = await new Promise((resolve, reject) => {
      const child = spawn('make', [task, `BASE_URL=http://127.0.0.1:${server.address().port}`], { cwd: root, env, stdio: 'ignore' });
      child.on('error', reject);
      child.on('exit', resolve);
    });
    assert.equal(status, 0, task);
  }
  assert.deepEqual(received.map(value => [value.method, value.path]), [['GET', '/health'], ['GET', '/openapi/v1.json'], ['POST', '/api/v1/auth/login'], ['POST', '/api/v1/auth/login']]);
  assert.deepEqual(received[2].body, { email: env.BACKOFFICE_SEED_EMAIL, password });
  assert.deepEqual(received[3].body, { email: env.GRID_OPERATOR_SEED_EMAIL, password });
});
