# 基本設計書

## システム構成概要
- ElsaServer: 起動時に`docs/workflows/`配下の`workflow-<TaskId>.json`を読み込み、TaskId→ワークフロー定義をオンメモリ保持。MassTransitメッセージで起動要求を受け付け、実行・状態通知を行う。
- MassTransit: 起動メッセージの受信および状態通知（Publish）に使用。
- Activities DLL: `Activities/`配下DLLをロードし、カスタムアクティビティを使用可能にする。

## データ構造（想定）
- `WorkflowCatalog`: `TaskId`→ワークフロー定義(JSON)のマップ。起動時読み込み。
- `RunContextStore`: `RunTaskId`→実行インスタンス（または実行ハンドル）のオンメモリマップ。
- `WorkflowMessage`（受信）: `TaskId`, `RunTaskId`, `Payload`（任意）。
- `WorkflowStatusMessage`（Publish）: `RunTaskId`, `Status`(`Running|Suspended|Finished|Error`), `TaskId`, `Detail`（任意）。

## 起動時処理フロー
1. `docs/workflows/`を走査し`workflow-<TaskId>.json`を読み込む。
2. JSONスキーマ/ルールに沿って検証し`WorkflowCatalog`に登録。エラーはログ出力し、必要に応じて起動を失敗させる設定とする。
3. `Activities/`配下DLLをロードしてアクティビティを有効化。

## ワークフロー起動フロー（MassTransit受信）
1. 受信メッセージから`TaskId`,`RunTaskId`を取得。
2. `WorkflowCatalog`から定義を取得できなければエラー状態をPublish。
3. 対応ワークフローを起動し、`RunContextStore`に紐づけを保持。
4. 実行開始時/サスペンド/完了/エラーで`WorkflowStatusMessage`をPublish。

## 状態通知の方針
- すべてのワークフローにMassTransit Publishアクティビティを組み込み、`RunTaskId`と`Status`を通知する。
- 通知内容はルールとサンプルで明示し、外部システムがRunTaskIdで追跡可能にする。

## ワークフローファイル命名・配置
- パス: `docs/workflows/`
- 命名: `workflow-<TaskId>.json`
- 設計資料: `docs/activities/<ワークフロー名>.workflow.md`
- ルール: `docs/workflows/rules.md`

## アクティビティロード
- パス: `Activities/`配下のDLLを起動時にロード。
- ルール: `docs/activities/rules.md`
- テンプレートプロジェクトを複製して作成し、ビルドでDLL出力。

## エラーハンドリング方針
- 読み込み失敗: エラーをログ。必須ワークフロー欠落時は起動失敗または警告運転を選択できるよう設計。
- 起動要求のTaskId未登録: エラー状態を即時Publish。
- 実行中例外: `Error`ステータスをPublishし詳細を含める。

## テスト観点（概要）
- 起動時読込: 正常/不正JSON/命名不備。
- 起動要求: 既知TaskId/RunTaskId、未知TaskId、RunTaskId重複時の扱い。
- 状態通知: Running→Finished, Error, Suspended の各経路。
- DLLロード: ActivitiesテンプレートDLLがロードされること。
- サンプルシナリオ: MassTransit経由で起動し、通知を受け取れること。

## 今後の詳細設計の指針
- ElsaServer内にロードサービス、起動ハンドラ、ステータスパブリッシャを分離。
- RunTaskId紐づけはスレッドセーフなディクショナリで保持。
- JSONスキーマ/ルール検証をユニットテスト可能なサービスとして実装。