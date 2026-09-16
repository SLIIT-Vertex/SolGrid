import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { randomBytes } from 'node:crypto';
import { spawn } from 'node:child_process';
import { parseEnv } from 'node:util';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

export const root = fileURLToPath(new URL('../', import.meta.url));
const defaults = {
  "BASE_URL": "http://localhost:5080",
  "ASPNETCORE_URLS": "http://localhost:5080",
  "ASPNETCORE_ENVIRONMENT": "Development",
  "MONGO_ROOT_USERNAME": "solgrid_admin",
  "MONGO_ROOT_PASSWORD": "",
  "MONGO_DATABASE": "SolGrid",
  "MONGO_USERS_COLLECTION": "Users",
  "MONGO_ENERGY_RESERVATIONS_COLLECTION": "EnergyReservations",
  "MONGO_INITIALIZE_ON_STARTUP": "true",
  "MONGO_PORT": "27017",
  "MONGO_TEST_PORT": "27018",
  "JWT_ISSUER": "SolGrid",
  "JWT_AUDIENCE": "SolGrid.Web",
  "JWT_SIGNING_KEY": "",
  "JWT_EXPIRES_MINUTES": "60",
  "WEB_APP_ORIGIN": "http://localhost:5173",
  "WEB_APP_DOCKER_ORIGIN": "http://localhost:8080",
  "SEED_DEVELOPMENT_USERS": "false",
  "BACKOFFICE_SEED_FIRST_NAME": "Backoffice",
  "BACKOFFICE_SEED_LAST_NAME": "Admin",
  "BACKOFFICE_SEED_EMAIL": "",
  "BACKOFFICE_SEED_PASSWORD": "",
  "GRID_OPERATOR_SEED_FIRST_NAME": "Grid",
  "GRID_OPERATOR_SEED_LAST_NAME": "Operator",
  "GRID_OPERATOR_SEED_EMAIL": "",
  "GRID_OPERATOR_SEED_PASSWORD": ""
};
const projects = {
  "API_PROJECT": "web-service/src/SolGrid.Api/SolGrid.Api.csproj",
  "DOMAIN_TESTS": "web-service/tests/SolGrid.Domain.Tests/SolGrid.Domain.Tests.csproj",
  "APPLICATION_TESTS": "web-service/tests/SolGrid.Application.Tests/SolGrid.Application.Tests.csproj",
  "API_TESTS": "web-service/tests/SolGrid.Api.Tests/SolGrid.Api.Tests.csproj",
  "INFRA_TESTS": "web-service/tests/SolGrid.Infrastructure.Tests/SolGrid.Infrastructure.Tests.csproj"
};
const secretKeys = [
  [
    "MongoDb:ConnectionString",
    "MONGO_CONNECTION_STRING"
  ],
  [
    "MongoDb:DatabaseName",
    "MONGO_DATABASE"
  ],
  [
    "MongoDb:UsersCollectionName",
    "MONGO_USERS_COLLECTION"
  ],
  [
    "MongoDb:EnergyReservationsCollectionName",
    "MONGO_ENERGY_RESERVATIONS_COLLECTION"
  ],
  [
    "MongoDb:InitializeOnStartup",
    "MONGO_INITIALIZE_ON_STARTUP"
  ],
  [
    "MongoDb:SeedDevelopmentUsers",
    "SEED_DEVELOPMENT_USERS"
  ],
  [
    "MongoDb:BackofficeSeedUser:FirstName",
    "BACKOFFICE_SEED_FIRST_NAME"
  ],
  [
    "MongoDb:BackofficeSeedUser:LastName",
    "BACKOFFICE_SEED_LAST_NAME"
  ],
  [
    "MongoDb:BackofficeSeedUser:Email",
    "BACKOFFICE_SEED_EMAIL"
  ],
  [
    "MongoDb:BackofficeSeedUser:Password",
    "BACKOFFICE_SEED_PASSWORD"
  ],
  [
    "MongoDb:GridOperatorSeedUser:FirstName",
    "GRID_OPERATOR_SEED_FIRST_NAME"
  ],
  [
    "MongoDb:GridOperatorSeedUser:LastName",
    "GRID_OPERATOR_SEED_LAST_NAME"
  ],
  [
    "MongoDb:GridOperatorSeedUser:Email",
    "GRID_OPERATOR_SEED_EMAIL"
  ],
  [
    "MongoDb:GridOperatorSeedUser:Password",
    "GRID_OPERATOR_SEED_PASSWORD"
  ],
  [
    "Jwt:Issuer",
    "JWT_ISSUER"
  ],
  [
    "Jwt:Audience",
    "JWT_AUDIENCE"
  ],
  [
    "Jwt:SigningKey",
    "JWT_SIGNING_KEY"
  ],
  [
    "Jwt:ExpiresMinutes",
    "JWT_EXPIRES_MINUTES"
  ],
  [
    "Cors:AllowedOrigins:0",
    "WEB_APP_ORIGIN"
  ],
  [
    "Cors:AllowedOrigins:1",
    "WEB_APP_DOCKER_ORIGIN"
  ]
];
export const descriptions = {
  "env-example": "Create .env from .env.example if missing",
  "env-init": "Create .env and generate local JWT/Mongo secrets",
  "secrets-init": "Initialize .NET user-secrets for the API",
  "secrets-set": "Store .env values in .NET user-secrets for local dotnet run",
  "secrets-list": "List configured API user-secrets keys",
  "secrets-clear": "Clear API user-secrets",
  "docker-up": "Start Mongo, backend, and web app with Docker Compose",
  "docker-down": "Stop Docker Compose services",
  "docker-ps": "Show Docker Compose service status",
  "docker-logs": "Follow backend and Mongo logs",
  "docker-build": "Build backend Docker image",
  "docker-test-mongo-up": "Start MongoDB for repository integration tests",
  "docker-test-mongo-down": "Stop MongoDB test container",
  "restore": "Restore backend packages",
  "build": "Build backend API",
  "test": "Run backend tests",
  "verify": "Build and test backend",
  "clean": "Clean backend projects",
  "format": "Format backend projects",
  "run": "Run API locally using .NET user-secrets",
  "health": "Check /health",
  "openapi": "Check OpenAPI JSON",
  "login-backoffice": "Login with configured Backoffice seed account",
  "login-grid-operator": "Login with configured GridOperator seed account",
  "web-install": "Install React dependencies",
  "web-dev": "Start React dev server",
  "web-build": "Build React app"
};

