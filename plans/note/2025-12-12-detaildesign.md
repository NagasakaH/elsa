# 2025-12-12 詳細設計メモ

- 詳細設計ドキュメント: `docs/detail-design.md`にコンポーネント・メッセージ契約・フロー・設定・サンプル案を集約。
- スコープ: MassTransit経由のTaskId/RunTaskIdワークフロー起動と状態通知（Running/Suspended/Finished/Error）。
- 主コンポーネント: WorkflowCatalogLoader(起動時読み込み/検証), ActivityAssemblyLoader(Activities配下DLL登録), RunContextStore(RunTaskId重複防止), StartWorkflowConsumer+Definition, WorkflowLauncher, WorkflowStatusPublisher, PayloadMapper。
- メッセージ契約: StartWorkflowCommand(TaskId, RunTaskId, Payload, Headers), WorkflowStatusEvent(TaskId, RunTaskId, Status, Detail, OccurredAtUtc)。起動キュー`workflow-start`、状態PublishはMassTransit標準。
- シーケンス: 起動時ロード→Consumer待受→Start受信でTaskId存在/RunTaskId重複チェック→Elsa実行→Running通知→Suspended/Finished/Errorで通知&RunContextStoreクリーンアップ。
- エラーポリシー: 未知TaskId/重複RunTaskId/マッピング失敗はError Publishし未起動、実行例外はError通知、Publishはリトライ設定で再送。読み込み厳格度は設定で選択。
- 設定: Loaderフォルダ/Strict、Busキュー/並列度、StatusPublishリトライ、Activitiesフォルダ、RunContext保持方針をappsettingsで管理。

## 2025-12-12 詳細設計ドラフト（detaildesign.agent出力）

### コンポーネント構成（現行コード準拠）
- `StartupInitializationHostedService`: 起動時に `ActivityAssemblyLoader` → `WorkflowCatalogLoader` を直列実行。失敗時挙動は各Optionsの`Strict`に従う。
- `ActivityAssemblyLoader` + `ActivityAssemblyOptions`: `Activities/` 配下DLLを `SearchPattern`（既定`*.dll`）でロードし `IActivityRegistry` に登録。Strict=false時は警告で継続。
- `WorkflowCatalogLoader` + `WorkflowCatalogOptions`: `docs/workflows/**/workflow-*.json` を正規表現 `workflow-(?<taskId>[a-zA-Z0-9_-]+).json` で走査し、Root必須・TaskId補完・Graph構築のうえ `WorkflowCatalog` に登録。Strict=trueで命名/Root欠落/逆シリアライズ失敗時は例外。
- `WorkflowCatalog`: TaskId→`WorkflowCatalogEntry`（Graph, DefinitionModel, SourcePath）を保持するスレッドセーフ辞書。
- `RunContextStore`: RunTaskId重複を拒否し、終了/異常/Suspendedで `TryRemove` するスレッドセーフ辞書。
- `StartWorkflowConsumer` + `StartWorkflowConsumerDefinition`: キュー名=`WorkflowBusOptions.StartQueueName`(既定`workflow-start`)、`PrefetchCount` と `ConcurrentMessageLimit` を設定。受信後 `WorkflowLauncher` に委譲。
- `WorkflowLauncher`: TaskId存在チェック→RunTaskId重複チェック→`RunWorkflowOptions` (`CorrelationId=RunTaskId`, `Input=PayloadMapper`, `Properties[TaskId/RunTaskId]`) を生成し `IWorkflowRunner.RunAsync` を実行。例外時はRunContextを解放しError通知。
- `IPayloadMapper` (`DefaultPayloadMapper`): Payloadを大文字小文字非依存ディクショナリへ正規化。
- `WorkflowStatusPublisher` + `WorkflowBusOptions`: `IPublishEndpoint` で `WorkflowStatusEvent` を送信。`WorkflowStarted/Executed/Finished` を購読し、SubStatus=Suspended/Faulted/Finishedでステータス送信＋RunContext解放。Publishは `StatusPublishRetryCount/DelaySeconds` でリトライ。

