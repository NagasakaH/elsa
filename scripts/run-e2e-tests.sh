#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${REPO_ROOT}/compose.yml"
SERVER_PROJECT="${ELSA_SERVER_PROJECT:-src/ElsaStudio/ElsaStudio.csproj}"
SERVER_URLS="${ELSA_SERVER_URLS:-http://127.0.0.1:5001}"
ELSA_BASE_URL="${ELSA_BASE_URL:-http://127.0.0.1:5001}"
E2E_SCREENSHOT_DIR="${E2E_SCREENSHOT_DIR:-${REPO_ROOT}/artifacts/e2e-screenshots}"
E2E_TEST_FILTER="${E2E_TEST_FILTER:-Category=E2E}"
USE_DOCKER_COMPOSE="${USE_DOCKER_COMPOSE:-true}"
KEEP_COMPOSE_UP="${KEEP_COMPOSE_UP:-false}"
START_CHROME_NOVNC="${START_CHROME_NOVNC:-false}"
CHROME_NOVNC_NAME="${CHROME_NOVNC_NAME:-chrome-novnc-e2e}"
CHROME_NOVNC_IMAGE="${CHROME_NOVNC_IMAGE:-vital987/chrome-novnc}"
PLAYWRIGHT_INSTALL_WITH_DEPS="${PLAYWRIGHT_INSTALL_WITH_DEPS:-false}"
SKIP_SERVER_START="${SKIP_SERVER_START:-false}"

SERVER_PID=""
CHROME_STARTED=false

cleanup() {
  if [ -n "${SERVER_PID}" ] && kill -0 "${SERVER_PID}" 2>/dev/null; then
    kill "${SERVER_PID}" 2>/dev/null || true
    wait "${SERVER_PID}" 2>/dev/null || true
  fi

  if [ "${CHROME_STARTED}" = "true" ]; then
    docker stop "${CHROME_NOVNC_NAME}" >/dev/null 2>&1 || true
  fi

  if [ "${USE_DOCKER_COMPOSE}" = "true" ] && [ "${KEEP_COMPOSE_UP}" != "true" ]; then
    docker compose -f "${COMPOSE_FILE}" down >/dev/null 2>&1 || true
  fi
}
trap cleanup EXIT

wait_for_endpoint() {
  local url="$1"
  local retries="${2:-90}"

  for ((i = 1; i <= retries; i++)); do
    if curl --silent --show-error --fail --insecure "${url}" >/dev/null; then
      return 0
    fi
    sleep 2
  done

  return 1
}

mkdir -p "${REPO_ROOT}/artifacts" "${E2E_SCREENSHOT_DIR}" "${REPO_ROOT}/artifacts/test-results"

if [ "${USE_DOCKER_COMPOSE}" = "true" ]; then
  echo "Starting dependencies with docker compose..."
  docker compose -f "${COMPOSE_FILE}" up -d postgres rabbitmq
fi

if [ "${START_CHROME_NOVNC}" = "true" ] && [ -z "${E2E_CHROME_NOVNC_CDP_URL:-}" ]; then
  echo "Starting chrome-novnc container..."
  docker run -d --rm --name "${CHROME_NOVNC_NAME}" -p 6080:6080 -p 9222:9222 "${CHROME_NOVNC_IMAGE}" >/dev/null
  export E2E_CHROME_NOVNC_CDP_URL="http://127.0.0.1:9222"
  CHROME_STARTED=true
fi

if [ -n "${E2E_CHROME_NOVNC_CDP_URL:-}" ]; then
  echo "Waiting for chrome-novnc CDP endpoint..."
  wait_for_endpoint "${E2E_CHROME_NOVNC_CDP_URL}/json/version" 60
fi

cd "${REPO_ROOT}"

echo "Building E2E test project and installing Playwright browsers..."
dotnet build tests/ElsaServer.E2ETests/ElsaServer.E2ETests.csproj --nologo
PLAYWRIGHT_SCRIPT="${REPO_ROOT}/tests/ElsaServer.E2ETests/bin/Debug/net8.0/playwright.sh"
PLAYWRIGHT_POWERSHELL_SCRIPT="${REPO_ROOT}/tests/ElsaServer.E2ETests/bin/Debug/net8.0/playwright.ps1"
if [ -f "${PLAYWRIGHT_SCRIPT}" ]; then
  if [ "${PLAYWRIGHT_INSTALL_WITH_DEPS}" = "true" ]; then
    bash "${PLAYWRIGHT_SCRIPT}" install --with-deps
  else
    bash "${PLAYWRIGHT_SCRIPT}" install
  fi
elif [ -f "${PLAYWRIGHT_POWERSHELL_SCRIPT}" ]; then
  if ! command -v pwsh >/dev/null 2>&1; then
    echo "pwsh is required to run ${PLAYWRIGHT_POWERSHELL_SCRIPT}" >&2
    exit 1
  fi

  if [ "${PLAYWRIGHT_INSTALL_WITH_DEPS}" = "true" ]; then
    pwsh "${PLAYWRIGHT_POWERSHELL_SCRIPT}" install --with-deps
  else
    pwsh "${PLAYWRIGHT_POWERSHELL_SCRIPT}" install
  fi
else
  echo "Playwright install script not found: ${PLAYWRIGHT_SCRIPT} or ${PLAYWRIGHT_POWERSHELL_SCRIPT}" >&2
  exit 1
fi

if [ "${SKIP_SERVER_START}" != "true" ]; then
  echo "Starting Elsa server (${SERVER_PROJECT})..."
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="${SERVER_URLS}" dotnet run --project "${SERVER_PROJECT}" >"${REPO_ROOT}/artifacts/e2e-server.log" 2>&1 &
  SERVER_PID=$!

  echo "Waiting for server endpoint: ${ELSA_BASE_URL}"
  if ! wait_for_endpoint "${ELSA_BASE_URL}" 90; then
    echo "Server did not become ready: ${ELSA_BASE_URL}" >&2
    tail -n 200 "${REPO_ROOT}/artifacts/e2e-server.log" || true
    exit 1
  fi
fi

echo "Running E2E tests..."
E2E_PROJECTS=(
  "tests/Activities.Loader.E2ETests/Activities.Loader.E2ETests.csproj"
  "tests/WorkflowCatalog.E2ETests/WorkflowCatalog.E2ETests.csproj"
  "tests/ElsaServer.E2ETests/ElsaServer.E2ETests.csproj"
)

for project in "${E2E_PROJECTS[@]}"; do
  result_file="$(basename "${project%.csproj}").trx"
  echo "Running ${project}"
  ELSA_BASE_URL="${ELSA_BASE_URL}" E2E_SCREENSHOT_DIR="${E2E_SCREENSHOT_DIR}" dotnet test "${project}" --filter "${E2E_TEST_FILTER}" --nologo --results-directory "${REPO_ROOT}/artifacts/test-results" --logger "trx;LogFileName=${result_file}"
done
