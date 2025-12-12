# 単体テスト・結合テスト計画（unittest.agent）

## 目的・前提
- 根拠: `docs/requirements.md` 要件1–12、`docs/detail-design.md` のコンポーネント設計・フロー・エラー処理、`docs/test-design.md` の観点。
- MassTransit経由での起動・状態通知、および起動時ロードまわりの振る舞いを単体テストでカバーし、重要な経路はテストハーネスを使った軽量結合テストで検証する。
- テスト順序は「詳細設計 → 単体テスト → 実装 → テスト」の原則に従う（要件12）。

## テストプロジェクト案（追加ファイル）
- `tests/ElsaServer.UnitTests/ElsaServer.UnitTests.csproj`
  - TargetFramework: net8.0
  - 依存: `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `NSubstitute`, `MassTransit.Testing`（バスハーネス）, `Microsoft.Extensions.Options`, `Microsoft.Extensions.Logging.Abstractions`。
- `tests/ElsaServer.IntegrationTests/ElsaServer.IntegrationTests.csproj`（任意）
  - 依存: 上記＋`MassTransit.Testing`、`Elsa.Testing.Shared` があれば活用。RabbitMQ実体を使わず InMemory / TestHarness で完結させる。
- ソリューション登録: `NagasakaEventSystem.sln` に両テストプロジェクトを追加予定。

## スコープと対象コンポーネント
- 起動時ロード: `ActivityAssemblyLoader`, `WorkflowCatalogLoader`, `WorkflowCatalog`。
- 実行・状態管理: `WorkflowLauncher`, `RunContextStore`, `DefaultPayloadMapper`。
- 状態通知: `WorkflowStatusPublisher`。
- メッセージ受信: `StartWorkflowConsumer`, `StartWorkflowConsumerDefinition`。
- サンプル/シナリオ: `docs/workflows/workflow-*.json`（サンプル含む）。

## 単体テストケース一覧（例）

### ActivityAssemblyLoader （要件1,7）
- ディレクトリ不存在: Strict=true で `DirectoryNotFoundException`、Strict=false で警告ログのみ。
- DLL内にアクティビティなし: Register が呼ばれないが例外なし。
- 正常: ExportedTypes を抽出し `IActivityRegistry.RegisterAsync` が呼ばれる。
- 読み込み失敗時: Strict=true で例外送出、Strict=false で警告ログ。

### WorkflowCatalogLoader （要件1,5,8）
- ディレクトリ不存在: Strict=true で例外、Strict=false で警告ログしスキップ。
- ファイル名不正 `workflow-*.json` 不一致: Strict=true で例外、false で警告。
- Root欠落: Strict=true で例外、false で警告。
- デフォルト補完: `DefinitionId`/`Name`/`Id`/`CreatedAt`/`IsLatest` が補完される。
- 正常: `WorkflowCatalog.Set` に TaskId と Graph/Model が保存される。
- 逆シリアライズ失敗: Strict=true で例外、false で警告。

### WorkflowCatalog
- `Set` 上書き動作と `TryGet`/`List` の挙動確認。

### RunContextStore （要件3）
- `TryAdd` で新規RunTaskIdが登録される。
- 重複RunTaskIdは `TryAdd` false。
- `TryRemove` で取り除きと TaskId 取得ができる。

### DefaultPayloadMapper
- null入力で空Dictionaryを返す（OrdinalIgnoreCase）。
- 既存辞書をコピーし、キー大文字小文字非依存になる。

### WorkflowLauncher （要件2–4）
- 未登録TaskId: `PublishAsync(Status=Error, detail="Unknown TaskId")` が呼ばれ、Runnerは呼ばれない。
- RunTaskId重複: `PublishAsync(Status=Error, detail="Duplicated RunTaskId")` が呼ばれ、Runnerは呼ばれない。
- 正常: `RunWorkflowOptions` に `CorrelationId=RunTaskId` と TaskId/RunTaskId properties が設定され、`_payloadMapper.Map` が呼ばれ、`IWorkflowRunner.RunAsync` が起動。
- `IWorkflowRunner.RunAsync` 例外: RunContextStore から削除され、`PublishAsync(Status=Error, detail=例外メッセージ)` が呼ばれる。

### WorkflowStatusPublisher （要件4）
- `WorkflowStarted` 受信で `Status=Running`, Detail="Started" をPublish。
- `WorkflowExecuted` SubStatus=Suspended で `Status=Suspended` Publish 後 RunContextStore.Remove。
- `WorkflowExecuted` SubStatus=Faulted で Incident.Message を含む `Status=Error` Publish 後 Remove。
- `WorkflowFinished` で `Status=Finished` Publish 後 Remove。
- `PublishAsync` リトライ: 1回目失敗→2回目成功する際、リトライ回数/Delayが設定値に従う。

### StartWorkflowConsumer / Definition （要件2,11）
- Consumer: `Consume` 呼び出しで `IWorkflowLauncher.LaunchAsync` が TaskId/RunTaskId 付きで呼ばれる。
- Definition: `EndpointName = StartQueueName`、`ConcurrentMessageLimit`/`PrefetchCount` が `WorkflowBusOptions` を反映。

## 結合テスト（MassTransit TestHarness）
- **起動要求→実行→Running通知**: InMemory ハーネス上で `StartWorkflowCommand` を送信し、`WorkflowStatusEvent(Status=Running)` を受信するまで検証。
- **重複RunTaskId**: 同一RunTaskIdを連続送信し、2件目で `Status=Error` (Duplicated) を受信。
- **未知TaskId**: 登録されていない TaskId を送信し `Status=Error` を受信。
- **Faulted経路**: 故意に例外を投げる単純ワークフローを登録し、`Status=Error` と Incident メッセージを確認。
- **Suspended経路（任意）**: Suspendedを返すワークフローで `Status=Suspended` を確認。

## システムテスト準備（docs 連携）
- `docs/workflows/samples` にあるサンプルを使用し、RabbitMQ を実体または TestHarness で代替。
- シナリオ: `StartWorkflowCommand(TaskId=sample-start, RunTaskId=uuid)` を送り、`Running→Finished` のステータス順序を確認（要件11）。
- 結果を `docs/test-design.md` にケースIDとして追記予定。

## 実装タスク（作業順）
1. `tests/ElsaServer.UnitTests` プロジェクトと依存パッケージを追加し、`NagasakaEventSystem.sln` に登録。
2. 各サービス用のテストクラスを `tests/ElsaServer.UnitTests/Services/*.cs` に配置。
3. MassTransit TestHarness を使った結合テストを `tests/ElsaServer.IntegrationTests` に追加（任意）。
4. サンプルワークフローを利用したシステムテストシナリオを `docs/test-design.md` に追記し、テストデータを `docs/workflows/` 配下に整備。
5. `dotnet test` を `process: test:solution` タスクで実行できるよう確認。

## 成果物の完了条件
- 上記ケースに対応するテストコードが `tests/` 配下に追加され、主要サービスの正常系・異常系を自動化できる。
- 要件1–4/7/8/11 の状態通知・ロード・RunTaskId管理に対するテストが赤緑で確認済み。
- MassTransit経由のシナリオが少なくとも1本（Running→Finished）が自動テストで通る。