### メッセージ契約（MassTransit）
- `StartWorkflowCommand`: `TaskId`(string, req) / `RunTaskId`(string, req) / `Payload`(IDictionary<string, object>?, optional, OrdinalIgnoreCase) / `Headers`(IDictionary<string, string>?, optional)。キュー=`workflow-start`（設定変更可）。
- `WorkflowStatusEvent`: `TaskId` / `RunTaskId` / `Status`(`Running|Suspended|Finished|Error`) / `Detail`(string?) / `OccurredAtUtc`(DateTimeOffset)。Publish経路はMassTransit標準。
- 設定キー: `WorkflowMessaging.StartQueueName`, `PrefetchCount`, `ConcurrentMessageLimit`, `StatusPublishRetryCount`, `StatusPublishRetryDelaySeconds`。

### 処理フロー詳細
1. **起動時ロード**: HostedServiceでActivities→Workflowsをロード。`Activities.Strict`=trueでDLL不在/ロード失敗時に起動失敗、`WorkflowCatalog.Strict`=trueで命名不備・Root欠落・逆シリアライズ失敗時に起動失敗。
2. **起動要求受信**: `StartWorkflowConsumer` がキュー`workflow-start`で受信しログ出力後、`WorkflowLauncher.LaunchAsync` を実行。
3. **起動処理**: CatalogにTaskIdが無ければError Publish（Unknown TaskId）。`RunContextStore.TryAdd`失敗でError Publish（Duplicated RunTaskId）。`RunWorkflowOptions` を生成し `IWorkflowRunner.RunAsync` 実行。例外時はRunContext解放しError Publish（例外メッセージ）。
4. **状態通知** (`WorkflowStatusPublisher`):
	- `WorkflowStarted` → Status=Running("Started")。
	- `WorkflowExecuted` SubStatus=Suspended → Status=Suspended("Suspended")＋RunContext解放。
	- `WorkflowExecuted` SubStatus=Faulted → Status=Error(Incidentメッセージ)＋RunContext解放。
	- `WorkflowFinished` → Status=Finished("Finished")＋RunContext解放。
	- `PublishAsync` は起動前エラー（Unknown/duplicated）でも利用し、リトライ設定を適用。

### エラーハンドリングとログ
- Loader: Strict=trueで例外スローしアプリ停止。Strict=falseで警告ログのみ。
- 起動要求: 未知TaskId/重複RunTaskId/マッピング例外時にError Publish（Detailに原因）、RunContextを未登録/解放。
- 実行中例外: ElsaのIncidentメッセージを`Detail`に含めError Publish。Publish失敗はリトライ後に警告ログ。

### サンプルワークフロー計画（システムテスト用）
- 目的: MassTransit経由の起動と Running→Finished / Error / Suspended 通知パスを確認する最小構成。
- TaskId例: `sample-basic-flow`。RunTaskIdは呼び出し元が採番。
- ロジック案: `WriteLine`でPayload `message` を出力 → MassTransit Publish(Status=Running) → `Delay`(短時間) → MassTransit Publish(Status=Finished)。例外経路では`Throw`を挿入しMassTransit Publish(Status=Error)。
- 配置パス案: `docs/workflows/samples/workflow-sample-basic-flow.json`（量産時は`docs/workflows/workflow-<TaskId>.json`へ昇格）。
- 設計資料パス案: `docs/activities/sample-basic-flow.workflow.md`（ワークフロー設計書）。
- 追加アクティビティ資料案: Publish専用アクティビティを整理する場合 `docs/activities/mass-transit-publish.activitie.md` にI/Oと必須ヘッダを記載。

