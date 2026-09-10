const { spawn, spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const args = process.argv.slice(2);
const isWin = process.platform === 'win32';
const composeFiles = {
  dev: path.join(__dirname, 'infrastructure', 'docker-compose.dev.yaml'),
  prod: path.join(__dirname, 'infrastructure', 'docker-compose.yaml')
};

function resolveEnvFile() {
  const envFile = path.join(__dirname, '.env');

  if (fs.existsSync(envFile)) {
    return envFile;
  }

  return path.join(__dirname, '.env.example');
}

function loadEnv(filePath) {
  const env = {};

  if (!fs.existsSync(filePath)) {
    return env;
  }

  const content = fs.readFileSync(filePath, 'utf8');

  for (const line of content.split(/\r?\n/)) {
    const trimmed = line.trim();

    if (!trimmed || trimmed.startsWith('#')) {
      continue;
    }

    const index = trimmed.indexOf('=');

    if (index > 0) {
      const key = trimmed.substring(0, index).trim();
      const value = trimmed.substring(index + 1).trim();
      env[key] = value;
    }
  }

  return env;
}

function runDockerCompose(composeArgs, mode = 'prod') {
  const result = spawnSync(
    'docker',
    [
      'compose',
      '-f',
      composeFiles[mode],
      '--env-file',
      resolveEnvFile(),
      ...composeArgs
    ],
    {
      stdio: 'inherit',
      shell: false
    }
  );

  return result.status ?? 1;
}

function getPostgresContainerId() {
  const result = spawnSync(
    'docker',
    [
      'compose',
      '-f',
      composeFiles.dev,
      '--env-file',
      resolveEnvFile(),
      'ps',
      '-q',
      'postgres'
    ],
    {
      encoding: 'utf8',
      shell: false
    }
  );

  return result.stdout?.trim();
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitForPostgresHealthy(timeoutMs = 60000) {
  const start = Date.now();

  console.log('⏳ Aguardando PostgreSQL ficar saudável...');

  while (Date.now() - start < timeoutMs) {
    const containerId = getPostgresContainerId();

    if (!containerId) {
      await sleep(2000);
      continue;
    }

    const result = spawnSync(
      'docker',
      [
        'inspect',
        '--format={{if .State.Health}}{{.State.Health.Status}}{{else}}unknown{{end}}',
        containerId
      ],
      {
        encoding: 'utf8',
        shell: false
      }
    );

    const status = result.stdout?.trim();

    if (status === 'healthy') {
      console.log('✅ PostgreSQL pronto');
      return;
    }

    if (status === 'unhealthy') {
      throw new Error('PostgreSQL ficou unhealthy');
    }

    console.log(`⏳ PostgreSQL: ${status || 'starting'}`);
    await sleep(2000);
  }

  throw new Error('Timeout aguardando PostgreSQL ficar saudável');
}

function prefixOutput(name, data, colorCode) {
  const lines = data.toString().split(/\r?\n/);

  for (const line of lines) {
    if (line.trim()) {
      console.log(`\x1b[${colorCode}m[${name}]\x1b[0m ${line}`);
    }
  }
}

function terminateChild(child) {
  if (!child || child.killed) {
    return;
  }

  if (isWin) {
    try {
      spawnSync(
        'taskkill',
        ['/F', '/T', '/PID', child.pid],
        { stdio: 'ignore' }
      );
    } catch {
      child.kill();
    }
  } else {
    child.kill('SIGTERM');
  }
}

async function startDevelopment(remainingArgs = []) {
  const dockerStatus = runDockerCompose(
    ['up', '-d', ...remainingArgs],
    'dev'
  );

  if (dockerStatus !== 0) {
    return dockerStatus;
  }

  try {
    await waitForPostgresHealthy();
  } catch (error) {
    console.error(`❌ Erro ao aguardar PostgreSQL: ${error.message}`);
    runDockerCompose(['down'], 'dev');
    return 1;
  }

  const devEnv = loadEnv(resolveEnvFile());
  const backendPort = devEnv.BACKEND_HTTP_PORT || '8080';
  const frontendPort = devEnv.FRONTEND_HTTP_PORT || '3000';
  const postgresPort = devEnv.POSTGRES_PORT || '5432';
  const connectionString = [
    `Host=localhost`,
    `Port=${postgresPort}`,
    `Database=${devEnv.POSTGRES_DB || 'si_vinicius_tengaten'}`,
    `Username=${devEnv.POSTGRES_USER || 'postgres'}`,
    `Password=${devEnv.POSTGRES_PASSWORD || 'senha_segura'}`
  ].join(';');
  const children = [];
  let isCleaningUp = false;

  const cleanup = (exitCode = 0) => {
    if (isCleaningUp) {
      return;
    }

    isCleaningUp = true;

    for (const child of children) {
      terminateChild(child);
    }

    runDockerCompose(['down'], 'dev');
    process.exit(exitCode);
  };

  process.on('SIGINT', () => cleanup());
  process.on('SIGTERM', () => cleanup());
  process.on('SIGHUP', () => cleanup());

  const backend = spawn(
    isWin ? 'dotnet.exe' : 'dotnet',
    [
      'watch',
      'run',
      '--project',
      'Backend.Web/Backend.Web.csproj',
      '--urls',
      `http://localhost:${backendPort}`
    ],
    {
      cwd: path.join(__dirname, 'Backend'),
      shell: false,
      env: {
        ...process.env,
        ...devEnv,
        ASPNETCORE_ENVIRONMENT: 'Development',
        DOTNET_USE_POLLING_FILE_WATCHER:
          devEnv.DOTNET_USE_POLLING_FILE_WATCHER || '1',
        ConnectionStrings__DefaultConnection: connectionString
      }
    }
  );

  children.push(backend);
  backend.stdout.on('data', data => prefixOutput('Backend', data, '36'));
  backend.stderr.on('data', data => prefixOutput('Backend', data, '31'));
  backend.on('error', error => {
    console.error(`❌ Erro ao iniciar o backend: ${error.message}`);
    cleanup(1);
  });
  backend.on('exit', code => {
    if (!isCleaningUp && code !== 0) {
      cleanup(code || 1);
    }
  });

  const frontend = spawn(
    isWin ? 'cmd.exe' : 'npm',
    isWin ? ['/c', 'npm', 'run', 'dev'] : ['run', 'dev'],
    {
      cwd: path.join(__dirname, 'frontend'),
      shell: false,
      env: {
        ...process.env,
        ...devEnv,
        PORT: frontendPort,
        NEXT_PUBLIC_API_URL: `http://localhost:${backendPort}`
      }
    }
  );

  children.push(frontend);
  frontend.stdout.on('data', data => prefixOutput('Frontend', data, '35'));
  frontend.stderr.on('data', data => prefixOutput('Frontend', data, '31'));
  frontend.on('error', error => {
    console.error(`❌ Erro ao iniciar o frontend: ${error.message}`);
    cleanup(1);
  });
  frontend.on('exit', code => {
    if (!isCleaningUp && code !== 0) {
      cleanup(code || 1);
    }
  });

  console.log(`✅ Backend em http://localhost:${backendPort}`);
  console.log(`✅ Frontend em http://localhost:${frontendPort}`);

  await new Promise(() => {});
  return 0;
}

const commands = {
  dev: startDevelopment,
  'dev:up': startDevelopment,
  'dev:down': (remainingArgs = []) => runDockerCompose(
    ['down', ...remainingArgs],
    'dev'
  ),
  up: (remainingArgs = []) => runDockerCompose([
    'up',
    '-d',
    '--build',
    ...remainingArgs
  ]),
  down: (remainingArgs = []) => runDockerCompose([
    'down',
    ...remainingArgs
  ]),
  'db:up': (remainingArgs = []) => runDockerCompose([
    'up',
    '-d',
    'postgres',
    ...remainingArgs
  ], 'dev'),
  'db:down': (remainingArgs = []) => runDockerCompose([
    'stop',
    'postgres',
    ...remainingArgs
  ], 'dev')
};

commands.prod = commands.up;
commands['prod:up'] = commands.up;
commands['prod:down'] = commands.down;

const command = args[0] || 'help';

if (command === 'help') {
  console.log('Uso: node infra.js [dev:up|dev:down|up|down|db:up|db:down|ps|logs|prod:<comando>]');
  console.log('');
  console.log('Desenvolvimento:');
  console.log('  dev:up    Sobe o PostgreSQL e inicia dotnet watch/npm run dev');
  console.log('  dev:down  Para e remove os containers do ambiente dev');
  console.log('');
  console.log('Stack completa:');
  console.log('  up        Sobe toda a stack e recria as imagens');
  console.log('  down      Para e remove os containers da stack');
  console.log('  db:up     Sobe somente o PostgreSQL no Compose dev');
  console.log('  db:down   Para somente o PostgreSQL no Compose dev');
  console.log('  ps        Exibe o estado dos serviços da stack');
  console.log('  logs      Exibe os logs dos serviços da stack');
  console.log('');
  console.log('Os aliases prod:* continuam disponíveis para compatibilidade.');
  process.exit(0);
}

(async () => {
  if (commands[command]) {
    process.exit(await commands[command](args.slice(1)));
  }

  if (command.startsWith('dev:')) {
    process.exit(
      runDockerCompose([
        command.substring('dev:'.length),
        ...args.slice(1)
      ], 'dev')
    );
  }

  if (command.startsWith('prod:')) {
    process.exit(
      runDockerCompose([
        command.substring('prod:'.length),
        ...args.slice(1)
      ])
    );
  }

  process.exit(runDockerCompose(args));
})();
