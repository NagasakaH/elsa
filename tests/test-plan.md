# テスト計画（単体・結合・システム）

## 目的
MassTransit経由のTaskId/RunTaskId起動と状態Publish機能について、詳細設計をカバーする単体・結合・システムテストのケースを整理する。

## テスト構成案
- Unit: `tests/ElsaServer.UnitTests` プロジェクト
- Integration: `tests/ElsaServer.IntegrationTests`（MassTransit TestHarness or RabbitMQ）
- System: `tests/ElsaServer.SystemTests`（docker compose + RabbitMQ 実機想定）

## 単体テスト対象・代表ケース
1. **WorkflowCatalogLoader**
   - 正常: 正しい命名/Rootありで登録される。
   - 異常: 命名不正、Root欠落、逆シリアライズ失敗（Strict=trueで例外、falseで警告）。
2. **ActivityAssemblyLoader**
   - 正常: DLL検出でIActivityが登録される。
   - 異常: DLLなし/ロード失敗（Strict=true例外、false警告）。
3. **RunContextStore**
   - TryAdd成功/重複拒否、TryRemoveで解放される。
4. **PayloadMapper**
   - 大文字小文字混在のキーが正規化される、null/空ディクショナリの扱い。
5. **WorkflowLauncher**
   - 未知TaskId→Error Publish。
   - RunTaskId重複→Error Publish。
   - 正常起動でRunWorkflowOptionsにTaskId/RunTaskIdが設定される。
   - RunAsync例外でRunContext解放＋Error Publish。
6. **WorkflowStatusPublisher**
   - WorkflowStarted→Running Publish。
   - WorkflowExecuted SubStatus=Suspended/Faultedで各ステータスPublish＋RunContext解放。
   - WorkflowFinishedでFinished Publish＋RunContext解放。
   - Publishリトライ設定が反映される。

## 結合テスト（MassTransit TestHarness想定）
- StartWorkflowConsumer経由でStartWorkflowCommandを送信し、Statusイベント受信を確認。
  - 正常: TaskId既知・RunTaskId新規 → Running→Finished を受信。
  - 未知TaskId: Error を受信。
  - RunTaskId重複: Error を受信。
  - 実行中Fault: Error を受信しDetailに例外含む。
  - Suspendedフローを持つワークフローで Suspended を受信。

## システムテスト（サンプルWF使用）
- サンプル`workflow-<TaskId>.json`（例: `workflow-sample-basic-flow.json`）をロード。
- RabbitMQ経由でStartWorkflowCommand送信し、状態通知を実キューで受信。
  - 正常シナリオ: Running→Finished。
  - Faultシナリオ: Error。
  - Suspendedシナリオ: Suspended。

## 成果物/タスク
- プロジェクト作成: `tests/ElsaServer.UnitTests/ElsaServer.UnitTests.csproj`, `tests/ElsaServer.IntegrationTests/ElsaServer.IntegrationTests.csproj`, 必要に応じ `tests/ElsaServer.SystemTests`。
- 各コンポーネントのテストクラスを追加。
- MassTransit TestHarnessセットアップとサンプルWFのフィクスチャを用意。
- CIで `dotnet test` を全プロジェクト実行。
