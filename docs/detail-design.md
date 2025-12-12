# 詳細設計書（MassTransit起動・状態通知）

## 目的と範囲
- MassTransit経由で`TaskId`/`RunTaskId`を指定したワークフロー起動と、実行状態をPublishで通知する処理の詳細設計を示す。
- ElsaServer内の起動時ロード、実行、状態通知、エラーハンドリング、設定、ワークフローJSONの配置・構造を対象とする。
- ElsaStudio等のUI改修は含まない。

## コンポーネント設計
- `StartupInitializationHostedService`: 起動時に`ActivityAssemblyLoader`→`WorkflowCatalogLoader`を直列実行。失敗時の挙動は各Loaderの`Strict`設定に従う。
- `ActivityAssemblyLoader`: `Activities/`配下DLLを`ActivityAssemblyOptions`(`Directory`, `SearchPattern`, `Strict`)でロードし、`IActivityRegistry`へ登録。Strict=false時は警告ログのみで継続。
- `WorkflowCatalogLoader`: `WorkflowCatalogOptions.Directory`（既定: `docs/workflows`）配下を`workflow-*.json`で走査し、`workflow-<TaskId>.json`パターンを正規表現で検証。`Strict=true`時はフォルダ不在・命名不備・Root欠落・逆シリアライズ失敗を例外として起動失敗させる。`WorkflowDefinitionModel`に`DefinitionId`/`Name`/`Id`/`CreatedAt`を補完し、`WorkflowDefinitionMapper`と`IWorkflowGraphBuilder`でGraphを構築し`WorkflowCatalog`に登録。
- `WorkflowCatalog`: `TaskId`→`WorkflowCatalogEntry`を保持するスレッドセーフ辞書。`Set`/`TryGet`/`List`を提供。
- `RunContextStore`: `RunTaskId`重複を防ぐスレッドセーフ辞書。`TryAdd`で重複検知、終了・異常で`TryRemove`。
- `StartWorkflowConsumer`（`StartWorkflowConsumerDefinition`）: `WorkflowMessaging.StartQueueName`（既定: `workflow-start`）で起動要求を受信。ログ出力後`IWorkflowLauncher.LaunchAsync`に委譲。Definitionで`PrefetchCount`と`ConcurrentMessageLimit`を`WorkflowBusOptions`から設定。
- `WorkflowLauncher`: 受信コマンドを実行ハンドラ。処理順序: (1) `WorkflowCatalog`から`TaskId`を検索し、未登録なら`WorkflowStatusPublisher.Error`送信 (2) `RunContextStore.TryAdd`で重複チェック、失敗時はError送信 (3) `RunWorkflowOptions`に`CorrelationId=RunTaskId`、`Input=IPayloadMapper.Map`、`Properties[TaskId/RunTaskId]`を設定し`IWorkflowRunner.RunAsync`を起動 (4) 例外時は`RunContextStore.TryRemove`とError送信。
- `IPayloadMapper`/`DefaultPayloadMapper`: 受信Payloadを`IDictionary<string, object>`へ正規化。必要に応じて拡張実装差替え可能。
- `WorkflowStatusPublisher`: `IPublishEndpoint`を用いて`WorkflowStatusEvent`をPublish。`WorkflowStarted`/`WorkflowExecuted`/`WorkflowFinished`通知を購読し、`WorkflowPropertyKeys.TaskId/RunTaskId`から識別子を取得。`WorkflowExecuted`で`Suspended`→`Suspended`送信+RunContext削除、`Faulted`→`Error`送信+削除。`WorkflowFinished`で`Finished`送信+削除。明示呼び出し用`PublishAsync`は`WorkflowBusOptions.StatusPublishRetryCount/Delay`でリトライ。

## メッセージ契約
- `StartWorkflowCommand`
  - `TaskId` (string, required): 起動するワークフローID。`workflow-<TaskId>.json`と1対1。
  - `RunTaskId` (string, required): 実行インスタンス識別子。`RunContextStore`で重複を拒否。
  - `Payload` (IDictionary<string, object>?, optional): ワークフローInputへマッピング。キーは大文字小文字非依存。
  - `Headers` (IDictionary<string, string>?, optional): 将来拡張用。現行はロギング/トレーシングで使用予定。
