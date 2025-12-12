# サンプルワークフロー設計書: sample-basic-flow

## 概要
MassTransit経由で起動されるシンプルなワークフロー。TaskId=`sample-basic-flow`、RunTaskIdは呼び出し元指定。起動時に開始ログ、短い待機、終了ログを実行し、ステータスはワークフローライフサイクルフック（WorkflowStatusPublisher）経由でPublishされる。

## 入力/出力
- 入力: なし（Payloadは使用しない）。
- 出力: なし。

## ステータス通知
- ワークフロー内で `PublishWorkflowStatus` アクティビティを使用し、MassTransitへ `WorkflowStatusEvent` をPublish。
- 併せて `WorkflowStatusPublisher` によるライフサイクルフック通知が補完。
- Status: `Running` (開始), `Finished` (正常終了), `Error` (例外), `Suspended` (サスペンド時)。

## アクティビティ構成
1. `WriteLineStart` (Elsa.WriteLine)
   - メッセージ: "Sample workflow started"
   - `canStartWorkflow`: true
2. `PublishRunning` (ElsaServer.Activities.PublishWorkflowStatus)
   - Status: Running
3. `ShortDelay` (Elsa.Delay)
   - 時間: 1秒
4. `WriteLineEnd` (Elsa.WriteLine)
   - メッセージ: "Sample workflow finished"
5. `PublishFinished` (ElsaServer.Activities.PublishWorkflowStatus)
   - Status: Finished

## 接続
- `WriteLineStart.Done` → `PublishRunning.In`
- `PublishRunning.Done` → `ShortDelay.In`
- `ShortDelay.Done` → `WriteLineEnd.In`
- `WriteLineEnd.Done` → `PublishFinished.In`

## エラー/サスペンド経路
- 本サンプルでは明示的なサスペンド/例外は含めない。テスト用には別途Fault/Suspendパスを持つバリエーションを用意する。

## 配置
- JSON: `docs/workflows/workflow-sample-basic-flow.json`
- 設計資料: `docs/activities/sample-basic-flow.workflow.md`

## 注意
- RunTaskId/TaskId入力は未指定の場合、ワークフローのプロパティから補完される。