### ファイルパス提案まとめ
- サンプルWF JSON: `docs/workflows/samples/workflow-sample-basic-flow.json`
- 本番WF配置規則: `docs/workflows/workflow-<TaskId>.json`
- ワークフロー設計書: `docs/activities/<WorkflowName>.workflow.md`（例: `docs/activities/sample-basic-flow.workflow.md`）
- カスタムアクティビティ設計書: `docs/activities/<ActivityName>.activitie.md`（例: `docs/activities/mass-transit-publish.activitie.md`）

## 2025-12-12 詳細設計 追加ドラフト（実装用観点）

- 起動順序とStrict挙動
	- StartupInitializationHostedServiceでActivities→Workflowsを直列ロード。Activities.Strict=trueはDLL欠落で例外、falseは情報ログのみ。WorkflowCatalog.Strict=trueは命名/Root欠落/逆シリアライズ失敗で例外。
- ワークフロー検証・補完
	- ファイル名regex `^workflow-(?<taskId>[a-zA-Z0-9_-]+)\.json$` でTaskId抽出。Root必須。DefinitionId/Name/Id/CreatedAt/IsLatestをデフォルト補完。
	- 状態通知アクティビティ必須（MassTransit PublishでRunTaskId/Status/Detail）。ルール検証はテストで担保。
- 起動処理詳細（StartWorkflowConsumer→WorkflowLauncher）
	- 受信: StartWorkflowCommand {TaskId, RunTaskId, Payload?, Headers?}。Queue=`workflow-start`。
	- Catalog TryGet失敗→WorkflowStatusEvent(Error,"Unknown TaskId") Publish。
	- RunContextStore.TryAdd失敗→WorkflowStatusEvent(Error,"Duplicated RunTaskId") Publish。
	- RunWorkflowOptions {CorrelationId=RunTaskId, Input=PayloadMapper.Map, Properties={TaskId,RunTaskId}} を生成し RunAsync。例外時 RunContextStore.Remove + Error Publish(detail=Exception.Message)。
- 状態通知詳細（WorkflowStatusPublisher）
	- WorkflowStarted→Running("Started")。
	- WorkflowExecuted SubStatus=Suspended→Suspended("Suspended") + RunContextStore.Remove。
	- WorkflowExecuted SubStatus=Faulted→Error(Incident.Message) + RunContextStore.Remove。
	- WorkflowFinished→Finished("Finished") + RunContextStore.Remove。
	- PublishはStatusPublishRetryCount/DelaySecondsでリトライ。
- メッセージ契約（再掲）
	- StartWorkflowCommand(TaskId:string, RunTaskId:string, Payload:IDictionary<string,object>?, Headers:IDictionary<string,string>?).
	- WorkflowStatusEvent(TaskId:string, RunTaskId:string, Status:Running|Suspended|Finished|Error, Detail:string?, OccurredAtUtc:DateTimeOffset)。
- サンプルWF具体化案
	- TaskId=`sample-basic-flow`。入力`message`をログ→Publish(Running)→Delay短時間→Publish(Finished)。例外経路でThrow→Publish(Error)。
	- JSON配置: `docs/workflows/samples/workflow-sample-basic-flow.json` を先に作成し、最終的に `docs/workflows/workflow-sample-basic-flow.json`（命名規則適用後に`workflow-<TaskId>.json`へ昇格）。
	- 設計書: `docs/activities/sample-basic-flow.workflow.md`。
	- 必要ならPublishアクティビティ設計書: `docs/activities/mass-transit-publish.activitie.md` にI/Oと必須ヘッダを記載。
- テスト観点（実装対応）
	- Loader: 正常/命名不正/Root欠落/Strict挙動。
	- Consumer+Launcher: Unknown TaskId、RunTaskId重複、正常起動、例外時Error Publish。
	- StatusPublisher: Running/Suspended/Error/FinishedのPublishとRunContext解放。
	- ActivitiesLoader: DLL未配置Strict挙動、登録数確認。
	- E2E: `workflow-start`にメッセージ送信しStatusを受信。
