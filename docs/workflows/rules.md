# ワークフロー作成ルール

## 配置・命名
- 配置: `docs/workflows/`
- ファイル名: `workflow-<TaskId>.json`（TaskIdと1対1）
- 設計資料: `docs/activities/<ワークフロー名>.workflow.md`

## 必須要素
- MassTransit Publishアクティビティで`RunTaskId`と`Status`を通知する経路を組み込むこと。
- `Status`は `Running | Suspended | Finished | Error` をサポート。
- `TaskId`はワークフローごとに固定。RunTaskIdは起動要求で受領し、そのまま通知に含める。
- 使用できるアクティビティは`docs/activities/`で定義されたもののみ。

## JSONルール（推奨フィールド例）
- `name`: ワークフロー名（設計資料と一致）
- `taskId`: `<TaskId>`
- `activities`: アクティビティ定義配列
- `connections`: アクティビティ間の接続
- 状態通知用アクティビティ: MassTransit Publishを使用し、`RunTaskId`,`Status`,`Detail`をpayloadに含める。

## ステータス通知の標準パターン
- 実行開始時: `Status = Running`
- 一時停止/待機: `Status = Suspended`
- 正常終了: `Status = Finished`
- エラー: `Status = Error`（例外内容をDetailに含める）

## 品質ルール
- 起動時読み込みで検証可能な構造・必須項目を満たすこと。
- 不要な外部依存を含めないこと。
- 例外経路でも状態通知が行われること。

## テスト指針
- ルール遵守検証: 命名、必須フィールド、ステータス通知アクティビティの有無。
- 実行テスト: MassTransit経由で起動し、想定したステータス遷移をPublishするか確認。

## サンプル
- `docs/workflows/samples/workflow-<TaskId>.json` を参考にする。