export function configuration(directory = root, environment = process.env) {
  const envPath = `${directory}/.env`;
  const config = { ...defaults, ...(existsSync(envPath) ? parseEnv(readFileSync(envPath, 'utf8')) : {}), ...environment };
  config.MONGO_CONNECTION_STRING ??= `mongodb://${encodeURIComponent(config.MONGO_ROOT_USERNAME)}:${encodeURIComponent(config.MONGO_ROOT_PASSWORD)}@localhost:${config.MONGO_PORT}/${config.MONGO_DATABASE}?authSource=admin`;
  return config;
}

export function initializeEnv(directory = root, generate = true) {
  let content = readFileSync(`${directory}/.env.example`, 'utf8');
  if (generate) {
    content = content.replace(/^MONGO_ROOT_PASSWORD=.*$/m, `MONGO_ROOT_PASSWORD=${randomBytes(32).toString('hex')}`)
      .replace(/^JWT_SIGNING_KEY=.*$/m, `JWT_SIGNING_KEY=${randomBytes(64).toString('hex')}`);
  }
  try {
    // Exclusive creation also protects against concurrent invocations.
    writeFileSync(`${directory}/.env`, content, { flag: 'wx', mode: 0o600 });
    console.log(generate ? '.env created with generated local MongoDB and JWT secrets.' : '.env created. Fill secret values before running Docker or secrets-set.');
  } catch (error) {
    if (error.code !== 'EEXIST') throw error;
    console.log('.env already exists; preserving its contents.');
  }
}

export function checkSecrets(config, directory = root) {
  if (!existsSync(`${directory}/.env`)) throw new Error('Create .env first: node scripts/tasks.mjs env-init');
  for (const key of ['MONGO_ROOT_PASSWORD', 'JWT_SIGNING_KEY']) {
    if (!config[key]) throw new Error(`Set ${key} in .env`);
  }
  if (Buffer.byteLength(config.JWT_SIGNING_KEY, 'utf8') < 32) throw new Error('JWT_SIGNING_KEY must be at least 32 bytes');
}

function execute(command, args, config, cwd = root) {
  return new Promise((resolve, reject) => {
    // npm.cmd requires a shell on Windows. Only fixed npm commands use it;
    // secret values and all other arguments go directly to the executable.
    const windowsNpm = process.platform === 'win32' && command === 'npm';
    const child = spawn(windowsNpm ? 'npm.cmd' : command, args, {
      cwd, env: config, stdio: 'inherit', shell: windowsNpm,
    });
    child.on('error', () => reject(new Error(`Unable to start ${command}. Check that it is installed and on PATH.`)));
    child.on('exit', (code, signal) => {
      if (code === 0) resolve();
      else {
        const error = new Error(`${command} failed (${signal ?? code}).`);
        error.exitCode = code ?? 1;
        reject(error);
      }
    });
  });
}

