const { spawn, spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const args = process.argv.slice(2);
const isWin = process.platform === 'win32';
const devEnvFile = path.join(__dirname, '.env.development');

function loadEnv(filePath) {
  const env = {};

  if (!fs.existsSync(filePath)) return env;
  
  const content = fs.readFileSync(filePath, 'utf8');
  
  for (const line of content.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const index = trimmed.indexOf('=');
    if (index > 0) {
      const key = trimmed.substring(0, index).trim();
      const val = trimmed.substring(index + 1).trim();
      env[key] = val;
    }
  }
  
  return env;
}

function runDockerCompose(composeArgs, isProd = false) {
  const envFile = isProd 
    ? (fs.existsSync(path.join(__dirname, '.env')) ? path.join(__dirname, '.env') : devEnvFile)
    : devEnvFile;
    
  const composeFile = isProd
    ? path.join(__dirname, 'infrastructure', 'docker-compose.yaml')
    : path.join(__dirname, 'infrastructure', 'docker-compose.dev.yaml');

  const fullArgs = ['compose', '-f', composeFile, '--env-file', envFile, ...composeArgs];
  const result = spawnSync('docker', fullArgs, { stdio: 'inherit', shell: false });
  return result.status;
}

const commands = {
  'dev:up': (remainingArgs = []) => {
    const dockerStatus = runDockerCompose(['up', 'postgres', '-d', ...remainingArgs]);
    if (dockerStatus !== 0) process.exit(dockerStatus || 1);

    const children = [];
    let isCleaningUp = false;

    const cleanup = () => {
      if (isCleaningUp) return;
      isCleaningUp = true;
      for (const child of children) {
        if (child && !child.killed) {
          if (isWin) {
            try { spawnSync('taskkill', ['/F', '/T', '/PID', child.pid], { stdio: 'ignore' }); } catch (e) { child.kill(); }
          } else {
            child.kill('SIGTERM');
          }
        }
      }
      runDockerCompose(['down']);
      process.exit(0);
    };

    process.on('SIGINT', cleanup);
    process.on('SIGTERM', cleanup);
    process.on('SIGHUP', cleanup);

    const prefixOutput = (name, data, colorCode) => {
      const lines = data.toString().split(/\r?\n/);
      for (const line of lines) {
        if (line.trim()) {
          console.log(`\x1b[${colorCode}m[${name}]\x1b[0m ${line}`);
        }
      }
    };

    const devEnv = loadEnv(devEnvFile);
    const backendPort = devEnv.BACKEND_HTTP_PORT || '8080';
    const frontendPort = devEnv.FRONTEND_HTTP_PORT || '3000';

    const dotnetCmd = isWin ? 'dotnet.exe' : 'dotnet';
    const backend = spawn(
      dotnetCmd, 
      ['run', '--project', 'Backend.Web', '--urls', `http://localhost:${backendPort}`], 
      { 
        cwd: path.join(__dirname, 'Backend'), 
        shell: false,
        env: { ...process.env, ...devEnv }
      }
    );
    children.push(backend);

    backend.stdout.on('data', (data) => prefixOutput('Backend', data, '36'));
    backend.stderr.on('data', (data) => prefixOutput('Backend', data, '31'));

    const npmCmd = isWin ? 'cmd.exe' : 'npm';
    const npmArgs = isWin ? ['/c', 'npm', 'run', 'dev'] : ['run', 'dev'];
    
    const frontend = spawn(
      npmCmd, 
      npmArgs, 
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

    frontend.stdout.on('data', (data) => prefixOutput('Frontend', data, '35'));
    frontend.stderr.on('data', (data) => prefixOutput('Frontend', data, '31'));

    setInterval(() => {}, 1000);
  },
  'dev:down': (remainingArgs = []) => {
    process.exit(runDockerCompose(['down', ...remainingArgs]));
  },
  'prod:up': (remainingArgs = []) => {
    process.exit(runDockerCompose(['up', '-d', '--build', ...remainingArgs], true));
  },
  'prod:down': (remainingArgs = []) => {
    process.exit(runDockerCompose(['down', ...remainingArgs], true));
  }
};

commands.dev = commands['dev:up'];
commands.down = commands['dev:down'];
commands.prod = commands['prod:up'];

const command = args[0] || 'help';

if (command === 'help') {
  console.log("Uso: node infra.js [dev:up|dev:down|prod:up|prod:down|<docker-compose-args>]");
  console.log("Para rodar comandos docker compose específicos em produção, use prod:<comando> (ex: prod:ps, prod:logs)");
  process.exit(0);
}

if (commands[command]) {
  commands[command](args.slice(1));
} else if (command.startsWith('prod:')) {
  const composeArgs = [command.substring(5), ...args.slice(1)];
  process.exit(runDockerCompose(composeArgs, true));
} else if (command.startsWith('dev:')) {
  const composeArgs = [command.substring(4), ...args.slice(1)];
  process.exit(runDockerCompose(composeArgs, false));
} else {
  process.exit(runDockerCompose(args, false));
}