- `WorkflowStatusEvent`
  - `TaskId` (string): 実行対象のTaskId。
  - `RunTaskId` (string): 実行インスタンスID。
  - `Status` (enum): `Running | Suspended | Finished | Error`。
  - `Detail` (string?): 状態メッセージ。開始時は"Started"、完了時"Finished"、サスペンド時"Suspended"、エラー時は例外/Incidentメッセージ。
  - `OccurredAtUtc` (DateTimeOffset): 通知生成UTC時刻。

## 処理フロー
1. **起動時ロード**: HostedServiceでActivities→Workflows順にロード。`Strict`で停止/継続を制御。
2. **起動要求受信** (`StartWorkflowConsumer`): キュー`workflow-start`で受信→ログ→`WorkflowLauncher`呼び出し。
3. **Launch処理** (`WorkflowLauncher`): カタログ存在確認→RunTaskId重複チェック→`RunWorkflowOptions`生成→`IWorkflowRunner.RunAsync`実行。例外時は`RunContextStore`を解放しError Publish。
4. **状態通知** (`WorkflowStatusPublisher`):
   - `WorkflowStarted`: `Running`送信。
   - `WorkflowExecuted` with `Suspended`: `Suspended`送信→RunContext解放。
   - `WorkflowExecuted` with `Faulted`: `Error`送信→RunContext解放。
   - `WorkflowFinished`: `Finished`送信→RunContext解放。
   - 明示呼び出し (`PublishAsync`): `Unknown TaskId` や `Duplicated RunTaskId` など起動前エラーを送信。
5. **クリーンアップ**: Suspended/Finished/Errorで`RunContextStore.TryRemove`。必要に応じて再開時に再登録する。

## ワークフローJSON設計と配置
- 配置: `docs/workflows/`（本番用）、サンプル: `docs/workflows/samples/`。
- 命名: `workflow-<TaskId>.json`。`TaskId`は英数字/`-`/`_`を許容。
- 必須要素: Root活動が存在し、MassTransit Publishアクティビティを組み込み`RunTaskId`と`Status`を送出する経路を含む。`DefinitionId`/`Name`/`Id`/`CreatedAt`は未指定でもLoaderが補完。
- サンプル案: `docs/workflows/workflow-sample-start.json` (TaskId=`sample-start`) — Flowchartで`Running`→(任意処理)→`Finished`をPublishし、例外分岐で`Error`をPublish。入力Payloadの`message`をWriteLineする例を含める。
- カスタムアクティビティ: `Activities/`に配置されたDLLを`ActivityAssemblyLoader`がロード。ワークフローから参照可能。

## エラー処理・ロギング
- **カタログロード**: Strict=trueで例外送出し起動失敗。Strict=falseで警告ログを残しスキップ。
- **起動要求**: 未知`TaskId`/重複`RunTaskId`/Payloadマッピング例外時は`WorkflowStatusEvent(Status=Error)`をPublishし、RunContextを登録/解放。
- **実行中例外**: ElsaのIncidentメッセージを`Detail`に含め`Error`送信。
- **Publish失敗**: `WorkflowBusOptions.StatusPublishRetry*`に従いリトライし、全失敗時は警告ログを残す。

## 設定項目（appsettings.json）
- `WorkflowCatalog`: `Directory` (default `docs/workflows`), `Strict` (bool)。
- `Activities`: `Directory` (default `Activities`), `SearchPattern` (default `*.dll`), `Strict` (bool)。
- `WorkflowMessaging`: `StartQueueName` (default `workflow-start`), `PrefetchCount` (default 16), `ConcurrentMessageLimit` (default 8), `StatusPublishRetryCount` (default 3), `StatusPublishRetryDelaySeconds` (default 2)。
- `MassTransit`: `Host`, `Port`, `VirtualHost`, `Username`, `Password`—RabbitMQ接続設定。

## 今後の実装・テスト指針
- サンプル`workflow-sample-start.json`をTaskId命名規則に合わせて`docs/workflows/`へ配置し、MassTransit Publishアクティビティを組み込む。
- `WorkflowCatalogLoader`のStrict挙動・ルール検証、`RunContextStore`重複拒否、`WorkflowStatusPublisher`の各ステータス送信とリトライを単体テストでカバー。
- E2E: RabbitMQ経由で`StartWorkflowCommand`を送信し、`WorkflowStatusEvent`をSubscribeして`Running→Finished`およびエラー経路を検証するシナリオを`docs/test-design.md`のケースに追加する。