export async function request(config, path, body) {
  const response = await fetch(`${config.BASE_URL}${path}`, {
    ...(body ? { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) } : {}),
    signal: AbortSignal.timeout(30000),
  });
  if (!response.ok) throw new Error(`HTTP ${response.status} ${response.statusText}`);
  console.log(await response.text());
}

export async function runTask(task, config = configuration()) {
  const run = (command, ...args) => execute(command, args, config);
  const apiArgs = ['--project', projects.API_PROJECT];
  const flags = ['--no-restore', '-v', 'minimal', '-m:1', '-nr:false', '-p:UseSharedCompilation=false'];
  switch (task) {
    case 'help':
      console.log('Usage: node scripts/tasks.mjs <task> [KEY=value ...]');
      for (const [name, description] of Object.entries(descriptions)) console.log(`  ${name.padEnd(26)} ${description}`);
      return;
    case 'env-example': return initializeEnv(root, false);
    case 'env-init': return initializeEnv();
    case 'check-runtime-secrets': return checkSecrets(config);
    case 'secrets-init': return run('dotnet', 'user-secrets', 'init', ...apiArgs);
    case 'secrets-set':
      checkSecrets(config);
      for (const [key, variable] of secretKeys) await run('dotnet', 'user-secrets', 'set', key, config[variable], ...apiArgs);
      console.log('API user-secrets updated from configuration.');
      return;
    case 'secrets-list': return run('dotnet', 'user-secrets', 'list', ...apiArgs);
    case 'secrets-clear': return run('dotnet', 'user-secrets', 'clear', ...apiArgs);
    case 'docker-up':
    case 'docker-build':
      checkSecrets(config);
      return run('docker', 'compose', '--env-file', '.env', ...(task === 'docker-up' ? ['up', '-d', '--build'] : ['build', 'backend']));
    case 'docker-down': return run('docker', 'compose', '--env-file', '.env', 'down');
    case 'docker-ps': return run('docker', 'compose', '--env-file', '.env', 'ps');
    case 'docker-logs': return run('docker', 'compose', '--env-file', '.env', 'logs', '-f', 'mongo', 'backend');
    case 'docker-test-mongo-up': return run('docker', 'compose', '-f', 'docker-compose.test.yml', 'up', '-d');
    case 'docker-test-mongo-down': return run('docker', 'compose', '-f', 'docker-compose.test.yml', 'down');
    case 'restore':
    case 'clean':
    case 'format':
      for (const project of Object.values(projects)) {
        await run('dotnet', task, project, ...(task === 'clean' ? ['-v', 'minimal'] : task === 'format' ? ['--no-restore'] : []));
      }
      return;
    case 'build': return run('dotnet', 'build', projects.API_PROJECT, ...flags);
    case 'test':
      for (const [key, project] of Object.entries(projects)) if (key !== 'API_PROJECT') await run('dotnet', 'test', project, ...flags);
      return;
    case 'verify':
      await runTask('build', config);
      return runTask('test', config);
    case 'run': return run('dotnet', 'run', ...apiArgs, '--no-launch-profile');
    case 'health': return request(config, '/health');
    case 'openapi': return request(config, '/openapi/v1.json');
    case 'login-backoffice':
    case 'login-grid-operator': {
      const prefix = task === 'login-backoffice' ? 'BACKOFFICE' : 'GRID_OPERATOR';
      for (const suffix of ['EMAIL', 'PASSWORD']) if (!config[`${prefix}_SEED_${suffix}`]) throw new Error(`Set ${prefix}_SEED_${suffix} in .env`);
      return request(config, '/api/v1/auth/login', { email: config[`${prefix}_SEED_EMAIL`], password: config[`${prefix}_SEED_PASSWORD`] });
    }
    case 'web-install': return execute('npm', ['install'], config, `${root}/web-app`);
    case 'web-dev': return execute('npm', ['run', 'dev'], config, `${root}/web-app`);
    case 'web-build': return execute('npm', ['run', 'build'], config, `${root}/web-app`);
    default: throw new Error(`Unknown task: ${task}. Run node scripts/tasks.mjs help.`);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  try {
    const overrides = {};
    for (const override of process.argv.slice(3)) {
      const match = /^(\w+)=(.*)$/s.exec(override);
      if (!match) throw new Error(`Expected KEY=value override: ${override}`);
      overrides[match[1]] = match[2];
    }
    const config = configuration(root, { ...process.env, ...overrides });
    await runTask(process.argv[2] ?? 'help', config);
  } catch (error) {
    console.error(error.message);
    process.exitCode = error.exitCode ?? 1;
  }
}
