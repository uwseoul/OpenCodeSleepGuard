# OpenCodeSleepGuard

OpenCode 터미널이 작업 중일 때 Windows 절전을 방지하고, 입력 대기 중에는 절전을 허용하는 백그라운드 프로그램입니다.

## 작동 원리

```
[5초 간격 루프]
  ├─ OpenCode / Node 프로세스 감지
  │   ├─ 없음 → 절전 설정 복원 후 자동 종료
  │   └─ 있음 → CPU 사용량 체크
  │       ├─ CPU > 5% (작업 중) → 절전 방지 🟢
  │       └─ CPU < 5% 3분 지속 (대기 중) → 절전 허용 ⚪
  └─ 프로세스 종료 감지 시 절전 복원 + 자동 종료
```

## 기능

- **절전 방지** — OpenCode가 작업 중(CPU > 5%)일 때 Windows 절전 모드 진입 방지
- **절전 허용** — OpenCode가 3분간 대기 상태면 절전 허용
- **자동 종료** — OpenCode 프로세스가 종료되면 절전 설정 복원 후 자동 종료
- **시스템 트레이** — 작업 상태 표시 (🟢 작업 중 / ⚪ 대기 중)
- **자동시작** — Windows 로그온 시 자동 실행 등록
- **저자원** — CPU < 1%, 메모리 < 50MB

## 설치

### 릴리즈에서 다운로드

[Releases](../../releases) 페이지에서 최신 버전 다운로드.

### 직접 빌드

```bash
git clone https://github.com/사용자/OpenCodeSleepGuard.git
cd OpenCodeSleepGuard
dotnet build -c Release
```

빌드 결과: `bin/Release/net8.0-windows/OpenCodeSleepGuard.exe`

## 사용법

### 실행

```bash
# 직접 실행
OpenCodeSleepGuard.exe
```

실행하면 시스템 트레이에 아이콘이 나타납니다.
- 🟢 초록: OpenCode가 작업 중 (절전 방지)
- ⚪ 회색: OpenCode가 대기 중 (절전 허용)

트레이 아이콘 우클릭 → **Exit**으로 종료합니다.

### 자동시작 등록

```bash
# 등록 (관리자 권한 필요)
OpenCodeSleepGuard.exe --install

# 해제
OpenCodeSleepGuard.exe --uninstall
```

## 설정

`appsettings.json`으로 설정을 변경할 수 있습니다. EXE와 같은 디렉토리에 위치해야 합니다.

```json
{
  "ProcessNames": ["opencode", "node"],
  "CpuThreshold": 5.0,
  "IdleTimeoutSeconds": 180,
  "CheckIntervalSeconds": 5
}
```

| 설정 | 기본값 | 설명 |
|------|--------|------|
| `ProcessNames` | `["opencode", "node"]` | 감시할 프로세스 이름 |
| `CpuThreshold` | `5.0` | 작업 중 판정 CPU 임계값 (%) |
| `IdleTimeoutSeconds` | `180` | 대기 후 절전 허용까지 시간 (초) |
| `CheckIntervalSeconds` | `5` | 상태 확인 간격 (초) |

## 기술 스택

- **언어**: C# .NET 8
- **대상 OS**: Windows 10/11
- **절전 API**: `SetThreadExecutionState` (kernel32.dll)
- **CPU 측정**: `Process.TotalProcessorTime` 델타 계산
- **프로세스 감지**: `Process.GetProcessesByName()`

## 프로젝트 구조

```
OpenCodeSleepGuard/
├── Program.cs          — 진입점, 메인 루프, 생명주기 관리
├── AppSettings.cs      — 설정 모델 + JSON 로더
├── SleepManager.cs     — SetThreadExecutionState P/Invoke 래퍼
├── CpuMonitor.cs       — CPU 사용량 측정
├── ProcessWatcher.cs   — OpenCode 프로세스 감지
├── TrayIcon.cs         — 시스템 트레이 아이콘
├── TaskScheduler.cs    — 자동시작 등록/해제
├── appsettings.json    — 설정 파일
└── OpenCodeSleepGuard.csproj
```

## 요구사항

- .NET 8.0 런타임 (또는 자체 포함 빌드)
- Windows 10/11

## 라이선스

MIT
